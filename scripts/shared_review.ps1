. "$PSScriptRoot/env.ps1"
foreach ($sample in @(@('landing','Validation','4'),@('failure','Validation','6'),@('llama','Build','0.35'),@('vote','Choice','1'))) {
    $name=$sample[0]
    & (Find-Godot) --path $ProjectRoot -- --solo --shared-review $name --language zh --capture (Join-Path $ProjectRoot "artifacts/shared-$name.png") --capture-phase $sample[1] --capture-delay $sample[2] --quit-after-seconds 9
    if ($LASTEXITCODE -ne 0) { throw "Shared review $name failed" }
}
