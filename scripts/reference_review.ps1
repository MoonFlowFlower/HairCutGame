. "$PSScriptRoot/env.ps1"
foreach ($view in @('front','side','top','oblique')) {
    & (Find-Godot) --path $ProjectRoot -- --lab --language zh --reference-view $view --capture "$ProjectRoot/artifacts/reference-$view.png" --quit-after-seconds 4
    if ($LASTEXITCODE -ne 0) { throw "Reference $view capture failed" }
}
& (Find-Godot) --path $ProjectRoot -- --lab --language zh --reference-view top --reference-goal 7 --capture "$ProjectRoot/artifacts/reference-bowl-top.png" --quit-after-seconds 4
if ($LASTEXITCODE -ne 0) { throw 'Reference bowl capture failed' }
