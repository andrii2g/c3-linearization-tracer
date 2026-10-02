#!/usr/bin/env python3
"""Independent Python C3 oracle for semantic fixtures; not a production engine."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
cases = json.loads((root / "fixtures/expected.json").read_text())["cases"]
checked = 0
for case in cases:
    if case["status"] == "invalidInput":
        continue  # Custom parser/graph validation must be tested by .NET.
    doc = json.loads((root / "fixtures" / case["file"]).read_text())
    definitions = {c["name"]: c["bases"] for c in doc["classes"]}
    built = {}
    failed = None

    def construct(name):
        global failed
        if name in built:
            return built[name]
        bases = tuple(construct(b) for b in definitions[name])
        try:
            cls = type(name, bases, {})
        except TypeError:
            if failed is None:
                failed = name
            raise
        built[name] = cls
        return cls

    try:
        target_type = construct(case["target"])
    except TypeError:
        assert case["status"] == "inconsistent", case["file"]
        assert failed == case["failingClass"], (case["file"], failed)
    else:
        assert case["status"] == "success", case["file"]
        order = [cls.__name__ for cls in target_type.__mro__ if cls is not object]
        assert order == case["mro"], (case["file"], order, case["mro"])
    checked += 1
print(f"PASS: {checked} valid/inconsistent fixture outcomes checked against Python C3.")
print("Partial prefixes, custom validation, .NET behavior, traces and SVGs require separate tests.")
