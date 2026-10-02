. "$PSScriptRoot/env.ps1"
& (Find-Godot) --path $ProjectRoot -- --hair-review --capture "$ProjectRoot/artifacts/hair-shell-states.png" --quit-after-seconds 5
if ($LASTEXITCODE -ne 0) { throw 'Hair shell visual review failed' }
