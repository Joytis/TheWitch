# Back-compat shim: the launcher is now tools/launch.ps1 with a -Character switch.
& (Join-Path $PSScriptRoot 'launch.ps1') -Character Witch @args
exit $LASTEXITCODE
