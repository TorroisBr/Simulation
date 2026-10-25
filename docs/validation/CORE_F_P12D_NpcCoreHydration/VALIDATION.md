# CORE-F P12-D NPC Core Hydration validation

## Candidate identity

- Repository candidate branch: `codex/core/CORE-F-p12d-core-hydration`.
- Base: `d14e7047f97b8a9572d5b37a05dc50f94a3c6e09`.
- Candidate: `5f2a9d25cb3c46586b92ce7b08f5ebbf4b38b31d`.
- Candidate tree: `b311d8089bdcd1824c6edd013d6a804929641ad6`.
- Scope: per-NPC P12 owner codecs and decoder, detached NPC F-row values, canonical section identity/schema mapping, and deterministic all-NPC batch decode before returning staged rows.

## Validation

- CoreValidation run `a1fde1b294684c63b55f3891a94b9663`: **783/783 cold and 783/783 warm PASS**. The default runner's build, CalendarDemo, and Lab gates also passed. Core runner manifest SHA-256: `a31440613fdbe145739187277e1d4a5d722ada14ef723a0898d0489f8e766bfb`.
- Core cold TRX SHA-256: `574a697e6ef3d57c1fa755cb741639e56538c0e6eb33c4c5524aee1b7d6efd78`; warm TRX SHA-256: `184b710afeb1f0b8b377d8f6742d827813614133e61e6a3b9f9d0e0ab2c1ec37`. Original reports and logs remain in the owner-only E: evidence store; they are not committed.
- Full Unity EditMode: **3264/3264 PASS**, run `50f2596df0c44ccaaec7525bd8f9bb73`; original XML SHA-256 `1ee21ed9d2e7b1800e72beffb4624822da753dbc863374fb2a66441610bb3381`.
- Official filtered Smoke: **5/5 PASS**, run `b8d97f024b524b1eb991e0266be72a63`; all five case identities are present in the full EditMode result. The full EditMode XML-only package is `CORE-F-P12D-NpcCoreHydration-AllEditMode.zip`, SHA-256 `9153fe0e7b22817de49c7bd8a0100c6e70f1cc8eeefdf986026d30e219df54f8`; manifest SHA-256 `48df36665280f358577de6fa71d8cff5bf5cde0dd9aea9a7e15c052b1abda495`. Package verification passed.
- `git diff --cached --check` passed before commit; the committed candidate contains no whitespace errors.

An initial PlayMode filter selected zero cases. It is excluded from all Smoke claims; the valid Smoke gate above used the repository's EditMode filter. CoreValidation ran while this worktree had a Unity Library, so this record makes no Library-free-environment claim.

## Scope and limitations

The extracted Core batch decoder sorts runtime identities ordinally, decodes all NPC rows against one decoded PersonStore snapshot, and returns neither list on any row failure. Unity retains host-specific projections and invokes the Core decoder. The changes preserve existing NPC section IDs/schema and the residence exclusivity rule between an NPC residence row and a bound person's duplicated life/residence facts.

This is an intermediate CORE-F candidate. It does not complete P12 image composition/restoration, establish P12-A readiness, deliver the detached .NET host, or promote Core canonical. Existing user ProjectSettings changes and unrelated untracked evidence were left outside the commit.