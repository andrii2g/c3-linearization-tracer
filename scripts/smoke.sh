#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
cli="src/C3Tracer.Cli/bin/Release/net10.0/c3-tracer.dll"
mkdir -p artifacts/smoke
dotnet "$cli" trace fixtures/diamond.json --json artifacts/smoke/diamond.json --svg artifacts/smoke/diamond.svg --force
test -s artifacts/smoke/diamond.json
test -s artifacts/smoke/diamond.svg
set +e
dotnet "$cli" trace fixtures/inconsistent-order.json --json artifacts/smoke/conflict.json --svg artifacts/smoke/conflict.svg --force
status=$?
set -e
if [ "$status" -ne 3 ]; then
  echo "Expected inconsistent hierarchy exit 3, got $status" >&2
  exit 1
fi
test -s artifacts/smoke/conflict.json
test -s artifacts/smoke/conflict.svg
dotnet "$cli" validate fixtures/diamond.json
