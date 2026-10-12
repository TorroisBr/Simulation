# Validation Evidence Packager Validation

Date: 2026-10-12

## Candidate identity

- Base: `6a954d77723044c382a10928c65c298cb80f999c` (`codex/phase12/canonical`)
- Implementation candidate: `7eaa35bb4d437b2308f5c89f466d5b8b58c6e623`
- Candidate tree: `b3c90497828e79c52fa56c0b46aa996102cd5275`
- Repository origin: `https://github.com/TorroisBr/Simulation`

## Validation

- PowerShell Core 7.6.5 and .NET 10.0.11 module import: PASS.
- Evidence packager synthetic self-tests: 41 assertions PASS.
- Existing Unity validation runner regression suite: 32/32 PASS.
- `git diff --check`: PASS.
- Unity was not rerun. The change adds a separate packaging tool and leaves the existing runner unchanged; the policy explicitly requires a read-only comparison of a real P12 report instead of a new Unity run solely for packaging.
- Both test suites ran in an isolated synthetic Unity project under the system temporary directory. This kept test fixtures out of the repository's `Assets`, `Packages`, and `Temp` directories.

## Read-only existing P12 report comparison

Source report from P12 canonical `6a954d77723044c382a10928c65c298cb80f999c`:

`docs/validation/P12GFactionPostGuardRecapture-20261012/AllEditMode/EditMode-20261012-041831-6db09f6370df4079a42ae33fb8ea5aaf.xml`

- Original XML: 2,934,264 bytes; SHA-256 `096831C23D4DF5F63AB1671221E38EE9A877DFF96D6EF2218C3DF323421711FF`, matching the committed record.
- Parsed result: Passed; 2,879 cases passed, zero failed, skipped, or inconclusive.
- The published `SANITIZATION.md` projection recipe recomputed to `C537AEBF3D50EFAF8AA7C917E484385D66E5E5C4E2A752E388F5A158D225567C`.
- The packager's complete source-to-normalized semantic projection matched. Normalized output: 1,489,786 bytes; SHA-256 `472D0718F2F3D08DF0ADE3EBFEAB66D3BE0651DE9397FBFE63FBD911FDBC3511`.
- All 305 `<output>` elements were removed. The normalized XML contains no absolute local-path match. The historical report was inspected read-only and was not packaged or rewritten.

## Review status

Independent exact-tip implementation review: PASS for implementation commit `7eaa35bb4d437b2308f5c89f466d5b8b58c6e623` and tree `b3c90497828e79c52fa56c0b46aa996102cd5275`. The durable review record is `INDEPENDENT_REVIEW_20261012.md`. This does not claim canonical adoption.
