param([Parameter(Mandatory)][string]$Log,[string]$Output='')
$ErrorActionPreference='Stop'
if(!$Output){$Output=[IO.Path]::ChangeExtension($Log,'timeline.csv')}
Get-Content -LiteralPath $Log | ForEach-Object {
  $e=$_|ConvertFrom-Json
  [pscustomobject]@{utc=$e.Utc;time_s=$e.Time;round=$e.Round;variant=$e.Variant;event=$e.Kind;player=$e.Actor;x=$e.Position.X;y=$e.Position.Y;z=$e.Position.Z;incident_id=$e.IncidentId;detail=$e.Detail}
} | Export-Csv -LiteralPath $Output -NoTypeInformation -Encoding utf8
Write-Host "Observer timeline: $Output"
