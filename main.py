from app import APPLICATION

from ciel.asgi.typing import HTTPScope, ASGIReceiveCallable, ASGISendCallable
from ciel.http import HTTPKernel

async def run(scope: HTTPScope, receive: ASGIReceiveCallable, send: ASGISendCallable) -> None:
    with APPLICATION.branch_for_request() as application:
        await application[HTTPKernel].handle(scope, receive, send)

def main():
    pass

if __name__ == "__main__":
    print("Test")
