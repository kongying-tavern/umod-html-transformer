#!/usr/bin/env bash
# 离线生成 Unity .meta 文件，与同目录 gen-meta.ps1 行为一致。
# 只为缺失的文件生成，已存在的 meta 保持不变。用法：在仓库任意位置执行本脚本。
set -euo pipefail
cd "$(dirname "$0")/.."

T_FOLDER=$'folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
T_CS=$'MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
T_ASM=$'AssemblyDefinitionImporter:\n  externalObjects: {}\n  mainObjectFileID: -1\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
T_DEF=$'DefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'

write_meta() {
  local target="$1" block="$2"
  if [ -e "$target.meta" ]; then return 1; fi
  local guid
  guid=$(od -An -N16 -tx1 /dev/urandom | tr -d ' \n')
  printf 'fileFormatVersion: 2\nguid: %s\n%s' "$guid" "$block" >"$target.meta"
  return 0
}

dirs=()
nf=0; ncs=0; nas=0; ndef=0

while IFS= read -r f; do
  dir=$(dirname "$f")
  while [ "$dir" != "." ]; do
    seen=0
    for d in "${dirs[@]}"; do [ "$d" = "$dir" ] && seen=1 && break; done
    [ "$seen" = 0 ] && dirs+=("$dir")
    dir=$(dirname "$dir")
  done
  leaf=$(basename "$f")
  case "$leaf" in
    .*)       continue ;;
    *.cs)     if write_meta "$f" "$T_CS";  then ncs=$((ncs+1)); fi ;;
    *.asmdef) if write_meta "$f" "$T_ASM"; then nas=$((nas+1)); fi ;;
    *)        if write_meta "$f" "$T_DEF"; then ndef=$((ndef+1)); fi ;;
  esac
done < <(git ls-files | grep -v '~/' | grep -v '\.meta$')

for d in "${dirs[@]}"; do
  if write_meta "$d" "$T_FOLDER"; then nf=$((nf+1)); fi
done

echo "folders=$nf cs=$ncs asmdef=$nas default=$ndef"
echo "total metas written: $((nf+ncs+nas+ndef))"
