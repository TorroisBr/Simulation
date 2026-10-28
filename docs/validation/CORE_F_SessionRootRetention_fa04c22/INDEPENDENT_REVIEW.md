# CORE-F session-root retention — independent review

## Verdict

**PASS — VALIDATED_CANDIDATE** for the bounded initial-session root retention slice and its published Unity evidence package. This is not Core canonical promotion or CORE-F acceptance.

## Identity and evidence checked

- Code candidate: `fa04c22e73656b96e07c446300bb688737be00d8`
- Code tree: `2a75eef38567a0c21056709c1eb80cca2defd59f`
- Parent: `f47f3d586f0e815e1e552ca6fadbcc77789bfaa3`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- CoreValidation: run `2b5a11a5a9b54b11ac514e9dbc072aa0`, cold/warm 820/820.
- Unity EditMode: run `ae1a1e2097354561a1157f0b5ea829a8`, 3269/3269.
- Official Smoke: run `b27f870bfa324214805ea779ed550eb3`, 5/5.
- Public package verifier: `VALID: runs=1 entries=1`.
- Archive SHA-256: `fb9d7da6e77aef3af3859050afcc5ee73731799f318ac8bef17f1123b94df712`.
- Manifest SHA-256: `75b7f962cce1dcefc1d04251c4fd3e83419300b79abcb86067e89492a51d9645`.

## Review findings

The retained initial-session composition is the exact object that produced the published execution state, effective definitions, and selected owner graph; the identity checks are reference-based. The restored-session overload does not claim or populate this genesis composition. Validation records bind to the pre-commit input snapshot; the changed input hashes match the candidate tree. The result archive and public record contain no detected workstation paths or credential-like values. The two pre-existing `ProjectSettings` files are stable validation inputs and are outside the candidate changes.

No actionable evidence integrity, privacy, scope, or identity finding remains. The result supports only initial genesis composition retention. P12-G capture and restoration remain outside Core for this slice, and detached hosting and full CORE-F acceptance remain incomplete.
