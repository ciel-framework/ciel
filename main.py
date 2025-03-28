import ciel.http
from ciel import Application
from pathlib import Path

from ciel.core.module import ModuleManifest, Module, ModuleRegister


async def app(scope, receive, send) -> None:
    application = Application(
        Path(__file__).parent,
        [
            ciel.http.MANIFEST
        ]
    )

def main():
    pass

if __name__ == "__main__":
    main()
