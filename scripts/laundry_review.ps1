. "$PSScriptRoot/env.ps1"
foreach ($action in @('glue','growth','wind')) {
    & (Find-Godot) --path $ProjectRoot -- --laundry-lab --language zh --laundry-action $action --capture (Join-Path $ProjectRoot "artifacts/laundry-$action.png") --quit-after-seconds 4.5
    if ($LASTEXITCODE -ne 0) { throw "Laundry $action capture failed" }
}
