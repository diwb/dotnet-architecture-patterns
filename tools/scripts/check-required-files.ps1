$required = @("README.md","LICENSE","CHANGELOG.md","CONTRIBUTING.md","CODE_OF_CONDUCT.md","SECURITY.md","docs/FINAL_AUDIT.md","docker-compose.yml")
$missing = $required | Where-Object { -not (Test-Path $_) }
if ($missing) { Write-Error ("Missing required files: " + ($missing -join ", ")); exit 1 }
Write-Host "Required files present."
