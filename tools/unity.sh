#!/usr/bin/env bash
set -euo pipefail
project_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
editor="${UNITY_EDITOR:-/home/nikoloz/Unity/Hub/Editor/6000.6.0f1/Editor/Unity}"
export LD_LIBRARY_PATH="$project_dir/.local-runtime/usr/lib${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}"
exec "$editor" -projectPath "$project_dir" "$@"
