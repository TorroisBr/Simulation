# CORE-F P12-F TravelParty Core stager - independent review

**Verdict:** VALIDATED_CANDIDATE for this bounded integration slice. Exact-tip code review passed; the documentation correction was independently rechecked. No actionable findings remain.

## Reviewed identities

- Repository: TorroisBr/Simulation; origin https://github.com/TorroisBr/Simulation.
- Current Core canonical at review: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea.
- Implementation base: d7aa82aca88a2beeecbfbabba165fe3916423772.
- Candidate branch: codex/core/CORE-F-p12d-core-hydration.
- Reviewed code commit: abb2e3f53d4cd4dc4ae6d32b68a64b1684d177db.
- Reviewed code tree: afd1785a4b0553edce145c43279982de3db91573.
- Exact docs/evidence candidate tip rechecked: 97b2d51cf7950f2288f1803f4aaeb262add11059.
- Candidate branch at recheck: 97b2d51cf7950f2288f1803f4aaeb262add11059.

## Review result

Independent reviewer /root/p12d_stage_review inspected the bounded implementation against the accepted P12-F current-base design, the exact candidate diff, and validation records. The Core stager validates selected NPC/F-row pairing, party IDs and reciprocal party membership, route endpoints, cardinality, and detached snapshot semantics; both restore/capture composition paths use the Core authority. It permits extra compatibility F rows while requiring each selected Core owner and party member to bind correctly. The Unity-only projection check remains at its adapter boundary, and the original Unity .meta identity is retained.

The reviewer confirmed the implementation source/test tree remained unchanged after abb2e3f. The docs-only successor changed only docs/validation/CORE_F_P12F_TravelPartyCoreStager_20261025/VALIDATION.md; it now distinguishes the committed Git LF blob hash from the Windows CRLF checkout hash. The review was rechecked on the exact 97b2d51 tip and passed with no remaining documentation finding.

## Validation evidence

The exact reviewed source tree has these retained results:

- CoreValidation run 975a398b31854324986c1bf9db8370ed: cold and warm, 798/798 each, no failures, errors, skips, not-executed, aborted, or timeout cases.
- Focused P12-F Unity EditMode run 8160e5cc96d24ac4a98a3628e911eaad: 25/25 PASS.
- Full Unity EditMode run 236c4b7152184234a1af1530c398ae97: 3264/3264 PASS.
- Official filtered Smoke run b20fe5cfdb614969800f16bdcc0cedb0: 5/5 PASS.
- git diff --check: PASS.
- The result-only Unity package, manifest, normalized XML, hashes, source snapshots, and private original evidence are documented in docs/validation/CORE_F_P12F_TravelPartyCoreStager_20261025/VALIDATION.md.

The raw .NET TRX/logs remain protected private evidence; this record does not claim that a public .NET evidence adapter/package is available. The documentation-only correction did not change executable source or tests, so tests were not rerun.

## Scope and limitations

This validates only the bounded Core TravelParty snapshot staging boundary used by P12-F composition. It does not complete P12-F or P12-G, establish P12-A readiness, change the accepted profile, close Phase 12 or CORE-F, or authorize canonical promotion. P12-D/P12-F owner cardinality, topology, exact-empty constraints, restore validation, and other accepted limits remain unchanged. Full CORE-F still requires the separately approved shared runtime/application, detached restoration, .NET host, and continuation work.