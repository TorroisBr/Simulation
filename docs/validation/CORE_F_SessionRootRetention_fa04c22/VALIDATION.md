# CORE-F session-root retention — validation record

Status: **VALIDATED_CANDIDATE** for the bounded session-root retention slice only.

## Source identity

- Candidate: `fa04c22e73656b96e07c446300bb688737be00d8`
- Candidate tree: `2a75eef38567a0c21056709c1eb80cca2defd59f`
- Parent: `f47f3d586f0e815e1e552ca6fadbcc77789bfaa3`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Candidate branch: `codex/core/CORE-F-shared-daily-v1-app-host`

The initial Core session now retains the exact `DailyV1GenesisComposition` object that produced its admitted execution state and selected owner graph. The publication path checks reference identity for execution state, effective definitions, and owner graph. Restored sessions continue through the existing overload and do not claim a retained genesis composition.

## Validation

- CoreValidation run `2b5a11a5a9b54b11ac514e9dbc072aa0`: cold and warm **820/820 PASS**.
- Unity EditMode run `ae1a1e2097354561a1157f0b5ea829a8`: **3269/3269 PASS**.
- Official Smoke run `b27f870bfa324214805ea779ed550eb3`: **5/5 PASS** (`Smoke` filter).
- `git diff --check`: PASS.

The full EditMode result is packaged as `core-session-root-retention-editmode.zip`; SHA-256 `fb9d7da6e77aef3af3859050afcc5ee73731799f318ac8bef17f1123b94df712`. Its manifest SHA-256 is `75b7f962cce1dcefc1d04251c4fd3e83419300b79abcb86067e89492a51d9645`. The repository evidence verifier reports `VALID: runs=1 entries=1`.

The EditMode runner captured a stable input snapshot at `f47f3d586f0e815e1e552ca6fadbcc77789bfaa3`; the later candidate commit contains the same tested implementation input hashes. The exact candidate source/tree and changed files were independently reviewed. Two pre-existing local `ProjectSettings` modifications were stable inputs, not candidate changes, and remain untouched. Original validation evidence remains in its protected E:-local location; no public .NET TRX package is claimed.

## Limits

This checkpoint retains the exact genesis composition only for newly started sessions. It does not move P12-G source capture, restore staging, or restored-session composition into Core. It does not deliver `DailyV1Application.TryStart`, a detached .NET host, full P12 restoration, or CORE-F acceptance. Core canonical is unchanged.
