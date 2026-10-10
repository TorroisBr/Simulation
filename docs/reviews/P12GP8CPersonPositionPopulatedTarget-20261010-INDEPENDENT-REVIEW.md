# P12-G P8-C Person-Position Populated Target — Independent Review

## Verdict

**PASS — VALIDATED_CANDIDATE**

- Candidate tip: `0561fbc54017108aaf0ce831da53081b329465f5`
- Canonical/base: `4c3371a1aa5838067acb5859742694ac6d653a3c`
- Code commit: `2b0e42db528266cd2411dafca6ef544d881a3a56`
- Code Git tree: `189bfc21e9df294007d79d7bf7eb6e6d91996359`
- Tested Assets tree: `903aecaecf94dd52f2c70845c8b53ac7ad77b0e2`

## Review findings

The candidate is a clean fast-forward from the supplied base. The code commit changes only `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs`; the remaining changes are the State/validation documentation and validation artifacts. No production code is changed.

The test registers the same stable Person in both source and uninterrupted control before the completed boundary. In the private restore candidate, it uses the existing staged P8-A Location and `PersonSpatialPositionStore.TrySetAt` to assign that Person a `StablePositionReference.ForLocation`. The exact target store's census witness reports its registered section and owner identity at cardinality/revision 1/1; the stored position is checked against the selected Location. The coordinator then rejects the explicitly-empty P8-C owner through the normal target-vector path before publication. Shared harness assertions verify source session/token/health/graph preservation, deterministic continuation parity against the control, and successful valid retry.

The updated State accurately follows the prior populated City-binding promotion and identifies this bounded Person-position case as the next §6.3 target gap. It preserves P12-G/P12-A `WAIT_DEPENDENCY`, P13 `BLOCKED`, and Phase 12 `OPEN`; no broader owner/shared-epoch, capture eligibility, export/hydration, or closure claim is made.

## Validation evidence

The exact-tip manifest binds validation to Assets tree `903aecaecf94dd52f2c70845c8b53ac7ad77b0e2` and records:

- Focused rejection: 21/21 PASS.
- ALL EditMode: 2,847/2,847 PASS.
- Official Smoke: 5/5 PASS.
- `git diff --check`: PASS.

I verified the focused XML header and the new populated Person-position case result (Passed), and verified the Smoke XML header (5/5, zero failures/skips). The manifest records XML and compressed-log SHA-256 values and says they were recomputed from retained local artifacts; the connector did not provide a way to independently recompute those hashes here. Protected ProjectSettings hashes are recorded unchanged, and untracked `.meta` files are excluded and untouched.

This review validates only the populated P8-C Person-position rejection case. It does not establish complete P12-G rejection/owner coverage, P12-A readiness, P12-B completion, P13 readiness, or Phase 12 closure.
