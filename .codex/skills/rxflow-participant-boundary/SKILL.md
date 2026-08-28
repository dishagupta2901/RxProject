---
name: rxflow-participant-boundary
description: Review diffs, logs, fixtures, and docs for accidental real-world identifiers/secrets and instructor-only content before a change is considered done.
---

Use before finishing any change that touches logs, test fixtures, seed data, or documentation. Confirm every identifier is synthetic (no real patient, prescription, corporate, or production data), confirm no secrets or credentials are committed, and confirm no answer keys, defect locations, or hints about intentionally seeded defects have leaked into the participant repository. Anything instructor-only belongs only in the sibling `../instructor` directory (`INSTRUCTOR.md` and `INSTRUCTOR-DIAGRAMS.md`), never inside this tree.
