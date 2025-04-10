from ciel import Application
from ciel.http import Request
from ciel.http import Controller
from ciel.http.facades import Response

class AuthController(Controller):

    def login(self, request: Request):
        return Response.response("""
        <html>
        <head>
        <title>Hello Clem</title>
        </head>
        <body>
        <h1>Hello Clem</h1>
        <p>Je t'aime</p>
        </body>
        </html>
        """, status=200)

    def test(self, app: Application):
        return 42