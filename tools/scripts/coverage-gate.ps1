$coverageFiles = Get-ChildItem -Recurse -Path TestResults -Filter coverage.cobertura.xml
if (-not $coverageFiles) {
  Write-Error "No Cobertura coverage files found under TestResults."
  exit 1
}

$excludedPackages = @("ArchitecturePatterns.Testing")
$packages = @{}
foreach ($file in $coverageFiles) {
  [xml]$xml = Get-Content -Raw -LiteralPath $file.FullName
  foreach ($package in $xml.coverage.packages.package) {
    $name = [string]$package.name
    if ($excludedPackages -contains $name) { continue }
    if (-not $packages.ContainsKey($name)) { $packages[$name] = @{} }
    foreach ($class in $package.classes.class) {
      foreach ($line in $class.lines.line) {
        $sourceFile = Split-Path ([string]$class.filename) -Leaf
        $key = "${sourceFile}:$($line.number)"
        $hits = [int]$line.hits
        if (-not $packages[$name].ContainsKey($key)) { $packages[$name][$key] = 0 }
        if ($hits -gt $packages[$name][$key]) { $packages[$name][$key] = $hits }
      }
    }
  }
}

$thresholds = @{
  "ArchitecturePatterns.SharedKernel" = 85
  "archtool" = 70
  "CleanArchitecture.Application" = 75
  "Hexagonal.Application" = 75
}
$totalCovered = 0
$totalLines = 0
$failed = $false
foreach ($name in ($packages.Keys | Sort-Object)) {
  $lines = $packages[$name]
  $lineCount = $lines.Count
  $covered = ($lines.Values | Where-Object { $_ -gt 0 }).Count
  $percent = if ($lineCount -eq 0) { 100 } else { [math]::Round(($covered * 100.0) / $lineCount, 2) }
  $totalCovered += $covered
  $totalLines += $lineCount
  Write-Host "${name}: $percent% ($covered/$lineCount lines)"
  if ($thresholds.ContainsKey($name) -and $percent -lt $thresholds[$name]) {
    Write-Error "$name coverage $percent% is below threshold $($thresholds[$name])%."
    $failed = $true
  }
}

$totalPercent = if ($totalLines -eq 0) { 0 } else { [math]::Round(($totalCovered * 100.0) / $totalLines, 2) }
Write-Host "Total: $totalPercent% ($totalCovered/$totalLines lines)"
if ($totalPercent -lt 80) {
  Write-Error "Total coverage $totalPercent% is below threshold 80%."
  $failed = $true
}

if ($failed) { exit 1 }
Write-Host "Coverage gate passed."

