#!/usr/bin/env bash
# HybridCLR 兼容性静态检查(bash 版),与 hybridclr-check.ps1 行为一致。
# 1. 热更侧危险 API 扫描 2. AssemblyName 一致性 3. link.xml 模板完整性
# 用法: bash Tool~/hybridclr-check.sh [热更目录csv, 默认 Runtime/HtmlTransformer]
set -euo pipefail
cd "$(dirname "$0")/.."

HOTUPDATE_DIRS="${1:-Runtime/HtmlTransformer}"
FAIL=0

echo "[check] hot-update assembly: dangerous API scan"
RISKY_PATTERNS="Activator\.CreateInstance|MakeGenericType|Assembly\.Load|AppDomain|Reflection\.Emit|DllImport|\bunsafe\b"
HITS=""
IFS="," read -ra DIRS <<< "$HOTUPDATE_DIRS"
for d in "${DIRS[@]}"; do
  d="$(echo "$d" | tr -d ' ')"
  [ -d "$d" ] || { echo "  FAIL: dir not found: $d"; FAIL=$((FAIL+1)); }
  while IFS= read -r f; do
    [ -z "$f" ] && continue
    hits=$(grep -nE "$RISKY_PATTERNS" "$f" 2>/dev/null || true)
    if [ -n "$hits" ]; then
      HITS="$HITS
$hits"
    fi
  done < <(find "$d" -name "*.cs" -type f 2>/dev/null)
done
if [ -n "$(printf "%s" "$HITS" | tr -d ' \n')" ]; then
  echo "  FAIL: dangerous API usage(s) in hot-update code:"
  printf "%s\n" "$HITS" | grep -v "^\$" | head -n 20 | sed "s/^/    /"
  FAIL=$((FAIL+1))
else
  echo "  PASS: no dangerous API in hot-update code"
fi

echo "[check] assembly name: asmdef vs csproj AssemblyName"
ASM_MISMATCH=""
while IFS= read -r asm; do
  [ -z "$asm" ] && continue
  asm_name=$(grep -o '"name"[[:space:]]*:[[:space:]]*"[^"]*"' "$asm" | head -n 1 | sed 's/.*"name"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/')
  csproj="${asm%.asmdef}.csproj"
  [ -f "$csproj" ] || continue
  an=$(grep -o '<AssemblyName>[^<]*</AssemblyName>' "$csproj" | head -n 1 | sed 's/<AssemblyName>//;s#</AssemblyName>##')
  if [ -z "$an" ]; then
    ASM_MISMATCH="$ASM_MISMATCH
$asm: csproj 缺 AssemblyName"
  elif [ "$an" != "$asm_name" ]; then
    ASM_MISMATCH="$ASM_MISMATCH
$asm: AssemblyName=$an != asmdef=$asm_name"
  fi
done < <(find . -name "*.asmdef" -not -path "*/.git/*" 2>/dev/null)
if [ -n "$(printf "%s" "$ASM_MISMATCH" | tr -d ' \n')" ]; then
  echo "  FAIL: assembly name mismatch(es):"
  printf "%s\n" "$ASM_MISMATCH" | grep -v "^\$" | sed "s/^/    /"
  FAIL=$((FAIL+1))
else
  echo "  PASS: all asmdef/csproj assembly names aligned"
fi

echo "[check] link.xml template: System.Private.Xml namespaces"
TPL="Tool~/link.xml.template"
[ -f "$TPL" ] || { echo "  FAIL: template missing: $TPL"; FAIL=$((FAIL+1)); }
for ns in "System.Xml" "System.Xml.XPath" "MS.Internal.Xml.XPath"; do
  grep -q "fullname=\"$ns\"" "$TPL" 2>/dev/null || { echo "  FAIL: template missing namespace: $ns"; FAIL=$((FAIL+1)); }
done
[ "$FAIL" -eq 0 ] && echo "  PASS: link.xml.template covers all System.Private.Xml namespaces"

echo ""
if [ "$FAIL" -eq 0 ]; then echo "HybridCLR check: ALL PASS"; exit 0; fi
echo "HybridCLR check: $FAIL FAILED"
exit 1
