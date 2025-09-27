import unittest
from pathlib import Path
from typing import Optional, List, Dict

from ciel.runtime.module import Module, ModuleManifest, _module_order

dummy_path = Path("/tmp")


def make_module(module_id: str, deps: Optional[List[str]] = None) -> Module:
    if deps is None:
        deps = []
    return Module(dummy_path, ModuleManifest(
        id=module_id,
        name=f"Module {module_id}",
        version="1.0",
        dependency=[{"id": dep, "version": ">=1.0"} for dep in deps]
    ))


class TestModuleOrder(unittest.TestCase):

    def check_dependencies_satisfied(self, mod_order: List[Module], modules: Dict[str, Module]) -> None:
        """Assert that all dependencies are before the module in order_ids."""
        positions: Dict[str, int] = {mod.manifest.id: i for i, mod in enumerate(mod_order)}
        for mod_id, mod in modules.items():
            for dep_repr in mod.manifest.dependency:
                dep = dep_repr['id']
                if dep in positions and mod_id in positions:
                    self.assertLess(
                        positions[dep], positions[mod_id],
                        f"{dep} must come before {mod_id}"
                    )

    def test_no_dependencies(self) -> None:
        a = make_module("A")
        b = make_module("B")
        modules = {"A": a, "B": b}
        order = _module_order(["A", "B"], modules)
        self.assertEqual({m.manifest.id for m in order}, {"A", "B"})
        self.check_dependencies_satisfied(order, modules)

    def test_diamond_dependencies(self) -> None:
        a = make_module("A")
        b = make_module("B")
        c = make_module("C", deps=["A"])
        d = make_module("D", deps=["B"])
        e = make_module("E", deps=["C", "D"])
        modules = {"A": a, "B": b, "C": c, "D": d, "E": e}
        order = _module_order(["E"], modules)
        self.assertIn("E", {m.manifest.id for m in order})
        self.check_dependencies_satisfied(order, modules)

    def test_missing_dependency(self) -> None:
        f = make_module("F", deps=["Z"])  # missing dep
        modules = {"F": f}
        with self.assertRaises(RuntimeError):
            _module_order(["F"], modules)

    def test_cyclic_dependencies(self) -> None:
        x = make_module("X", deps=["Y"])
        y = make_module("Y", deps=["X"])
        modules = {"X": x, "Y": y}
        with self.assertRaises(RuntimeError):
            _module_order(["X"], modules)

    def test_complex_graph(self) -> None:
        a = make_module("A")
        b = make_module("B")
        c = make_module("C", deps=["A"])
        d = make_module("D", deps=["B"])
        e = make_module("E", deps=["C", "D"])
        f = make_module("F", deps=["C"])
        g = make_module("G", deps=["E", "F"])
        h = make_module("H", deps=["A", "B"])
        i = make_module("I", deps=["G"])
        modules = {"A": a, "B": b, "C": c, "D": d,
                   "E": e, "F": f, "G": g, "H": h, "I": i}
        order = _module_order(["I", "H"], modules)
        order_ids = {m.manifest.id for m in order}
        self.assertIn("I", order_ids)
        self.assertIn("H", order_ids)
        self.check_dependencies_satisfied(order, modules)

    def test_complex_cycle(self) -> None:
        x = make_module("X", deps=["Y"])
        y = make_module("Y", deps=["Z"])
        z = make_module("Z", deps=["X"])  # closes the cycle
        w = make_module("W", deps=["X"])  # depends on X but outside the cycle
        modules = {"X": x, "Y": y, "Z": z, "W": w}

        with self.assertRaises(RuntimeError) as cm:
            _module_order(["W"], modules)

        self.assertIn("Cyclic", str(cm.exception))  # or "cycle" depending on error msg
