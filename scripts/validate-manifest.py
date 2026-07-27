#!/usr/bin/env python3
"""Validate the Teams app manifest against the official JSON Schema.

The source manifest contains {{...}} placeholder tokens (filled in by
scripts/package.ps1). We substitute valid dummy values in-memory, then validate
the result against the vendored Teams schema so structural errors — including
unknown/disallowed properties like a stray "packageName" — fail loudly here and
in CI rather than at upload time.
"""
import json
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MANIFEST = os.path.join(ROOT, "appPackage", "manifest.json")
SCHEMA = os.path.join(ROOT, "schema", "MicrosoftTeams.v1.17.schema.json")

# Valid dummy values for the packaging placeholders.
TOKENS = {
    "{{APP_ID}}": "00000000-0000-0000-0000-000000000000",
    "{{CONTENT_BASE}}": "https://example.com/app",
    "{{VALID_DOMAIN}}": "example.com",
}


def main():
    # Load + substitute tokens so the instance is schema-valid where a real
    # build would put concrete values.
    try:
        raw = open(MANIFEST, encoding="utf-8").read()
    except FileNotFoundError:
        print(f"Manifest not found: {MANIFEST}")
        return 1
    for tok, val in TOKENS.items():
        raw = raw.replace(tok, val)
    try:
        m = json.loads(raw)
    except json.JSONDecodeError as e:
        print(f"Manifest is not valid JSON: {e}")
        return 1

    # Icon files must exist (schema only checks the string, not the file).
    icon_errors = []
    for kind in ("color", "outline"):
        fn = m.get("icons", {}).get(kind)
        if fn and not os.path.exists(os.path.join(ROOT, "appPackage", fn)):
            icon_errors.append(f"icon file missing: {fn} (run scripts/make-icons.py)")

    # JSON Schema validation against the vendored Teams schema.
    schema_errors = []
    try:
        import jsonschema
    except ImportError:
        print("jsonschema not installed — run: pip install jsonschema")
        print("(skipping schema validation; structural errors will NOT be caught)")
        if icon_errors:
            for e in icon_errors:
                print("  -", e)
            return 1
        return 0

    schema = json.load(open(SCHEMA, encoding="utf-8"))
    validator = jsonschema.Draft4Validator(schema)
    for err in sorted(validator.iter_errors(m), key=lambda e: list(e.path)):
        loc = "/".join(str(p) for p in err.path) or "(root)"
        schema_errors.append(f"at '{loc}': {err.message}")

    errors = schema_errors + icon_errors
    if errors:
        print("Manifest validation FAILED:")
        for e in errors:
            print("  -", e)
        return 1

    print(f"Manifest OK: {m['name']['short']} v{m['version']} "
          f"(schema {m['manifestVersion']}, validated against vendored JSON Schema)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
