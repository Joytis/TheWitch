<#
.SYNOPSIS
    Fetches + builds the Witch's fork of the GDCubism Live2D GDExtension and stages it (plus a
    sample model) so the Witch renders as Live2D. Run by tools/setup.ps1; idempotent.

.DESCRIPTION
    GDCubism (github.com/MizunagiKB/gd_cubism, MIT) must be compiled against Live2D's Cubism SDK
    for Native, which sits behind Live2D's license agreements. The script downloads the pinned
    SDK version once you accept them (prompted, or -AcceptLive2DLicense), and caches the zip in
    build/live2d/ so later runs never ask again. -SdkZip uses a zip you downloaded yourself.

    The fork is the pinned upstream tag plus the transforms in Invoke-ForkPatch, so another mod
    shipping its own GDCubism can't collide with ours (Godot keeps whichever extension registers
    a class name first, and pcks loaded later override earlier res:// paths):
      - every GDCubism* class -> WitchCubism* (WitchCubismUserModel, ...)
      - shaders loaded from res://TheWitch/live2d/shader/ instead of res://addons/gd_cubism/
      - library built as libwitch_cubism.*.dll, loaded via witch_cubism.gdextension
    Bump $ForkRev whenever the transforms change; the checkout is then re-cloned and rebuilt.

    Prerequisites (checked up front): git, Python 3 (scons is pip-installed for the user if
    missing), Visual Studio 2022+ or Build Tools with the C++ x64 toolset.

    Steps (each skipped when its output already exists, unless -Rebuild):
      1. download the Cubism SDK zip to build/live2d/ (license prompt)
      2. clone gd_cubism <Tag> (+ godot-cpp submodule) into build/live2d/gd_cubism, apply the fork
      3. unpack the SDK into its thirdparty/ folder
      4. scons platform=windows target=template_release
      5. stage the native side into TheWitch/live2d/ (gitignored, .gdignore'd; the csproj copies
         it to mods/TheWitch/live2d/ on build, bundle-workshop.ps1 ships it):
           witch_cubism.gdextension, bin/libwitch_cubism.windows.release.x86_64.dll, licenses/
         and a Live2D sample model from the SDK into TheWitch/TheWitch/live2d/samples/<sample
         lowercased>/ (gitignored; packed into the .pck)
      6. copy the shaders to TheWitch/TheWitch/live2d/shader/ (gitignored; packed into the .pck)
    Steps 5-6 need a `dotnet publish` to reach the game.

    Then launch normally (e.g. the "Debug: Combat" task on the Witch).

.EXAMPLE
    ./tools/live2d/setup-live2d.ps1
    ./tools/live2d/setup-live2d.ps1 -AcceptLive2DLicense          # non-interactive
    ./tools/live2d/setup-live2d.ps1 -SdkZip ~/Downloads/CubismSdkForNative-5-r.3.zip
#>
param(
    [string]$SdkZip = '',
    [string]$SdkVersion = '5-r.3',     # newest SDK at GDCubism v0.9.1's release; newer ones change Framework APIs
    [string]$Tag = 'v0.9.1',           # last GDCubism release (2025-03)
    [string]$Sample = 'Hiyori',        # folder under <SDK>/Samples/Resources/
    [switch]$AcceptLive2DLicense,
    [switch]$Rebuild
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$work = Join-Path $repo 'build/live2d'
$src = Join-Path $work 'gd_cubism'
$stage = Join-Path $repo 'TheWitch/live2d'
$ForkRev = '1'                     # bump when Invoke-ForkPatch changes
$forkMarker = Join-Path $src '.witch-fork'
$extName = 'witch_cubism'
$dllName = "lib$extName.windows.release.x86_64.dll"
$builtDll = Join-Path $src "demo/addons/gd_cubism/bin/$dllName"
# Must match Live2DRuntime.ShaderResDir.
$shaderRes = 'res://TheWitch/live2d/shader/'

function Write-Step($msg) { Write-Host "==> [live2d] $msg" -ForegroundColor Cyan }

# git/scons chat on stderr; under 'Stop' Windows PowerShell turns that into a terminating error
# when output is redirected, so judge native tools by exit code only.
function Invoke-Native([scriptblock]$cmd, [string]$what) {
    $ErrorActionPreference = 'Continue'
    & $cmd 2>&1 | ForEach-Object { Write-Host $_ }
    if ($LASTEXITCODE -ne 0) { throw "$what failed (exit $LASTEXITCODE)" }
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

# --- 2. GDCubism source + fork ----------------------------------------------
# The fork's transforms over upstream (see .DESCRIPTION). Fails loudly when upstream no longer
# matches what they expect, rather than building something that collides.
function Invoke-ForkPatch {
    $utf8 = New-Object System.Text.UTF8Encoding($false)
    $files = Get-ChildItem (Join-Path $src 'src') -Recurse -File -Include '*.cpp', '*.hpp', '*.h'
    $shaderHits = 0
    foreach ($f in $files) {
        $text = [IO.File]::ReadAllText($f.FullName)
        $new = [regex]::Replace($text, '\bGDCubism(?=[A-Z])', 'WitchCubism')
        $shaderHits += ([regex]::Matches($new, [regex]::Escape('res://addons/gd_cubism/res/shader/'))).Count
        $new = $new.Replace('res://addons/gd_cubism/res/shader/', $shaderRes)
        if ($new -ne $text) { [IO.File]::WriteAllText($f.FullName, $new, $utf8) }
        if ($new -match 'GDCubism[A-Z]') { throw "Fork patch missed a GDCubism class name in $($f.FullName)" }
    }
    if ($shaderHits -eq 0) { throw "Fork patch found no shader paths to relocate; upstream changed." }

    # SConstruct names the library after the single demo/addons/*/*.gdextension.
    $ext = Join-Path $src 'demo/addons/gd_cubism/gd_cubism.gdextension'
    if (-not (Test-Path $ext)) { throw "Fork patch: $ext not found; upstream changed." }
    Rename-Item $ext "$extName.gdextension"
}

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
    Invoke-ForkPatch
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
    Write-Step "GDCubism already built ($dllName)."
}

# --- 5. stage ---------------------------------------------------------------
# Start clean so renamed or dropped files (e.g. the pre-fork gd_cubism.*) never ship.
Get-ChildItem $stage -Force -ErrorAction SilentlyContinue | Where-Object Name -ne '.gdignore' | Remove-Item -Recurse -Force
New-Item -ItemType Directory -Force (Join-Path $stage 'bin') | Out-Null
Copy-Item $builtDll (Join-Path $stage "bin/$dllName") -Force
# Notices for what the dll contains: GDCubism (MIT), godot-cpp (MIT), Cubism SDK (Live2D licenses).
$licenses = Join-Path $stage 'licenses'
New-Item -ItemType Directory -Force $licenses | Out-Null
Copy-Item (Join-Path $src 'LICENSE.en.adoc') (Join-Path $licenses 'gd_cubism-LICENSE.adoc')
Copy-Item (Join-Path $src 'godot-cpp/LICENSE.md') (Join-Path $licenses 'godot-cpp-LICENSE.md')
Copy-Item (Join-Path $sdk.FullName 'LICENSE.md') (Join-Path $licenses 'CubismSdk-LICENSE.md')
Copy-Item (Join-Path $sdk.FullName 'NOTICE.md') (Join-Path $licenses 'CubismSdk-NOTICE.md')
# Loaded at runtime from disk, outside res://; the game is a release template, but list debug too
# so the editor-run game (MegaDot) resolves the same binary.
@"
[configuration]

entry_symbol = "gd_cubism_library_init"
compatibility_minimum = 4.3

[libraries]

windows.debug.x86_64 = "bin/$dllName"
windows.release.x86_64 = "bin/$dllName"
"@ | Set-Content (Join-Path $stage "$extName.gdextension") -Encoding ascii

# Models go in the pck, NOT next to the dll: the game's ModManager treats every *.json under
# mods/ as a mod manifest. Samples land in a gitignored folder that bundle-workshop.ps1 refuses.
$modelSrc = Join-Path $sdk.FullName "Samples/Resources/$Sample"
if (-not (Test-Path $modelSrc)) { throw "Sample model not found: $modelSrc" }
$samples = Join-Path $repo 'TheWitch/TheWitch/live2d/samples'
$modelDst = Join-Path $samples $Sample.ToLowerInvariant()
New-Item -ItemType Directory -Force $samples | Out-Null
if (Test-Path $modelDst) { Remove-Item $modelDst -Recurse -Force }
Copy-Item $modelSrc $modelDst -Recurse

# --- 6. shaders -------------------------------------------------------------
# At the res:// path the fork loads them from ($shaderRes).
$shaderDst = Join-Path $repo 'TheWitch/TheWitch/live2d/shader'
if (Test-Path $shaderDst) { Remove-Item $shaderDst -Recurse -Force }
New-Item -ItemType Directory -Force $shaderDst | Out-Null
Copy-Item (Join-Path $src 'demo/addons/gd_cubism/res/shader/*.gdshader') $shaderDst -Force
# Pre-fork location; the export filter excludes addons/* again, so it's just clutter.
$oldShaders = Join-Path $repo 'TheWitch/addons/gd_cubism'
if (Test-Path $oldShaders) { Remove-Item $oldShaders -Recurse -Force }

Write-Step "Staged into $stage (sample model: $($Sample.ToLowerInvariant()))."
Write-Step "Next: dotnet publish TheWitch/TheWitch.csproj, then launch the Witch."
