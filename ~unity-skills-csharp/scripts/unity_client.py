"""
Unity HTTP client — async interface to UnityHttpServer.cs
Requires: pip install aiohttp
"""

import asyncio
import json
import sys
from pathlib import Path
from typing import Any, Optional

import aiohttp

# ---------------------------------------------------------------------------
# Config — reads ~unity-skills-csharp/assets/config.json
# ---------------------------------------------------------------------------

_CONFIG_FILENAME = "config.json"
_DEFAULT_PORT    = 7800


def _load_port() -> int:
    config_path = Path(__file__).resolve().parent.parent / "assets" / _CONFIG_FILENAME
    if not config_path.exists():
        return _DEFAULT_PORT
    try:
        data = json.loads(config_path.read_text(encoding="utf-8"))
        return int(data.get("port", _DEFAULT_PORT))
    except (json.JSONDecodeError, ValueError, KeyError):
        return _DEFAULT_PORT

# ---------------------------------------------------------------------------
# Constants — mirror UnityHttpServer.cs
# ---------------------------------------------------------------------------

PORT     = _load_port()
BASE_URL = f"http://localhost:{PORT}"

PATH_STATUS = "/status"
PATH_CALL   = "/call"

KEY_SUCCESS   = "success"
KEY_ERROR     = "error"
KEY_STATUS    = "status"
KEY_PORT      = "port"
KEY_ERRORS    = "errors"
KEY_MENU_ITEM = "menuItem"

STATUS_IDLE          = "idle"
STATUS_COMPILING     = "compiling"
STATUS_COMPILE_ERROR = "compile_error"
STATUS_EXECUTING     = "executing"
STATUS_NOT_CONNECTED = "not_connected"

_CONNECT_TIMEOUT = aiohttp.ClientTimeout(total=0.5)
_REQUEST_TIMEOUT = aiohttp.ClientTimeout(total=10)

# ---------------------------------------------------------------------------
# Low-level helpers
# ---------------------------------------------------------------------------

async def check_connection(session: aiohttp.ClientSession) -> bool:
    """Return True if Unity's HTTP server is reachable on PORT."""
    try:
        async with session.get(BASE_URL + PATH_STATUS, timeout=_CONNECT_TIMEOUT) as resp:
            return resp.status == 200
    except Exception:
        return False


async def get_status(session: aiohttp.ClientSession) -> dict[str, Any]:
    async with session.get(BASE_URL + PATH_STATUS, timeout=_REQUEST_TIMEOUT) as resp:
        resp.raise_for_status()
        return await resp.json(content_type=None)


async def call_menu(session: aiohttp.ClientSession, menu_item: str) -> dict[str, Any]:
    async with session.post(
        BASE_URL + PATH_CALL,
        json={KEY_MENU_ITEM: menu_item},
        timeout=_REQUEST_TIMEOUT,
    ) as resp:
        resp.raise_for_status()
        return await resp.json(content_type=None)


# ---------------------------------------------------------------------------
# Client
# ---------------------------------------------------------------------------

class UnityClient:
    """Reusable async client for Unity's local HTTP server (port 7800).

    Session is created lazily on first use and reused across calls.
    auto-reconnects when the connection is lost.

    Explicit lifecycle (optional):
        async with UnityClient() as client: ...   # closes session on exit
        await client.close()                      # manual close
    """

    def __init__(self) -> None:
        self._connected = False
        self._session: Optional[aiohttp.ClientSession] = None

    # ------------------------------------------------------------------
    # Session management
    # ------------------------------------------------------------------

    def _get_session(self) -> aiohttp.ClientSession:
        """Return the shared session, creating it lazily if needed."""
        if self._session is None or self._session.closed:
            self._session = aiohttp.ClientSession()
        return self._session

    async def close(self) -> None:
        """Close the underlying session and reset connection state."""
        if self._session and not self._session.closed:
            await self._session.close()
        self._session = None
        self._connected = False

    async def __aenter__(self) -> "UnityClient":
        return self

    async def __aexit__(self, *_) -> None:
        await self.close()

    # ------------------------------------------------------------------
    # Connection
    # ------------------------------------------------------------------

    async def connect(self) -> bool:
        """Probe port 7800. Returns True if Unity is reachable."""
        self._connected = await check_connection(self._get_session())
        return self._connected

    @property
    def is_connected(self) -> bool:
        return self._connected

    @property
    def port(self) -> int:
        return PORT

    # ------------------------------------------------------------------
    # API
    # ------------------------------------------------------------------

    async def status(self) -> dict[str, Any]:
        """Return Unity's current status, auto-reconnecting if needed.

        Response fields:
            success  bool
            status   idle | compiling | compile_error | executing | not_connected
            port     int   (when connected)
            errors   list  (when status == compile_error)
        """
        if not self._connected and not await self.connect():
            return {KEY_SUCCESS: False, KEY_STATUS: STATUS_NOT_CONNECTED}
        try:
            return await get_status(self._get_session())
        except Exception as exc:
            self._connected = False
            return {KEY_SUCCESS: False, KEY_STATUS: STATUS_NOT_CONNECTED, KEY_ERROR: str(exc)}

    async def call(self, menu_item: str) -> dict[str, Any]:
        """Execute a Unity menu item (e.g. 'File/Save Project'), auto-reconnecting if needed.

        Response fields:
            success   bool
            menuItem  str  (when success)
            error     str  (when not success)
        """
        if not self._connected and not await self.connect():
            return {KEY_SUCCESS: False, KEY_ERROR: f"Unity not found on port {PORT}"}
        try:
            return await call_menu(self._get_session(), menu_item)
        except Exception as exc:
            self._connected = False
            return {KEY_SUCCESS: False, KEY_ERROR: str(exc)}

    async def wait_for_idle(self, poll_interval: float = 0.5, timeout: float = 60) -> bool:
        """Poll until Unity is idle (or timeout seconds pass). Returns True if idle."""
        deadline = asyncio.get_event_loop().time() + timeout
        while asyncio.get_event_loop().time() < deadline:
            s = (await self.status()).get(KEY_STATUS, STATUS_NOT_CONNECTED)
            if s in (STATUS_IDLE, STATUS_NOT_CONNECTED, STATUS_COMPILE_ERROR):
                return s == STATUS_IDLE
            await asyncio.sleep(poll_interval)
        return False


# ---------------------------------------------------------------------------
# Module-level singleton — reused across all convenience calls
# ---------------------------------------------------------------------------

_default_client = UnityClient()


async def check_unity_status() -> dict[str, Any]:
    return await _default_client.status()


async def execute_menu_item(menu_item: str) -> dict[str, Any]:
    return await _default_client.call(menu_item)


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------

def _print_json(data: Any) -> None:
    print(json.dumps(data, ensure_ascii=False, indent=2))


async def _main() -> None:
    args = sys.argv[1:]

    if not args or args[0] == "status":
        _print_json(await check_unity_status())

    elif args[0] == "call" and len(args) >= 2:
        _print_json(await execute_menu_item(" ".join(args[1:])))

    else:
        print("Usage:")
        print("  python unity_client.py status")
        print(f'  python unity_client.py call "File/Save Project"')
        sys.exit(1)


if __name__ == "__main__":
    asyncio.run(_main())
