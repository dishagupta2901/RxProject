# Codex profile threat model — RxFlow

## Environment blocker (read this first)

The Codex CLI is **not runnable on this development machine** as of
2026-08-28. `C:\Users\user\AppData\Local\Programs\OpenAI\Codex\bin` is a
Windows junction pointing at `%USERPROFILE%\.codex\packages\standalone\current\bin`,
and that target directory does not exist — `Get-Command codex` / `where.exe
codex` return nothing, and `%USERPROFILE%\.codex\packages` is entirely
absent. `%USERPROFILE%\.codex\` does hold prior app state (`config.toml`,
`rules\default.rules`, `skills\`, session databases, `auth.json`), so Codex
has been used here before; the standalone CLI package itself is currently
missing or mid-update. This is an install/repair issue outside this repo's
scope — see `docs/codex-profile-operations.md`, "Unblocking the CLI."

Consequence: everything in this threat model that says a control is
"enforced by Codex's sandbox/approval engine" is a documented *design*
claim, backed by official docs read on 2026-08-28
(`docs/config-file/config-basic`, `docs/config-file/config-reference`,
`codex/sandboxing`, `codex/agent-approvals-security`,
`codex/non-interactive-mode`), not an executed observation. Two of those
doc pages disagreed on how a named profile's fields are declared (inline
`[profiles.<name>]` tables vs. standalone `$CODEX_HOME/<name>.config.toml`
files) — an ambiguity that can only be resolved by running `codex
--version` / `codex config` against a live install, which is not possible
right now. Both are documented in
`.codex/config.profiles.example.toml`. Nothing here claims "verified" for
CLI-dependent behavior; see the lab's final report for the explicit
BLOCKED/evidence split.

## Trust boundaries

| Boundary | Description |
|---|---|
| Managed / org policy (`requirements.toml`, enterprise managed configuration) | Highest precedence per official docs; can forbid `approval_policy = "never"` outright. This repo cannot see or override it — profiles here must fail to load rather than silently relax if a managed policy conflicts. |
| User config (`~/.codex/config.toml`) | Machine-specific; contains plugins/MCP/marketplace/trust settings already in place on this machine. Never overwritten by anything in this repo. |
| Repository config (`.codex/config.toml`, only for trusted projects) | Where a human would install the example profiles from this lab, after review. |
| Repository rules (`.codex/rules/*.rules`) | Command-prefix allow/deny reinforcement — not a sandbox boundary. |
| The five RxFlow profiles | Scoped intent for a specific task shape (analyst/developer/security/ci/remediator); each maps to `sandbox_mode` + `approval_policy`, nothing more privileged. |

## Configuration precedence (as documented)

1. CLI flags / `--config` overrides
2. Project config files (`.codex/config.toml`), root to cwd — closest wins, trusted projects only
3. Profile files selected via `--profile`
4. User config (`~/.codex/config.toml`)
5. System config (`/etc/codex/config.toml`)
6. Built-in defaults

Untrusted projects skip project-level config, hooks, and rules entirely
while still loading user/system config — meaning an attacker who gets a
victim to open an *untrusted* clone cannot use a malicious
`.codex/config.toml` or `.codex/rules/*` to weaken anything; the profiles
in this repo only take effect once a human has explicitly trusted this
project.

## Escape routes considered

| Route | Mitigation / enforcing layer |
|---|---|
| Symlinks pointing outside the writable root | Repo has `core.symlinks=false` (see `subagents.md`) — a checked-in symlink round-trips as a plain text path reference here, not a real link. `cleanup-remediation-worktree.ps1` and `create-remediation-worktree.ps1` resolve paths with `[System.IO.Path]::GetFullPath` and compare against the *primary worktree's* resolved root, not string prefixes on unresolved input. |
| `..` path traversal in a prompt/argument | `create-remediation-worktree.ps1` rejects any target path that resolves inside the primary worktree after full-path resolution; the same check runs before `cleanup-remediation-worktree.ps1` will touch anything. |
| Shell wrappers / alternate flags that dodge `prefix_rule` matching | Explicitly out of scope for rules to solve — the lab and this design treat `sandbox_mode`/`approval_policy` as the real boundary, rules as reinforcement only. `.codex/config.profiles.example.toml` says this plainly. |
| Response files / argument splitting | Same as above — not solvable by prefix-matching rules; relies on the sandbox's own process/filesystem boundary. |
| Git hooks | `rx-developer`/`rx-remediator` sandbox denies writes outside the workspace-write root by default; a malicious hook committed to this repo still only runs with the same sandboxed identity as everything else in that profile — it cannot escalate beyond `workspace-write`. |
| Build-lifecycle scripts (MSBuild targets, `Directory.Build.props` hooks) | Same reasoning — confined to whatever sandbox the invoking profile has. `rx-analyst`/`rx-security`/`rx-ci` never invoke `dotnet build`, so this class of script never runs under a read-only profile in the first place. |
| Environment variables widening approval/sandbox | `.codex/config.profiles.example.toml` deliberately adds no `writable_roots`; nothing in this repo sets `CODEX_*` environment overrides. `run-ci-analysis.ps1` and `verify-profile.ps1` do not read or forward any ambient `CODEX_*` variables into the invocation beyond what the user's own shell already has. |
| Docker mounts / Docker socket | Not applicable to `rx-developer`/`rx-remediator` as authored — no Compose/Testcontainers lifecycle command is in the allow-list in `.codex/rules/rxflow-operating-profiles.rules`. If a future change adds one, it must be a prompt/controlled candidate per the lab's command policy, not silently allowed. |
| Writable tool caches (NuGet global packages folder, etc.) | Outside the declared writable roots; `dotnet restore` writing to a shared global cache under network access = false is a known limitation — see below. |

## Known limitations / UNVERIFIED items

- Exact profile-declaration syntax (`[profiles.<name>]` table vs. standalone
  profile file) — UNVERIFIED, see above.
- All sandbox/approval/network/MCP enforcement claims for all five profiles
  — BLOCKED, no working `codex` CLI to execute against.
- Whether `dotnet restore` under `rx-developer` with
  `sandbox_workspace_write.network_access = false` fails closed cleanly or
  needs an explicit approval prompt for the NuGet HTTP call — BLOCKED,
  same reason.
- Whether a global NuGet package cache outside the workspace is written to
  by `dotnet restore`/`build` regardless of sandbox settings — not
  evaluated; flag before enabling any restore step in these profiles for
  real use.
