# CORE-F Selected Person Residence Binding — Independent Exact-tip Review

**Verdict: `VALIDATED_CANDIDATE`**

## Exact identities

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Candidate commit: `357cf9357500b486224467aafb5635ca81898c5f`
- Candidate tree: `3c8bd35c376de462a963dbde49d15f63693523f7`
- Base commit: `8eac58267a49ac97ad3f0e36ed37d4c2c4f7a11f`
- Current `codex/core/canonical`: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Live remote candidate and canonical refs were verified at those same SHAs.

The review used a clean detached checkout of the pushed candidate. The only dirty files in the implementation worktree were pre-existing protected ProjectSettings/validation-document changes and private evidence; none are included in the candidate commit. This review does not alter or promote the implementation branch.

## Reviewed behavior

The candidate changes only the selected Person residence-bind execution path and adds Core and Unity tests plus validation evidence. It preserves the existing `runtime.person.residence-bind` admission scope and reserves only `p12b.person-life-residence/{PersonId}`.

For the exact selected Daily-v1 profile with no unselected NPC runtime member, the exact `CityRuntime` world projection, selected City owner, exact owner graph, and exact graph-bound `PersonStore`, the wrapper calls the new Core prepare/commit operation. Mixed or unselected NPC rosters continue through `PersonResidenceMembershipSystem` and its Unity presence projection.

The Core operation verifies graph/definition, PersonStore, City-owner, Person registration and object identity; rejects dead or differently assigned Persons; uses the selected resident census for capacity; preflights revision capacity; and revalidates the Person, City population revision/value, resident census, and Person revision before committing. A successful bind writes only the Person life/residence owner. It does not change the City aggregate or population revision. Same-City binding is idempotent and does not write. Stale, capacity, identity, and revision failures are rejected before mutation. The exact operation scope publishes one successful Person revision and one shared mutation epoch; rejected/no-op results do not publish an epoch.

The full candidate diff removes no existing assertions. The only replaced production lines are the prior selected-path delegation to the Unity membership system; that call remains in the compatibility fallback. No RNG, daily ordering, time, identity allocator, persistence schema, or public API changes were introduced. I found no actionable correctness, atomicity, determinism, or contract defect.

## Validation and evidence

- Official CoreValidation: `-SkipLab`, cold 1097/1097 and warm 1097/1097; zero failed; build and demo gates passed. Run `4c73d8617402424b9004c4071830eddf`. The protected run manifest reports stable compile inputs with digest `cd915cc09d01b1c2628d90821053299653e958a076466ff53f8ee7522656349b`. Its SHA-256 is `c1bb8a700edf1cdd4923463d47b876e61f539c7a31d68cbf996b1b6419c0c235`. The original cold/warm TRX reports were present and their SHA-256 values matched the manifest: `34e180fe87b4c76ee879f3dac5fafd62f1a6001d47fa39aa8fbecb8c01acafaf` and `dd035309b1dd3e2e0a67ec5425a4a9f1b0c3ce497d62d2817ad468d3899290c7`.
- Focused official P12 EditMode: 33/33 passed, zero failed/skipped/inconclusive; run `2358d8b116314cf1af4a254b57c861ad`.
- Official full EditMode: 3316/3316 passed, zero failed/skipped/inconclusive; run `89af83a22e6a43c7b0d552f71c9ab533`.
- Official EditMode Smoke name filter: 5/5 passed, zero failed/skipped/inconclusive; run `492d11bd39604746a1c85fae595b6700`.
- P12, full EditMode, and Smoke input inventories were stable before/after and share SHA-256 `04eb8d44f1d6c7cb712211e57e06616db164c4ab8fb27804d161e671ac0bf1c3`. I independently compared focused P12 and Smoke identities/outcomes with the full report; all 33 and all 5 are present.
- The normalized full EditMode archive and manifest passed the official packager's verification against the protected source run. Archive SHA-256: `ef98b80c61c1af1c190b1f7859b7fe00891a35107d48b8d3df29278d4b37fb34`; manifest SHA-256: `7f2a739bc1d2af87a8da0220787b101cbfb06bf731c73de9522310391bff1ba2`. The public package contains 3316 cases and passed the local-path privacy check.
- `git diff --check`: passed for the candidate diff.

The captured Core and Unity source hashes bind the implementation and test content. On the clean candidate checkout, Git's configured CRLF conversion changes the raw checkout bytes for two source/test files; after LF normalization their hashes match the captured validation inputs. `SimulationRuntime.cs` matches its captured raw hash. No source text differs.

The P12 filtered XML was not separately packageable: the official normalizer rejects its parent `Simulation` suite count as inconsistent with descendants. The original report remains unchanged and private; the full normalized report passed and contains every focused P12 case. A Smoke category-filter attempt returned zero cases and is excluded; the successful `TestFilter=Smoke` run above is authoritative.

CoreValidation's original TRX/logs remain in protected owner-only storage. No public normalized .NET TRX package is claimed: the current validation policy records that adapter as awaiting its own independent adoption review. The CoreValidation run used a checkout containing a Unity `Library` directory and `-SkipLab`; it does not prove Library-independent Core execution or the separate Lab gate. Unity's captured input inventory also includes the protected local ProjectSettings state present during validation, which is excluded from this candidate. These limitations are recorded and do not change the reviewed source/test content; they must not be expanded into full CORE-F, detached-host, Lab, or canonical-promotion claims.

## Integration limits

This is one bounded selected Person residence-bind operation. It does not complete CORE-F runtime composition or the detached .NET host, and it does not establish broader owner coverage, global quiescence, or Phase readiness. The Unity compatibility fallback intentionally remains for mixed/unselected NPC rosters. No canonical promotion or Phase closure is authorized by this review.