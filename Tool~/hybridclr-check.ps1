<#
.SYNOPSIS
  HybridCLR 兼容性静态检查。把 10 轮分析的隐患固化为可重复检查:
    1. 热更侧危险 API 扫描(反射/动态加载/平台 IO/unsafe)
    2. csproj AssemblyName 与 asmdef 名一致性(防热更按名解析失败, r7)
    3. 热更侧零 XPath 引擎依赖(库用 Descendants 遍历, 无需 link.xml)
  Run: pwsh -File Tool~/hybridclr-check.ps1
#>
param(
  # 目标扫描的程序集源码目录(csv),默认 HtmlTransformer(布局 A: HAP 留 AOT、HT 进热更)
  [string]$HotUpdateDirs = "Runtime/HtmlTransformer"
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Set-Location $repoRoot
$fail = 0

function Check([string]$name, [scriptblock]$body) {
  Write-Host ("[check] " + $name)
  try { & $body } catch { Write-Host ("  FAIL: " + $_.Exception.Message); $script:fail++ }
}

# ---- 1. 热更侧危险 API 扫描 ----
Check "hot-update assembly: dangerous API scan" {
  $dirs = $HotUpdateDirs -split "," | ForEach-Object { $_.Trim() } | Where-Object { $_ }
  $riskyPatterns = @(
    "Activator\.CreateInstance",   # 反射实例化
    "MakeGenericType",               # 泛型反射(AOT 补全风险, r3)
    "Assembly\.Load",               # 动态加载
    "AppDomain",                     # 动态加载/卸载域
    "Reflection\.Emit",             # 动态代码生成
    "DllImport",                     # P/Invoke
    "\bunsafe\b"                   # unsafe 代码
  )
  $hits = @()
  foreach ($d in $dirs) {
    $full = Join-Path $repoRoot $d
    if (-not (Test-Path $full)) { throw ("dir not found: " + $d) }
    Get-ChildItem $full -Recurse -Filter "*.cs" | ForEach-Object {
      $f = $_.FullName
      foreach ($p in $riskyPatterns) {
        Select-String -Path $f -Pattern $p -ErrorAction SilentlyContinue | ForEach-Object {
          $hits += ($_.Path.Substring($repoRoot.Length + 1) + ":" + $_.LineNumber + "  " + $_.Line.Trim())
        }
      }
    }
  }
  if ($hits.Count -gt 0) {
    Write-Host ("  FAIL: " + $hits.Count + " dangerous API usage(s) in hot-update code:")
    $hits | Select-Object -First 20 | ForEach-Object { Write-Host ("    " + $_) }
    $script:fail++
  } else {
    Write-Host "  PASS: no dangerous API in hot-update code"
  }
}

# ---- 2. AssemblyName 一致性(asmdef vs csproj, r7) ----
Check "assembly name: asmdef vs csproj AssemblyName" {
  $errors = @()
  Get-ChildItem $repoRoot -Recurse -Filter "*.asmdef" | ForEach-Object {
    $asmPath = $_.FullName
    $asmName = (Get-Content $asmPath -Raw | ConvertFrom-Json).name
    $csproj = Join-Path (Split-Path $asmPath) ($_.BaseName + ".csproj")
    if (-not (Test-Path $csproj)) { return }  # asmdef 无对应 csproj(如 HAP 在上游),跳过
    $xml = [xml](Get-Content $csproj -Raw)
    $an = $xml.Project.PropertyGroup.AssemblyName
    if (-not $an) { $errors += ($asmPath.Substring($repoRoot.Length+1) + ": csproj 缺 AssemblyName(默认=文件名 " + $_.BaseName + ", 与 asmdef 名 " + $asmName + " 可能不符)") }
    elseif ($an -ne $asmName) { $errors += ($asmPath.Substring($repoRoot.Length+1) + ": AssemblyName=" + $an + " != asmdef=" + $asmName) }
  }
  if ($errors.Count -gt 0) {
    Write-Host ("  FAIL: " + $errors.Count + " assembly name mismatch(es):")
    $errors | ForEach-Object { Write-Host ("    " + $_) }
    $script:fail++
  } else {
    Write-Host "  PASS: all asmdef/csproj assembly names aligned"
  }
}

# ---- 3. 热更侧零 XPath 引擎依赖(方案 A: 库用 Descendants 遍历, 不依赖 System.Xml.XPath) ----
Check "hot-update assembly: zero XPath-selector dependency" {
  $pats = @(
    "SelectNodes",
    "SelectSingleNode",
    "System\.Xml\.XPath"
  )
  $hits = @()
  foreach ($d in ($HotUpdateDirs -split "," | ForEach-Object { $_.Trim() } | Where-Object { $_ })) {
    $full = Join-Path $repoRoot $d
    Get-ChildItem $full -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue | ForEach-Object {
      foreach ($p in $pats) {
        Select-String -Path $_.FullName -Pattern $p -ErrorAction SilentlyContinue | ForEach-Object {
          $hits += ($_.Path.Substring($repoRoot.Length + 1) + ":" + $_.LineNumber + "  " + $_.Line.Trim())
        }
      }
    }
  }
  if ($hits.Count -gt 0) {
    Write-Host ("  FAIL: " + $hits.Count + " XPath-selector usage(s) in hot-update code (use Descendants instead):")
    $hits | Select-Object -First 10 | ForEach-Object { Write-Host ("    " + $_) }
    $script:fail++
  } else {
    Write-Host "  PASS: no XPath-selector dependency in hot-update code"
  }
}

Write-Host ""
if ($fail -eq 0) { Write-Host "HybridCLR check: ALL PASS"; exit 0 }
else { Write-Host ("HybridCLR check: " + $fail + " FAILED"); exit 1 }
