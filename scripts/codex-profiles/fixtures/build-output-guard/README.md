# Build-output guard fixture

This directory stands in for a build-output path (e.g. `bin/`, `obj/`,
`TestResults/`) that a **read-only** profile (`rx-analyst`, `rx-security`,
`rx-ci`) must never populate.

`scripts/codex-profiles/verify-profile.ps1` checks that no new file appears
under a temp copy of this directory after a read-only profile run. It is a
synthetic, disposable fixture — it holds no real build output and nothing
here should be treated as project source.
