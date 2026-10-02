. "$PSScriptRoot/env.ps1"
dotnet run --project tests/Core/CoreTests.csproj
if ($LASTEXITCODE -ne 0) { throw 'Core tests failed' }
