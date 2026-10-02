param([string]$Address='127.0.0.1',[int]$Port=7777)
. "$PSScriptRoot/env.ps1"
& (Find-Godot) --path $ProjectRoot -- --v06-variant-b --join $Address --port $Port
