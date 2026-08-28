# Codex profile operations — RxFlow

## Day-to-day usage

- Investigation / architecture questions → `rx-analyst`.
- Implementing a change + running tests → `rx-developer`.
- Adversarial review of a diff or dependency → `rx-security`.
- Automated PR analysis in CI → `scripts/codex-profiles/run-ci-analysis.ps1`
  (wraps `rx-ci`; never invoke `rx-ci` interactively — it is designed to
  never wait for input).
- Fixing a confirmed defect from `rx-security`/`rx-ci` output →
  1. `pwsh -File scripts/codex-profiles/create-remediation-worktree.ps1`
  2. `cd` into the printed isolated worktree path
  3. run `codex --profile rx-remediator ...` from there
  4. hand off the resulting patch/artifact
  5. `pwsh -File scripts/codex-profiles/cleanup-remediation-worktree.ps1 -Path <that path>`

## Unblocking the CLI

This machine's `codex` executable is currently unresolvable (see
`docs/codex-profile-threat-model.md`, "Environment blocker"). Before any of
the verification harness's BLOCKED tests can be re-run:

1. Reinstall or repair the Codex CLI package so that
   `%USERPROFILE%\.codex\packages\standalone\current\bin\codex.exe` (the
   junction target) actually exists, or point the
   `C:\Users\user\AppData\Local\Programs\OpenAI\Codex\bin` junction at a
   working install.
2. Confirm with `codex --version` and `Get-Command codex`.
3. Re-run `pwsh -File scripts/codex-profiles/verify-profile.ps1 -Profile <name>`
   for each of the five profiles and `pwsh -File
   scripts/codex-profiles/run-ci-analysis.ps1` — none of this repo's
   scripts need to change to pick up a repaired CLI.

## Rollback — removing everything this lab added

Every artifact this lab added is new, isolated files — nothing existing was
modified except `.gitignore` (one additive block). To fully revert:

```
git rm .codex/config.toml .codex/config.profiles.example.toml
git rm .codex/rules/rxflow-operating-profiles.rules
git rm -r scripts/codex-profiles
git rm docs/codex-operating-profiles.md docs/codex-profile-threat-model.md docs/codex-profile-operations.md docs/codex-profile-matrix.json
```

`.codex/config.toml` is the **live** project-scoped config — removing it
immediately stops the five profiles from loading for this project. It does
not touch `~/.codex/config.toml` or any other user/managed configuration.

Then remove the "Codex operating-profile verification harness (Lab 12)"
block from `.gitignore` by hand (it is clearly delimited with a comment
header).

If a human already copied a `[profiles.<name>]` block from
`.codex/config.profiles.example.toml` into their real `~/.codex/config.toml`
or a real `.codex/config.toml`, removing this repo's files does **not**
remove that copy — the human who installed it must remove those specific
lines themselves, diffing against their own config, since this repo cannot
see or safely edit either file.

## Safe user-config uninstall (if a profile was installed per the guide above)

1. Open the config file the profile was pasted into.
2. Locate the specific `[profiles.rx-*]` block(s) added.
3. Delete only those blocks — leave every other section (plugins,
   marketplaces, MCP servers, trust entries, other profiles) untouched.
4. Save and re-run `codex --version` / `codex config` to confirm the file
   still parses.

## Break-glass

If a profile's restrictions block a genuinely urgent, authorized action,
the answer is **not** to edit repository rules or profile config to widen
access. Break-glass is a human/managed-policy decision:

- Use the Codex interactive session directly (no profile flag), which
  restores normal per-action approval prompts, or
- Have someone with access to managed/org policy grant a scoped, audited
  exception there.

No file in this repository can grant that exception on its own — that is
the point of keeping enforcement in `sandbox_mode`/`approval_policy` (and,
above that, managed policy) rather than in repo-editable text.
