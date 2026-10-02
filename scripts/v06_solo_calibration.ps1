param([ValidateRange(1,5)][int]$Runs=1,[switch]$Rendered)
# Old helicopter scheduling recipe is replaced by actual party material-path calibration.
for($i=0;$i -lt $Runs;$i++){& "$PSScriptRoot/party_capacity.ps1";if($LASTEXITCODE -ne 0){throw 'Party calibration failed'}}
if($Rendered){& "$PSScriptRoot/v06_review.ps1"}
