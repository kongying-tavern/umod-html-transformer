<#
.SYNOPSIS
  Unity import smoke test.
  Two-phase: scaffold temp project with -createProject, then inject this repo as a package and import.
  Run: pwsh -File Tool~/smoke-test.ps1
#>
param(
  [string]$UnityPath,
  # 覆盖 package.json 的 "unity" 字段指定的目标主版本
  [string]$UnityVersion
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$tmpProject = Join-Path $env:TEMP ("upm-smoke-" + (Get-Date -Format "yyyyMMddHHmmss"))
$logScaffold = Join-Path $tmpProject "scaffold.log"
$logImport = Join-Path $tmpProject "import.log"

# Probe Unity
if (-not $UnityVersion) {
  $pkg = Get-Content (Join-Path $repoRoot "package.json") -Raw | ConvertFrom-Json
  $UnityVersion = $pkg.unity
  if (-not $UnityVersion) { Write-Error "package.json missing \"unity\" field"; exit 1 }
}
Write-Host ("Target Unity version: " + $UnityVersion)
if (-not $UnityPath) {
  $hubEditor = Join-Path $env:LOCALAPPDATA "Programs\Unity Hub\Editor"
  $pattern = "^" + [regex]::Escape($UnityVersion)
  $c = Get-ChildItem $hubEditor -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -match $pattern } | Sort-Object Name -Descending
  if (-not $c) { Write-Error ("Unity " + $UnityVersion + ".x not found; pass -UnityPath explicitly"); exit 1 }
  $UnityPath = Join-Path $c[0].FullName "Editor\Unity.exe"
}
if (-not (Test-Path $UnityPath)) { Write-Error ("Unity not found: " + $UnityPath); exit 1 }
Write-Host ("Unity: " + $UnityPath)

# Phase 1: scaffold a standard project (built-in modules only, no network)
Write-Host "Phase 1: scaffolding temp project..."
# Unity.exe is a GUI app: PowerShell & does not wait, use Start-Process -Wait
$p1 = Start-Process -FilePath $UnityPath -ArgumentList @("-batchmode","-nographics","-quit","-createProject",$tmpProject,"-logFile",$logScaffold) -PassThru -Wait
Wait-Process -Id $p1.Id -Timeout 600 -ErrorAction SilentlyContinue
# Judge by manifest.json existence (no $LASTEXITCODE for GUI apps)
if (-not (Test-Path (Join-Path $tmpProject "Packages\manifest.json"))) {
  Write-Error "Scaffold failed (no manifest.json produced). Log tail:"
  if (Test-Path $logScaffold) { Get-Content $logScaffold -Tail 15 | ForEach-Object { Write-Host $_ } }
  exit 1
}
Write-Host "Phase 1 done."

# Phase 2: strip dotnet build leftovers (bin/obj) from the repo
# Unity ignores .gitignore; it would treat bin/obj as package assets and generate metas for them.
# bin/obj are excluded by .gitignore, so deleting them is safe for the repo.
Get-ChildItem $repoRoot -Recurse -Directory -Force -ErrorAction SilentlyContinue |
  Where-Object { $_.Name -in @("bin","obj") -and $_.FullName -notmatch "\\[^\\]*~[\\/]" } |
  ForEach-Object { Remove-Item $_.FullName -Recurse -Force -ErrorAction SilentlyContinue }
Write-Host "cleaned bin/obj from repo"

# Phase 3: inject this repo as a local package
$manifestPath = Join-Path $tmpProject "Packages\manifest.json"
$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$escapedRoot = $repoRoot -replace "\\","/"
$manifest.dependencies | Add-Member -NotePropertyName "site.yuanshen.htmltransformer" -NotePropertyValue ("file:" + $escapedRoot) -Force
$manifest | ConvertTo-Json -Depth 5 | Set-Content $manifestPath -Encoding UTF8
Write-Host "manifest.json:"
Get-Content $manifestPath

# Phase 4: import and compile
Write-Host "Phase 4: importing package (may take several minutes)..."
$p2 = Start-Process -FilePath $UnityPath -ArgumentList @("-batchmode","-nographics","-quit","-projectPath",$tmpProject,"-logFile",$logImport) -PassThru -Wait
Wait-Process -Id $p2.Id -Timeout 600 -ErrorAction SilentlyContinue
Write-Host ("Unity import exit: " + $p2.ExitCode)

# Analyze log
if (-not (Test-Path $logImport)) { Write-Error "No import log generated"; exit 1 }
$csErrors = Select-String -Path $logImport -Pattern "error CS\d+" | ForEach-Object { $_.Line.Trim() }
$imported = Select-String -Path $logImport -Pattern "site\.yuanshen\.htmltransformer" -Quiet

Write-Host "=== Results ==="
Write-Host ("Package present in import session: " + $imported)
Write-Host ("Compile errors: " + $csErrors.Count)
if ($csErrors.Count -gt 0) {
  Write-Host "--- errors ---"
  $csErrors | ForEach-Object { Write-Host $_ }
} else {
  Write-Host "No compile errors."
}

$result = if ($csErrors.Count -eq 0) { "PASS" } else { "FAIL" }
if ($result -eq "PASS") {
  Write-Host "cleaning up..."
  Remove-Item $tmpProject -Recurse -Force -ErrorAction SilentlyContinue
} else {
  Write-Host ("FAIL: keeping temp project for inspection: " + $tmpProject)
}
Write-Host ("Smoke test: " + $result)
if ($result -eq "FAIL") { exit 1 }
