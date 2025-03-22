from ciel import Application
from pathlib import Path

async def app(scope, receive, send) -> None:
    application = Application.configure(Path(__file__).parent).build()
    await application.handle(scope, receive, send)
