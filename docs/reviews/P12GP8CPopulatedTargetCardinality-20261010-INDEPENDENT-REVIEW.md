# P12-G P8-C Populated Target Cardinality — Independent Review

## Verdict

**PASS — VALIDATED_CANDIDATE**

- Exact candidate tip: `0f2a45acf07f6469b9c48c5390ac5ec79bd496fa`
- Canonical/base: `0acfd53d6a1090b7f40f9c55186babd7d281c527`
- Code commit: `70f442f46898f80b61d581e360fed882eb579c11`
- Code Git tree: `8b75d03f4f7a1e775a4b69fd847671ac0d66374c`
- Tested Assets tree: `0581ee266cd0b9c2822aaa7f2ff9978a0f52ef08`

## Review findings

Git comparison confirms the candidate is a clean fast-forward: one code commit and one validation-manifest commit ahead of canonical. The code commit changes only `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs`; no production code changes are present.

The new `p8c-city-location-binding-populated-target` case selects the staged City deterministically by RuntimeId and the sole staged P8-A Location, then successfully binds the two in the private restored candidate. The target P8-C census provider observes that exact store at cardinality/revision 1/1 and retains the expected City-to-Location binding. Restore rejects through the target owner-vector path before publication with `TargetOwnerVectorFailed`. The shared harness verifies the original active session, completed-boundary token, owner-thread health and graph remain intact, that subsequent continuation matches an uninterrupted control, and that a later valid restore succeeds.

This is bounded §6.3 rejection evidence for a populated P8-C target owner. It changes no profile contract and makes no P12-G/P12-A readiness, P12-B completion, broader owner/epoch, capture-eligibility, export/hydration, P13, or Phase-closure claim.

## Validation evidence

The exact-tip manifest records validation against Assets tree `0581ee266cd0b9c2822aaa7f2ff9978a0f52ef08`:

- Focused rejection: 20/20 PASS.
- ALL EditMode: 2,846/2,846 PASS.
- Official Smoke: 5/5 PASS.
- `git diff --check`: PASS.

The manifest records XML and compressed-log SHA-256 values for each suite and reports protected ProjectSettings hashes unchanged and user `.meta` files excluded. I reviewed those manifest values and exact-tree linkage; I did not independently recompute artifact SHA-256 values from raw bytes through the GitHub connector. The manifest’s validation claims should remain subject to the orchestrator’s artifact preflight.

No other findings.
