from pathlib import Path
from typing import List, Dict
from .module import load_modules, enumerate_modules, Module


class Runtime:
    module_paths: List[Path]
    modules: Dict[str, Module]

    def __init__(self, module_paths: List[Path]):
        self.module_paths = module_paths
        self.modules = enumerate_modules(self.module_paths)

    def load_modules(self, modules: List[str]) -> None:
        load_modules(modules, self.modules)
