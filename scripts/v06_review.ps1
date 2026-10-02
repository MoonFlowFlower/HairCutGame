. "$PSScriptRoot/env.ps1"
$engine=Find-Godot
foreach($scene in @('table','tolerance','soft','walk','growth-spray')) {
    $prefix=Join-Path $ProjectRoot "artifacts/v06-$scene"
    $launch=@('--path',('"'+$ProjectRoot+'"'),'--','--solo','--v06-variant-b','--v06-review',$scene,'--capture',('"'+$prefix+'.png"'),'--capture-phase','Build','--capture-delay','0.35','--language','zh','--quit-after-seconds','1.3')
    $proc=Start-Process -FilePath $engine -ArgumentList $launch -WindowStyle Hidden -PassThru -RedirectStandardOutput "$prefix.log" -RedirectStandardError "$prefix.err"
    try {
        if(!$proc.WaitForExit(20000)){throw "Capture timed out: $scene"}
        if($proc.ExitCode -ne 0 -or (Get-Content "$prefix.err" -Raw) -match 'ERROR:|Exception'){throw "Capture failed: $scene"}
        if(!(Test-Path "$prefix.png")){throw "Missing image: $scene"}
    } finally {if(!$proc.HasExited){Stop-Process -Id $proc.Id}}
    Write-Host "V06_CAPTURE $scene"
}
