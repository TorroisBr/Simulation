# Independent Review — Selected Daily-v1 Immigration and Emigration

## Verdict

**PASS — exact-tip candidate validated; no actionable findings.**

## Reviewed identity

- Repository: `https://github.com/TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Candidate commit: `9f514cdb2ddc1d96bcb6353b1d6862ecdf282cae`
- Candidate tree: `54c75a25372974435bcc001e39019b22b30b4197`
- Base commit: `ecb26c45e3957e70973ea91bb2531605af91d288`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Remote candidate matched the exact commit. Canonical remained unchanged.

## Findings

The selected Core operation preserves distinct P12 operation identities for immigration and emigration. It binds the exact NPC and City owners in the selected graph and exactly one residence authority: the registered PersonStore Person when materialized, otherwise the NPC residence owner. It validates definition and owner identity, life/residence state, population and resident-floor constraints, writer admission, and revision capacity before committing.

The City owner validates its proposed transition and P12 writer admission before invoking the residence commit callback. After that callback succeeds, the remaining City value/revision write and P12 notification do not fail in the ordinary path. Each successful operation advances one City revision, one residence revision, and one reserved P12 epoch while emitting the exact City aggregate and residence census rows. The Person-backed route avoids a duplicate NPC-residence row.

Mixed or unselected runtime rosters continue to use the existing scoped Unity lifecycle route. Failure to build the admitted operation scope faults admission closed. The added tests are additive and cover both Person-backed and NPC-only owners, operation identity, census, revisions, epoch advancement, and saturated-revision rejection. No existing assertion was weakened.

The implementation remains an intermediate CORE-F checkpoint. P12 scope/reservation ownership and the compatibility fallback remain in the Unity host; this candidate does not complete Core-owned application startup, full runtime execution, detached hosting, or CORE-F acceptance, and it does not promote Core canonical.

## Validation and artifact review

- Official CoreValidation: 1,092/1,092 cold and 1,092/1,092 warm, run `a7659ababbc04c30b1428ffefedc7662`; raw evidence remains private because the approved public normalization adapter does not accept its manifest schema.
- Final normalized-metadata full EditMode: 3,312/3,312; zero failed, skipped, or inconclusive, run `a760a2ed4a3a4dc0b85f2e2395302dfc`; input SHA-256 `99c9ef9e86a5b72a892dca5238366f7b0c2438988de6cf271104fd564158fbca`.
- Official Smoke filter: 5/5, run `4be60d781fa54a4cb002b2e747fcd452`, with the same input digest. Its filtered report is retained privately; the packager rejected the standalone XML because the parent `Simulation` suite count did not match its descendants. The verified full EditMode archive contains all five Smoke tests.
- Final Unity archive package verification: PASS; one run and one XML entry. Archive SHA-256 `d09475440e33d7e88488d92cd02024a156d9c5c0ef03fab55fc00690a58c2c08`; manifest SHA-256 `faf1a4d392bb035808d6cc0d219e580f0548e7f9fb38580378be582c67462196`.
- Source and test SHA-256 identities match `VALIDATION.md`; published manifest scan found no local workstation paths or credential-like values.
- `git diff --check`: PASS.

No tests were rerun as part of this independent review.