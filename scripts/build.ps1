. "$PSScriptRoot/env.ps1"
$godot = Find-Godot
$packages = Join-Path (Split-Path $godot -Parent) 'GodotSharp/Tools/nupkgs'
if (Test-Path -LiteralPath $packages) { dotnet nuget update source GodotLocal --source $packages --configfile NuGet.Config }
dotnet build ProjectHairball.csproj --nologo
if ($LASTEXITCODE -ne 0) { throw 'C# build failed' }
& $godot --headless --path $ProjectRoot --editor --import --quit
if ($LASTEXITCODE -ne 0) { throw 'Godot import failed' }
