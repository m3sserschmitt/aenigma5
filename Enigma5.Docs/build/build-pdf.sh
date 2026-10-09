#!/bin/bash

# Aenigma - Federated messaging system
# Copyright © 2023-2026 Romulus-Emanuel Ruja <romulus.ruja@aenigma.ro>

# This file is part of Aenigma project.

# Aenigma is free software: you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.

# Aenigma is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.

# You should have received a copy of the GNU General Public License
# along with Aenigma.  If not, see <https://www.gnu.org/licenses/>.

# Builds a single PDF from all chapters of the technical documentation.
# Requires pandoc and weasyprint (sudo apt-get install pandoc weasyprint) and python3.

set -Eeuo pipefail

BUILD_DIR="$(cd "$(dirname "$0")" && pwd)"
DOCS_DIR="$(dirname "$BUILD_DIR")"
DIST_DIR="$DOCS_DIR/dist"
WORK_DIR="$(mktemp -d)"
OUTPUT_FILE="$DIST_DIR/aenigma-technical-documentation.pdf"
TITLE="Aenigma"
SUBTITLE="Technical Documentation of the Node Software"
DATE="$(date +%Y-%m-%d)"

trap 'rm -rf "$WORK_DIR"' EXIT

for tool in pandoc weasyprint python3; do
    command -v "$tool" >/dev/null 2>&1 || { echo "Error: $tool is required but not installed." >&2; exit 1; }
done

# Chapters in reading order, followed by the appendices.
mapfile -t CHAPTERS < <(cd "$DOCS_DIR" && ls [0-9][0-9]-*.md appendix-*.md 2>/dev/null | sort)
if [[ ${#CHAPTERS[@]} -eq 0 ]]; then
    echo "Error: no chapters found in $DOCS_DIR." >&2
    exit 1
fi

# Convert every chapter on its own so heading identifiers can be made unique per chapter.
PARTS=()
for file in "${CHAPTERS[@]}"; do
    name="${file%.md}"
    echo "Converting $file"
    pandoc "$DOCS_DIR/$file" \
        --from gfm \
        --to json \
        --lua-filter "$BUILD_DIR/links.lua" \
        --lua-filter "$BUILD_DIR/academic.lua" \
        --metadata chapter="$name" \
        --output "$WORK_DIR/$name.json"
    PARTS+=("$WORK_DIR/$name.json")
done

# Merge the chapters into one document.
python3 - "$WORK_DIR/merged.json" "${PARTS[@]}" <<'PYTHON'
import json
import sys

output, parts = sys.argv[1], sys.argv[2:]
merged = None
for part in parts:
    with open(part, encoding="utf-8") as stream:
        document = json.load(stream)
    if merged is None:
        merged = document
        merged["meta"] = {}
    else:
        merged["blocks"].extend(document["blocks"])

with open(output, "w", encoding="utf-8") as stream:
    json.dump(merged, stream)
PYTHON

mkdir -p "$DIST_DIR"
echo "Rendering $OUTPUT_FILE"
(
    cd "$BUILD_DIR"
    pandoc "$WORK_DIR/merged.json" \
        --from json \
        --standalone \
        --toc \
        --toc-depth=2 \
        --metadata title="$TITLE" \
        --metadata subtitle="$SUBTITLE" \
        --metadata date="$DATE" \
        --metadata lang=en-US \
        --css pdf.css \
        --pdf-engine=weasyprint \
        --output "$OUTPUT_FILE"
)

echo "Done: $OUTPUT_FILE"
