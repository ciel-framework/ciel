import ciel.http
from ciel import Application

from pathlib import Path


async def app(scope, receive, send) -> None:
    application = Application(
        Path(__file__).parent,
        [
            ciel.http.HttpModule()
        ]
    )

def main():
    pass

if __name__ == "__main__":
    main()
