# Codex operating profiles for RxFlow

Five Codex CLI operating profiles for working in this repository:
`rx-analyst`, `rx-developer`, `rx-security`, `rx-ci`, `rx-remediator`.

This document is the profile reference and install guide. See also:

- [`docs/codex-profile-threat-model.md`](codex-profile-threat-model.md) — trust boundaries, escape routes, environment blocker.
- [`docs/codex-profile-operations.md`](codex-profile-operations.md) — day-to-day usage, rollback, break-glass.
- [`docs/codex-profile-matrix.json`](codex-profile-matrix.json) — machine-readable version of the table below.

## Status: profiles are now live at the project scope

`.codex/config.toml` (repo-scoped) now contains all five `[profiles.<name>]`
blocks and **will load automatically** for this project, because this
project is already marked trusted (see
`[projects.'c:\users\user\documents\rxproject']` in this machine's
`~/.codex/config.toml`) — project-scoped config only loads for trusted
projects, and it merges with, never overwrites, that user-level file.

`.codex/config.profiles.example.toml` is kept alongside it as the versioned
reference/rollback baseline (identical profile content, extra install
narrative) — if `.codex/config.toml` is ever removed, that file documents
exactly what to restore.

**Caveat, unchanged from authoring time:** the exact profile-declaration
syntax (`[profiles.<name>]` tables vs. standalone
`$CODEX_HOME/<name>.config.toml` files) is still **UNVERIFIED** — official
Codex docs fetched 2026-08-28 disagreed, and the CLI is still not runnable
on this machine to disambiguate (see the threat model doc, "Environment
blocker"). Both files were validated as syntactically correct TOML (Python
`tomllib`, executed 2026-08-28), which proves the file parses — it does
not prove Codex itself recognizes `[profiles.<name>]` as the profile form.
If your installed Codex expects the standalone-file form instead, these
blocks will be silently ignored; run `codex config` after repairing the CLI
to confirm which form applies, per
[docs/codex-profile-operations.md](codex-profile-operations.md).

## Selecting a profile

`codex --profile <name>` (interactive) or `codex exec --profile <name> ...`
(non-interactive) once the CLI is repaired — see "Unblocking the CLI" in
[docs/codex-profile-operations.md](codex-profile-operations.md).

## Profile matrix

| Profile | Purpose | Sandbox | Approval | Network default | MCP | Writable roots | Prohibited |
|---|---|---|---|---|---|---|---|
| `rx-analyst` | Architecture mapping, investigation, evidence-based reporting | `read-only` | `untrusted` | disabled | read-only tools only, when configured | none | file edits, build output, dependency installs, DB mutation, ticket updates, deployment |
| `rx-developer` | Local code changes + tests | `workspace-write` | `on-request` | disabled unless approved | inherits user/system config | default cwd-scoped root only — no added roots | deployment, publication, release, prod DB, destructive git |
| `rx-security` | Adversarial review of source/config/dependencies | `read-only` | `untrusted` | disabled unless a specific advisory lookup is approved | read-only security/advisory sources only, when configured | none | exploit execution off-local, code edits, secret/credential retrieval, unauthorized active scanning |
| `rx-ci` | Unattended, non-interactive PR analysis | `read-only` | `never` | disabled | none permitted | none | any wait-for-input path, deployment/merge tokens |
| `rx-remediator` | Controlled patch creation, isolated | `workspace-write` | `on-request` | disabled unless approved, controlled source | inherits user/system config | isolated worktree + its `.rxflow-artifacts` dir only | push, PR merge/approve, publish, release, deploy, access outside its worktree |

`rx-analyst` and `rx-security` share an identical sandbox/approval/network
boundary; they are separate profiles because their intended prompt/skill
framing differs (general investigation vs. adversarial threat-hunting), not
their enforced access.

## Enforcing layer for each guarantee

Per-profile config is reinforcement, not the sole boundary, for two of the
requirements:

- **rx-developer / rx-remediator write confinement**: enforced by Codex's
  own `workspace-write` sandbox default (writes confined to the launched
  working directory; home/parent/`/`/credential dirs denied by default),
  not by a `writable_roots` list — see
  `.codex/config.profiles.example.toml` for why adding roots was
  deliberately avoided.
- **rx-remediator isolation from the primary checkout**: enforced by launching
  Codex with the isolated worktree (created by
  `scripts/codex-profiles/create-remediation-worktree.ps1`) as the working
  directory, so the sandbox default above scopes to that directory alone.
- **Command policy** (`.codex/rules/rxflow-operating-profiles.rules`,
  `prefix_rule(pattern=[...], decision=...)` syntax — confirmed from this
  machine's own `~/.codex/rules/default.rules`) blocks specific
  known-dangerous invocations (publish/push/destructive-git/kubectl/helm/
  terraform apply-destroy) as defense in depth. It is not a substitute for
  sandbox_mode/approval_policy.

## Stack scope

This repository is **.NET-only** (`RxFlow.slnx`, `global.json` pinning SDK
`10.0.400`, no `pom.xml`/`build.gradle*`/`mvnw`/`gradlew` anywhere).
`rx-developer`/`rx-remediator` validation commands are therefore
`dotnet restore|build|test|format` only — the lab's Java examples are
inapplicable here and are omitted rather than fabricated.

## Verification status

Every claim in this document about *enforced* behavior (as opposed to
*intended* behavior) is currently **UNVERIFIED**: the Codex CLI is not
resolvable on this machine (see the threat model doc). Three narrower
things *have* been executed for real and are not fabricated —
CI fail-closed-when-tool-missing, and the remediation worktree
create/cleanup mechanics — see
[`docs/codex-profile-matrix.json`](codex-profile-matrix.json)
`evidenceStatus` fields and the final lab report for the exact commands and
outputs.
