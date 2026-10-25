# CORE-F P12-F ActorChoice Codec Source Integration Review

- Verdict: **VALIDATED_CANDIDATE (PASS)**; no actionable findings.
- Independent reviewer: `/root/p12f_actor_codec_review`.
- Reviewed implementation: `88ca9532a06483935c5bca880c4c43c94c596373` (tree `a222cf98c4e3e96f56468d5d1b4c358c8db17784`).
- Validation documentation successor: `7ea3a3907791e11bffb13d229b2174af8f980acf`; it changes only `docs/validation/CORE_F_P12F_CoreOwnerSnapshots_20261025/VALIDATION.md`.
- Implementation base: `6bb9bd688ef91974083ab37348b0c01c0aa74776`.
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`, an ancestor of the candidate.
- Candidate remote preflight: `origin/codex/core/CORE-F-p12d-core-hydration` at `7ea3a3907791e11bffb13d229b2174af8f980acf`; Core canonical at `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

## Scope and source identity

The change moves the existing `P12AActorChoiceCodec` and its `.meta` file from the Unity scripts folder into `Packages/com.simulation.core/Runtime/Persistence`, adds the explicit Core SDK compile entry and Core test-project entry, and adds three Core tests. The reviewer verified the codec source and `.meta` Git blob identities are unchanged across the move, the Unity GUID remains `b104e2579c8f4f759c8a5d72745b013e`, and existing friend-assembly access is reused. The edit adds no public API, duplicate owner, writer, or runtime operation.

The encoded representation and production semantics remain unchanged. Coverage includes terminal history/sequence/identity round-trip, the exact empty initial owner, and rejection of pending/tampered row-key data. The reviewer found no weakened assertions or changes to admission, owner identity, wire format, P12 scope, revisions, or mutation authority.

## Exact-tip validation verified

- Official CoreValidation run `ac0c88eacaad4f38a09c6d208d73355b`: 761/761 cold and 761/761 warm. The reviewer matched all 384 manifest input hashes to the candidate; the validation record contains the private manifest and TRX digests.
- Official Unity focused run `225b373adbcf4ebba8624ce851d3c046`, Unity `6000.3.9f1`, filter `P12AContinuationCodecTests`: 37/37 passed, zero failures, skips, or inconclusive cases. The reviewer matched the private manifest and XML to the exact detached candidate tree and confirmed the five new ActorChoice cases.
- `git diff --check`: PASS.

The Unity source was tested from a detached E: validation worktree at the exact candidate commit/tree. The reviewer verified all 1,884 recorded source inputs. The source checkout's unrelated ProjectSettings edits were preserved and excluded from the candidate.

## Limits

This is a focused Core source-ownership candidate. It has no full EditMode or Smoke result at this source tip; those gates are not claimed. Core TRX and filtered Unity evidence remain in private evidence storage and are not represented as a public TRX-EVIDENCE-v1 package. This review does not promote Core canonical, complete P12-F or CORE-F, or establish the detached .NET host. Revalidate affected suites at the appropriate integration gate.
