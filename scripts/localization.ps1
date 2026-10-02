. "$PSScriptRoot/env.ps1"
& (Find-Godot) --headless --path $ProjectRoot -- --localization-check
if ($LASTEXITCODE -ne 0) { throw 'Language switching checks failed' }
