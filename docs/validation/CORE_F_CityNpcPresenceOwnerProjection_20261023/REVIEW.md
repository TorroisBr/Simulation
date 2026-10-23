# Independent exact-tip review — City/NPC presence owner projection

## Verdict

**VALIDATED_CANDIDATE — PASS.** No actionable findings.

## Reviewed identity

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Candidate commit: `d0f4df4eef9cf17e6fa8a4fe7edece6335cfced3`
- Candidate tree: `9f21005c02eb42231dc4f269a74eb4411d727fec`
- Exact base: `cebd8cd201149e5192880e32411a5b5ad7237b7d`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Remote branch was verified at the candidate commit; canonical was verified unchanged.
- Review performed independently by `/root/coref_action_boundary_review` against the full candidate diff and the City/NPC owner, P12, and CORE-F runtime contracts.

## Findings

The selected City `ImportantNpcs` view is resolved from the ordered Core `DailyV1CityNpcPresenceOwner` RuntimeId vector through the owning runtime registry. The projection checks exact registered NPC object identity and RuntimeId, preserves owner ordering, and fails closed if an owner ID cannot resolve. Selected City binding preflights all projections before applying bindings. Membership snapshots, preflight/commit projections, and Core-to-P12 bridge reads use the same owner-derived view. Membership mutations and revision changes remain owned by the Core presence owner.

Dynamic person materialization remains supported: an exact registered runtime NPC need not already have a selected definition owner to appear in selected City membership. The candidate preserves the legacy/unselected City list behavior. The updated composition and travel tests verify exact projection identity, ordered membership, stale legacy-cache isolation, owner revision, and the existing P12 mutation epoch behavior without weakening the no-arrival assertion.

The independent reviewer verified that validation inputs match the reviewed production and test source hashes (4/4), the final evidence archive and manifest identities reconcile, all required counts match the packaged evidence, and exact-base `git diff --check` passes.

## Validation evidence verified

- Unity EditMode: 3255/3255 passed; no failed, skipped, or inconclusive tests.
- Official Smoke filter: 5/5 passed on the same source snapshot.
- Final EditMode run: `248d2eba39584bcb878f7147df80a004`.
- Final Smoke run: `0bbd592133a3425294316439ee2cbe2f`.
- Final EditMode input snapshot SHA-256: `cf723fb492519db7d724ccbe7298ecf1a66d4dd540f9a77233ce1efe164e70b3`.
- Final EditMode original XML SHA-256: `19344a882131e3c66fa6ff7d28688687773aa731cd2293b28ab8e04538c60575`.
- Final normalized result archive SHA-256: `a95443adc8483c72812ff590a118ace651ee55ab7dca3f449e7a40b6b814eb98`.
- Final package manifest SHA-256: `f3651c4f8c8adfdcddb4db069f78973a67cf4d8351b2396926822d5cb88341ba`.
- `git diff --check`: PASS.

The validation record is `VALIDATION.md` in this directory. It distinguishes the final full-suite package from the superseded earlier package and records that filtered NUnit output was retained privately because its suite totals are not a complete-run representation.

## Limits and integration constraints

This review validates only the bounded selected City/NPC presence projection change. It does not promote Core canonical or claim completion of CORE-F, operational `DailyV1Application`, P12 detached restoration, a detached .NET host, or separate-process T/T+1/T+2 parity. Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
