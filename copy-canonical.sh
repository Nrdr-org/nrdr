#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TRUNK_DIST="${SCRIPT_DIR}/../canonical/dist"
DEST_DIR="${SCRIPT_DIR}/wwwroot/canonical"

if [ ! -d "${TRUNK_DIST}" ]; then
  echo "Error: Trunk output not found at ${TRUNK_DIST}" >&2
  echo "Run 'trunk build --release' in the canonical directory first." >&2
  exit 1
fi

rm -rf "${DEST_DIR}"
mkdir -p "${DEST_DIR}"
cp -r "${TRUNK_DIST}/." "${DEST_DIR}/"

echo "Copied canonical output to ${DEST_DIR}"
