param([ValidateSet(1,2,4)][int]$Players=1,[switch]$FullDuration,[int]$Port=17777,[switch]$DisconnectClient,[switch]$Access,[switch]$RenderedHost,[string]$CapturePhase='Build',[switch]$Ego,[ValidateSet('Off','A','B')][string]$Variant='Off',[switch]$PartyTrials,[switch]$PartyAccidents,[string]$Executable='',[switch]$Voice,[switch]$VoiceFiltered,[switch]$RealAudio,[switch]$PartyBodies,[switch]$Impaired,[switch]$HumanCustomer,[string]$Twist="",[int]$ReturningRounds=0,[switch]$Cat,[int]$VisualPhase=0)
. "$PSScriptRoot/env.ps1"
if($PartyBodies){$Variant='B';if($Players -lt 2){throw 'PartyBodies requires 2/4 peers'}}
if($PartyAccidents){$PartyTrials=$true}
if ($PartyTrials) {
    if ($Players -lt 2 -or $DisconnectClient -or $Access -or $Ego) { throw 'PartyTrials requires 2/4 normal B peers without other smoke fixtures' }
    $Variant='B'
}
$packaged=[bool]$Executable
if($packaged -and !$PSBoundParameters.ContainsKey('Variant')){$Variant='B'}
if($packaged -and $Access){throw 'Packaged Access requires a separate package engine check; do not run workspace access checks'}
$engine = if($packaged){(Resolve-Path -LiteralPath $Executable).Path}else{Find-Godot}
if ($Access) { & "$PSScriptRoot/access_check.ps1" }
$suffix = if ($DisconnectClient) { '-disconnect' } elseif ($FullDuration) { '-full' } else { '' }
if ($Access) { $suffix += '-access' }
if ($Ego) { $suffix += '-ego' }
if ($PartyTrials) { $suffix += '-partytrials' }
if ($Variant -ne 'Off') { $suffix += '-v06-'+$Variant }
$runId = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
$prefixName=if($packaged){'package-smoke'}else{'smoke'}
$outDir = Join-Path $ProjectRoot "artifacts/$prefixName-$Players$suffix-$runId"
Write-Host "Smoke evidence: $outDir"
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
if($packaged){
    $packageDirectory=Split-Path $engine -Parent
    @{executable=$engine;sha256=(Get-FileHash -LiteralPath $engine -Algorithm SHA256).Hash;workspacePathPassed=$false;buildInfo=(Get-Content (Join-Path $packageDirectory 'BUILD_INFO.json') -Raw | ConvertFrom-Json);assemblySha256=(Get-FileHash -LiteralPath (Join-Path $packageDirectory 'data_ProjectHairball_windows_x86_64/ProjectHairball.dll') -Algorithm SHA256).Hash} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $outDir 'executable.json') -Encoding utf8
}
$runClock=[System.Diagnostics.Stopwatch]::StartNew()
$processes = @()
try {
    if($Impaired){$proxy=Start-Process -FilePath (Get-Command python).Source -ArgumentList @(('"'+$PSScriptRoot+'/net_proxy.py"'),'--listen',($Port+1),'--server',$Port,'--profile','cross-border','--duration','110','--report',('"'+$outDir+'/proxy.json"')) -WindowStyle Hidden -PassThru -RedirectStandardOutput "$outDir/proxy.log" -RedirectStandardError "$outDir/proxy.err";$processes+=$proxy;Start-Sleep -Milliseconds 500}
    for ($i=0; $i -lt $Players; $i++) {
        $prefix = Join-Path $outDir "peer-$i"
        $modeArgs = if ($Players -eq 1) { @('--solo') } elseif ($i -eq 0) { @('--host') } else { @('--join','127.0.0.1') }
        $smokeArg = if ($FullDuration) { '--full-smoke' } else { '--smoke' }
        $audioArgs=if($RealAudio){@('--audio-driver','WASAPI')}else{@()}
        $projectArgs=if($packaged){@()}else{@('--path',('"' + $ProjectRoot + '"'))}
        $launchArgs = @('--headless') + $audioArgs + $projectArgs + @('--') + $modeArgs + @($smokeArg,'--expected',$Players,'--port',$(if($Impaired -and $i -gt 0){$Port+1}else{$Port}),'--report',('"' + $prefix + '.json"'))
        if ($RenderedHost -and $i -eq 0) { $launchArgs=$launchArgs[1..($launchArgs.Length-1)]+@('--capture',('"'+$outDir+'/shared-'+$CapturePhase+'.png"'),'--capture-phase',$CapturePhase,'--capture-delay','3.1','--language','zh') }
        if($VisualPhase -gt 0 -and $i -eq 0){$launchArgs+=@('--visual-phase',$VisualPhase,'--hair-profile','Human')}
        if($HumanCustomer){$launchArgs+=@('--human-customer-smoke','--qa-customer','2')}else{$launchArgs+='--force-ai-customer'}
        if($Cat){$launchArgs+="--qa-cat"}
        if($ReturningRounds){$launchArgs+=@("--qa-returning",$ReturningRounds)}
        if($Twist){$launchArgs+=@("--qa-twist",$Twist)}
        if ($Access) { $launchArgs += '--access-smoke' }
        if ($Ego) { $launchArgs += '--ego-smoke' }
        if ($RealAudio) { $launchArgs += '--voice-test-mute' }
        if ($Voice) { $launchArgs += '--voice-inject' }
        if ($VoiceFiltered) { $launchArgs += @('--voice-filter-test','2') }
        if ($PartyTrials) { $launchArgs += '--b-party-smoke' }
        if ($PartyBodies) { $launchArgs += '--party-body-smoke' }
        if ($PartyAccidents) { $launchArgs += '--party-accident-smoke' }
        if ($Variant -ne 'Off') { $launchArgs += '--v06-variant-'+$Variant.ToLowerInvariant() }
        if ($DisconnectClient -and $i -eq 0) { $launchArgs += @('--expected-final',($Players-1)) }
        if ($DisconnectClient -and $i -eq ($Players-1)) { $launchArgs += @('--quit-after-seconds',12) }
        $launchDirectory=if($packaged){$packageDirectory}else{$ProjectRoot}
        $proc = Start-Process -FilePath $engine -WorkingDirectory $launchDirectory -ArgumentList $launchArgs -WindowStyle Hidden -PassThru -RedirectStandardOutput "$prefix.log" -RedirectStandardError "$prefix.err"
        $processes += $proc
        if ($i -eq 0 -and $Players -gt 1) { Start-Sleep -Seconds 2 }
    }
    while (@($processes | Where-Object { -not $_.HasExited -and (!$Impaired -or $_.Id -ne $proxy.Id) }).Count -gt 0) {
        if(@($processes | Where-Object { $_.HasExited -and $_.ExitCode -ne 0 }).Count){throw "A smoke peer failed; see $outDir"}
        if ($runClock.Elapsed.TotalSeconds -gt 410) { throw 'Smoke timed out' }
        Start-Sleep -Milliseconds 500
    }
    for ($i=0; $i -lt $Players; $i++) {
        $prefix = Join-Path $outDir "peer-$i"
        Get-Content "$prefix.log"
        if ($processes[$(if($Impaired){$i+1}else{$i})].ExitCode -ne 0) { throw "Peer $i exited with $($processes[$(if($Impaired){$i+1}else{$i})].ExitCode)" }
        if ($DisconnectClient -and $i -eq ($Players-1)) { continue }
        if (-not (Test-Path -LiteralPath "$prefix.json")) { throw "Peer $i did not write a report" }
        $report = Get-Content "$prefix.json" -Raw | ConvertFrom-Json
        if($Twist -and $report.secrets.twist -ne $Twist){throw "Requested twist was not active"}
        if($Cat -and $report.secrets.catActions -lt 1){throw "Cat did not act"}
        if($Twist -eq "Family" -and $report.secrets.familyEdited){throw "Family edited hair"}
        if (-not $report.success) { throw "Peer $i failed: $($report.reason)" }
        if($Voice){$voiceReport=Get-Content "$prefix.voice.json" -Raw | ConvertFrom-Json;if($voiceReport.sentPackets -lt 20){throw 'Voice source did not send'};if($VoiceFiltered -and $i -eq 1 -and $voiceReport.opusPackets -ne 0){throw 'Filtered sender encoded Opus'};foreach($stream in $voiceReport.streams){if($stream.decoded -lt 20){throw 'Voice recipient did not decode'}}}
        if($Voice -and $RealAudio){foreach($stream in $voiceReport.streams){$samples=$stream.decoded*$(if($stream.opus -gt 0){960}else{1920});if($stream.playbackDiscarded/[double]$samples -gt .01 -or $stream.mixedSeconds/$stream.speakerSeconds -lt .95){throw "Voice playback consumption failure: peer $i actor $($stream.actor)"}}}
        if ($report.variant -ne $Variant) { throw "Peer $i variant mismatch" }
        if ($Variant -eq 'B' -and -not $report.liveResolved) { throw "Peer $i did not receive live resolution" }
        if ($PartyTrials) {
            $party=$report.party
            if(!$party.enabled -or !$party.qaFixture -or $party.playerBuilt){throw 'Missing QA boundary'}
            if(!$party.success -or $party.placements -lt 2 -or $party.pickups -lt 2 -or $party.slips -lt 1 -or $party.leave -ne 'Done' -or $party.bellActor -le 1){throw 'Missing remote retry/bell/exit facts'}
            foreach($fact in @('slipSeen','workPreserved','supplyPreserved')){if(!$party.$fact){throw "Missing party fact $fact"}}
            if($i -eq 0 -and ($party.shapeInjections -ne 1 -or $party.glueInjections -ne 1 -or $party.inputPackets -le 0)){throw 'Missing ordinary remote input evidence'}
        } elseif ($HumanCustomer) {
            if($report.secrets.targetBuildLeak -or $report.secrets.buildLeak -or !$report.secrets.targetRevealed){throw 'Human-customer privacy failure'}
            $needsTarget=if($Twist -in @('Reverse','SplitInfo')){$report.secrets.actor -ne $report.secrets.customer}else{$report.secrets.actor -eq $report.secrets.customer -or ($Twist -eq 'Family' -and $report.secrets.family -eq $report.secrets.actor)}
            if($ReturningRounds -and (!$report.secrets.returning -or $report.secrets.history -ne $ReturningRounds -or $report.secrets.round -ne $ReturningRounds)){throw "Returning session incomplete"}
            if($i -gt 0 -and $needsTarget -and $report.secrets.targetReceived -notcontains $report.secrets.actor){throw 'Expected private target missing'}
            if(!$needsTarget -and $report.secrets.targetReceived.Count -gt 0){throw 'Target sent to an ineligible role'}
        } elseif ($Variant -eq 'B') {
            foreach($fact in @('leave:ToMirror','leave:Mirror','leave:ToDoor','attention:Notice','attention:Commit','attention:React')) {
                if($report.experimentFacts -notcontains $fact){throw "Peer $i missed authoritative live fact: $fact"}
            }
            if($Players -gt 1 -and $report.experimentFacts -notcontains 'brace'){throw "Peer $i missed remote brace"}
        }
        $errors = Get-Content "$prefix.err" -Raw
        if ($errors -match 'ERROR:|Exception|SMOKE_FAIL') { throw "Peer $i engine error: $errors" }
        if ((Get-Content "$prefix.log" -Raw) -match 'ERROR:|Exception|SMOKE_FAIL') { throw "Peer $i logged an engine failure" }
    }
    if ($Players -gt 1) {
        $hostLog = Get-Content (Join-Path $outDir 'peer-0.log') -Raw
        if ($PartyTrials -and $hostLog -notmatch ('SMOKE_ALL_CLIENTS_ACKNOWLEDGED '+($Players-1))) { throw 'Call-trial host did not drain all completion acknowledgements' }
        $hashes = [regex]::Matches($hostLog,'HOST_FINAL_HASH ([A-F0-9]+)') | ForEach-Object { $_.Groups[1].Value }
        for ($i=1; $i -lt $Players; $i++) {
            if ($DisconnectClient -and $i -eq ($Players-1)) { continue }
            $clientLog = Get-Content (Join-Path $outDir "peer-$i.log") -Raw
            if ($PartyTrials -and $clientLog -notmatch 'SMOKE_COMPLETION_ACKNOWLEDGED') { throw "Peer $i did not receive completion release" }
            $hash = [regex]::Match($clientLog,'CLIENT_FINAL[^\r\n]*hash=([A-F0-9]+)').Groups[1].Value
            if (-not $hash -or $hashes -notcontains $hash) { throw "Peer $i final facts do not match a host snapshot" }
        }
        if ($Ego) {
            $facts=[regex]::Matches($hostLog,'EGO_NETWORK_RESULT job=\d+ hash=[A-F0-9]+') | ForEach-Object { $_.Value }
            if ($facts.Count -ne 3) { throw 'Missing host ego result checks' }
            for ($i=1; $i -lt $Players; $i++) {
                if ($DisconnectClient -and $i -eq ($Players-1)) { continue }
                $clientLog=Get-Content (Join-Path $outDir "peer-$i.log") -Raw
                foreach ($fact in $facts) { if (-not $clientLog.Contains($fact)) { throw "Peer $i spotlight/order result mismatch: $fact" } }
            }
        }
        if ($DisconnectClient -and $hostLog -notmatch 'PEER_LEFT') { throw 'Host never processed client disconnect' }
    }
    Write-Host "SMOKE_SUITE_OK players=$Players fullDuration=$FullDuration partyTrials=$PartyTrials"
    Write-Host "SMOKE_WALL_SECONDS $($runClock.Elapsed.TotalSeconds.ToString('0.000',[Globalization.CultureInfo]::InvariantCulture)) packaged=$packaged"
} finally {
    foreach ($proc in $processes) { if (-not $proc.HasExited) { & taskkill /PID $proc.Id /T /F | Out-Null } }
}


