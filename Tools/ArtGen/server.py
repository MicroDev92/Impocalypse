"""Local helper for the art generator.

Serves this folder and accepts PNG uploads from the generator page, writing them
to ./out. Bound to 127.0.0.1 only; filenames are restricted to plain *.png names.

    python server.py 8090      then open http://127.0.0.1:8090/
"""
import http.server
import os
import sys
import urllib.parse

ROOT = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(ROOT, "out")
os.makedirs(OUT, exist_ok=True)


class Handler(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=ROOT, **kwargs)

    def do_POST(self):
        url = urllib.parse.urlparse(self.path)
        if url.path != "/save":
            self.send_error(404)
            return

        name = os.path.basename(urllib.parse.parse_qs(url.query).get("name", [""])[0])
        safe = name.endswith(".png") and all(c.isalnum() or c in "._-" for c in name)
        if not safe:
            self.send_error(400, "bad name")
            return

        length = int(self.headers.get("Content-Length", "0"))
        data = self.rfile.read(length)
        with open(os.path.join(OUT, name), "wb") as f:
            f.write(data)

        self.send_response(200)
        self.send_header("Content-Type", "text/plain")
        self.end_headers()
        self.wfile.write(b"ok")

    def end_headers(self):
        self.send_header("Cache-Control", "no-store")
        super().end_headers()

    def log_message(self, *args):
        pass


if __name__ == "__main__":
    port = int(sys.argv[1]) if len(sys.argv) > 1 else 8090
    http.server.ThreadingHTTPServer(("127.0.0.1", port), Handler).serve_forever()
