# Independent exact-tip review: selected-profile census lease quiescence

**Verdict: PASS**
Review date: 2026-11-23
Reviewed implementation commit: f2202a051063cbd6bfa5ce95627dc3a858984dbe
Reviewed implementation tree: 1ade3ae48ff8d7728830b1b91466bedaa76ff44b
Candidate branch: codex/core/CORE-F-restore-stage-transaction

## Review scope

This exact-tip review rechecked the previously identified omission in the Core live-provider census seal: an advance lease may be active without an operation scope.

The correction adds an active-lease check before provider sampling and repeats that check at the final synchronized seal boundary. The regression acquires a lease without opening an operation scope, confirms seal rejection leaves the registry and publication-readiness state unset, disposes the lease, and confirms a fresh seal succeeds.

The change preserves the existing census contract and exact 89-family requirement. It adds no registry, does not widen selected-profile owner authority, and does not alter Unity staging, P18 host projections, or mixed/unselected NPC compatibility.

## Review findings

No code findings remain in the bounded correction.

The committed change contains only the intended Core implementation and Core regression test. Protected ProjectSettings edits, private evidence and unrelated untracked files are outside the commit.

## Evidence reviewed

The committed-tip CoreValidation run is bound to commit f2202a051063cbd6bfa5ce95627dc3a858984dbe and tree 1ade3ae48ff8d7728830b1b91466bedaa76ff44b. It passed 1192/1192 cold and warm with stable input identity.

The Unity focused, full EditMode and Smoke manifests report stable inputs. Their repository identity fields name the preceding dbafcfdd commit and tree, but the changed production-file hash in the full run matches the reviewed f2202a051063cbd6bfa5ce95627dc3a858984dbe source. Full EditMode passed 3325/3325 and Smoke passed 5/5. The full EditMode package passed the repository verifier.

Detailed run IDs, source hashes, package hashes and preservation notes are in the accompanying validation record:
docs/validation/CORE_F_CoreOwnedCensusRegistrySeal_20261122/VALIDATION.md

## Limits

This is a review of the bounded lease-quiescence correction. It does not claim all 89 census providers are Core-composed, does not complete P12 or CORE-F, and does not request or imply Core canonical promotion.