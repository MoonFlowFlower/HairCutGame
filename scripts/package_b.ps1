param([ValidatePattern('^[A-Za-z0-9_-]+$')][string]$BuildId=(Get-Date -Format 'yyyyMMdd-HHmmss'))
. "$PSScriptRoot/env.ps1"
$engine=Find-Godot
$name="ProjectHairball-B-Windows-$BuildId"
$packageRoot=Join-Path $ProjectRoot "artifacts/packages/$name"
$bundle=Join-Path $packageRoot 'ProjectHairball-B'
$archive="$packageRoot.zip"
if((Test-Path -LiteralPath $packageRoot) -or (Test-Path -LiteralPath $archive)){throw "Build ID already exists: $BuildId; use a new ID."}
New-Item -ItemType Directory -Path $bundle -Force | Out-Null
$exportLog=Join-Path $ProjectRoot "artifacts/package-b-export-$BuildId.log"
$savedCompilationMode=$env:UseSharedCompilation
try{
    # A persistent compiler server can inherit the console wrapper's output pipe.
    $env:UseSharedCompilation='false'
    & $engine --headless --path $ProjectRoot --export-release 'Windows B Playtest' "$bundle/ProjectHairball-B.exe" --quit *> $exportLog
}finally{$env:UseSharedCompilation=$savedCompilationMode}
if($LASTEXITCODE -ne 0 -or (Get-Content $exportLog -Raw) -match 'ERROR:|Export failed|Build failed'){throw "Export failed; see $exportLog"}
$runtime=Join-Path $bundle 'data_ProjectHairball_windows_x86_64'
foreach($file in @('ProjectHairball.dll','hostfxr.dll','coreclr.dll','ProjectHairball.runtimeconfig.json')) {
    if(!(Test-Path -LiteralPath (Join-Path $runtime $file))){throw "Missing bundled runtime: $file"}
}
$config=Get-Content "$runtime/ProjectHairball.runtimeconfig.json" -Raw | ConvertFrom-Json
if(!$config.runtimeOptions.includedFrameworks){throw 'Expected a self-contained runtime'}
$protocol=[int]([regex]::Match((Get-Content "$ProjectRoot/src/Bootstrap/NetworkRecovery.cs" -Raw),'NetProtocol=(\d+)').Groups[1].Value)
$readme=Get-Content "$ProjectRoot/docs/packaging/READ_ME_B.txt" -Raw
"BUILD: $BuildId | B | Protocol $protocol`r`n`r`n$readme" | Set-Content "$bundle/READ_ME_FIRST.txt" -Encoding utf8
Get-ChildItem "$ProjectRoot/docs/packaging" -File -Filter 'B_THREE_ROUND_*' | Where-Object Extension -in '.txt','.csv' | Copy-Item -Destination $bundle
Copy-Item -LiteralPath "$ProjectRoot/docs/packaging/Open_Test_Logs.cmd" -Destination $bundle
New-Item -ItemType Directory -Path "$bundle/licenses" | Out-Null
Get-ChildItem "$ProjectRoot/docs/packaging" -Filter '*LICENSE.txt' | Copy-Item -Destination "$bundle/licenses"
Get-ChildItem "$ProjectRoot/docs/packaging" -Filter '*THIRD-PARTY.txt' | Copy-Item -Destination "$bundle/licenses"
@{build=$BuildId;variant='B';platform='Windows x64';engine=(& $engine --version | Out-String).Trim();dotnet=$config.runtimeOptions.includedFrameworks;networkProtocol=$protocol;features=@('16 bilingual condition-filtered twist cards with private reverse/family/split information','shop cat with reversible mass and prop consequences','opt-in bounded glued dragging and AI alertness','three/four-round returning-head sessions and ordered target/photo review','rotating human customer with private target cards','authoritative standing QTE and player-driven departure','feature-only customer voice','result target and photo, pies and cosmetic feedback','five-second return cut','measurable styles and 30 bilingual target cards','in-game raw and species-filtered voice without recording','material contact jelly glue strings and ice chips','placement spotlight and visual sink','before-after group photos and factual hair portraits','hold G throw and automatic empty-hand catch','opt-in downed bodies and E slap revive','chair rotation at 45 degrees per second','two-handle scissors and hose sprayer','two-person large platform and ladder support','150-second construction','four independent bottles','wig patches and recycling','molds holes and center of mass','first combo discovery wall','bounded teammate accidents and rescues','private secret tasks and result reveal','base pay plus tips','local 24-photo gallery','explicit factual actions','accelerating growth spray','table commission placement and retry','spatial landing feedback','fresh repaired landing feedback','zero-growth contact feedback','positive tail supply display','30-second team pause','automatic reconnect','bounded chunk snapshots','connection diagnostics','customer walk to mirror and door','failure reasons','input prediction and reconciliation','buffered motion','render camera','network telemetry');createdUtc=[DateTime]::UtcNow.ToString('O')} | ConvertTo-Json -Depth 4 | Set-Content "$bundle/BUILD_INFO.json" -Encoding utf8
Get-ChildItem -LiteralPath $bundle -Recurse -File | ForEach-Object {
    $relative=[IO.Path]::GetRelativePath($bundle,$_.FullName).Replace('\','/')
    '{0}  {1}' -f (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash,$relative
} | Set-Content "$bundle/SHA256SUMS.txt" -Encoding utf8
Compress-Archive -LiteralPath $bundle -DestinationPath $archive -CompressionLevel Optimal
Get-FileHash -LiteralPath $archive -Algorithm SHA256 | ConvertTo-Json | Set-Content "$archive.sha256.json"
Write-Host "PACKAGE_B_OK $archive"

