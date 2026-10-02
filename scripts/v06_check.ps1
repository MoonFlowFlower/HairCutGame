. "$PSScriptRoot/env.ps1"
& (Find-Godot) --headless --path $ProjectRoot -- --solo --v06-variant-b --v06-check
if($LASTEXITCODE -ne 0){throw 'v0.6 engine checks failed'}
