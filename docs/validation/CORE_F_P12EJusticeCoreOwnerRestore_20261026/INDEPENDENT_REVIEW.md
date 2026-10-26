# Independent P12-E Core Justice restore review

## Reviewed identity

- Candidate tip: `15b81cffe172b4031c4cbd0e362c28e3b290d376`
- Candidate tree: `1774fecc5964fe9c3e9d5a1eb3052c8c5b673cba`
- Implementation commit: `6ecb3f8d14c596cb171dd5e715fb300ab9ecef0f`
- Implementation tree: `7e2a92ff719067893dc54c5cda6279363b928b39`
- Base: `e8496bda09fe321d804b711571949ff668c82ce0`
- The reviewed tip after the implementation commit is documentation-only.

## Verdict

**PASS — no actionable implementation, test-integrity, or evidence-integrity findings.**

The Core snapshot hydrator requires the exact staged Core City, NPC, and Person roots. It rejects missing, ambiguous, or cross-kind runtime IDs; checks legacy null-Person bindings and reciprocal non-null PersonStore/materialized-NPC bindings; reconstructs ordered wanted and sentence rows; preserves exact local revision; and links each sentence to the exact wanted owner selected by ordinal. Rejection returns no staged owner and does not mutate the supplied roots or PersonStore.

The ordered Core restore stage publishes only after all seven P12-E owner groups succeed. Both package restore paths pass the same staged Core owner roots to Justice hydration and require the Unity `JusticeSystem` projection to retain that exact Core Justice owner. The adapter checks source scalar values, actual Unity target/Person bindings, row order, and exact sentence/warrant references. Review found no duplicate Justice authority, receipt change, or P12 mutation-boundary widening.

The Core tests cover ordered rows, scalar values, exact revision, person-backed and legacy targets, missing/ambiguous roots, mismatched target/City/Person bindings, and whole-stage rejection. Existing Unity assertions remain intact and now additionally verify same-owner reference identity.

## Evidence review

The implementation source hashes match the official validation inputs. The approved evidence packager verified the full EditMode package against its protected original run manifest. Independent evidence review confirmed the declared package/archive hashes, one normalized XML entry, coherent 3271/3271 outcomes, no output element, and no detected local paths or token patterns.

Results recorded in [`VALIDATION.md`](VALIDATION.md): CoreValidation 865/865 cold and warm; focused Justice 11/11; full EditMode 3271/3271; filtered Smoke 5/5. No tests were rerun for this review.

The raw CoreValidation TRX remains in protected local retention; no public DotNetTrx package is claimed because the runner did not emit the required source-context schema. The CoreValidation run also detected this worktree's Unity `Library`, so it does not establish Library-independent execution. These limitations are stated in the validation record and do not weaken the reported run results.

This is an independent candidate review only. It does not promote Core canonical or declare CORE-F complete. Unrelated ProjectSettings changes and existing untracked files remain outside the candidate.
