#!/usr/bin/env bash
set -euo pipefail
project_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# Headless asset generation has no need to connect to the desktop audio server.
export ALSOFT_DRIVERS=null
exec blender --background --factory-startup -noaudio --python "$project_dir/tools/create_bird.py" "$@"
