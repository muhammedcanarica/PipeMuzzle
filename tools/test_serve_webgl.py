import gzip
from functools import partial
from http.server import ThreadingHTTPServer
from pathlib import Path
from contextlib import contextmanager
import threading
import unittest
from uuid import uuid4
from urllib.error import HTTPError
from urllib.request import urlopen

from serve_webgl import WebGLHandler


@contextmanager
def asset_fixture_directory():
    directory = Path(__file__).resolve().parents[1] / 'Temp' / f'webgl-headers-{uuid4().hex}'
    directory.mkdir(parents=True)
    try:
        yield directory
    finally:
        for path in directory.iterdir():
            path.unlink()
        directory.rmdir()


class WebGLServerTests(unittest.TestCase):
    def test_compressed_asset_headers_and_missing_asset_response(self):
        with asset_fixture_directory() as directory:
            payload = gzip.compress(b'wasm test payload')
            (Path(directory) / 'game.wasm.gz').write_bytes(payload)
            (Path(directory) / 'game.framework.js.br').write_bytes(b'test')
            handler = partial(WebGLHandler, directory=directory)
            with ThreadingHTTPServer(('127.0.0.1', 0), handler) as server:
                thread = threading.Thread(target=server.serve_forever, daemon=True)
                thread.start()
                base = f'http://127.0.0.1:{server.server_port}'
                try:
                    with urlopen(base + '/game.wasm.gz?version=rc1') as response:
                        self.assertEqual(response.headers['Content-Type'], 'application/wasm')
                        self.assertEqual(response.headers['Content-Encoding'], 'gzip')
                        self.assertEqual(response.read(), payload)
                    with urlopen(base + '/game.framework.js.br') as response:
                        self.assertEqual(response.headers['Content-Type'], 'application/javascript')
                        self.assertEqual(response.headers['Content-Encoding'], 'br')
                    with self.assertRaises(HTTPError) as missing:
                        urlopen(base + '/missing.wasm.gz')
                    self.assertEqual(missing.exception.code, 404)
                    self.assertIsNone(missing.exception.headers['Content-Encoding'])
                finally:
                    server.shutdown()
                    thread.join()


if __name__ == '__main__':
    unittest.main()
