. "$PSScriptRoot/env.ps1"
$engine=Find-Godot
foreach($language in @('zh','en')) { foreach($scene in @('contact-soft','failure-soft')) {
    $prefix=Join-Path $ProjectRoot "artifacts/landing-reasons-$scene-$language"
    $phase=if($scene -eq 'failure-soft'){'Results'}else{'Build'}
    $launch=@('--path',('"'+$ProjectRoot+'"'),'--','--solo','--v06-variant-b','--v06-review',$scene,'--capture',('"'+$prefix+'.png"'),'--capture-phase',$phase,'--capture-delay','0.35','--language',$language,'--quit-after-seconds','1.5')
    $proc=Start-Process -FilePath $engine -ArgumentList $launch -WindowStyle Hidden -PassThru -RedirectStandardOutput "$prefix.log" -RedirectStandardError "$prefix.err"
    try {
        if(!$proc.WaitForExit(20000)){throw "Capture timed out: $scene/$language"}
        if($proc.ExitCode -ne 0 -or (Get-Content "$prefix.err" -Raw) -match 'ERROR:|Exception'){throw "Capture failed: $scene/$language"}
        if(!(Test-Path "$prefix.png")){throw "Missing image: $scene/$language"}
    } finally {if(!$proc.HasExited){Stop-Process -Id $proc.Id}}
    Write-Host "LANDING_FEEDBACK_CAPTURE $scene/$language"
} }
