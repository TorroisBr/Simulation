# CORE-F selected solo-travel correction review

**Verdict:** PASS for the focused correction.

- Candidate code/test commit: `a8b6bd247cc7748d147393d03549ef65775f7a0d`.
- Git tree: `c3c6db34ba988ae87981ac4524f60eaa1129c1cb`.
- Parent: `275f38c8ad2cbfb93fc4a650d654cdab3de50a07`.
- Reviewer: independent read-only review agent, not the implementation author.

## Finding and resolution

The prior selected solo-travel integration passed the destination City as a
projection candidate to `TryCommitSelectedTravelTransition` only when the
travel plan was arriving. The previous `NpcRuntime.AdvanceTravelDay` behavior
performed that same projection-consistency preflight for every non-null
destination City, including intermediate days. The correction restores the
unconditional non-null candidate and does not change travel ordering or the
commit path.

The added `SelectedIntermediateTravelRejectsInconsistentDestinationCityProjection`
EditMode regression creates a conflicting same-ID selected NPC projection at
the destination. It confirms the destination projection check fails and that
intermediate travel rejects without changing remaining days, the Core travel
revision, City membership, or Core City-presence IDs.

## Exact-tip validation

Both official Unity EditMode captures used Unity `6000.3.9f1`, the exact
candidate commit/tree above, and stable inputs:

| Suite | Run ID | Result | Original XML SHA-256 | Original log SHA-256 |
| --- | --- | ---: | --- | --- |
| `GeneralizedSpatialTravelTests` | `4a8d538edd234622835d23c2fbb69801` | 19/19 PASS | `e1a23523a97c8face99e0222deb830dc025940c90d83243d096f150dd540cd9d` | `ba6bbeb04a81401498f796629f882aa98c21bc1cf988b4534156d477a33bf874` |
| `P12TravelPartyAdvanceTests` | `0f0724144e2f4921afc057f59cbc5017` | 10/10 PASS | `85436f162538a5ba101b575074a27e2f077e32f1f1713fb1f491f7ca18d11aff` | `73567b20b4b14f6b45f5e8ec586a1a67a2c09b794a68a3508eae6b093e05e488` |

Original XML and logs remain in private local retention; this record publishes
only their hashes and result identities. `git diff --check` passes. The earlier
full Unity, Smoke, and .NET results remain tied to their previously recorded
candidate tips and are not relabeled as runs of this correction tip.

This review closes only the cited travel-preflight finding. It does not review
or promote the complete CORE-F application/host package, and does not claim
detached restoration or .NET continuation readiness.
