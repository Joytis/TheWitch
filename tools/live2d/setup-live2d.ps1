<#
.SYNOPSIS
    Fetches + builds the Witch's fork of the GDCubism Live2D GDExtension and stages it so the
    Witch renders as Live2D (her models are committed art). Run by tools/setup.ps1; idempotent.

.DESCRIPTION
    Live2D is required: the Witch has no static sprite, so every platform the game ships on needs
    its native library staged before a release (bundle-workshop.ps1 refuses otherwise).

    Two modes:
      - default: build the Windows x64 library locally (for day-to-day work on this machine).
        Libraries for other platforms already staged (from -FromCI) are kept.
      - -FromCI: download the Windows/Linux/macOS libraries, licenses and shaders from the
        `live2d-build` GitHub Actions workflow (run it manually first; latest successful run, or
        -RunId <id>) and stage those instead. Needs the GitHub CLI (gh), logged in. This is what
        a release should ship.

    GDCubism (github.com/MizunagiKB/gd_cubism, MIT) must be compiled against Live2D's Cubism SDK
    for Native, which sits behind Live2D's license agreements. The local build downloads the
    pinned SDK version once you accept them (prompted, or -AcceptLive2DLicense), and caches the
    zip in build/live2d/ so later runs never ask again. -SdkZip uses a zip you downloaded yourself.

    The fork is the pinned upstream tag plus the transforms in tools/live2d/gdcubism_fork.py
    (shared with the CI build; see its docstring for the list). Bump FORK_REV there whenever the
    transforms change; the checkout is then re-cloned and rebuilt.

    Prerequisites for the local build (checked up front): git, Python 3 (scons is pip-installed for
    the user if missing), Visual Studio 2022+ or Build Tools with the C++ x64 toolset.

    Steps (each skipped when its output already exists, unless -Rebuild):
      1. download the Cubism SDK zip to build/live2d/ (license prompt)
      2. clone gd_cubism <Tag> (+ godot-cpp submodule) into build/live2d/gd_cubism, apply the fork
      3. unpack the SDK into its thirdparty/ folder
      4. scons platform=windows target=template_release
      5. stage the native side into TheWitch/live2d/ (gitignored, .gdignore'd; the csproj copies
         it to mods/TheWitch/live2d/ on build, bundle-workshop.ps1 ships it):
           witch_cubism.gdextension (lists every platform), bin/<per-platform library>, licenses/
         (and deletes TheWitch/TheWitch/live2d/samples/, where older versions staged an SDK sample)
      6. copy the shaders to TheWitch/TheWitch/live2d/shader/ (gitignored; packed into the .pck)
    Steps 5-6 need a `dotnet publish` to reach the game.

    Then launch normally (e.g. the "Debug: Combat" task on the Witch).

.EXAMPLE
    ./tools/live2d/setup-live2d.ps1
    ./tools/live2d/setup-live2d.ps1 -AcceptLive2DLicense          # non-interactive
    ./tools/live2d/setup-live2d.ps1 -SdkZip ~/Downloads/CubismSdkForNative-5-r.3.zip
    ./tools/live2d/setup-live2d.ps1 -FromCI                        # all platforms, from the workflow
#>
param(
    [string]$SdkZip = '',
    [string]$SdkVersion = '5-r.3',     # newest SDK at GDCubism v0.9.1's release; newer ones change Framework APIs
    [string]$Tag = 'v0.9.1',           # last GDCubism release (2025-03)
    [switch]$AcceptLive2DLicense,
    [switch]$Rebuild,
    [switch]$FromCI,                   # stage the live2d-build workflow's artifacts instead of building
    [string]$RunId = ''                # with -FromCI: that run (default: latest successful)
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$work = Join-Path $repo 'build/live2d'
$src = Join-Path $work 'gd_cubism'
$stage = Join-Path $repo 'TheWitch/live2d'
$forkScript = Join-Path $PSScriptRoot 'gdcubism_fork.py'
$forkMarker = Join-Path $src '.witch-fork'
$extName = 'witch_cubism'
# Library per platform, as SConstruct names it (macOS is a .framework folder).
$libs = [ordered]@{
    windows = "lib$extName.windows.release.x86_64.dll"
    linux   = "lib$extName.linux.release.x86_64.so"
    macos   = "lib$extName.macos.release.framework"
}
$builtDll = Join-Path $src "demo/addons/gd_cubism/bin/$($libs.windows)"
$shaderDst = Join-Path $repo 'TheWitch/TheWitch/live2d/shader'

function Write-Step($msg) { Write-Host "==> [live2d] $msg" -ForegroundColor Cyan }

# git/scons chat on stderr; under 'Stop' Windows PowerShell turns that into a terminating error
# when output is redirected, so judge native tools by exit code only.
function Invoke-Native([scriptblock]$cmd, [string]$what) {
    $ErrorActionPreference = 'Continue'
    & $cmd 2>&1 | ForEach-Object { Write-Host $_ }
    if ($LASTEXITCODE -ne 0) { throw "$what failed (exit $LASTEXITCODE)" }
}

# --- staging (shared by both modes) ---------------------------------------------
# $libSources: platform -> built library path (file or .framework folder). Other platforms'
# libraries already in bin/ are kept unless replaced; anything else is cleared so renamed or
# dropped files (e.g. the pre-fork gd_cubism.*) never ship.
function Write-Stage($libSources, [string]$licenseDir, [string]$shaderDir) {
    New-Item -ItemType Directory -Force $stage | Out-Null
    Get-ChildItem $stage -Force | Where-Object { $_.Name -notin '.gdignore', 'bin' } | Remove-Item -Recurse -Force
    $bin = Join-Path $stage 'bin'
    New-Item -ItemType Directory -Force $bin | Out-Null
    Get-ChildItem $bin -Force | Where-Object { $_.Name -notin $libs.Values } | Remove-Item -Recurse -Force
    foreach ($platform in $libSources.Keys) {
        $dst = Join-Path $bin $libs[$platform]
        if (Test-Path $dst) { Remove-Item $dst -Recurse -Force }
        Copy-Item $libSources[$platform] $dst -Recurse
    }
    Copy-Item $licenseDir (Join-Path $stage 'licenses') -Recurse
    # Loaded at runtime from disk, outside res://. No debug/release tag, so the editor-run game
    # (MegaDot, a debug build) resolves the same binaries as the release game.
    @"
[configuration]

entry_symbol = "gd_cubism_library_init"
compatibility_minimum = 4.3

[libraries]

windows.x86_64 = "bin/$($libs.windows)"
linux.x86_64 = "bin/$($libs.linux)"
macos = "bin/$($libs.macos)"
"@ | Set-Content (Join-Path $stage "$extName.gdextension") -Encoding ascii

    # The Witch's own models are committed under TheWitch/TheWitch/live2d/<name>/. Older versions of
    # this script staged an SDK sample model (Hiyori) beside them; remove it so it can't reach the pck.
    $oldSamples = Join-Path $repo 'TheWitch/TheWitch/live2d/samples'
    if (Test-Path $oldSamples) { Remove-Item $oldSamples -Recurse -Force; Write-Step "Removed stale SDK sample models ($oldSamples)." }

    # Shaders at the res:// path the fork loads them from (Live2DRuntime.ShaderResDir).
    if (Test-Path $shaderDst) { Remove-Item $shaderDst -Recurse -Force }
    New-Item -ItemType Directory -Force $shaderDst | Out-Null
    Copy-Item (Join-Path $shaderDir '*.gdshader') $shaderDst -Force
    # Pre-fork location; the export filter excludes addons/* again, so it's just clutter.
    $oldShaders = Join-Path $repo 'TheWitch/addons/gd_cubism'
    if (Test-Path $oldShaders) { Remove-Item $oldShaders -Recurse -Force }

    $staged = @($libs.Keys | Where-Object { Test-Path (Join-Path $bin $libs[$_]) })
    $missing = @($libs.Keys | Where-Object { $_ -notin $staged })
    Write-Step "Staged into $stage (platforms: $($staged -join ', '))."
    if ($missing) { Write-Host "    Missing: $($missing -join ', ') - the Witch is invisible there. Run the live2d-build workflow, then -FromCI." -ForegroundColor Yellow }
    Write-Step "Next: dotnet publish TheWitch/TheWitch.csproj, then launch the Witch."
}

# --- -FromCI: stage the live2d-build workflow's artifacts ---------------------------
if ($FromCI) {
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) { throw "GitHub CLI (gh) not found on PATH." }
    if (-not $RunId) {
        $RunId = "$(gh run list --workflow live2d-build.yml --status success --limit 1 --json databaseId --jq '.[0].databaseId')".Trim()
        if (-not $RunId) { throw "No successful live2d-build run found. Run it from the Actions tab first (manual, Live2D license box ticked)." }
    }
    $dl = Join-Path $work "ci/$RunId"
    if (Test-Path $dl) { Remove-Item $dl -Recurse -Force }
    Write-Step "Downloading live2d-build run $RunId..."
    Invoke-Native { gh run download $RunId --dir $dl } 'gh run download'
    $sources = [ordered]@{}
    foreach ($platform in $libs.Keys) {
        $p = Join-Path $dl "live2d-$platform/bin/$($libs[$platform])"
        if (-not (Test-Path $p)) { throw "Run $RunId has no $platform library ($p); did that job fail?" }
        $sources[$platform] = $p
    }
    Write-Stage $sources (Join-Path $dl 'live2d-common/licenses') (Join-Path $dl 'live2d-common/shader')
    return
}

# --- prerequisites ----------------------------------------------------------
if (-not (Get-Command git -ErrorAction SilentlyContinue)) { throw "git not found on PATH." }

$python = if (Get-Command py -ErrorAction SilentlyContinue) { @('py', '-3') }
          elseif (Get-Command python -ErrorAction SilentlyContinue) { @('python') }
          else { throw "Python 3 not found (needed for scons). Install it from python.org." }
function Invoke-Python([string[]]$pyArgs, [string]$what) {
    $exe = $python[0]; $pre = @($python | Select-Object -Skip 1)
    Invoke-Native { & $exe @pre @pyArgs } $what
}
$hasScons = & {
    $ErrorActionPreference = 'Continue'
    & $python[0] @($python | Select-Object -Skip 1) -c "import SCons" 2>&1 | Out-Null
    $LASTEXITCODE -eq 0
}
if (-not $hasScons) {
    Write-Step "scons not installed; pip-installing it for this user..."
    Invoke-Python @('-m', 'pip', 'install', '--user', 'scons') 'pip install scons'
}

$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
$vs = if (Test-Path $vswhere) {
    & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
}
if (-not $vs) {
    throw "No MSVC C++ x64 toolset found. Install Visual Studio 2022+ (or Build Tools) with the 'Desktop development with C++' workload."
}

# --- 1. Cubism SDK ----------------------------------------------------------
$sdkName = "CubismSdkForNative-$SdkVersion"
if (-not $SdkZip) {
    $SdkZip = Join-Path $work "$sdkName.zip"
    if (-not (Test-Path $SdkZip)) {
        if (-not $AcceptLive2DLicense) {
            Write-Host ""
            Write-Host "The Live2D Cubism SDK for Native is licensed by Live2D Inc. Downloading it means accepting:" -ForegroundColor Yellow
            Write-Host "  - Live2D Proprietary Software License Agreement  https://www.live2d.com/eula/live2d-proprietary-software-license-agreement_en.html"
            Write-Host "  - Live2D Open Software License Agreement         https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html"
            Write-Host "  (download page: https://www.live2d.com/en/sdk/download/native/)"
            $answer = Read-Host "Type 'yes' to accept and download $sdkName"
            if ($answer -ne 'yes') { throw "Live2D license not accepted; skipping Live2D setup." }
        }
        Write-Step "Downloading $sdkName..."
        New-Item -ItemType Directory -Force $work | Out-Null
        $ProgressPreference = 'SilentlyContinue'   # Invoke-WebRequest's progress bar is very slow on 5.1
        Invoke-WebRequest "https://cubism.live2d.com/sdk-native/bin/$sdkName.zip" -OutFile "$SdkZip.part" -UseBasicParsing
        Move-Item "$SdkZip.part" $SdkZip -Force
    }
}
if (-not (Test-Path $SdkZip)) { throw "SDK zip not found: $SdkZip" }

# --- 2. GDCubism source + fork (tools/live2d/gdcubism_fork.py) -----------------------------
$ForkRev = "$(& $python[0] @($python | Select-Object -Skip 1) $forkScript --rev)".Trim()
if (-not $ForkRev) { throw "Could not read FORK_REV from $forkScript" }

if (Test-Path $src) {
    $have = "$(& { $ErrorActionPreference = 'Continue'; git -C $src describe --tags 2>&1 | Select-Object -First 1 })"
    $haveFork = if (Test-Path $forkMarker) { (Get-Content $forkMarker -Raw).Trim() } else { '' }
    if ($Rebuild -or $have -ne $Tag -or $haveFork -ne $ForkRev) {
        Write-Step "Removing gd_cubism checkout ($have fork '$haveFork', want $Tag fork '$ForkRev')..."
        Remove-Item $src -Recurse -Force
    }
}
if (-not (Test-Path $src)) {
    Write-Step "Cloning gd_cubism $Tag..."
    New-Item -ItemType Directory -Force $work | Out-Null
    Invoke-Native { git clone --depth 1 --branch $Tag --recurse-submodules --shallow-submodules https://github.com/MizunagiKB/gd_cubism.git $src } 'git clone'
    Write-Step "Applying the Witch fork (rev $ForkRev)..."
    Invoke-Python @($forkScript, $src) 'gdcubism_fork.py'
    Set-Content $forkMarker $ForkRev -Encoding ascii
}

# --- 3. SDK into thirdparty/ (SConstruct picks the highest CubismSdkForNative* folder) ---------
$third = Join-Path $src 'thirdparty'
$sdk = Get-ChildItem $third -Directory -Filter 'CubismSdkForNative*' -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $sdk) {
    Write-Step "Unpacking $(Split-Path $SdkZip -Leaf)..."
    Expand-Archive -Path $SdkZip -DestinationPath $third
    $sdk = Get-ChildItem $third -Directory -Filter 'CubismSdkForNative*' | Select-Object -First 1
}
# The SDK ships Core libs per MSVC toolset (140..143) and SConstruct looks up the folder named
# after the detected toolset; newer toolsets (VS 2026 = 145) link the newest one fine.
$libRoot = Join-Path $sdk.FullName 'Core/lib/windows/x86_64'
$newest = Get-ChildItem $libRoot -Directory | Sort-Object { [int]$_.Name } | Select-Object -Last 1
foreach ($v in ([int]$newest.Name + 1)..149) {
    $dir = Join-Path $libRoot $v
    if (-not (Test-Path $dir)) { Copy-Item $newest.FullName $dir -Recurse }
}

# --- 4. build ---------------------------------------------------------------
if ($Rebuild -or -not (Test-Path $builtDll)) {
    Write-Step "Building GDCubism (scons, a few minutes on first run)..."
    Push-Location $src
    try {
        Invoke-Python @('-m', 'SCons', 'platform=windows', 'arch=x86_64', 'target=template_release', "-j$([Environment]::ProcessorCount)") 'scons'
    } finally { Pop-Location }
} else {
    Write-Step "GDCubism already built ($($libs.windows))."
}

# --- 5-6. stage ---------------------------------------------------------------
# Notices for what the library contains: GDCubism (MIT), godot-cpp (MIT), Cubism SDK (Live2D licenses).
$licenseTmp = Join-Path $work 'licenses'
if (Test-Path $licenseTmp) { Remove-Item $licenseTmp -Recurse -Force }
New-Item -ItemType Directory -Force $licenseTmp | Out-Null
Copy-Item (Join-Path $src 'LICENSE.en.adoc') (Join-Path $licenseTmp 'gd_cubism-LICENSE.adoc')
Copy-Item (Join-Path $src 'godot-cpp/LICENSE.md') (Join-Path $licenseTmp 'godot-cpp-LICENSE.md')
Copy-Item (Join-Path $sdk.FullName 'LICENSE.md') (Join-Path $licenseTmp 'CubismSdk-LICENSE.md')
Copy-Item (Join-Path $sdk.FullName 'NOTICE.md') (Join-Path $licenseTmp 'CubismSdk-NOTICE.md')
Write-Stage ([ordered]@{ windows = $builtDll }) $licenseTmp (Join-Path $src 'demo/addons/gd_cubism/res/shader')
