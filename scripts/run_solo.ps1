param([switch]$Lab,[switch]$Laundry,[switch]$Materials)
. "$PSScriptRoot/env.ps1"
if ($Materials) { & (Find-Godot) --path $ProjectRoot -- --material-lab } elseif ($Laundry) { & (Find-Godot) --path $ProjectRoot -- --laundry-lab } elseif ($Lab) { & (Find-Godot) --path $ProjectRoot -- --lab } else { & (Find-Godot) --path $ProjectRoot -- --solo --v06-variant-b }
