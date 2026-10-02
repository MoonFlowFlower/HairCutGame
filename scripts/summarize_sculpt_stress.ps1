param([Parameter(Mandatory=$true)][string]$Directory)
$evidence = (Resolve-Path -LiteralPath $Directory).Path
$pattern = 'SCULPT_STRESS_SAMPLE time=(\d+) piles=(\d+) flights=(\d+) fragments=(\d+) mass=([0-9.]+) physics_ms=([0-9.]+) memory_mb=(\d+) nodes=(\d+) visual=(\d+)(?: impacts=(\d+) crises=(\d+))?'
$peers = @()
function Mean($rows, $key) {
    if (@($rows).Count -eq 0) { return $null }
    return [Math]::Round(($rows | Measure-Object -Property $key -Average).Average, 2)
}
for ($peer = 0; $peer -lt 4; $peer++) {
    $report = Get-Content -LiteralPath (Join-Path $evidence "peer-$peer.json") -Raw | ConvertFrom-Json
    if (-not $report.success) { throw "Peer $peer did not complete successfully" }
    $samples = @(Get-Content -LiteralPath (Join-Path $evidence "peer-$peer.log") | ForEach-Object {
        if ($_ -match $pattern) {
            [pscustomobject]@{ time=[int]$Matches[1]; piles=[int]$Matches[2]; flights=[int]$Matches[3]; fragments=[int]$Matches[4]; mass=[double]::Parse($Matches[5],[Globalization.CultureInfo]::InvariantCulture); physics_ms=[double]::Parse($Matches[6],[Globalization.CultureInfo]::InvariantCulture); memory_mb=[int]$Matches[7]; nodes=[int]$Matches[8]; visual=[int]$Matches[9]; impacts=[int]$Matches[10]; crises=[int]$Matches[11] }
        }
    })
    if ($samples.Count -eq 0) { throw "Peer $peer has no samples" }
    foreach ($sample in $samples) {
        if ($sample.piles -gt 2048 -or $sample.flights -gt 64 -or $sample.fragments -gt 12 -or $sample.visual -gt 16640 -or $sample.impacts -gt 2048 -or $sample.crises -gt 128) { throw "Peer $peer exceeded a debris budget at $($sample.time)s" }
    }
    $warm = @($samples | Where-Object {$_.time -ge 120 -and $_.time -le 240})
    $late = @($samples | Where-Object {$_.time -ge $samples[-1].time-120})
    $peers += [ordered]@{
        peer=$peer; elapsed=$report.elapsed; samples=$samples.Count; last=$samples[-1]
        max_piles=($samples | Measure-Object piles -Maximum).Maximum
        max_flights=($samples | Measure-Object flights -Maximum).Maximum
        max_fragments=($samples | Measure-Object fragments -Maximum).Maximum
        max_visual=($samples | Measure-Object visual -Maximum).Maximum
        max_impacts=($samples | Measure-Object impacts -Maximum).Maximum
        max_crises=($samples | Measure-Object crises -Maximum).Maximum
        max_nodes=($samples | Measure-Object nodes -Maximum).Maximum
        min_memory_mb=($samples | Measure-Object memory_mb -Minimum).Minimum
        max_memory_mb=($samples | Measure-Object memory_mb -Maximum).Maximum
        warm_memory_mb=(Mean $warm 'memory_mb'); late_memory_mb=(Mean $late 'memory_mb')
        warm_physics_ms=(Mean $warm 'physics_ms'); late_physics_ms=(Mean $late 'physics_ms')
    }
}
$summary = [ordered]@{ directory=$evidence; sampled_budgets_passed=$true; note='Physics values are means over approximately 10-second windows, not full render frame times. Warm window=120-240s; late=final 120s. Concurrent local workloads may affect timing.'; peers=$peers }
$summary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $evidence 'metrics.json')
$summary | ConvertTo-Json -Depth 6
