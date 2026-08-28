#!/usr/bin/env bash
# Unity 导入冒烟测试(bash 版),与同目录 smoke-test.ps1 行为一致。
# 四阶段:建临时工程 → 清理 bin/obj → 注入本仓库为本地包 → 批处理导入编译。
# 用法: UNITY_PATH=... bash Tool~/smoke-test.sh (不传则自动探测 Unity 2021.3.x)
set -euo pipefail
cd "$(dirname "$0")/.."

# ---- 临时工程路径 ----
TMP_BASE="${TMPDIR:-${TEMP:-/tmp}}"
TMP_PROJECT="$TMP_BASE/upm-smoke-$(date +%Y%m%d%H%M%S)"
LOG_SCAFFOLD="$TMP_PROJECT/scaffold.log"
LOG_IMPORT="$TMP_PROJECT/import.log"

# ---- 探测 Unity ----
# 目标 Unity 主版本单一来源:package.json 的 "unity" 字段,可用 UNITY_VERSION 覆盖
if [ -z "${UNITY_VERSION:-}" ]; then
  # 从 package.json 读 "unity" 字段(单一来源)
  while IFS= read -r line; do
    if [[ $line =~ "unity"[[:space:]]*:[[:space:]]*"([^"]+)" ]]; then
      UNITY_VERSION="${BASH_REMATCH[1]}"
      break
    fi
  done < package.json
  if [ -z "$UNITY_VERSION" ]; then echo 'package.json missing "unity" field' >&2; exit 1; fi
fi
UNITY="${UNITY_PATH:-}"
if [ -z "$UNITY" ]; then
  HUB="${LOCALAPPDATA:-$HOME/AppData/Local}/Programs/Unity Hub/Editor"
  # 取 Hub 安装目录里匹配该主版本前缀的最新版
  for ent in "$HUB"/"$UNITY_VERSION"*; do
    [ -e "$ent" ] || continue
    UNITY=$(find "$ent" -maxdepth 2 -type f -name "Unity.exe" 2>/dev/null | sort -V | tail -n 1)
    [ -n "$UNITY" ] && break
  done
  # Linux 版 Unity
  if [ -z "$UNITY" ]; then
    for ent in /opt/unity/"$UNITY_VERSION"* "$HOME"/Unity/"$UNITY_VERSION"*; do
      [ -e "$ent" ] || continue
      UNITY=$(find "$ent" -maxdepth 2 -type f -name Unity 2>/dev/null | sort -V | tail -n 1)
      [ -n "$UNITY" ] && break
    done
  fi
fi
if [ -z "$UNITY" ] || [ ! -x "$UNITY" ]; then
  echo "Unity $UNITY_VERSION.x not found; set UNITY_PATH explicitly" >&2
  exit 1
fi
echo "Unity: $UNITY"

# ---- 路径转换(MSYS/git-bash 下把 POSIX 路径转成 Windows 格式给 Unity.exe) ----
PROJECT_ARG="$TMP_PROJECT"
ROOT_FWD="$PWD"
if command -v cygpath >/dev/null 2>&1; then
  PROJECT_ARG=$(cygpath -m "$TMP_PROJECT")
  ROOT_FWD=$(cygpath -m "$PWD")
fi

# ---- Phase 1: 建临时工程(仅内置模块,无网络) ----
echo "Phase 1: scaffolding temp project..."
# Unity.exe 是 GUI 程序,shell 不保证等待;后台启动 + 轮询 manifest.json 判定完成
"$UNITY" -batchmode -nographics -quit -createProject "$PROJECT_ARG" -logFile "$LOG_SCAFFOLD" >/dev/null 2>&1 &
UNITY_PID=$!
i=0
while [ ! -f "$TMP_PROJECT/Packages/manifest.json" ] && [ "$i" -lt 600 ]; do sleep 1; i=$((i+1)); done
wait "$UNITY_PID" 2>/dev/null || true
if [ ! -f "$TMP_PROJECT/Packages/manifest.json" ]; then
  echo "Scaffold failed (no manifest.json produced). Log tail:" >&2
  [ -f "$LOG_SCAFFOLD" ] && tail -n 15 "$LOG_SCAFFOLD" >&2 || true
  exit 1
fi
echo "Phase 1 done."

# ---- Phase 2: 清理仓库内 dotnet 构建残留(bin/obj) ----
# Unity 不读 .gitignore,会把 bin/obj 当包资产生成 meta;跳过 *~ 目录(Unity 本就不扫)与 .git
find . -type d \( -name bin -o -name obj \) \
  ! -path "./.git/*" ! -path "*/~/*" -exec rm -rf {} + 2>/dev/null || true
echo "cleaned bin/obj from repo"

# ---- Phase 3: 注入本仓库为本地包 ----
MANIFEST="$TMP_PROJECT/Packages/manifest.json"
if ! grep -q "site.yuanshen.htmltransformer" "$MANIFEST"; then
  perl -pi -e 's#("dependencies": \{)#$1\n    "site.yuanshen.htmltransformer": "file:'"$ROOT_FWD"'",#' "$MANIFEST"
fi
echo "manifest.json:"
cat "$MANIFEST"

# ---- Phase 4: 批处理导入 + 编译 ----
echo "Phase 4: importing package (may take several minutes)..."
"$UNITY" -batchmode -nographics -quit -projectPath "$PROJECT_ARG" -logFile "$LOG_IMPORT" >/dev/null 2>&1 &
UNITY_PID=$!
i=0
while [ ! -f "$LOG_IMPORT" ] && [ "$i" -lt 600 ]; do sleep 1; i=$((i+1)); done
wait "$UNITY_PID" 2>/dev/null || true
echo "Unity import finished (pid $UNITY_PID)"

# ---- 分析日志 ----
if [ ! -f "$LOG_IMPORT" ]; then echo "No import log generated" >&2; exit 1; fi
CS_ERRORS=$(grep -E "error CS[0-9]+" "$LOG_IMPORT" | sed "s/^[[:space:]]*//" || true)
CS_COUNT=$(printf "%s\n" "$CS_ERRORS" | grep -c "error CS" || true)
if grep -q "site.yuanshen.htmltransformer" "$LOG_IMPORT"; then IMPORTED="True"; else IMPORTED="False"; fi

echo "=== Results ==="
echo "Package present in import session: $IMPORTED"
echo "Compile errors: $CS_COUNT"
if [ "$CS_COUNT" -gt 0 ]; then
  echo "--- errors ---"
  printf "%s\n" "$CS_ERRORS"
else
  echo "No compile errors."
fi

if [ "$CS_COUNT" -eq 0 ]; then
  echo "cleaning up..."
  rm -rf "$TMP_PROJECT" 2>/dev/null || true
  RESULT="PASS"
else
  echo "FAIL: keeping temp project for inspection: $TMP_PROJECT"
  RESULT="FAIL"
fi
echo "Smoke test: $RESULT"
[ "$RESULT" = "PASS" ]
