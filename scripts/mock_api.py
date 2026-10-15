#!/usr/bin/env python3
"""Tiny fake of Heeler's control API so the UI can be run and previewed without a server.

  python3 scripts/mock_api.py /tmp/heeler-mock.sock
  WOLF_SOCKET_PATH=/tmp/heeler-mock.sock Godot --path src
"""
import json, os, socketserver, struct, sys, time, zlib, colorsys
from http.server import BaseHTTPRequestHandler
from urllib.parse import urlparse, parse_qs

TITLES = ["Steam", "RetroArch", "Pegasus", "Firefox", "PrismLauncher", "Lutris", "Kodi", "Heroic",
          "EmulationStation", "Desktop", "Prusa Slicer", "Moonlight"]


def apps():
    return [{
        "id": str(i), "title": t, "icon_png_path": f"mock/{i}.png", "start_virtual_compositor": True,
        "render_node": "/dev/dri/renderD128",
        "runner": {"type": "docker", "name": t.replace(" ", ""), "image": f"ghcr.io/games-on-whales/{t.lower()}:edge",
                   "mounts": [], "env": [], "devices": [], "ports": []},
    } for i, t in enumerate(TITLES)]


PROFILES = [
    {"id": "p1", "name": "Cody", "apps": apps()},
    {"id": "p2", "name": "Guest", "apps": apps()[:6]},
]
if os.environ.get("MOCK_NO_PROFILES"):  # a fresh install: no accounts yet
    PROFILES = []


def poster(i, w=400, h=600):
    """Gradient poster with a soft disc, as a PNG built with the stdlib only."""
    r1, g1, b1 = (int(c * 255) for c in colorsys.hsv_to_rgb((i * 0.11) % 1, 0.65, 0.95))
    r2, g2, b2 = (int(c * 255) for c in colorsys.hsv_to_rgb((i * 0.11 + 0.15) % 1, 0.8, 0.35))
    rows = []
    for y in range(h):
        t = y / (h - 1)
        row = bytearray([0])
        for x in range(w):
            k = t * 0.7 + (x / w) * 0.3
            r, g, b = (int(a + (c - a) * k) for a, c in ((r1, r2), (g1, g2), (b1, b2)))
            d = ((x - w / 2) ** 2 + (y - h * 0.42) ** 2) ** 0.5
            if d < w * 0.28:
                f = 0.35 * (1 - d / (w * 0.28))
                r, g, b = (int(v + (255 - v) * f) for v in (r, g, b))
            row += bytes((r, g, b))
        rows.append(bytes(row))
    raw = b"".join(rows)

    def chunk(kind, data):
        c = struct.pack(">I", len(data)) + kind + data
        return c + struct.pack(">I", zlib.crc32(kind + data) & 0xFFFFFFFF)

    return (b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 2, 0, 0, 0))
            + chunk(b"IDAT", zlib.compress(raw, 6)) + chunk(b"IEND", b""))


class Handler(BaseHTTPRequestHandler):
    protocol_version = "HTTP/1.1"

    def log_message(self, fmt, *args):
        sys.stderr.write("mock-api: " + fmt % args + "\n")

    def _send(self, code, body, ctype="application/json"):
        data = body if isinstance(body, bytes) else json.dumps(body).encode()
        self.send_response(code)
        self.send_header("Content-Type", ctype)
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)

    def do_GET(self):
        u = urlparse(self.path)
        p = u.path.removeprefix("/api/v1")
        if p == "/profiles":
            self._send(200, {"success": True, "profiles": PROFILES})
        elif p == "/admin":
            self._send(200, {"success": True, "admin_profile_id": PROFILES[0]["id"] if PROFILES else None,
                             "password_set": True, "setup_required": False})
        elif p == "/apps":
            self._send(200, {"success": True, "apps": apps()})
        elif p == "/lobbies":
            self._send(200, {"success": True, "lobbies": [{
                "id": "lobby1", "name": "Steam co-op", "icon_png_path": "mock/0.png", "profile_id": "p1",
                "started_by_profile_id": "p2", "pin_required": False, "multi_user": True,
                "stop_when_everyone_leaves": True, "connected_sessions": ["a", "b"],
                "runner": {"type": "docker", "name": "Steam", "image": "ghcr.io/games-on-whales/steam:edge"}}]})
        elif p == "/sessions":
            self._send(200, {"success": True, "sessions": [{
                "app_id": "0", "client_id": "123456789", "client_ip": "127.0.0.1", "video_width": 1920,
                "video_height": 1080, "video_refresh_rate": int(os.environ.get("MOCK_REFRESH", "60")),
                "audio_channel_count": 2, "client_settings": {}}]})
        elif p == "/clients":
            self._send(200, {"success": True, "clients": []})
        elif p == "/docker/images/inspect":
            self._send(200, {"Id": "mock"})
        elif p == "/utils/get-icon":
            name = parse_qs(u.query).get("icon_path", ["0"])[0]
            idx = int("".join(ch for ch in name if ch.isdigit()) or 0)
            self._send(200, poster(idx), "image/png")
        elif p == "/events":
            self.send_response(200)
            self.send_header("Content-Type", "text/event-stream")
            self.send_header("Cache-Control", "no-cache")
            self.end_headers()
            try:
                while True:
                    self.wfile.write(b":keepalive\n\n")
                    self.wfile.flush()
                    time.sleep(5)
            except (BrokenPipeError, ConnectionResetError):
                pass
            self.close_connection = True
        else:
            self._send(404, {"success": False, "error": "mock: not found"})

    def do_POST(self):
        n = int(self.headers.get("Content-Length", 0))
        body = self.rfile.read(n) if n else b""
        if urlparse(self.path).path.endswith("/profiles/add"):
            profile = json.loads(body)
            profile["apps"] = apps()
            PROFILES.append(profile)
        self._send(200, {"success": True, "lobby_id": "mock-lobby"})


class Server(socketserver.ThreadingMixIn, socketserver.UnixStreamServer):
    daemon_threads = True

    def get_request(self):
        req, _ = super().get_request()
        return req, ("mock", 0)


if __name__ == "__main__":
    path = sys.argv[1] if len(sys.argv) > 1 else "/tmp/heeler-mock.sock"
    if os.path.exists(path):
        os.remove(path)
    print(f"mock Heeler API on {path}", file=sys.stderr)
    Server(path, Handler).serve_forever()
