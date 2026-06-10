$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$outputs = Join-Path (Split-Path -Parent $root) "outputs"
$appOut = Join-Path $outputs "WindowsScreenTimeExe"
$portableDir = Join-Path $outputs "WindowsScreenTimePortable"
$installerDir = Join-Path $outputs "WindowsScreenTimeInstaller"
$packageDir = Join-Path $root "WindowsScreenTimeApp\package"

dotnet publish (Join-Path $root "WindowsScreenTimeApp\WindowsScreenTimeApp.csproj") `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:EnableCompressionInSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o $appOut

Remove-Item -Force -ErrorAction SilentlyContinue (Join-Path $appOut "WindowsScreenTime.pdb")
New-Item -ItemType Directory -Force -Path $portableDir, $installerDir, $packageDir | Out-Null

foreach ($name in @("WindowsScreenTime.exe", "app.ico", "app-icon.png", "README.md")) {
  Copy-Item -Force -LiteralPath (Join-Path $appOut $name) -Destination $portableDir
  Copy-Item -Force -LiteralPath (Join-Path $appOut $name) -Destination $packageDir
}

$zip = Join-Path $outputs "WindowsScreenTime-Portable.zip"
Remove-Item -Force -ErrorAction SilentlyContinue $zip
Compress-Archive -Path (Join-Path $portableDir "*") -DestinationPath $zip -Force

dotnet publish (Join-Path $root "WindowsScreenTimeSetupBuilder\WindowsScreenTimeSetupBuilder.csproj") `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:EnableCompressionInSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o $installerDir

Remove-Item -Force -ErrorAction SilentlyContinue (Join-Path $installerDir "WindowsScreenTimeSetup.pdb")
