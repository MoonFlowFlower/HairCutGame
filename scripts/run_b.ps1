param([ValidateSet('Solo','Host','Join')][string]$Mode='Solo',[string]$Address='127.0.0.1',[int]$Port=7777,[string]$Log='',[switch]$LegacyArt)
& "$PSScriptRoot/run_ab.ps1" -Variant B -Mode $Mode -Address $Address -Port $Port -Log $Log -LegacyArt:$LegacyArt
