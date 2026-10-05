"""Serve a Unity Gzip/Brotli WebGL build locally with deployment headers.

Usage: python tools/serve_webgl.py Builds/WebGL/RC1 --port 8765
"""
import argparse
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path


class WebGLHandler(SimpleHTTPRequestHandler):
    def guess_type(self, path):
        if path.endswith(('.gz', '.br')):
            path = path.rsplit('.', 1)[0]
        if path.endswith('.wasm'):
            return 'application/wasm'
        if path.endswith('.js'):
            return 'application/javascript'
        if path.endswith('.data'):
            return 'application/octet-stream'
        return super().guess_type(path)

    def end_headers(self):
        path = self.translate_path(self.path)
        if Path(path).is_file() and path.endswith('.gz'):
            self.send_header('Content-Encoding', 'gzip')
        elif Path(path).is_file() and path.endswith('.br'):
            self.send_header('Content-Encoding', 'br')
        self.send_header('Cache-Control', 'no-cache')
        super().end_headers()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('directory', type=Path)
    parser.add_argument('--port', type=int, default=8765)
    args = parser.parse_args()
    directory = args.directory.resolve()
    if not (directory / 'index.html').is_file():
        parser.error(f'No WebGL index.html in {directory}')
    handler = partial(WebGLHandler, directory=str(directory))
    with ThreadingHTTPServer(('127.0.0.1', args.port), handler) as server:
        print(f'Serving {directory} at http://127.0.0.1:{args.port}', flush=True)
        try:
            server.serve_forever()
        except KeyboardInterrupt:
            pass


if __name__ == '__main__':
    main()
