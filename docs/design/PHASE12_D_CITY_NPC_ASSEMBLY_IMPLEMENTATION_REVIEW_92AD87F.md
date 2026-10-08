# P12-D City/NPC assembly implementation exact-tip review

**Review ID:** P12D-CITY-NPC-ASSEMBLY-IMPLEMENTATION-REVIEW-92AD87F  
**Verdict:** VALIDATED_CANDIDATE

## Exact refs

- Repository: `TorroisBr/Simulation`
- Current P12 canonical and candidate base: `1c7b906c172d9e47020996888db64bd2516b451a`
- Candidate branch: `codex/phase12/P12DCityNpcAssemblyImplementation`
- Exact candidate tip: `92ad87f2a9fffa1f4fb7e1f7d996af48ddfad50e`
- Reviewed code commit: `4d93a4aa38ed66712c7c8b0951a61e0303d6c7af`
- Code commit tree: `f5634025e3e6f82cd6f2460f3fe45383d77ce6fb`
- Reviewed `Assets` tree: `60ba6768fd1d9c01bfe365fc29ffca4eed1e9a6c`
- Candidate root tree: `05691f8cf96b0fa924808f17673f854dfda37193`

The remote candidate ref matched the reviewed SHA; the current canonical ref matched the stated base. The candidate is a clean descendant of that base. The cumulative `git diff --check` passes.

## Review result

I independently reviewed the complete base-to-candidate diff, the accepted P12-D City/NPC relation-order design and its current-base revalidation, the owning P12 brief/state, the implementation validation records, and the candidate worktree. The earlier review findings are corrected:

- City capture returns transient token/stamp/owner-vector identity evidence. Staging carries that evidence through `P12DCityMembershipLinker`.
- Before graph validation or any membership fill, the relation assembler requires the City evidence and both D/F projection evidence objects to share the exact token, capture stamp, and owner-vector references.
- New token, stamp, and vector mismatch cases reject before publication and assert that City membership stays empty and its revision unchanged.
- The design revalidation document no longer contains trailing whitespace; cumulative diff-check passes.

The assembly validates unique City and NPC identities, exact roster coverage, ordered pending NPC membership, and reciprocal City/location links before filling any City list. A two-City failure case confirms earlier valid members are not partially linked if a later relation is invalid. Fill preserves captured order and revision and invokes no gameplay mutators. The staged-presence factory assigns direct references without calling the gameplay starting-City constructor or adding City membership.

The transient NPC projection evidence checks the two excluded receipt-owner families against the exact census rows, owner identities, cardinalities, and revisions; it rejects populated, missing, or mismatched evidence. The implementation adds no receipt export/replay, P12-B semantics, runtime publication, profile-wide export/hydration, P12-A/P13 behavior, or Phase-closure claim.

## Validation evidence verified

No Unity tests were rerun during review. The exact-tip follow-up record is `docs/validation/P12DCityNpcAssembly/VALIDATION-FOLLOWUP-CITY-IDENTITY-4D93A4A.md`. I checked its committed worktree source hashes and validation XML/log hashes; each compressed log decompresses to the recorded raw log hash.

| Suite | Result |
|---|---:|
| `P12DCityRootOwnerSnapshotTests` | 16/16 PASS |
| `P12DNpcReceiptOwnerCensusTests` | 13/13 PASS |
| ALL EditMode | 2591/2591 PASS |
| Official Smoke | 5/5 PASS |
| Cumulative `git diff --check` | PASS |

The candidate diff contains only City/NPC assembly code and tests, its reviewed design-revalidation record, and validation/evidence files. No unrelated ProjectSettings edits or untracked `.meta` files are committed.

## Integration boundary

This is a reviewed P12-D City/NPC relation-order assembly candidate only. It does not complete P12-D or Phase 12, implement runtime/bootstrap integration, or establish profile-wide owner export/hydration. Preserve P12-A as `WAIT_DEPENDENCY`, P13 as blocked, and Phase 12 as open. Canonical promotion remains a separate step under the current Execution Model.
