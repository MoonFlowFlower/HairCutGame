param([Parameter(Mandatory)][string]$RunDirectory)
$runPath=(Resolve-Path -LiteralPath $RunDirectory).Path
$owned=@(Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'ProjectHairball-B.exe' -and $_.CommandLine.Contains($runPath) })
if($owned.Count -ne 4){throw "Expected 4 endurance peers, found $($owned.Count)"}
$rows=Join-Path $runPath 'resources.csv'
$deadline=(Get-Date).AddMinutes(35)
while((Get-Date) -lt $deadline){
    $alive=0
    foreach($entry in $owned){
        $proc=Get-Process -Id $entry.ProcessId -ErrorAction SilentlyContinue
        if(!$proc -or $proc.ProcessName -ne 'ProjectHairball-B'){continue}
        $alive++
        [pscustomobject]@{utc=[DateTime]::UtcNow.ToString('O');pid=$proc.Id;workingSet=$proc.WorkingSet64;privateBytes=$proc.PrivateMemorySize64;handles=$proc.HandleCount;cpuSeconds=$proc.TotalProcessorTime.TotalSeconds} | Export-Csv -LiteralPath $rows -Append -NoTypeInformation
    }
    if(!$alive){break}
    Start-Sleep -Seconds 5
}
Write-Output "RESOURCE_WATCH_DONE $rows"
