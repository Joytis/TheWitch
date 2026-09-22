<#
.SYNOPSIS
    Common entry point for Supabase analytics operations.

.DESCRIPTION
    Dispatches to the python scripts in tools/analytics/. Charts live in the
    interactive dashboard (pages/analytics.html), fed by the aggregate JSON that
    'export' mode writes to pages/analytics-data/ (Witch) or
    pages/analytics-data/<character>/ (same script CI runs nightly via
    .github/workflows/analytics.yml).

    Read access uses the service_role key from supabase-service-key.local.txt
    (gitignored, next to this script) or the SUPABASE_READ_KEY env var.

.PARAMETER Character
    export mode: Witch | Augur. Omitted = the historical unfiltered Witch export
    into pages/analytics-data/ (works before the `character` column exists).
    Given = filters runs by the mod id (TheWitch / TheAugur; needs
    migrations/001_character_column.sql applied) and writes
    pages/analytics-data/<witch|augur>/ unless -OutDir overrides it.

.EXAMPLE
    ./tools/analytics/analytics.ps1 -Mode export
    ./tools/analytics/analytics.ps1 -Mode export -Character Augur
    ./tools/analytics/analytics.ps1 -Mode seed -Runs 20
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('export', 'seed')]
    [string]$Mode,

    # export mode: which character mod's runs to export (see .PARAMETER Character).
    [ValidateSet('', 'Witch', 'Augur')]
    [string]$Character = '',

    # export mode: output folder override (default derives from -Character).
    [string]$OutDir = '',

    # export mode: keep fabricated mod_version='seed-test' rows (local testing only).
    [switch]$IncludeSeed,

    # seed mode: how many fabricated runs to insert.
    [int]$Runs = 20
)

$scriptDir = $PSScriptRoot
$modIds = @{ Witch = 'TheWitch'; Augur = 'TheAugur' }

switch ($Mode) {
    'export' {
        $pyArgs = [System.Collections.Generic.List[string]]::new()
        $pyArgs.Add((Join-Path $scriptDir 'export_stats.py'))
        if ($Character) { $pyArgs.Add('--character'); $pyArgs.Add($modIds[$Character]) }
        if ($OutDir) { $pyArgs.Add('--out-dir'); $pyArgs.Add($OutDir) }
        if ($IncludeSeed) { $pyArgs.Add('--include-seed') }
        py @pyArgs
    }
    'seed' {
        py (Join-Path $scriptDir 'seed_test_runs.py') --runs $Runs
    }
}
exit $LASTEXITCODE
