. "$PSScriptRoot/env.ps1"
& (Find-Godot) --headless --path $ProjectRoot -- --walkthrough --report artifacts/walkthrough.json
if ($LASTEXITCODE -ne 0) { throw 'Normal shop walkthrough failed' }
