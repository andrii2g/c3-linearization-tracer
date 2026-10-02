#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet restore c3-linearization-tracer.slnx
dotnet build c3-linearization-tracer.slnx -c Release --no-restore
dotnet test c3-linearization-tracer.slnx -c Release --no-build
bash scripts/smoke.sh
