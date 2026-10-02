param([Parameter(Mandatory)][string]$SourceBundle,[Parameter(Mandatory)][ValidatePattern('^[A-Za-z0-9_-]+$')][string]$BuildId)
. "$PSScriptRoot/env.ps1"
$source=(Resolve-Path -LiteralPath $SourceBundle).Path
$info=Get-Content "$source/BUILD_INFO.json" -Raw | ConvertFrom-Json
$name="ProjectHairball-B-Windows-$BuildId"
$root=Join-Path $ProjectRoot "artifacts/packages/$name"
$bundle=Join-Path $root 'ProjectHairball-B'
$archive="$root.zip"
if((Test-Path -LiteralPath $root) -or (Test-Path -LiteralPath $archive)){throw 'Build ID already exists'}
New-Item -ItemType Directory -Path $root | Out-Null
Copy-Item -LiteralPath $source -Destination $bundle -Recurse
$oldBuild=$info.build
$info.build=$BuildId
$info | Add-Member -NotePropertyName sourceBuild -NotePropertyValue $oldBuild
$info | Add-Member -NotePropertyName docsOnlyRepack -NotePropertyValue $true
$info.createdUtc=[DateTime]::UtcNow.ToString('O')
$info | ConvertTo-Json -Depth 6 | Set-Content "$bundle/BUILD_INFO.json" -Encoding utf8
$readme=Get-Content "$ProjectRoot/docs/packaging/READ_ME_B.txt" -Raw
"BUILD: $BuildId | B | Protocol $($info.networkProtocol)`r`n`r`n$readme" | Set-Content "$bundle/READ_ME_FIRST.txt" -Encoding utf8
foreach($file in @('B_THREE_ROUND_PROTOCOL.txt','B_THREE_ROUND_RECORD.csv')){Copy-Item -LiteralPath "$ProjectRoot/docs/packaging/$file" -Destination $bundle -Force}
# Verify every other file byte-for-byte, including the executable, PCK and entire bundled runtime.
$changed=@('BUILD_INFO.json','READ_ME_FIRST.txt','B_THREE_ROUND_PROTOCOL.txt','B_THREE_ROUND_RECORD.csv','SHA256SUMS.txt')
$identical=0
foreach($file in Get-ChildItem -LiteralPath $source -Recurse -File){
    $relative=[IO.Path]::GetRelativePath($source,$file.FullName)
    if($relative -in $changed){continue}
    if((Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath (Join-Path $bundle $relative)).Hash){throw "Runtime changed: $relative"}
    $identical++
}
Get-ChildItem -LiteralPath $bundle -Recurse -File | Where-Object Name -ne 'SHA256SUMS.txt' | ForEach-Object {
    $relative=[IO.Path]::GetRelativePath($bundle,$_.FullName).Replace('\','/')
    '{0}  {1}' -f (Get-FileHash -LiteralPath $_.FullName).Hash,$relative
} | Set-Content "$bundle/SHA256SUMS.txt" -Encoding utf8
Compress-Archive -LiteralPath $bundle -DestinationPath $archive -CompressionLevel Optimal
Get-FileHash -LiteralPath $archive | ConvertTo-Json | Set-Content "$archive.sha256.json"
@{source=$source;build=$BuildId;sourceBuild=$oldBuild;identicalRuntimeFiles=$identical;changed=$changed;executableSha256=(Get-FileHash "$bundle/ProjectHairball-B.exe").Hash;dllSha256=(Get-FileHash "$bundle/data_ProjectHairball_windows_x86_64/ProjectHairball.dll").Hash;pckSha256=(Get-FileHash "$bundle/ProjectHairball-B.pck").Hash} | ConvertTo-Json -Depth 4 | Set-Content "$archive.repack.json"
Write-Host "PACKAGE_DOCS_OK $archive identicalRuntimeFiles=$identical"
