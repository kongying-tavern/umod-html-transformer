$ErrorActionPreference = 'Stop'
$utf8 = New-Object System.Text.UTF8Encoding($false)
function New-Guid {
  $b = New-Object byte[] 16
  [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b)
  return (($b | ForEach-Object { $_.ToString('x2') }) -join '')
}
function Write-Meta([string]$targetPath, [string]$block) {
  $metaPath = $targetPath + '.meta'
  if (Test-Path $metaPath) { return $false }
  [System.IO.File]::WriteAllText($metaPath, ("fileFormatVersion: 2`nguid: " + (New-Guid) + "`n" + $block), $utf8)
  return $true
}
$T_FOLDER = "folderAsset: yes`nDefaultImporter:`n  externalObjects: {}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"
$T_CS     = "MonoImporter:`n  externalObjects: {}`n  serializedVersion: 2`n  defaultReferences: []`n  executionOrder: 0`n  icon: {instanceID: 0}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"
$T_ASM    = "AssemblyDefinitionImporter:`n  externalObjects: {}`n  mainObjectFileID: -1`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"
$T_DEF    = "DefaultImporter:`n  externalObjects: {}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"

$files = git ls-files | Where-Object { $_ -notmatch '~/' -and (Split-Path $_ -Leaf) -notlike '.*' -and (Split-Path $_ -Leaf) -notlike '*.meta' }
$nFolders = 0; $nCs = 0; $nAsm = 0; $nDef = 0
$dirs = @{}
foreach ($f in $files) {
  $full = Join-Path (Get-Location).Path $f
  $dir = Split-Path $f -Parent
  while ($dir) { $dirs[$dir] = $true; $p = Split-Path $dir -Parent; if (-not $p) { break } else { $dir = $p } }
  $leaf = Split-Path $f -Leaf
  if ($leaf -like '*.cs')         { if (Write-Meta $full $T_CS)  { $nCs++ } }
  elseif ($leaf -like '*.asmdef') { if (Write-Meta $full $T_ASM) { $nAsm++ } }
  else                            { if (Write-Meta $full $T_DEF) { $nDef++ } }
}
foreach ($d in $dirs.Keys) { if (Write-Meta (Join-Path (Get-Location).Path $d) $T_FOLDER) { $nFolders++ } }
Write-Host ('folders=' + $nFolders + ' cs=' + $nCs + ' asmdef=' + $nAsm + ' default=' + $nDef)
Write-Host ('total metas written: ' + ($nFolders + $nCs + $nAsm + $nDef))
