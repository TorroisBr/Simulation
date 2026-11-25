# Independent Review — CORE-F P12-D Restored City/NPC Membership

Verdict: **PASS — exact-tip review; no actionable implementation defects found.**

## Identities

- Candidate branch: `codex/core/CORE-F-p12d-core-hydration`.
- Reviewed candidate tip: `7ab638b18c0ee1abf3542b9e1155953abed2b70a`.
- Code commit/tree: `e57f6aa0fad70952bf6acc83d9ce0b52d8371744` / `01057713b5ed89fafc821574e2d6e49c32bf2040`.
- Review delta base: `67691bb8b2314f674da1ffb2bb64aca576451e17`.
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Candidate and canonical refs were verified remotely; the candidate is a fast-forward from its base, and canonical is an ancestor of the candidate base.

The review was performed independently and read-only against the full base-to-candidate diff, current P12-D City/NPC staged-graph contract, validation record, and result package.

## Findings

The P12-D stager validates ordered City membership against each selected NPC's restored current City and Location before it invokes the generic graph factory. It rejects missing, dangling, duplicate, and nonreciprocal links. The implementation reads membership in captured order and does not sort or mutate it. The change remains confined to P12-D restore staging; the generic runtime graph factory is unchanged.

Tests cover ordered membership that differs from NPC roster order and each added rejection case. Existing exact-owner identity, deterministic ordering, NPC-only admission, and empty-vector rejection remain. No assertions were weakened, and no duplicate authority or scope expansion was found.

## Validation evidence inspected

- Official CoreValidation: `723/723` cold and `723/723` warm; run `098cc44aff34485291de4a6dd5b5f0b3`. The protected source/test hashes match the committed files; manifest SHA-256 `cd0ac83a075074c56f1fe7ee880122c4fae43901534b4655d2c7bef3d5113e5e`.
- Focused Unity `P12GPoliticalBindingRestoreTests`: `15/15`; run `25bd2c6a786d4644829985de69cec68f`.
- Full Unity EditMode: `3263/3263`; run `f9665ee152d647e2b72f747047bde82e`.
- Official Smoke: `5/5`; run `a913000fa1ba4b208f302c88e609d972`.
- Unity input snapshots were stable before/after each run. The focused and Smoke test identities are present in the full-suite report.
- The approved privacy-safe full-suite package is `../validation/CORE_F_P12D_RestoredCityNpcMembership_20261024/coref-p12d-restored-city-npc-membership-20261024.zip`, SHA-256 `f0172dbd897e080cf6ea6c6ec9b55d93cccb38b13b72bfdbdd4eeec9a278d929`; manifest SHA-256 `2a97206801274c74253a4dc7306cc15599d944f25298281e478d4f8cfe21c156`. Package verification and privacy scans passed.
- `git diff --check`: PASS.

## Limitations

CoreValidation original reports remain in protected local retention and use `simulation-core-validation/v1`; no public TRX package or Library-independence claim is made. Filtered Unity originals remain private; the complete full-suite result package contains all selected cases. Pre-existing ProjectSettings changes were excluded from the candidate and were not inspected or changed by the reviewer.

This review covers only the bounded restored City/NPC membership validation. It does not complete P12-D, CORE-F, Phase 12, or detached .NET continuation, and does not authorize canonical promotion or Phase closure.
