from ..controllers.auth import AuthController
from ciel.http.facades import Route

Route.group('/auth', lambda: [
    Route.get('/login', AuthController.login),
])