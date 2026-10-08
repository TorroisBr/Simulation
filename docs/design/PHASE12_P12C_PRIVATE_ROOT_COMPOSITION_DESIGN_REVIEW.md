# P12-C Private Root Composition Design Review

**Verdict:** PASS — bounded implementation contract.

**Reviewed design commit:** `b35a7f876097439794937f06197943bd6ea08189`.
**Reviewed design blob:** `ca42353d02a7f773196980cc60009206724de3ea`.
**Review baseline:** P12 canonical `6886f5876c756a7abb86c541f6d783da885941d0`.

## Findings

The contract closes the P12-C gap between individually validated owner snapshots and a coherent private Daily-v1 root. It stages the already-promoted allocator, sequence, P8-A geography, P9-B manifest, and deterministic-random roots without adding a serialized envelope or live publication path.

The review required two clarifications, now included in the reviewed design:

- compare the full P8 Hex, Location, and scale facts with the retained P9 authored-output records, including exact profile identity/schema and invariant-culture length-prefixed values;
- reject duplicate geography provenance tags even when one copy matches P8 and another contradicts it.

The design keeps complete profile-wide identity/reference validation and publication with P12-G. It does not make P12-A ready, claim P12-G behavior, or add a product/architecture decision. The P12-C scope was already accepted in the P12 Brief and decomposition, so no new checkpoint acceptance is required.

The final exact-blob re-review passed. This is design review only; implementation and validation remain separate.
