. "$PSScriptRoot/env.ps1"
& (Find-Godot) --path $ProjectRoot -- --solo --sculpt-stress --language zh --capture "$ProjectRoot/artifacts/sculpt-brush.png" --capture-phase Build --capture-delay 14 --quit-after-seconds 16
if ($LASTEXITCODE -ne 0) { throw 'Brush visual review failed' }
& (Find-Godot) --path $ProjectRoot -- --solo --sculpt-stress --sculpt-floor-view --language zh --capture "$ProjectRoot/artifacts/sculpt-floor.png" --capture-phase Build --capture-delay 26 --quit-after-seconds 28
if ($LASTEXITCODE -ne 0) { throw 'Debris visual review failed' }
