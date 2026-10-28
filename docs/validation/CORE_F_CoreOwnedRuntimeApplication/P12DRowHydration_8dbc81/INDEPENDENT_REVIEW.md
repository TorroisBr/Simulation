# Independent exact-tip review: Core-owned P12-D row hydration

## Verdict

**VALIDATED_CANDIDATE — PASS.** No actionable implementation or test-integrity findings on the reviewed increment. This is candidate review only; it does not promote canonical or claim complete CORE-F acceptance.

## Exact identities and scope

- Repository: TorroisBr/Simulation
- Candidate branch/tip: codex/core/CORE-F-core-owned-runtime-application / 8dbc81bc4307d2da3e35e6970552512117711514
- Candidate tree: e0516e65196204d1210837894579577d9ce9355e
- Base: b558410f8d71ec5bc924a90cc310f4c3959a72a5
- Core canonical at review: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- The candidate remote branch was verified at the exact candidate SHA; Core canonical was independently checked at the stated SHA.
- The commit changes exactly seven files: two Unity P12-D restore files, three Core application/persistence files, and two Core test files.

Scope is limited to staging selected P12-D Core City/NPC owners directly from decoded D/F rows under the existing application restore authority, before Unity creates the same host projections.

## Review findings

No actionable findings.

- D and F rows remain paired by list index and must have equal cardinality. The moved Core row factory retains the existing exact RuntimeId equality check and required receipt-list check. The previous Unity helper is now a forwarding call to that same Core factory, so legacy callers keep the same conversion.
- Core uses the existing selected-owner stager and hydrators after row conversion. Those retain unique City/NPC identity checks, admitted effective-definition identity, location/route/party membership checks, exact owner IDs, all persisted revisions, and reciprocal City/NPC relationship validation. The new regression asserts definition object identity, owner IDs, non-zero revisions from both D and F, and restored City/NPC links. A mismatched D/F NPC identity is rejected with a null stage.
- The new application entry checks the existing bootstrap or exact-session replacement staging authority before inspecting row inputs. Its failure outputs and result ordering match the existing P12-D staging entry. Tests cover unauthorized and authorized replacement ordering plus initial bootstrap admission.
- The Unity package now routes D/F rows through Core and then builds projections from the staged Core owners. No changes were made to subsequent P12-D relation validation, P12-E/F/G staging, final admission, or atomic publication. The restored Core stage remains private until the established complete package succeeds.
- Existing assertions were not weakened. The additional tests exercise row mismatch rejection, identity/revision fidelity, and application-gate ordering. No TryStart behavior or persistence profile scope was expanded.
- The outer restore package now reports malformed paired-row conversion through its existing generic InvalidRelation path rather than the old helper's InvalidOwnerSnapshot path. The malformed rows are still rejected before any owner stage escapes; I found no contract requiring that internal diagnostic category to remain stable.

## Exact-source validation evidence

The private E: manifests were accessible. Their changed source/test hashes match the current candidate worktree:

- P12DDailyV1OwnerPackage.cs: 60ba4d14564be15c60b61e92dffb7a2182d09722579c0f6a3ee93bc7c89497e4
- P12DNpcRootOwnerSnapshot.cs: b7175c17ecfc0e29bf685716a3f2918353455542655050e7053aff4ed1d7bd2e
- DailyV1Application.cs: a8466c463632a813697868aa625afb55cb99300f2aaa692011e15c648bb1db28
- DailyV1P12DNpcCoreOwnerHydrator.cs: 8cfc248da0bc330cb73a1f5d3d827c58b0a9a299181abb7381067acfd59e1320
- DailyV1P12DSelectedCoreOwnerStager.cs: 8b5b58d192c65fb5cf69d7c122990d340119727c0e28f3e16e7c81a9f02b2ca8
- DailyV1ApplicationSessionGateTests.cs: 848c18550c266a3cfe6326d20b3dbf5f04421a9bf63296b9317d470bd82f772c
- DailyV1P12DSelectedCoreOwnerStagerTests.cs: 21915a99b56accc15d1eb780f15ab2ecf2179f24ac9b66fd453cc8a12f95446b

Official CoreValidation passed 907/907 cold and 907/907 warm, run f1669d20e9c843ca8860da45927f1547. Its manifest records base commit/tree b558410f / 7b836dac plus a stable working-tree input digest; hashes for the five changed Core/test inputs match the candidate source.

The stable focused restore run passed 1/1, run dc1e7026f8a740c584595aad596de1c8. Full Unity EditMode passed 3275/3275, run 5a77298b1783468db231fb6f0d1f72bd. Official Smoke passed 5/5, run 5240c63e35a849aea6510e0f58cbe31f. All three Unity manifests report stable inputs and match the changed Unity and Core production-source hashes above. The protected manifests and original artifacts were not modified.

The supplied run references do not include a separate git diff --check result. Inspection of the exact GitHub patch found no added trailing whitespace.

## Limits

This validates the bounded P12-D Core row-staging increment only. It does not establish full P12 restoration/application completion, CORE-F completion, or canonical promotion.
