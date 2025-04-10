from pathlib import Path

from ciel import Application
from ciel.http import HTTPModule

APPLICATION = Application(
    Path(__file__).parent.parent,
    [
        HTTPModule()
    ]
)

from . import http

__all__ = [
    "APPLICATION"
]