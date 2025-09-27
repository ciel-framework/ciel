import logging
import sys
import tomllib
from collections import defaultdict
from dataclasses import dataclass, field
from enum import Enum
from importlib import import_module
from importlib.util import spec_from_file_location, module_from_spec
from pathlib import Path
from types import ModuleType
from typing import List, TypedDict, Self, Dict, Optional, Set
from collections import deque

__logger__ = logging.getLogger(__name__)


class AuthorDict(TypedDict):
    name: str
    email: str


class DependencyDict(TypedDict):
    id: str
    version: str


MANIFEST_FILE_NAME = "manifest.toml"


@dataclass
class ModuleManifest:
    id: str
    name: str
    version: str
    description: Optional[str] = None
    licence: str = "Proprietary"
    authors: List[AuthorDict] = field(default_factory=list)
    dependency: List[DependencyDict] = field(default_factory=list)


class ModuleState(Enum):
    NOT_LOADED = "Not Loaded"
    LOADED = "Loaded"


class Module:
    path: Path
    manifest: ModuleManifest
    state: ModuleState
    python_module: Optional[ModuleType]

    def __init__(self, path: Path, manifest: ModuleManifest) -> None:
        self.path = path
        self.manifest = manifest
        self.state = ModuleState.NOT_LOADED
        self.python_module = None

    def __eq__(self: Self, other: object) -> bool:
        if not isinstance(other, Module):
            return False
        return self.manifest.id == other.manifest.id

    def __hash__(self: Self) -> int:
        return hash(self.manifest.id)

    def __repr__(self: Self) -> str:
        return f"Module({self.manifest.id}[{self.manifest.version}])"

    def _import_package(self) -> Optional[ModuleType]:
        init_file = self.path / '__init__.py'
        if not init_file.is_file():
            return None
        package_name = f'ciel.modules.{self.manifest.id}'
        spec = spec_from_file_location(package_name, init_file)
        if spec is None or spec.loader is None:
            raise ImportError(f"Could not load package spec for {self.path}")
        module = module_from_spec(spec)
        sys.modules[package_name] = module
        spec.loader.exec_module(module)
        return module

    def load(self: Self) -> None:
        self.python_module = self._import_package()
        self.state = ModuleState.LOADED
        __logger__.info(f"Loaded module {self.manifest.id}")


class BaseModule(Module):

    def __init__(self) -> None:
        module_path = (Path(__file__).parent.parent / 'modules' / 'base').absolute()
        super().__init__(module_path, ModuleManifest(
            id="base",
            name="Base",
            version="0.1.0"
        ))

    def _import_package(self) -> Optional[ModuleType]:
        return import_module('ciel.modules.base')


def enumerate_modules(module_paths: List[Path]) -> Dict[str, "Module"]:
    res: Dict[str, Module] = {
        'base': BaseModule()
    }

    for module_path in module_paths:
        module_path = Path(module_path)
        for child in module_path.iterdir():
            if child.is_dir() and (manifest_file := (child / MANIFEST_FILE_NAME)).is_file():
                try:
                    with open(manifest_file, "rb") as f:
                        manifest = ModuleManifest(**tomllib.load(f))
                        res[manifest.id] = Module(child, manifest)
                except OSError:
                    __logger__.error("Failed to load module at %s", child, exc_info=True)

    return res


def _module_order(modules_ids: List[str], available_modules: Dict[str, Module]) -> List[Module]:
    queue: deque[str] = deque(modules_ids)
    modules_to_load: Dict[str, str] = dict()

    while queue:
        mod_id = queue.popleft()
        if mod_id in modules_to_load:
            continue

        for dep_repr in available_modules[mod_id].manifest.dependency:
            if dep_repr['id'] not in available_modules:
                raise RuntimeError(f"Module {dep_repr['id']} (requested by {mod_id}) is unavailable")
            # TODO: Check version
            queue.append(dep_repr['id'])

        modules_to_load[mod_id] = mod_id

    modules_with_no_deps: deque[str] = deque()
    modules_needs: Dict[str, Set[str]] = defaultdict(set)
    modules_needed_by: Dict[str, Set[str]] = defaultdict(set)

    for mod_id in modules_to_load:
        mod = available_modules[mod_id]
        if not mod.manifest.dependency:
            modules_with_no_deps.append(mod_id)
        else:
            for dep_repr in mod.manifest.dependency:
                modules_needs[mod_id].add(dep_repr['id'])
                modules_needed_by[dep_repr['id']].add(mod_id)

    ordered_modules: List[Module] = []
    while modules_with_no_deps:
        mod_id = modules_with_no_deps.popleft()
        mod = available_modules[mod_id]
        del modules_to_load[mod_id]
        ordered_modules.append(mod)
        for dep_id in modules_needed_by[mod_id]:
            modules_needs[dep_id].remove(mod_id)
            if not modules_needs[dep_id]:
                modules_with_no_deps.append(dep_id)

    if modules_to_load:
        raise RuntimeError(f"Cyclic dependency detected among: {modules_to_load}")
    return ordered_modules


def load_modules(modules_ids: List[str], available_modules: Dict[str, Module]) -> None:
    for mod in _module_order(modules_ids, available_modules):
        mod.load()
