param([int]$Port=7777)
. "$PSScriptRoot/env.ps1"
& (Find-Godot) --path $ProjectRoot -- --v06-variant-b --host --port $Port
