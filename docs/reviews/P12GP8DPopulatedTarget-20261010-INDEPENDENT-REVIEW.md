# P12-G P8-D Populated Route-Observation Target — Independent Review

## Verdict

**PASS — VALIDATED_CANDIDATE**

- Candidate branch/tip: `codex/phase12/P12GP8DPopulatedTarget` at `dfc36d90c3daa151c241887b2ba4a0c4783363f6`
- Canonical/base: `cabf47185bf4b5d157157a4cb48b4f897db25127`
- Code commit: `5a36ba5a3d1d27201e52156428ef2da32bbfacbd`
- Code Git tree: `59fbb4406a7248e371a768b2aa73b7ada8f1369b`
- Tested Assets tree: `fb53856cec5ed6ca36c170aab7eb6dee90ca3d46`

## Review findings

The candidate is a clean fast-forward from the supplied base. Its implementation change is confined to the EditMode test file; the other candidate paths are validation manifest and test artifacts. No production or ProjectSettings changes appear in the diff, and no protected `.meta` files are included.

The `p8d-route-observation-populated-target` fixture registers the same stable Person in source and uninterrupted control before advancing both to the completed boundary. In the private restore candidate, it uses the existing staged P8-A Location and records a direct observation with stable source/origin provenance, observed and received day equal to the source completed-boundary day, confidence 1000, and explicit precision identity. The Person is present in the staged target PersonStore; `TryRecordObservation` exercises the normal owner write and the test requires it to succeed. The exact P8-D target-owner witness reports the installed store identity at cardinality/revision 1/1. Normal target-vector validation rejects the explicitly-empty P8-D owner with `TargetOwnerVectorFailed` / `OwnerCoverageIncomplete` before publication.

The shared corruption harness preserves the active source session, completed-boundary token, owner-thread health, and graph; subsequent source continuation matches the uninterrupted control; and a later valid restore succeeds. This proves only bounded rejection evidence. It does not claim that populated route observations are eligible for hydration or expand the selected profile.

The State file at the supplied base still calls the P8-C Person-position populated-target case the next item. This P8-D candidate advances a different target-owner case before that State ordering was refreshed. This is a documentation/readiness-order mismatch to reconcile in State; it does not invalidate the P8-D test contract or this review.

## Validation evidence

The exact candidate manifest identifies code commit/tree and tested Assets tree above. It reports:

- Focused rejection: 22/22 PASS.
- ALL EditMode: 2,848/2,848 PASS.
- Official Smoke: 5/5 PASS.
- `git diff --check`: PASS.

I verified the focused XML header and the new P8-D populated observation test result (Passed), and verified the Smoke XML header (5/5, zero failures/skips). The manifest records XML, compressed-log, and uncompressed-log SHA-256 values and says compressed logs were decompressed and checked. I did not independently recompute those hashes through the GitHub connector. Protected ProjectSettings hashes are recorded unchanged; `.meta` files were excluded and untouched.

No broader P12-G/P12-A readiness, complete owner/shared-epoch coverage, capture eligibility, export/hydration, P13 readiness, or Phase closure is inferred.