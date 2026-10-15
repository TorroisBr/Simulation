# CORE-F null-roster reconciliation — independent review/adoption

**Date:** 2026-10-15. **Verdict:** PASS.
**Provider-switch technical readiness:** READY_FOR_IMPLEMENTATION after this
reviewed correction is present on architecture canonical. Full CORE-F remains
WAIT_DEPENDENCY. No provider implementation is delivered by this record.

- Architecture base: `bec3998f697f362944aa5b551d45998145f4e67e`.
- Exact substantive candidate: `a6a9a93e9b4712cbc0d223c84f0182d4a785a1b3`.
- Source/evidence baseline: Core `64610d6c4a5cfaec2c9e14fdb70b5e7ed5d5aeaa`.
- Candidate: `codex/architecture/core-f-null-roster-reconciliation`.
- Target: `codex/architecture/world-identity-projection`.
- Author: root General Architect. Independent read-only Luna reviewer:
  `/root/wp2_final_review`; not an author of the candidate.

## Independent checks and conclusion

The reviewer checked the full three-document base-to-candidate diff against
current Core source and the adopted technical contract. The single inaccurate
bullet is explicitly superseded by the [normative correction and Master handoff](CORE_F_NULL_ROSTER_PROVIDER_SWITCH_MASTER_HANDOFF.md).
The immutable original design, prior reviews and oracle evidence retain their
historical identities; the correction neither rewrites them nor invents a new
null/JSON/domain admission policy.

The reviewer independently verified the design/CSV hashes, current provider and
codec source blobs,247 capture rows plus one run row, exact sequence18 input and
its53-byte UTF-8 SHA. The fixture's null-reference marker is distinct from its
null-field marker. The observed result is decode success, one non-null default
roster member and two null IDs. The promoted codec already reproduces it.

Source order confirms exact activation-payload equality precedes roster validation.
An ordinary tampered payload fails the earlier guard; an invalid member reaching
the roster validator fails before owner snapshot preparation or commit. The
timeline commits only on successful preparation. The contract correctly scopes
no-write claims to this attempt and makes no whole-advance rollback/token promise.
The decode value remains observable to codec callers without becoming admitted
world state.

The bounded implementation surface uses the existing typed Core codec with
lossless private DTO conversion and preserves original raw payload checks,
versions/identities/order, roster/City validation, snapshot/recipient commits and
replay. The existing integrated fixture's private-reader reflection dependency
is explicitly removed through a test-owned legacy JsonUtility oracle; no new
production generic serializer/shim is required. Historical vectors stay immutable.

The required implementation gates include explicit default-row decode plus both
activation-mismatch and reached-roster rejection/no-write paths, existing positive
and replay/frozen-pairing regressions, fresh Unity focused/ALL/Smoke and retained
LongRun cases, full input/fixture identity, and exact-tip implementation review.
Prior .NET171/171 reuse is conditional on unchanged full effective inputs. Owner
migration, shared runtime/restore, net8 hosting and detached continuation remain
independently dependency-gated. P12 and CORE-X guarantees are unchanged.

## Findings and successor applicability

No blocking finding. One nonblocking clarity nit requested identifying
`f6fee7d1...` explicitly as the sequence18 input JSON SHA. The adoption successor
labels its53 UTF-8 bytes/hash, clarifies ID allocation/RNG draw wording, links this
review and records the verdict. These are documentation-only clarifications of
the reviewed content. Obtain independent exact-tip successor applicability before
canonical promotion; no domain test rerun or new raw-report comparison is claimed.

Normal refreshed-ref/compatible-fast-forward, docs-only scope, clean worktree,
link/privacy and `git diff --check` preflight apply. Preserve current Core64610d6c,
numbered canonical refs and active implementation worktrees. No routine human
approval or product decision remains. Master consumes the promoted handoff,
corrects the quoted Core design prose and owning readiness record in its isolated
implementation branch, and performs separate implementation/validation/promotion.
