from ciel.http import Request, route

class AuthController:

    @route("GET", "/login")
    def login(self, request: Request):
        pass