param([ValidateSet('lan','domestic','cross-border','relay-stress')][string]$Profile='cross-border',[switch]$Legacy,[switch]$Rendered,[int]$Port=18100,[string]$Executable='',[ValidateSet(2,4)][int]$Players=2,[switch]$RequireReplay,[switch]$Voice,[switch]$VoiceFiltered,[switch]$RealAudio,[switch]$HumanCustomer,[string]$Twist="",[switch]$Cat,[string]$EvidenceDirectory='')
. "$PSScriptRoot/env.ps1"
$engine=if($Executable){(Resolve-Path -LiteralPath $Executable).Path}else{Find-Godot}
$python=(Get-Command python).Source
$tag=if($Legacy){'legacy'}else{'new'}
$outDir=Join-Path $ProjectRoot ('artifacts/net-'+$Profile+'-'+$tag+'-'+(Get-Date -Format 'yyyyMMdd-HHmmss-fff')+'-'+$Port+'-'+$PID)
if($EvidenceDirectory){$outDir=(Resolve-Path -LiteralPath $EvidenceDirectory).Path}else{New-Item -ItemType Directory -Force $outDir | Out-Null}
$processes=@()
$roles=@('host','client');if($Players -eq 4){$roles+=@('client2','client3')}
$games=@()
try {
    if(!$EvidenceDirectory){
    $proxyArgs=@(('"'+$PSScriptRoot+'/net_proxy.py"'),'--listen',($Port+1),'--server',$Port,'--profile',$Profile,'--report',('"'+$outDir+'/proxy.json"'),'--duration','270')
    $proxy=Start-Process -FilePath $python -ArgumentList $proxyArgs -WindowStyle Hidden -PassThru -RedirectStandardOutput "$outDir/proxy.log" -RedirectStandardError "$outDir/proxy.err"
    $processes+=$proxy
    $until=(Get-Date).AddSeconds(10);while(!(Test-Path "$outDir/proxy.json")){if((Get-Date) -gt $until){throw 'Proxy startup failed'};Start-Sleep -Milliseconds 100}
    foreach($role in $roles) {
        $netArgs=if($role -eq 'host'){@('--host','--port',$Port)}else{@('--join','127.0.0.1','--port',($Port+1))}
        $launch=@('--headless');if($Rendered -and $role -eq 'client'){$launch=@()}
        if($RealAudio){$launch+=@('--audio-driver','WASAPI')}
        if(!$Executable){$launch+=@('--path',('"'+$ProjectRoot+'"'))}
        $launch+=@('--log-file',('"'+$outDir+'/'+$role+'.log"'),'--')+$netArgs+@('--v06-variant-b','--full-smoke','--expected',$Players,'--net-walk','--report',('"'+$outDir+'/'+$role+'.json"'))
        if($Rendered -and $role -eq 'client'){$launch+=@('--capture',('"'+$outDir+'/client.png"'),'--capture-phase','Build','--capture-delay','60','--language','zh')}
        if($HumanCustomer){$launch+=@('--human-customer-smoke','--qa-customer','2')}else{$launch+='--force-ai-customer'}
        if($Cat){$launch+="--qa-cat"}
        if($Twist){$launch+=@("--qa-twist",$Twist)}
        if($Legacy){$launch+=@('--legacy-net-motion','--legacy-camera')}
        if($RealAudio){$launch+='--voice-test-mute'}
        if($Voice){$launch+='--voice-inject'}
        if($VoiceFiltered){$launch+=@('--voice-filter-test','2')}
        $proc=Start-Process -FilePath $engine -ArgumentList $launch -WindowStyle Hidden -PassThru -RedirectStandardOutput "$outDir/$role.stdout" -RedirectStandardError "$outDir/$role.err"
        $processes+=$proc;$games+=$proc;if($role -eq 'host'){Start-Sleep -Seconds 2}
    }
    $scenarioClock=[System.Diagnostics.Stopwatch]::StartNew()
    while(@($games | Where-Object {!$_.HasExited}).Count){if(@($games | Where-Object {$_.HasExited -and $_.ExitCode -ne 0}).Count){throw 'Game exited early; inspect peer logs'};if( $scenarioClock.Elapsed.TotalSeconds -gt 240){throw 'Network scenario timed out'};Start-Sleep -Milliseconds 500}
    foreach($game in $games){if($game.ExitCode -ne 0){throw 'Nonzero game exit'}}
    $games | ForEach-Object {@{pid=$_.Id;exitCode=$_.ExitCode}} | ConvertTo-Json | Set-Content "$outDir/process-exits.json"
    }
    foreach($role in $roles){
        $report=Get-Content "$outDir/$role.json" -Raw | ConvertFrom-Json
        if(!$report.success -or $report.variant -ne 'B' -or !$report.liveResolved){throw "$role did not finish correctly"}
        if($Twist -and $report.secrets.twist -ne $Twist){throw "Requested twist was not active"}
        if($Cat -and $report.secrets.catActions -lt 1){throw "Cat did not act"}
        if($Twist -eq "Family" -and $report.secrets.familyEdited){throw "Family edited hair"}
        if($HumanCustomer){if($report.secrets.targetBuildLeak -or $report.secrets.buildLeak -or !$report.secrets.targetRevealed){throw "Human privacy failure: $role"};$needsTarget=if($Twist -in @('Reverse','SplitInfo')){$report.secrets.actor -ne $report.secrets.customer}else{$report.secrets.actor -eq $report.secrets.customer -or ($Twist -eq 'Family' -and $report.secrets.family -eq $report.secrets.actor)};if($role -ne 'host' -and $needsTarget -and $report.secrets.targetReceived -notcontains $report.secrets.actor){throw 'Expected private target missing'};if(!$needsTarget -and $report.secrets.targetReceived.Count -gt 0){throw 'Target sent to ineligible role'}}
        if($Voice){$v=Get-Content "$outDir/$role.voice.json" -Raw|ConvertFrom-Json;if($v.sentPackets -lt 100){throw 'Voice injection failed'};foreach($stream in $v.streams){if($stream.decoded -lt 100 -or $stream.latencySamples -lt 100 -or $null -eq $stream.latencyEstimateMs -or $stream.latencyEstimateMs -ge 400){throw "Voice latency/forwarding failure: $role actor $($stream.actor)"}}}
        if($Voice -and $RealAudio){foreach($stream in $v.streams){$samples=$stream.decoded*$(if($stream.opus -gt 0){960}else{1920});if($stream.playbackDiscarded/[double]$samples -gt .01 -or $stream.mixedSeconds/$stream.speakerSeconds -lt .95){throw "Voice playback consumption failure: $role actor $($stream.actor)"}}}
        if(!$Legacy -and !$HumanCustomer){foreach($fact in @('leave:ToMirror','leave:Mirror','leave:ToDoor','attention:Notice','attention:Commit','attention:React','brace')){if($report.experimentFacts -notcontains $fact){throw "$role missed live cue: $fact"}}}
        foreach($file in @("$outDir/$role.err","$outDir/$role.log")){if((Get-Content $file -Raw) -match 'ERROR:|SMOKE_FAIL|Unhandled exception'){throw "Engine error in $file"}}
    }
    foreach($role in $roles | Where-Object {$_ -ne 'host'}){
        if($RequireReplay -and (Get-Content "$outDir/$role.log" -Raw) -notmatch 'REPLAY_RECEIVED frames=[1-9]'){throw "$role did not receive the recorded replay"}
        $hash=[regex]::Match((Get-Content "$outDir/$role.log" -Raw),'CLIENT_FINAL[^\r\n]*hash=([A-F0-9]+)').Groups[1].Value
        if(!$hash -or (Get-Content "$outDir/host.log" -Raw) -notmatch "HOST_FINAL_HASH $hash"){throw "Final authoritative state differs: $role"}
        $motion=Get-Content "$outDir/$role.net.json" -Raw | ConvertFrom-Json
        if(!$Legacy -and ($motion.headMotionSamples -lt 100 -or $motion.headTravel -lt 2.5 -or $motion.headMaxSpeed -gt $(if($HumanCustomer){4}else{3}))){throw "Head motion regression: $role"}
        if(!$Legacy -and ($motion.walkTicks -lt 1000 -or $motion.reverseTicks/[double]$motion.walkTicks -gt .05 -or $motion.hardCorrections -gt 0)){throw "Walking correction regression: $role"}
    }
    if(@($games | Where-Object {$_.ExitCode -ne 0}).Count){throw 'Nonzero game exit'}
    @{profile=$Profile;players=$Players;legacy=[bool]$Legacy;renderedClient=[bool]$Rendered;hash=$hash;passed=$true;client=(Get-Content "$outDir/client.net.json" -Raw | ConvertFrom-Json);proxy=(Get-Content "$outDir/proxy.json" -Raw | ConvertFrom-Json)} | ConvertTo-Json -Depth 6 | Set-Content "$outDir/summary.json"
    Write-Host "NET_SCENARIO_OK $outDir"
} finally {foreach($proc in $processes){if(!$proc.HasExited){Stop-Process -Id $proc.Id}}}
