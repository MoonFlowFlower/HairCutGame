# Material feasibility through Core's actual E/tool/route paths; actor poses are simulated.
. "$PSScriptRoot/env.ps1"
$outFile=Join-Path $ProjectRoot ('artifacts/party-capacity-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.log')
& "$PSScriptRoot/test.ps1" *> $outFile
if($LASTEXITCODE -ne 0){throw 'Capacity checks failed'}
$text=Get-Content $outFile -Raw
foreach($waste in @('False','True')){if($text -notmatch "PARTY_CAPACITY waste=$waste extraBottlesUsed=0"){throw "Missing capacity recipe: $waste"}}
Write-Host "PARTY_CAPACITY_OK normal and wasted-bottle flat-wig recipe; simulated poses, real material actions; $outFile"
