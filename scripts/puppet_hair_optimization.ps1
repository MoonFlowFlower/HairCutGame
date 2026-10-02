param([ValidateRange(0,3)][int]$Round=3,[switch]$Interactive,[switch]$Check,[string]$Output='', [string]$Shot='', [ValidateSet(0,1,2,4)][int]$Shells=0,[switch]$FuzzOff,[switch]$OldInterior,[switch]$NoSmoothing,[switch]$NoWarning,[switch]$NoPrefall,[switch]$NoMicrofiber,[switch]$LegacySampling,[switch]$LegacyMaterials,[switch]$Motion)
if(!$Output){$Output=Join-Path (Split-Path $PSScriptRoot -Parent) ('artifacts/puppet-hair-v03/round'+$Round+'-'+(Get-Date -Format 'yyyyMMdd-HHmmss-fff')+'-'+$PID)}
$flags=@()
if($FuzzOff){$flags+='no-hair-fuzz'}
if($OldInterior){$flags+='old-cut-interior'}
if($NoSmoothing){$flags+='no-hair-smoothing'}
if($NoWarning){$flags+='no-hair-warning'}
if($NoPrefall){$flags+='no-hair-prefall'}
if($NoMicrofiber){$flags+='no-hair-microfiber'}
if($LegacySampling){$flags+='legacy-hair-sampling'}
if($LegacyMaterials){$flags+=@('legacy-fabric','legacy-hair-lighting')}
if($Motion){$flags+='hair-motion'}
& "$PSScriptRoot/puppet_lab.ps1" -Round 6 -HairOptimizeRound $Round -HairReview -HairShells $Shells -HairOptions $flags -Interactive:$Interactive -Check:$Check -Shot $Shot -Output $Output
