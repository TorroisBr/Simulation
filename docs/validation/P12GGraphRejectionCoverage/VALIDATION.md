# P12-G restored graph rejection coverage — validation record

## Candidate identity

- P12 canonical base: `3c1575530a1d44c3932767b6e8879be2e6672dc8`.
- Prior integrated in-memory restore candidate: `33f61e571ac0eaf5740fd9beb282e5d179c47a4f`.
- Added test commits: `92206b206db15001f4abdcf4531d57cde227078e`, `b51256d8ec04e1e341d1738c3856db2fc457f2d1`, and `3239c321070b94afd8d22ebd03a04e2151fe4a73`.
- Exact code tip: `3239c321070b94afd8d22ebd03a04e2151fe4a73`.
- Candidate Git tree: `6c9dd26bd8a078f893444f3628674228786d3b94`.
- Candidate `Assets` tree: `e60b27df5677c6116cb7e04aeed5582934d27866`.
- Changed executable path: `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` (blob `825189e123e6d5e42cfd39c9f18167424670e6a5`). Production code is unchanged from `33f61e5`.
- Unity Editor: `6000.3.9f1`; validation used `Tools/UnityValidation/Invoke-UnityValidation.ps1`.

## Added integrated rejection evidence

The current test-only delta adds these cases to the existing private-candidate restore path:

- wrong-family registry key by aliasing a City registry key to an NPC-family identity (this demonstrates typed allocator-family rejection, not a separate global duplicate-ID detector);
- an extra, valid P8 Location in the authoritative `SpatialAuthorityStore`, anchored to a live Hex (the prior case only corrupted the runtime identity index);
- transfer of the still-valid source boundary token into the target runtime immediately after target admission, before the coordinator returns the candidate;
- rejection by the final active-session publication gate while an operation is held.

The corruption cases reject before active-reference exchange. They retain the source active-session reference, source owner graph and boundary health; the token-drift and publication-gate cases also advance against an uninterrupted control and then complete a valid restore retry with matching owner truth. Existing cases cover allocator high-water, shared `SimulationRecordSequence`, NPC `MoneyAccount` revision, missing P8 Location identity, TravelParty reciprocal binding, exact-empty Expedition census, and every exposed private restore stage. Existing success tests compare the full included owner projection and continuation roots across two subsequent boundaries; separate dynamic-NPC coverage verifies membership-transition reconstruction and continuation parity.

## Validation results

All listed XMLs report `Passed`, with zero failed, skipped, or inconclusive tests. Raw logs are packaged in [`UnityValidationLogs-20261010.zip`](UnityValidationLogs-20261010.zip) (SHA-256 `A0531502D1049CF73783358C72E14950398E48579C91B73F90FCA87672945FEC`, 3,910,948 bytes); hashes below are SHA-256.

| Gate | Result | XML | XML SHA-256 | Raw log SHA-256 |
|---|---:|---|---|---|
| Focused `SimulationRuntimeAdmissionTests` | 113/113 | [`Focused/EditMode-20261010-153913-01ef85dc179c40df84881e21b00b6ea6.xml`](Focused/EditMode-20261010-153913-01ef85dc179c40df84881e21b00b6ea6.xml) | `22E895FC37B65362147CAC11277D957F9D67DC6DC259EE60EF330717DC56562C` | `A6285439F4E51F74CD97D680208AF8CBF693D2CA6F16329250662B779F27F5E2` |
| ALL EditMode | 2787/2787 | [`AllEditMode/EditMode-20261010-153939-17a2d9c694ca44f6b073616776933f85.xml`](AllEditMode/EditMode-20261010-153939-17a2d9c694ca44f6b073616776933f85.xml) | `DEB711F12F398D911F6D76A57A7C7F9C45447316FE073899C8FCE7422DC3026D` | `06A450B489292DBD126E33B71845AF516677DEB335E5635D21F4048B0112FDE2` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 | [`OfficialSmoke/EditMode-20261010-154020-eeb7ee49b37749658b70ab309452e757.xml`](OfficialSmoke/EditMode-20261010-154020-eeb7ee49b37749658b70ab309452e757.xml) | `6774529BE9F82EE37CE4750877D76EC645FC85BEA581C065888EAE1FD4B5D22F` | `08D14D69108BAF32C582231A980B56B8640F0C370BFC71C72E46279BE7739457` |
| `git diff --check` | PASS | `3c15755..3239c32` | — | — |

## Evidence limits and status

This advances only integrated rejection and failure-atomicity evidence for the in-memory coordinator. It does not complete P12-G. The design's serialized-envelope parse, structural limits, digest and compatibility matrix remain full P12-G obligations; the current coordinator accepts an active session directly and does not choose or parse a persistence representation. The technical design leaves final persistent encoding to P12-A, so this increment does not claim envelope support.

Also outstanding are the complete section/compatibility rejection matrix; systematic B-F definition/provider and cross-section corruption coverage; failure injection inside each owner hydrator/validator and publication-owner lifecycle disposal; and exact P8/P9 lineage variants. P12-G's full evidence contract remains open even though in-memory round-trip and multi-boundary parity tests exist. Do not infer persistence, capture eligibility, P12-A export/hydration readiness, or P13 readiness.

Independent review of the exact amended candidate is pending. P12-G remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains `OPEN`. Unrelated ProjectSettings edits and untracked `.meta` files are excluded.
