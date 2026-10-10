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

## Latest code candidate — evolved Person/Genealogy restore (2026-10-10)

- Canonical base: `3c1575530a1d44c3932767b6e8879be2e6672dc8`.
- Code commit: `fdc9ac26ff015015399253555747f387d5f2207d`.
- Code Git tree: `244d91042ed14cf43955789e2780d47d9c9d5099`.
- Code `Assets` tree: `d12e8f6a5f75c986b09b3dcad95ffe8ddb4406c0`.
- Changed code paths: `Assets/_Project/Scripts/SimulationRuntime.cs` and
  `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs`.
- Review record for prior exact code `3239c32` remains historical evidence only;
  independent review of this changed code tip is pending.

The integrated restore test registers two Persons, binds one existing NPC to
the child Person, adds a valid parentage edge, advances the source and an
equivalent control, restores the source, then checks exact Person identity,
NPC binding, genealogy direction, owner-section cardinality and full included
owner projection. It compares the restored graph with the uninterrupted
control on two subsequent daily boundaries.

The first focused execution exposed a real restore-admission defect: the
runtime treated Genealogy, PoliticalKnowledge and PoliticalDecision as if they
must still have bootstrap-empty state after their reconstructed owners were
staged. The fix keeps empty-state checks for a fresh bootstrap and lets a
restored continuation establish its census baseline from the reconstructed
required owners. The one-test rerun and all required suites then passed.

| Gate | Result | Artifact | XML SHA-256 | Raw log SHA-256 |
|---|---:|---|---|---|
| Focused new restore test | 1/1 | `PersonGenealogy-20261010/EditMode-20261010-155531-4b697725cd274ecc93392748c7621b54.xml` | `1B300D9E54E922D02EA79881CE0D57B6DE5ADDF8262609856F05FD747B60FEB8` | `D8FBEEC42E44007DCB9574ED85441A4366250C42AEAA0DCC0993FD3F49AABB8C` |
| Focused `SimulationRuntimeAdmissionTests` | 114/114 | `PersonGenealogy-20261010/EditMode-20261010-155553-d597e6991df44dd298bad66ed052f3d7.xml` | `38A1A801DE7B1EC138591DA35EBD42AAAAF722071F45C932F21FA830D2D6B4A9` | `BE5DD63553A566D76CF1099AAD8B15DA191306CE480234903F59D31D695873A4` |
| ALL EditMode | 2788/2788 | `PersonGenealogy-20261010/EditMode-20261010-155611-a7c43d24e1d04c11826f6d5291d357af.xml` | `FCC9956E091779A005F00289BE2E995E22008FE8EEB1D60BD311E5D0F020C066` | `9AD6E85E79BA025D224E16064AB0E02DA8047151C2DA3EA74082368E21DD307D` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 | `PersonGenealogy-20261010/EditMode-20261010-155649-8d0d50edeb48496bb6f54965f38debcf.xml` | `2AE3986BDD1DF03DBA4DED6CDB9ECD4985349F8A345493EA724B79C522A75F92` | `03B59BDFAFD1A4AEC0CEB7C7E1EA3BCAA73D6745A7D712A282ABB35A6E507A7F` |
| `git diff --check` | PASS | Code commit `fdc9ac2` | — | — |

Passing XMLs and raw logs are archived in
[`PersonGenealogy-20261010/UnityValidationLogs-20261010.zip`](PersonGenealogy-20261010/UnityValidationLogs-20261010.zip)
(SHA-256 `DA7CDBDB621255609AD0E3CAC1B0F29AD397DE0BB61668B0074D35685B449EBF`,
4,186,868 bytes). The artifact set excludes the initial failing diagnostic run
and includes the passing one-test rerun, full focused suite, ALL EditMode and
official Smoke outputs.

This is an integrated Person/Genealogy restore and continuation-parity
increment only. It does not close the remaining P12-G rejection-compatibility
matrix, systematic B–F/cross-section corruption matrix, owner-hydrator and
validator failure injection, publication-owner disposal cases, or exact P8/P9
lineage variants. Serialized-envelope parsing and its compatibility matrix
remain at the documented P12-A boundary. P12-G remains incomplete; P12-A is
still `WAIT_DEPENDENCY`; P13 is still `BLOCKED`; Phase 12 remains `OPEN`.
Unrelated ProjectSettings changes and untracked `.meta` files remain outside
this candidate.
