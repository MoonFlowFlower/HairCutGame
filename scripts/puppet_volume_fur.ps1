param([ValidateSet(4,8,12,16,32,64,96,128)][int]$Shells=4,[switch]$Bundles,[switch]$TuftVolume,[switch]$Hybrid,[switch]$SurfaceSamples,[switch]$LegacyAppearance,[switch]$FlatClumps,[switch]$LegacyPolish,[switch]$Fragments,[switch]$Dynamics,[ValidateSet('normal','trimmed','carved')][string]$State='normal',[switch]$NoResponse,[switch]$NoCutFeedback,[switch]$CutReview,[switch]$DeepContact,[switch]$EdgeContact,[switch]$CutContact,[switch]$ContactPixels,[switch]$Record,[ValidateSet(0,1,2,4)][int]$DynamicHeads=0,[switch]$Interactive,[switch]$Check,[switch]$Motion,[switch]$Pilot,[string]$Shot='',[string]$Output='')
$flags=@('r7-review','r7-volume')
if($Bundles){$flags+='r7-bundle-field'}
if($TuftVolume){$flags+=@('r7-bundle-field','r7-tuft-volume')}
if($Hybrid){$flags+=@('r7-bundle-field','r7-tuft-volume','r7-tuft-hybrid')}
if($SurfaceSamples){$flags+=@('r7-bundle-field','r7-tuft-volume','r7-surface-samples')}
if($SurfaceSamples -and !$LegacyAppearance){$flags+=@('fur-soft-sampling','fur-lighting-fix')}
if($SurfaceSamples -and !$LegacyAppearance -and !$FlatClumps){$flags+='fur-clump-depth'}
if($SurfaceSamples -and !$LegacyAppearance -and !$LegacyPolish){$flags+=@('fur-volume-undercoat','puppet-soft-fabric')}
if($Fragments){$flags+='fur-fragments'}
if($Dynamics -or $DynamicHeads -or $CutReview){$flags+=@('fur-dynamics',('fur-dynamic-'+$State))}
if($NoResponse){$flags+='fur-no-response'}
if($NoCutFeedback){$flags+='fur-no-cut-feedback'}
if($CutReview){$flags+='fur-cut-review'}
if($DeepContact -or $EdgeContact -or $CutContact){$flags+='fur-contact-deep'}
if($EdgeContact){$flags+='fur-contact-edge'}
if($CutContact){$flags+='fur-contact-cut'}
if($ContactPixels){if($DynamicHeads -ne 1){throw 'ContactPixels requires DynamicHeads 1'};$flags+='fur-contact-pixels'}
if($Record){$flags+='fur-record'}
if($DynamicHeads){$flags+=@('fur-dynamic-perf',('fur-heads-'+$DynamicHeads))}
if($Motion){$flags+='hair-motion'}
if($Pilot){$flags+='volume-pilot'}
if(!$Output){$Output=Join-Path (Split-Path $PSScriptRoot -Parent) ('artifacts/puppet-volume-principle/run-'+$Shells+'-'+(Get-Date -Format 'yyyyMMdd-HHmmss-fff')+'-'+$PID)}
& "$PSScriptRoot/puppet_lab.ps1" -Round 6 -HairOptimizeRound 3 -HairShells $Shells -HairOptions $flags -Interactive:$Interactive -Check:$Check -Shot $Shot -Output $Output
