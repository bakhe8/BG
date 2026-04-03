import os
import re
import sys


CORE_LEVERS = [
    "Visual Sequencing",
    "Reduce Parallelism",
    "Reduce Decision Density",
    "Decision Weighting in Queues",
    "Silent Alternative Path",
    "Contextual Entry",
]


def count_checked_core_levers(body: str) -> int:
    count = 0
    for lever in CORE_LEVERS:
        pattern = rf"^\s*-\s*\[[xX]\]\s*{re.escape(lever)}\s*$"
        if re.search(pattern, body, re.MULTILINE):
            count += 1
    return count


def field_has_value(body: str, label: str) -> bool:
    lines = body.splitlines()
    start_index = None

    target = f"- {label}:"
    for i, line in enumerate(lines):
        if line.strip().lower().startswith(target.lower()):
            start_index = i
            break

    if start_index is None:
        return False

    inline = lines[start_index].split(":", 1)[1].strip()
    if inline:
        return True

    # Allow value in the following lines until a new section or next field starts.
    for j in range(start_index + 1, min(start_index + 6, len(lines))):
        candidate = lines[j].strip()
        if not candidate:
            continue
        if candidate.startswith("##"):
            return False
        if candidate.startswith("-"):
            return False
        return True

    return False


def main() -> int:
    body = os.getenv("PR_BODY") or ""
    errors = []

    checked_count = count_checked_core_levers(body)
    if checked_count != 1:
        errors.append(
            "Select exactly one primary core lever in the PR template "
            f"(found {checked_count})."
        )

    for required_label in [
        "Cognitive-load source removed",
        "Structural constraint added",
        "Expected measurable impact (metric + direction)",
    ]:
        if not field_has_value(body, required_label):
            errors.append(f"Fill required field: '{required_label}'.")

    if errors:
        print("PR Workflow Reform Gate failed:")
        for item in errors:
            print(f"- {item}")
        return 1

    print("PR Workflow Reform Gate passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
