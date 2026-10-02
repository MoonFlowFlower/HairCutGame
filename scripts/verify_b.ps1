param([switch]$Network,[switch]$Rendered,[switch]$SoloCalibration)
# Shared engine/core checks remain; archived A-specific matches are opt-in elsewhere.
& "$PSScriptRoot/verify_v06.ps1" -Network:$Network -Rendered:$Rendered -SoloCalibration:$SoloCalibration
if($LASTEXITCODE -ne 0){throw 'B mainline verification failed'}
