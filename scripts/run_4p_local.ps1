param([int]$Port=7777)
. "$PSScriptRoot/env.ps1"
$engine = Find-Godot
$items = @(@('--host'), @('--join','127.0.0.1'), @('--join','127.0.0.1'), @('--join','127.0.0.1'))
for ($i=0; $i -lt 4; $i++) {
    $position = "$(( $i % 2 ) * 800),$([math]::Floor($i / 2) * 510)"
    $launchArgs = @('--path', ('"' + $ProjectRoot + '"'), '--resolution','800x500','--position',$position,'--') + $items[$i] + @('--v06-variant-b','--port', $Port)
    Start-Process -FilePath $engine -ArgumentList $launchArgs -WindowStyle Hidden
    if ($i -eq 0) { Start-Sleep -Milliseconds 1200 }
}
