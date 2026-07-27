#!/usr/bin/env python3
"""Lightweight validation of the Teams app manifest.

Checks the source manifest is valid JSON, has the required fields, respects
Teams' length limits, and that the referenced icon files exist. Tolerates the
{{...}} placeholder tokens (those are filled in by scripts/package.ps1).
"""
import json
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MANIFEST = os.path.join(ROOT, "appPackage", "manifest.json")

REQUIRED_TOP = [
    "manifestVersion", "version", "id", "name",
    "description", "icons", "developer", "accentColor",
]
# Teams store limits.
LIMITS = {"name.short": 30, "name.full": 100, "description.short": 80}


def main():
    errors = []
    try:
        with open(MANIFEST, encoding="utf-8") as f:
            m = json.load(f)
    except FileNotFoundError:
        print(f"Manifest not found: {MANIFEST}")
        return 1
    except json.JSONDecodeError as e:
        print(f"Manifest is not valid JSON: {e}")
        return 1

    for key in REQUIRED_TOP:
        if key not in m:
            errors.append(f"missing top-level key: {key}")

    for sub in ("short", "full"):
        if sub not in m.get("name", {}):
            errors.append(f"name.{sub} missing")
        if sub not in m.get("description", {}):
            errors.append(f"description.{sub} missing")

    for path, limit in LIMITS.items():
        a, b = path.split(".")
        val = m.get(a, {}).get(b, "")
        if len(val) > limit:
            errors.append(f"{path} is {len(val)} chars (max {limit})")

    for kind in ("color", "outline"):
        fn = m.get("icons", {}).get(kind)
        if not fn:
            errors.append(f"icons.{kind} missing")
            continue
        if not os.path.exists(os.path.join(ROOT, "appPackage", fn)):
            errors.append(f"icon file missing: {fn} (run scripts/make-icons.py)")

    dev = m.get("developer", {})
    for key in ("name", "websiteUrl", "privacyUrl", "termsOfUseUrl"):
        if not dev.get(key):
            errors.append(f"developer.{key} missing")

    if errors:
        print("Manifest validation FAILED:")
        for e in errors:
            print("  -", e)
        return 1

    print(f"Manifest OK: {m['name']['short']} v{m['version']} "
          f"(schema {m['manifestVersion']})")
    return 0


if __name__ == "__main__":
    sys.exit(main())
