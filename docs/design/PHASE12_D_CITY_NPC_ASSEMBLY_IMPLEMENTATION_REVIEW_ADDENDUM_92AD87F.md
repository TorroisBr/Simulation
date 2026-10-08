# P12-D City/NPC assembly review addendum — superseding verdict

**Review ID:** P12D-CITY-NPC-ASSEMBLY-IMPLEMENTATION-REVIEW-ADDENDUM-92AD87F  
**Verdict:** NEEDS_CHANGES  
**Supersedes:** `docs/design/PHASE12_D_CITY_NPC_ASSEMBLY_IMPLEMENTATION_REVIEW_92AD87F.md` at review-branch commit `d1771afad18041b34dafde434a547765afce7b6e`.

The candidate reviewed remains `92ad87f2a9fffa1f4fb7e1f7d996af48ddfad50e`, code `4d93a4aa38ed66712c7c8b0951a61e0303d6c7af`, against base `1c7b906c172d9e47020996888db64bd2516b451a`.

## Finding

City capture identity is not inseparably bound to the City snapshot that produced it. `P12DCityRootOwnerSnapshot.TryCapture` returns a snapshot and `P12DCityCaptureIdentityEvidence` as separate outputs. `P12DCityRootOwnerSnapshot.TryStage` accepts an identity-evidence argument but the snapshot itself does not retain or compare its original identity. Consequently, a caller can capture snapshot S under token/stamp/vector A, stage S while passing evidence B, and provide D/F evidence B. The linker and assembler then compare only B and accept City state captured under A as coherent with D/F captured under B.

The new negative tests use the identity returned with each City snapshot, so they cover City-vs-D/F mismatch but not a swapped snapshot/evidence pair. This leaves the design requirement to reject split or mismatched City/D/F capture evidence unenforced for that call path.

## Required correction

Bind the evidence to the snapshot at capture time and have staging carry that bound evidence automatically, or use a capture envelope that cannot pair a snapshot with unrelated evidence. Add a swapped-pair rejection test that proves the City membership list and revision remain unchanged. Then record a fresh exact-tip review after the corrected code and required validation.

The earlier review's checks for ancestry, source diff, cumulative diff-check, and exact validation artifacts remain factual for this tip, but they do not override this correctness finding. No candidate or canonical ref was changed by this addendum.
