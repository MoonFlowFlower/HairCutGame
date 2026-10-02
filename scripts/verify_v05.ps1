param([switch]$Network,[switch]$FullDuration,[switch]$Stress)
# The existing runner discovers the expanded core and engine suites; keep one implementation.
& "$PSScriptRoot/verify_v04.ps1" -Network:$Network -FullDuration:$FullDuration -Stress:$Stress
if ($LASTEXITCODE -ne 0) { throw 'v0.5 regression verification failed' }
Write-Host 'V05_VERIFY_OK'
