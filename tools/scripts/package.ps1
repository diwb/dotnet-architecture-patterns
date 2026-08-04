New-Item -ItemType Directory -Force artifacts | Out-Null
dotnet publish src/ArchitecturePatterns.Cli/ArchitecturePatterns.Cli.csproj -c Release -o artifacts/archtool
Compress-Archive -Path artifacts/archtool/* -DestinationPath artifacts/archtool.zip -Force
"Release notes for dotnet-architecture-patterns v1.0.0" | Set-Content artifacts/RELEASE_NOTES.md
Get-FileHash artifacts/archtool.zip -Algorithm SHA256 | ForEach-Object { "$($_.Hash)  archtool.zip" } | Set-Content artifacts/SHA256SUMS
