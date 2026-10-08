# P12-D City/NPC assembly implementation exact-tip review

**Review ID:** P12D-CITY-NPC-ASSEMBLY-IMPLEMENTATION-REVIEW-483EDB<br>
**Verdict:** VALIDATED_CANDIDATE

## Exact refs

- Repository: `TorroisBr/Simulation`
- Current P12 canonical and candidate base: `1c7b906c172d9e47020996888db64bd2516b451a`
- Candidate branch: `codex/phase12/P12DCityNpcAssemblyImplementation`
- Exact candidate tip: `483edb791f523d797e9b52e88eda00fc34eaa5ff`
- Reviewed code commit: `5aceb2b7ce49ffe009489627731cd6689fe2d200`
- Code commit tree: `bdc866e9a87091d029909aaa3767c44d1120337f`
- Reviewed `Assets` tree: `e6c0776052dbfdd8a83ce51eb15fd15cf9d89b2c`
- Candidate root tree: `503e9e6c04e8a227a6e170fc84b7adde16313381`

The remote candidate ref and canonical ref matched those exact SHAs during review. The candidate is a clean descendant of the canonical base.

## Review result

I independently reviewed the complete base-to-candidate diff, the superseding review addendum at `d36d63a91f5440527a35d1bea724ef89ac4b1623`, the P12-D technical design and current State, and the implementation validation follow-up.

The snapshot/evidence pairing finding is addressed. The staging API now creates a privately constructed `StagingCaptureEnvelope` that owns the exact City snapshot and its token/stamp/owner-vector identity together. The snapshot exposes no staging method; staging proceeds through that envelope, which carries itself into the City membership linker. The relation assembler checks the envelope identity against both D and F evidence before graph validation or any membership fill. No staging signature accepts a separate capture-evidence argument.

The swapped-context test captures a City envelope under context A, supplies D/F projection evidence from context B, and verifies rejection before publication with the membership list empty and revision unchanged. Tests also cover token/stamp/vector mismatch and the previously reviewed graph constraints: uniqueness, exact roster coverage, order/revision preservation, reciprocal City/location links, exact-zero receipt-owner evidence, no gameplay mutation during staging, and no partial membership fill for a later multi-City relation failure.

The diff remains within bounded P12-D City/NPC assembly and its design/validation evidence. It adds no P18 receipt export/replay, P12-B mutation semantics, runtime publication, profile-wide export/hydration, P12-A/P13 behavior, or Phase-closure claim.

## Validation verified

No tests were rerun during review. Exact-code validation is recorded in `docs/validation/P12DCityNpcAssembly/VALIDATION-FOLLOWUP-CAPTURE-ENVELOPE-5ACEB2B.md`. I verified the exact candidate worktree source hashes, all follow-up XML counts and SHA-256 values, and that every compressed log decompresses to the recorded raw-log hash.

| Suite | Result |
|---|---:|
| `P12DCityRootOwnerSnapshotTests` | 17/17 PASS |
| `P12DNpcReceiptOwnerCensusTests` | 13/13 PASS |
| ALL EditMode | 2592/2592 PASS |
| Official Smoke | 5/5 PASS |
| Cumulative `git diff --check` | PASS |

The candidate diff includes only City/NPC assembly code and tests, the reviewed design-revalidation document, and validation/evidence files. It contains no ProjectSettings edits or `.meta` files. The candidate worktree's unrelated ProjectSettings and untracked metadata remain outside the commit.

## Integration boundary

This is a reviewed City/NPC relation-order assembly candidate only. P12-D and Phase 12 remain open; runtime/bootstrap integration and broader accepted D owner coverage remain outstanding. Preserve P12-A as `WAIT_DEPENDENCY` and P13 as blocked. Canonical promotion remains separate under the current Execution Model.
