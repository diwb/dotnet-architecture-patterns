$tracked = git ls-files
if (-not $tracked) {
  $tracked = Get-ChildItem -Recurse -File | Where-Object { $_.FullName -notmatch '\\(bin|obj|\.git|artifacts)\\' } | ForEach-Object { $_.FullName }
}
$pattern = '(password|secret|token|api-key)\s*[:=]\s*["''][^"'']+'
$matches = Select-String -Path $tracked -Pattern $pattern -CaseSensitive:$false -ErrorAction SilentlyContinue
if ($matches) { $matches | ForEach-Object { Write-Error $_ }; exit 1 }
Write-Host 'No hard-coded secrets detected.'
