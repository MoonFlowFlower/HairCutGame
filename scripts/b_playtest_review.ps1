param([string]$OutputDirectory='',[string[]]$Languages=@('zh','en'))
. "$PSScriptRoot/env.ps1"
$engine=Find-Godot
if(!$OutputDirectory){$OutputDirectory=Join-Path $ProjectRoot ('artifacts/b-playtest-review-'+(Get-Date -Format 'yyyyMMdd-HHmmss-fff'))}
$OutputDirectory=[System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
foreach($language in $Languages){
    foreach($scene in @('table','growth-spray','soft','tolerance','walk','results')){
        $prefix=Join-Path $OutputDirectory "$scene-$language"
        $delay=if($scene -eq 'growth-spray'){'1.55'}else{'0.35'}
        $quit=if($scene -eq 'growth-spray'){'2.1'}else{'1.3'}
        $launch=@('--path',('"'+$ProjectRoot+'"'),'--','--solo','--v06-variant-b','--v06-review',$scene,'--capture',('"'+$prefix+'.png"'),'--capture-phase',$(if($scene -eq 'results'){'Results'}else{'Build'}),'--capture-delay',$delay,'--language',$language,'--quit-after-seconds',$quit)
        $proc=Start-Process -FilePath $engine -ArgumentList $launch -WindowStyle Hidden -PassThru -RedirectStandardOutput "$prefix.log" -RedirectStandardError "$prefix.err"
        try{
            if(!$proc.WaitForExit(20000)){throw "Capture timed out: $scene / $language"}
            if($proc.ExitCode -ne 0 -or (Get-Content "$prefix.err" -Raw) -match 'ERROR:|Exception'){throw "Capture failed: $scene / $language"}
            if(!(Test-Path -LiteralPath "$prefix.png")){throw "Missing image: $scene / $language"}
        }finally{if(!$proc.HasExited){Stop-Process -Id $proc.Id}}
        Write-Host "B_PLAYTEST_CAPTURE $scene $language $prefix.png"
    }
}
# Table and growth-spray use the normal starting head; other scenarios are injected presentation fixtures.
Write-Host "B_PLAYTEST_REVIEW $OutputDirectory"
