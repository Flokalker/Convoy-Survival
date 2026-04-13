#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BUILD_DIR="$ROOT_DIR/Builds/WebGL"
PORT="${1:-8080}"

if [ ! -d "$BUILD_DIR" ]; then
  echo "WebGL build not found in $BUILD_DIR"
  echo "Create it first via Unity: Tools > Roadtrip World > Build WebGL Localhost"
  exit 1
fi

echo "Serving WebGL build on http://localhost:$PORT"
python3 - <<'PY' "$PORT" "$BUILD_DIR"
import http.server
import socketserver
import functools
import sys

PORT = int(sys.argv[1])
DIRECTORY = sys.argv[2]

class NoCacheHandler(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *args, directory=None, **kwargs):
        super().__init__(*args, directory=directory, **kwargs)

    def end_headers(self):
        self.send_header("Cache-Control", "no-store, no-cache, must-revalidate, max-age=0")
        self.send_header("Pragma", "no-cache")
        self.send_header("Expires", "0")
        super().end_headers()

    def guess_type(self, path):
        if path.endswith(".wasm"):
            return "application/wasm"
        if path.endswith(".js"):
            return "application/javascript"
        return super().guess_type(path)

handler = functools.partial(NoCacheHandler, directory=DIRECTORY)

with socketserver.TCPServer(("", PORT), handler) as httpd:
    print(f"Serving HTTP on :: port {PORT} (http://[::]:{PORT}/) ...")
    httpd.serve_forever()
PY
