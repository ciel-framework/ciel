from ciel import Application
from pathlib import Path

from ciel.asgi.typing import HTTPScope, ASGIReceiveCallable, ASGISendCallable
from ciel.http import HTTPKernel, HTTPModule
from ciel.http.facades import Response


async def app(scope: HTTPScope, receive: ASGIReceiveCallable, send: ASGISendCallable) -> None:
    application = Application(
        Path(__file__).parent,
        [
            HTTPModule()
        ]
    )

    await application[HTTPKernel].handle(scope, receive, send)

def main():
    pass

if __name__ == "__main__":
    main()