param(
  [Parameter(Mandatory = $true)]
  [string]$Version,
  [string]$Runtime = "win-x64",
  [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$publishDir = Join-Path $root "publish"
$releasesDir = Join-Path $root "Releases"

if (Test-Path $publishDir) {
  Remove-Item -Recurse -Force $publishDir
}
if (Test-Path $releasesDir) {
  Remove-Item -Recurse -Force $releasesDir
}

Set-Location $root

dotnet publish "Membrain/Membrain.csproj" -c $Configuration -r $Runtime -p:SelfContained=true -p:PublishSingleFile=false -p:Version=$Version -o $publishDir

vpk pack --packId Membrain --packVersion $Version --packDir $publishDir --mainExe Membrain.exe --packTitle "Membrain" --icon "Membrain/assets/app.ico" --outputDir $releasesDir

Write-Host "Done. Check the Releases folder in: $root"
