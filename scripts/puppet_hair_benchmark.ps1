param([string]$Output='')
$ErrorActionPreference='Stop'
if(!$Output){$Output=Join-Path (Split-Path $PSScriptRoot -Parent) ('artifacts/puppet-hair-v03/matrix-'+(Get-Date -Format 'yyyyMMdd-HHmmss-fff')+'-'+$PID)}
$Output=[IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Force -Path $Output | Out-Null
$rows=@()
# Sequential single-engine samples. This script never rebuilds or replaces assemblies.
foreach($layers in @(0,1,2,4)){
    foreach($load in @('hero','four','four-six','four-max')){
        $run=Join-Path $Output ("shell$layers-$load")
        & "$PSScriptRoot/puppet_hair_optimization.ps1" -Round 3 -Shells $layers -Shot "opt-perf-$load" -Output $run
        $data=Get-Content -Raw -LiteralPath (Join-Path $run 'metrics.json') | ConvertFrom-Json
        $s=$data.shots[0]
        if($s.gpuMs -le 0 -or $s.samples -lt 60){throw "Missing measured GPU sample: $run"}
        $rows+=[pscustomobject]@{Load=$load;Shells=$layers;ActualShellDraws=$s.optimization.allShellLayers;Hair=$s.optimization.activeHair;Debris=$s.optimization.debris;GpuMs=$s.gpuMs;FrameMs=$s.frameMs;Fps=$s.fps;P99Ms=$s.p99Ms;Samples=$s.samples;FiberTriangles=$s.optimization.allFiberTriangles;Path=$run}
        $rows | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Output 'matrix.json')
    }
}
$rows | Export-Csv -NoTypeInformation -LiteralPath (Join-Path $Output 'matrix.csv')
$rows | Format-Table Load,Shells,ActualShellDraws,Hair,Debris,GpuMs,Fps -AutoSize
