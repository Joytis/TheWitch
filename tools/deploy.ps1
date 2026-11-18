<#
.SYNOPSIS
    Release pipeline for a character mod: verify the one source tree against both game branches
    (public + beta installs), then tag, upload both builds to the Workshop and push.

.DESCRIPTION
    Run it after the version bump + patch notes are committed (/version-bump). Stages, in order:

        preflight          git state, version/notes/tag, Steam, both game installs, tools
        publish-<branch>   dotnet publish -p:Sts2Branch=<branch> (+ BirdDebug), record artifact hashes
        testall-<branch>   launch.ps1 -TestAll -Headless against that install
        autoslay-<branch>  -AutoSlayRuns full bot runs (autoslay-run.ps1)
        review             GATE: look at the verification results before anything is committed or uploaded
        marketing          card-grid gallery images (+ in-game shots with -Screenshots), committed
                           (the gallery itself is synced to marketing.json by upload-<first branch>)
        upload-public      bundle-workshop.ps1 -Upload (+ gallery sync); the exact files that were tested
        tag                git tag <version> (local, pushed by the last stage)
        flag-public        GATE: mark the new revision as the public-branch version on the Workshop page
        upload-beta        same, beta build
        flag-beta          GATE: mark the new revision as the beta-branch version
        push               git push origin <branch> + the tag

    The pipeline owns the version tag: it is created only once the first upload has succeeded, so a
    tag means "this version is on the Workshop", and preflight refuses to start a version whose tag
    already exists (locally or on origin). Never tag a release by hand.

    Everything is verified on BOTH branches before anything outward-facing happens. Game runs are
    serial (every instance shares %APPDATA%/SlayTheSpire2/logs/godot.log).

    State + logs live in build/deploy/<ModId>-<version>/ (gitignored): state.json and one
    NN-<stage>.log per stage. A failed stage stops the run, records the cause, and posts it to the
    deploy webhook; fix the cause and re-run with -Resume (finished stages are skipped). A source
    change invalidates the verification, so -Resume refuses when HEAD moved: use -Restart.

    A gate stops the run until you have done the manual step. In a terminal it waits right there
    (Enter = done, "later" = stop); unattended (no console, or -NoPrompt) it exits 2 and
    -Resume -Ack continues.

    In a terminal a full run also asks once before it starts, and offers resume / restart when a
    run for this version already exists. -Yes skips the start question; without a console a
    full run refuses to start unless -Yes is given.

    Notifications go to $env:DEPLOY_WEBHOOK_URL or tools/deploy-webhook.local.txt (a Discord
    webhook URL; gitignored). This is deliberately NOT tools/discord-webhook.local.txt, which is
    the player-facing "updated on the Workshop" announcement posted by bundle-workshop.ps1.

    Exit codes: 0 done, 1 a stage failed, 2 waiting at a gate.

.PARAMETER Branches
    Game branches to verify + upload (default public,beta).

.PARAMETER VerifyOnly
    Only preflight + publish/testall/autoslay. No git requirements, nothing outward-facing.
    Uses its own run dir (build/deploy/<ModId>-verify) and always starts fresh.

.PARAMETER Marketing
    With -VerifyOnly: also run the marketing stage as a preview. It renders the images and prints
    how the live Workshop gallery differs from marketing.json (a dry run), but commits and
    uploads nothing. Combine with -Skip to preview the marketing alone.

.PARAMETER Resume
    Continue the existing run for this version: finished stages are skipped, a failed one re-runs.

.PARAMETER Ack
    With -Resume: confirm the gate the run is waiting at.

.PARAMETER NoPrompt
    Never ask anything on the console: gates exit 2, an existing run needs -Resume or -Restart.
    Implied when there is no interactive console.

.PARAMETER Yes
    Skip the "release this version?" question a full run asks in a terminal. Required to start a
    full run where there is no console to ask on (a script, a scheduler).

.PARAMETER Restart
    Archive the existing run dir for this version and start over.

.PARAMETER From
    With -Resume: re-run from this stage on (it and every later stage reset to pending).

.PARAMETER Skip
    Stage names to skip (recorded as 'skipped' in state.json and the summary).

.PARAMETER AutoSlayRuns
    Full AutoSlay runs per branch (default 1; 0 = none). Each is up to 25 minutes.

.PARAMETER AllowStrayErrors
    Do not fail testall on [ERROR] lines the harness did not attribute to a test item.

.PARAMETER Screenshots
    Also re-capture the in-game marketing screenshots (opens game windows; hands off the mouse).

.PARAMETER NoPush
    Leave the release commit(s) and tag local.

.PARAMETER Plan
    Print the stage list (with the current run's status, if any) and exit.

.EXAMPLE
    ./tools/deploy.ps1 -VerifyOnly                    # is the tree releasable on both branches?
    ./tools/deploy.ps1 -VerifyOnly -Marketing         # ... and preview the marketing images + gallery diff
    ./tools/deploy.ps1                                # full release of the committed version
    ./tools/deploy.ps1 -Resume -Ack                   # after flagging the revision on the Workshop page
    ./tools/deploy.ps1 -Resume -Skip autoslay-beta    # after judging a bot failure to be unrelated
#>
[CmdletBinding()]
param(
    [ValidateSet('Witch','Augur')]
    [string]$Character = 'Witch',
    [string[]]$Branches = @('public','beta'),
    [switch]$VerifyOnly,
    [switch]$Marketing,
    [switch]$Resume,
    [switch]$Ack,
    [switch]$Restart,
    [string]$From = '',
    [string[]]$Skip = @(),
    [int]$AutoSlayRuns = 1,
    [switch]$AllowStrayErrors,
    [switch]$Screenshots,
    [switch]$NoPush,
    [switch]$NoPrompt,
    [switch]$Yes,
    [string]$GitBranch = 'main',
    [switch]$Plan
)

$ErrorActionPreference = 'Stop'
# `powershell -File deploy.ps1 -Skip a,b` hands a list over as ONE string; accept both shapes.
$Branches = @($Branches | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim().ToLowerInvariant() } | Where-Object { $_ })
$Skip     = @($Skip | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
foreach ($b in $Branches) { if ('public', 'beta' -notcontains $b) { throw "Unknown game branch '$b' (public | beta)." } }
$Interactive = (-not $NoPrompt) -and [Environment]::UserInteractive -and (-not [Console]::IsInputRedirected)
if ($Marketing -and -not $VerifyOnly) { throw '-Marketing is the -VerifyOnly preview; a full run always includes the marketing stage.' }
$repoRoot = Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'characters.ps1')
$Char      = Get-BirdCharacter $Character
$ModId     = $Char.ModId
$csproj    = Join-Path $repoRoot $Char.Csproj
$workshop  = Join-Path $repoRoot $Char.Workshop
$manifest  = Join-Path $repoRoot (Join-Path $Char.ProjectDir "$ModId.json")
$Version   = (Get-Content $manifest -Raw | ConvertFrom-Json).version
$notesFile = Join-Path $repoRoot "Docs/patch-notes/$Version.md"
$psExe     = (Get-Process -Id $PID).Path
$utf8      = New-Object System.Text.UTF8Encoding($false)

$runName = if ($VerifyOnly) { "$ModId-verify" } else { "$ModId-$Version" }
$runDir  = Join-Path $repoRoot "build/deploy/$runName"
$statePath = Join-Path $runDir 'state.json'

# ---------------------------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------------------------
function Write-Step($msg) { Write-Host "==> $msg" -ForegroundColor Cyan }

# ConvertFrom-Json gives PSCustomObjects (PS 5.1 has no -AsHashtable); state is edited as hashtables.
function ConvertTo-Hashtable($obj) {
    if ($null -eq $obj) { return $null }
    if ($obj -is [System.Management.Automation.PSCustomObject]) {
        $h = [ordered]@{}
        foreach ($p in $obj.PSObject.Properties) { $h[$p.Name] = ConvertTo-Hashtable $p.Value }
        return $h
    }
    if ($obj -is [System.Collections.IEnumerable] -and $obj -isnot [string]) {
        return ,@($obj | ForEach-Object { ConvertTo-Hashtable $_ })
    }
    return $obj
}

function Save-State {
    [System.IO.File]::WriteAllText($statePath, ($script:State | ConvertTo-Json -Depth 8), $utf8)
}

# Runs a native command, streaming its output to the console AND appending it to $Log (UTF-8).
# Returns the exit code. ErrorActionPreference is relaxed for the call: under PS 5.1 a native
# stderr line arrives as an ErrorRecord and would otherwise abort the pipeline.
function Invoke-Logged([string]$Log, [string]$Exe, [string[]]$ArgList) {
    $writer = New-Object System.IO.StreamWriter($Log, $true, $utf8)
    $old = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $writer.WriteLine("`$ $Exe $($ArgList -join ' ')")
        & $Exe @ArgList 2>&1 | ForEach-Object { $l = "$_"; $writer.WriteLine($l); Write-Host $l }
        $code = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $old
        $writer.Dispose()
    }
    return $code
}

# One of the repo's own tools/*.ps1, in a child PowerShell (they call `exit`).
function Invoke-Tool([string]$Log, [string]$Script, [string[]]$ArgList) {
    $all = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', (Join-Path $PSScriptRoot $Script)) + $ArgList
    return (Invoke-Logged $Log $psExe $all)
}

# Quiet native call: stdout lines back, stderr dropped, exit code in $script:NativeExit.
# (ErrorActionPreference relaxed for the same PS 5.1 stderr reason as Invoke-Logged.)
function Invoke-Native([string]$Exe, [string[]]$ArgList) {
    $old = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $out = & $Exe @ArgList 2>$null
        $script:NativeExit = $LASTEXITCODE
    } finally { $ErrorActionPreference = $old }
    return $out
}

function Invoke-Git([string[]]$GitArgs) {
    $out = Invoke-Native 'git' (@('-C', $repoRoot) + $GitArgs)
    if ($script:NativeExit -ne 0) { throw "git $($GitArgs -join ' ') failed (exit $($script:NativeExit))." }
    return $out
}

# Commit a tag points at, or $null when the tag does not exist.
function Get-TagCommit([string]$Tag) {
    $sha = Invoke-Native 'git' @('-C', $repoRoot, 'rev-parse', '-q', '--verify', "refs/tags/$Tag^{commit}")
    if ($script:NativeExit -ne 0) { return $null }
    return "$sha".Trim()
}

function Get-LogLines([string]$Log) { if (Test-Path $Log) { return @(Get-Content $Log) } else { return @() } }

# Last N non-empty lines: the fallback "why" for a stage with no specific extractor.
function Get-LogTail([string]$Log, [int]$Count = 20) {
    return (@(Get-LogLines $Log | Where-Object { $_.Trim() }) | Select-Object -Last $Count) -join "`n"
}

# Compiler / MSBuild errors, deduped (msbuild prints each twice: inline + in the summary).
function Get-BuildErrors([string]$Log) {
    $errs = @(Get-LogLines $Log | Where-Object { $_ -match '(: |\b)error\b( [A-Z]+\d+)?\s*:' } |
        ForEach-Object { ($_ -replace '\s*\[[^\]]+proj\]\s*$', '').Trim() } | Select-Object -Unique)
    if ($errs.Count -eq 0) { return (Get-LogTail $Log) }
    $shown = ($errs | Select-Object -First 12) -join "`n"
    if ($errs.Count -gt 12) { $shown += "`n... +$($errs.Count - 12) more" }
    return $shown
}

function Get-ModFiles([string]$Branch) {
    $dir = Join-Path (Join-Path $script:State.installs[$Branch].path 'mods') $ModId
    return @("$ModId.dll", "$ModId.pck", "$ModId.json") | ForEach-Object { Join-Path $dir $_ }
}

function Get-ArtifactHashes([string]$Branch) {
    $h = [ordered]@{}
    foreach ($f in (Get-ModFiles $Branch)) {
        if (-not (Test-Path $f)) { throw "Build output missing: $f" }
        $h[(Split-Path $f -Leaf)] = (Get-FileHash $f -Algorithm SHA256).Hash
    }
    return $h
}

# "Ship exactly what was tested": the mods-folder files must still be the ones publish-<branch> produced.
function Assert-Artifacts([string]$Branch) {
    $want = $script:State.artifacts[$Branch]
    if (-not $want) { throw "No recorded build for '$Branch' (publish-$Branch has not run)." }
    $have = Get-ArtifactHashes $Branch
    foreach ($k in $want.Keys) {
        if ($have[$k] -ne $want[$k]) {
            throw "$k in the $Branch install's mods folder is not the build publish-$Branch produced (something rebuilt it). Re-run with -Resume -From publish-$Branch."
        }
    }
}

function Get-WebhookUrl {
    if ($env:DEPLOY_WEBHOOK_URL) { return $env:DEPLOY_WEBHOOK_URL }
    $f = Join-Path $PSScriptRoot 'deploy-webhook.local.txt'
    if (Test-Path $f) { return (Get-Content $f -Raw).Trim() }
    return $null
}

function Send-Notify([string]$Title, [string]$Body, [int]$Color) {
    $url = Get-WebhookUrl
    if (-not $url) { return }
    if ($Body.Length -gt 3800) { $Body = $Body.Substring(0, 3800) + "`n... (truncated)" }
    $payload = @{ embeds = @(@{ title = $Title; description = $Body; color = $Color }) } | ConvertTo-Json -Depth 6
    try {
        Invoke-RestMethod -Uri $url -Method Post -ContentType 'application/json; charset=utf-8' `
            -Body ([System.Text.Encoding]::UTF8.GetBytes($payload)) | Out-Null
    } catch {
        Write-Host "[warn] deploy notification failed: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

function Get-ItemUrl {
    $idFile = Join-Path $workshop 'mod_id.txt'
    if (Test-Path $idFile) { return "https://steamcommunity.com/sharedfiles/filedetails/?id=$((Get-Content $idFile -Raw).Trim())" }
    return '(no mod_id.txt)'
}

# ---------------------------------------------------------------------------------------------
# Stages. Each body gets its log path (+ its game branch); it throws (message = the cause) to fail the stage and may
# return a short detail string for the summary.
# ---------------------------------------------------------------------------------------------
$Stages = New-Object System.Collections.Generic.List[object]
function Add-Stage([string]$Name, [scriptblock]$Body, [string]$Branch = '', [switch]$Gate, [switch]$Outward, [switch]$Always) {
    $Stages.Add([pscustomobject]@{ Name = $Name; Body = $Body; Branch = $Branch; Gate = [bool]$Gate; Outward = [bool]$Outward; Always = [bool]$Always })
}

Add-Stage 'preflight' -Always {
    param($log)
    $problems = @()
    $warnings = @()

    if (-not $VerifyOnly) {
        $current = (Invoke-Git @('rev-parse', '--abbrev-ref', 'HEAD'))
        if ($current -ne $GitBranch) { $problems += "On git branch '$current', expected '$GitBranch'." }
        # Fresh runs only: by resume time the uploads have rewritten workshop.json's changeNote, and
        # the artifact hashes (Assert-Artifacts) are what guarantee the tested build is the one shipped.
        $dirty = @()
        if (-not $Resume) { $dirty = @(Invoke-Git @('status', '--porcelain')) }
        if ($dirty.Count -gt 0) { $problems += "Working tree is not clean ($($dirty.Count) path(s)); commit the version bump + notes first." }
        if (-not (Test-Path $notesFile)) { $problems += "Patch notes missing: Docs/patch-notes/$Version.md (run /version-bump)." }
        # The tag is written by this pipeline after the first upload, so on a fresh run an existing
        # tag means the version is already on the Workshop. (A resumed run may have tagged it itself.)
        if (-not $Resume) {
            $tagSha = Get-TagCommit $Version
            if ($tagSha) {
                $problems += "Tag $Version already exists (at $($tagSha.Substring(0, 8))): that version has been released. Bump the version (/version-bump)."
            } else {
                $remote = Invoke-Native 'git' @('-C', $repoRoot, 'ls-remote', '--tags', 'origin', "refs/tags/$Version")
                if ($script:NativeExit -ne 0) { $warnings += "Could not ask origin whether tag $Version exists (offline?); only the local tags were checked." }
                elseif ("$remote".Trim()) { $problems += "Tag $Version already exists on origin: that version has been released. Fetch tags and bump the version (/version-bump)." }
            }
        }
        if (-not (Test-Path (Join-Path $workshop 'mod_id.txt'))) { $problems += "No mod_id.txt in $($Char.Workshop)." }
        if (-not (Test-Path (Join-Path $PSScriptRoot 'ModUploader-win-x64/ModUploader.exe'))) { $problems += 'tools/ModUploader-win-x64/ModUploader.exe not found.' }

        $null = Invoke-Native 'node' @((Join-Path $repoRoot 'Docs/card-data/regen.js'), '--check')
        if ($script:NativeExit -ne 0) { $problems += 'Card docs are stale: run `node Docs/card-data/regen.js` and commit.' }
    }

    if (-not (Get-Process -Name steam -ErrorAction SilentlyContinue)) { $problems += 'Steam is not running (the game and the uploader need the client, logged in).' }
    if (Get-Process -Name SlayTheSpire2 -ErrorAction SilentlyContinue) { $problems += 'Slay the Spire 2 is running; close it (test runs share godot.log).' }

    $godot = (Invoke-Native 'dotnet' @('msbuild', $csproj, '-getProperty:GodotPath', '-nologo') | Select-Object -Last 1)
    if (-not $godot -or -not (Test-Path $godot.Trim())) { $problems += "GodotPath not set or missing ('$godot'); publish cannot export the .pck." }

    foreach ($b in $Branches) {
        try {
            $path = Get-Sts2BranchPath $b
            $info = Join-Path $path 'release_info.json'
            $ver = if (Test-Path $info) { (Get-Content $info -Raw | ConvertFrom-Json).version } else { 'unknown' }
            $script:State.installs[$b] = [ordered]@{ path = $path; gameVersion = $ver }
            Write-Host ("    {0,-6} {1}  ({2})" -f $b, $ver, $path)
            # No cached public copy = the Steam install stands in for it, which is only right while
            # Steam is actually on the public branch.
            $steamPath = $null
            try { $steamPath = Get-SteamSts2Path } catch {}
            if ($b -eq 'public' -and $steamPath -and ([IO.Path]::GetFullPath($path) -ieq [IO.Path]::GetFullPath($steamPath))) {
                $steamBranch = Get-SteamSts2Branch $steamPath
                if ($steamBranch -and $steamBranch -ne 'public') {
                    $problems += "The public build would use the Steam install, but Steam has the game on its '$steamBranch' branch. Switch Steam to public, then cache it: tools/setup-game-install.ps1 -Branch public."
                }
            }
        } catch { $problems += $_.Exception.Message }
    }
    if ($Branches.Count -eq 2 -and $script:State.installs['public'] -and $script:State.installs['beta']) {
        if ($script:State.installs['public'].path -eq $script:State.installs['beta'].path) { $problems += 'Public and beta resolve to the same install folder.' }
        elseif ($script:State.installs['public'].gameVersion -eq $script:State.installs['beta'].gameVersion) {
            $warnings += "Both installs are game $($script:State.installs['public'].gameVersion); is the beta copy up to date?"
        }
    }
    if (-not (Get-WebhookUrl)) { $warnings += 'No deploy webhook (DEPLOY_WEBHOOK_URL or tools/deploy-webhook.local.txt): failures are reported on this console only.' }

    foreach ($w in $warnings) { Write-Host "[warn] $w" -ForegroundColor Yellow }
    if ($problems.Count -gt 0) { throw ($problems -join "`n") }
    return (($Branches | ForEach-Object { "$_ = game $($script:State.installs[$_].gameVersion)" }) -join ', ')
}

foreach ($branch in $Branches) {
    Add-Stage "publish-$branch" -Branch $branch {
        param($log, $branch)
        $started = Get-Date
        $code = Invoke-Logged $log 'dotnet' @('publish', $csproj, "-p:Sts2Branch=$branch", '-nologo')
        if ($code -ne 0) { throw "dotnet publish ($branch) failed, exit $code.`n$(Get-BuildErrors $log)" }
        $code = Invoke-Logged $log 'dotnet' @('build', (Join-Path $repoRoot $BirdDebugCsproj), "-p:Sts2Branch=$branch", '-nologo')
        if ($code -ne 0) { throw "dotnet build BirdDebug ($branch) failed, exit $code.`n$(Get-BuildErrors $log)" }
        $pck = (Get-ModFiles $branch)[1]
        if (-not (Test-Path $pck) -or (Get-Item $pck).LastWriteTime -lt $started) {
            throw "Publish did not write a fresh .pck ($pck). The Godot export step failed:`n$(Get-LogTail $log 15)"
        }
        $script:State.artifacts[$branch] = Get-ArtifactHashes $branch
        return "dll $($script:State.artifacts[$branch]["$ModId.dll"].Substring(0, 12))"
    }

    Add-Stage "testall-$branch" -Branch $branch {
        param($log, $branch)
        Assert-Artifacts $branch
        $code = Invoke-Tool $log 'launch.ps1' @('-Character', $Char.Key, '-Branch', $branch, '-Solo', '-TestAll', '-Headless', '-TailLog')
        $lines = Get-LogLines $log
        $summary = $lines | Where-Object { $_ -match '^Items: (\d+) run, (\d+) passed, (\d+) failed \| stray errors: (\d+)' } | Select-Object -Last 1
        if (-not $summary) { throw "No smoke-test report in the log (game exit $code); the harness never finished.`n$(Get-LogTail $log 15)" }
        $null = $summary -match '^Items: (\d+) run, (\d+) passed, (\d+) failed \| stray errors: (\d+)'
        $run = [int]$Matches[1]; $failed = [int]$Matches[3]; $stray = [int]$Matches[4]
        $bad = ($code -ne 0) -or ($run -eq 0) -or ($failed -gt 0) -or ($stray -gt 0 -and -not $AllowStrayErrors)
        if ($bad) {
            $at = 0
            for ($i = $lines.Count - 1; $i -ge 0; $i--) { if ($lines[$i] -match 'SMOKE TEST REPORT') { $at = $i; break } }
            $report = ($lines[$at..($lines.Count - 1)] | Select-Object -First 30) -join "`n"
            throw "Smoke tests failed on $branch (game exit $code).`n$report"
        }
        return $summary
    }

    Add-Stage "autoslay-$branch" -Branch $branch {
        param($log, $branch)
        if ($AutoSlayRuns -le 0) { return 'no runs requested' }
        Assert-Artifacts $branch
        $outDir = Join-Path $runDir "autoslay-$branch"
        $seeds = @()
        for ($i = 1; $i -le $AutoSlayRuns; $i++) {
            $code = Invoke-Tool $log 'autoslay-run.ps1' @('-Character', $Char.Key, '-Branch', $branch, '-OutDir', $outDir, '-Index', "$i")
            $sumFile = Get-ChildItem $outDir -Filter ("{0:D3}-*.summary.json" -f $i) -ErrorAction SilentlyContinue |
                Sort-Object LastWriteTime -Descending | Select-Object -First 1
            if (-not $sumFile) { throw "AutoSlay run $i on $branch wrote no summary (exit $code).`n$(Get-LogTail $log 15)" }
            $s = Get-Content $sumFile.FullName -Raw | ConvertFrom-Json
            $seeds += $s.seed
            if ($code -ne 0) {
                $why = @("AutoSlay run $i on $branch failed: seed $($s.seed), reached $($s.lastFloor), completed=$($s.completed), game exit $($s.exitCode).")
                if ($s.failure) { $why += ($s.failure -split "`n" | Select-Object -First 6) }
                foreach ($e in @($s.errors | Select-Object -First 5)) { $why += ("{0}x {1}" -f $e.count, $e.message); if ($e.frame) { $why += "    $($e.frame)" } }
                $why += "Reproduce: ./tools/autoslay-run.ps1 -Character $($Char.Key) -Branch $branch -OutDir out -Seed $($s.seed)"
                $why += "Summary: $($sumFile.FullName)"
                throw ($why -join "`n")
            }
        }
        return "seeds $($seeds -join ', ')"
    }
}

if (-not $VerifyOnly) {
    # GATE: both branches are verified; nothing is committed or uploaded until someone has looked at the results.
    Add-Stage 'review' -Gate {
        param($log)
        $rows = foreach ($s in $script:Stages) {
            if ($s.Name -eq 'review') { break }
            $st = $script:State.stages[$s.Name]
            "- $($s.Name): $($st.status)$(if ($st.detail) { " - $($st.detail)" })"
        }
        return "Review the verification results, then continue to marketing + upload:`n$($rows -join "`n")`nLogs: $runDir"
    }
}

if ((-not $VerifyOnly) -or $Marketing) {
    Add-Stage 'marketing' {
        param($log)
        $code = Invoke-Logged $log 'node' @((Join-Path $PSScriptRoot 'render-marketing-cards.js'), '--character', $Char.Key.ToLowerInvariant())
        if ($code -ne 0) { throw "render-marketing-cards.js failed, exit $code.`n$(Get-LogTail $log 15)" }
        if ($Screenshots) {
            if (-not $script:State.installs['public']) { throw '-Screenshots needs the public branch (the shots are taken in the public install).' }
            $code = Invoke-Tool $log 'capture-marketing.ps1' @('-Character', $Char.Key, '-Sts2Path', $script:State.installs['public'].path)
            if ($code -ne 0) { throw "capture-marketing.ps1 failed, exit $code.`n$(Get-LogTail $log 15)" }
        }
        $rel = "$($Char.Workshop)/marketing"
        $changed = @(Invoke-Git @('status', '--porcelain', '--', $rel))
        if ($VerifyOnly) {
            # Preview: what would be committed, and what the upload would change in the live gallery.
            foreach ($c in $changed) { Write-Host "    changed: $c" }
            $code = Invoke-Tool $log 'bundle-workshop.ps1' @('-Character', $Char.Key, '-UpdateMarketing', '-MarketingDryRun')
            if ($code -ne 0) { throw "Gallery dry run failed, exit $code.`n$(Get-LogTail $log 15)" }
            return "$($changed.Count) image file(s) differ from HEAD (left uncommitted); gallery diff in the log"
        }
        if ($changed.Count -eq 0) { return 'no image changes' }
        Invoke-Git @('add', '--', $rel) | Out-Null
        Invoke-Git @('commit', '-m', "Marketing: regenerate gallery images for $Version", '--', $rel) | Out-Null
        $sha = (Invoke-Git @('rev-parse', 'HEAD'))
        $script:State.pipelineCommits = @($script:State.pipelineCommits) + $sha
        return "$($changed.Count) file(s) committed as $($sha.Substring(0, 8))"
    }
}

if (-not $VerifyOnly) {
    foreach ($branch in $Branches) {
        Add-Stage "upload-$branch" -Branch $branch -Outward {
            param($log, $branch)
            Assert-Artifacts $branch
            $toolArgs = @('-Character', $Char.Key, '-Branch', $branch, '-SkipPublish', '-Upload', '-ChangeNoteFile', $notesFile)
            if ($branch -eq $Branches[0]) { $toolArgs += '-UpdateMarketing' }
            $code = Invoke-Tool $log 'bundle-workshop.ps1' $toolArgs
            # bundle-workshop writes this upload's change note into the tracked workshop.json; put the
            # committed file back so the tree is clean again for the push and for the next release.
            Invoke-Git @('checkout', '--', "$($Char.Workshop)/workshop.json") | Out-Null
            if ($code -ne 0) { throw "Workshop upload ($branch) failed, exit $code.`n$(Get-LogTail $log 15)" }
            return "uploaded the $branch build (game $($script:State.installs[$branch].gameVersion))"
        }

        if ($branch -eq $Branches[0]) {
            # Only now: the tag records "this version is on the Workshop" (see preflight).
            Add-Stage 'tag' {
                param($log)
                $existing = Get-TagCommit $Version
                $head = (Invoke-Git @('rev-parse', 'HEAD'))
                if ($existing -eq $head) { return 'already tagged' }
                if ($existing) { throw "Tag $Version already exists at $($existing.Substring(0, 8)), not at HEAD ($($head.Substring(0, 8))). It was not made by this run; resolve it by hand." }
                Invoke-Git @('tag', $Version) | Out-Null
                return "$Version -> $($head.Substring(0, 8))"
            }
        }

        Add-Stage "flag-$branch" -Branch $branch -Gate {
            param($log, $branch)
            return "Mark the revision just uploaded as the **$branch** game-branch version on the Workshop page: $(Get-ItemUrl)"
        }
    }

    Add-Stage 'push' -Outward {
        param($log)
        if ($NoPush) { return 'skipped (-NoPush)' }
        $code = Invoke-Logged $log 'git' @('-C', $repoRoot, 'push', 'origin', $GitBranch, "refs/tags/$Version")
        if ($code -ne 0) { throw "git push failed, exit $code.`n$(Get-LogTail $log 10)" }
        return "pushed $GitBranch + $Version"
    }
}

$stageNames = @($Stages | ForEach-Object { $_.Name })
foreach ($n in (@($Skip) + $From | Where-Object { $_ })) {
    if ($stageNames -notcontains $n) { throw "Unknown stage '$n'. Stages: $($stageNames -join ', ')" }
}

# ---------------------------------------------------------------------------------------------
# Run state
# ---------------------------------------------------------------------------------------------
$head = (Invoke-Git @('rev-parse', 'HEAD'))
$State = $null
if ($Plan) {
    if (Test-Path $statePath) { $State = ConvertTo-Hashtable (Get-Content $statePath -Raw | ConvertFrom-Json) }
    Write-Host "$ModId $Version  ($runDir)"
    $i = 0
    foreach ($s in $Stages) {
        $i++
        $st = if ($State -and $State.stages[$s.Name]) { $State.stages[$s.Name].status } else { 'pending' }
        $kind = if ($s.Gate) { 'gate' } elseif ($s.Outward) { 'outward' } else { '' }
        Write-Host ("  {0,2}  {1,-18} {2,-8} {3}" -f $i, $s.Name, $kind, $st)
    }
    exit 0
}

if ((Test-Path $statePath) -and $Interactive -and -not ($VerifyOnly -or $Restart -or $Resume)) {
    $prev = ConvertTo-Hashtable (Get-Content $statePath -Raw | ConvertFrom-Json)
    $at = @($prev.stages.Keys | Where-Object { $prev.stages[$_].status -notin 'done', 'skipped' }) | Select-Object -First 1
    Write-Host "A run for $ModId $Version already exists$(if ($at) { ", stopped at '$at' ($($prev.stages[$at].status))" } else { ' and finished' })." -ForegroundColor Yellow
    $answer = (Read-Host '[R]esume it, re[S]tart from scratch, or [C]ancel? (r/s/C)').Trim().ToLowerInvariant()
    if ($answer -eq 'r') { $Resume = $true }
    elseif ($answer -eq 's') { $Restart = $true }
    else { Write-Host 'Cancelled.'; exit 0 }
}
if (Test-Path $statePath) {
    if ($VerifyOnly -or $Restart) {
        $archive = "$runDir.$((Get-Item $statePath).LastWriteTime.ToString('yyyyMMdd-HHmmss'))"
        if ($VerifyOnly) { Remove-Item $runDir -Recurse -Force } else { Move-Item $runDir $archive; Write-Step "Archived previous run to $archive" }
    } elseif ($Resume) {
        $State = ConvertTo-Hashtable (Get-Content $statePath -Raw | ConvertFrom-Json)
        # Verification is only valid for the source it ran on: HEAD may have moved only by our own commits.
        $new = @(Invoke-Git @('rev-list', "$($State.headAtStart)..HEAD"))
        $foreign = @($new | Where-Object { @($State.pipelineCommits) -notcontains $_ })
        if ($foreign.Count -gt 0) { throw "HEAD moved by $($foreign.Count) commit(s) since this run verified the build. Start over with -Restart." }
    } else {
        throw "A run for $ModId $Version already exists ($runDir). Use -Resume to continue it or -Restart to start over."
    }
} elseif ($Resume) {
    throw "Nothing to resume: no run for $ModId $Version ($runDir)."
}

# A release is outward-facing: starting one takes a "y" at the console or an explicit -Yes.
if ((-not $State) -and (-not $VerifyOnly) -and (-not $Interactive) -and (-not $Yes)) {
    throw "Not starting a release of $ModId $Version without confirmation: there is no console to ask on, so pass -Yes (or use -VerifyOnly for a dry run)."
}
if ((-not $State) -and (-not $VerifyOnly) -and $Interactive -and (-not $Yes)) {
    Write-Host ''
    Write-Host "Release $ModId $Version to the Steam Workshop?" -ForegroundColor Yellow
    Write-Host "  builds   : $($Branches -join ' + ')   (each verified first: publish, smoke tests, $AutoSlayRuns AutoSlay run(s))"
    Write-Host "  commit   : $($head.Substring(0, 8)) on $GitBranch"
    Write-Host "  then     : tag $Version, upload, $(if ($NoPush) { 'no push' } else { "push to origin/$GitBranch" })"
    if ((Read-Host 'Proceed? (y/N)').Trim().ToLowerInvariant() -ne 'y') { Write-Host 'Cancelled.'; exit 0 }
}
if (-not $State) {
    New-Item -ItemType Directory -Force $runDir | Out-Null
    $State = [ordered]@{
        modId = $ModId; version = $Version; verifyOnly = [bool]$VerifyOnly
        startedAt = (Get-Date).ToString('s'); headAtStart = $head; pipelineCommits = @()
        installs = [ordered]@{}; artifacts = [ordered]@{}; stages = [ordered]@{}
    }
}
foreach ($k in 'installs', 'artifacts', 'stages') { if (-not $State[$k]) { $State[$k] = [ordered]@{} } }
foreach ($s in $Stages) {
    if (-not $State.stages[$s.Name]) { $State.stages[$s.Name] = [ordered]@{ status = 'pending' } }
}
if ($From) {
    $reset = $false
    foreach ($s in $Stages) {
        if ($s.Name -eq $From) { $reset = $true }
        if ($reset) { $State.stages[$s.Name] = [ordered]@{ status = 'pending' } }
    }
}
foreach ($n in $Skip) {
    if ($State.stages[$n].status -ne 'done') { $State.stages[$n] = [ordered]@{ status = 'skipped' } }
}
Save-State

function Get-Summary {
    $rows = foreach ($s in $Stages) {
        $st = $State.stages[$s.Name]
        $mark = switch ($st.status) { 'done' { '[ok]' } 'skipped' { '[skip]' } 'failed' { '[FAIL]' } 'waiting' { '[wait]' } default { '[ ]' } }
        $time = if ($st.seconds) { " $([Math]::Round($st.seconds / 60, 1))m" } else { '' }
        "{0,-7}{1}{2}" -f $mark, $s.Name, $time
    }
    return ($rows -join "`n")
}

# ---------------------------------------------------------------------------------------------
# Runner
# ---------------------------------------------------------------------------------------------
Write-Step "Deploy $ModId $Version  [$($Branches -join ' + ')]$(if ($VerifyOnly) { '  (verify only)' })"
Write-Host "    run dir: $runDir"
$resumeCmd = "./tools/deploy.ps1 -Character $($Char.Key) -Resume"
$index = 0
foreach ($s in $Stages) {
    $index++
    $st = $State.stages[$s.Name]
    if ($st.status -eq 'skipped') { Write-Host "--- $($s.Name): skipped" -ForegroundColor DarkGray; continue }
    if ($st.status -eq 'done' -and -not $s.Always) { Write-Host "--- $($s.Name): done" -ForegroundColor DarkGray; continue }

    if ($s.Gate) {
        $todo = & $s.Body $null $s.Branch
        if ($st.status -eq 'waiting' -and $Ack) {
            $st.status = 'done'; $st.ackedAt = (Get-Date).ToString('s'); Save-State
            Write-Host "--- $($s.Name): acknowledged" -ForegroundColor Green
            continue
        }
        $notify = ($st.status -ne 'waiting')
        $st.status = 'waiting'; $st.detail = $todo; Save-State
        Write-Host ''
        Write-Host "PAUSED at $($s.Name)" -ForegroundColor Yellow
        Write-Host "  $($todo -replace '\*\*', '')"
        if ($notify) { Send-Notify "$ModId $Version - action needed: $($s.Name)" "$todo`n`nThen: ``$resumeCmd -Ack`` (or Enter in the waiting terminal)" 16763904 }
        if ($Interactive) {
            $answer = (Read-Host '  Press Enter once that is done, or type "later" to stop here').Trim().ToLowerInvariant()
            if ($answer -ne 'later') {
                $st.status = 'done'; $st.ackedAt = (Get-Date).ToString('s'); Save-State
                Write-Host "--- $($s.Name): acknowledged" -ForegroundColor Green
                continue
            }
        }
        Write-Host "  Then: $resumeCmd -Ack"
        exit 2
    }

    $log = Join-Path $runDir ("{0:D2}-{1}.log" -f $index, $s.Name)
    if (Test-Path $log) { Remove-Item $log -Force }
    Write-Step "$($s.Name)"
    $st.status = 'running'; $st.startedAt = (Get-Date).ToString('s'); $st.log = $log
    $st.Remove('detail'); $st.Remove('cause')
    Save-State
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $detail = & $s.Body $log $s.Branch | Select-Object -Last 1
        $st.status = 'done'; $st.detail = "$detail"
    } catch {
        $st.status = 'failed'; $st.cause = $_.Exception.Message
    }
    $st.seconds = [Math]::Round($sw.Elapsed.TotalSeconds)
    Save-State

    if ($st.status -eq 'failed') {
        Write-Host ''
        Write-Host "FAILED at $($s.Name) after $([Math]::Round($st.seconds / 60, 1)) min" -ForegroundColor Red
        Write-Host $st.cause -ForegroundColor Red
        if (Test-Path $log) { Write-Host "Log: $log" }
        Write-Host "State: $statePath"
        if (-not $VerifyOnly) { Write-Host "After fixing: $resumeCmd   (a code fix needs a commit, then -Restart)" }
        $fence = '```'
        $body = "**Stage:** $($s.Name)  ($([Math]::Round($st.seconds / 60, 1)) min)`n$fence`n$($st.cause)`n$fence`n**Log:** $log`n`n$(Get-Summary)"
        Send-Notify "$ModId $Version - deploy FAILED at $($s.Name)" $body 15158332
        exit 1
    }
    Write-Host "    ok: $($st.detail)" -ForegroundColor Green
}

$State.finishedAt = (Get-Date).ToString('s')
Save-State
$total = [Math]::Round(((Get-Date) - [datetime]$State.startedAt).TotalMinutes)
Write-Host ''
Write-Step "$(if ($VerifyOnly) { 'Verified' } else { 'Deployed' }) $ModId $Version"
Write-Host (Get-Summary)
$title = if ($VerifyOnly) { "$ModId - verify passed" } else { "$ModId $Version - deployed" }
Send-Notify $title "$(Get-Summary)`n`nStarted $($State.startedAt), $total min wall clock." 3066993
exit 0
