# P12-G restored-boundary admission — independent implementation review

**Verdict:** `VALIDATED_CANDIDATE` — exact-tip implementation review passed within the bounded admission contract.

**P12 canonical base/current tip:** `678b01dc9c9dddf05cbd0a64033afc7b1ed1615b`

**Architecture baseline:** `47eff220c7ce00f6e7c759bdc2b76780bb46f628`

**Code candidate:** `6f0dde51b1100c201346bb8a12c15f51952a5848`

**Code `Assets` tree:** `d7b179d40b8a0e70ade5ff8592028448bf7a7192`

**Validation evidence tip:** `f9f2b1df8adf5a4911617bf389e159d16018ff95`

**Reviewer:** `actor_choice_impl_review`, independent of the implementation author.

## Review result

The full base-to-code diff is limited to `SimulationRuntime.cs`, `SimulationRuntimeAdmissionTests.cs`, and the current-base design revalidation. The separate validation tip adds test artifacts and their manifest; it leaves the code tree unchanged. The exact Assets tree matches the validation manifest. No unrelated source, ProjectSettings, or `.meta` files are part of the candidate.

`TryAdmitRestoredDailyBoundary` follows the revalidated contract. It checks the selected Daily-v1 profile and owner thread, exact `WorldId` reference and constructor-captured initial day, a nonnegative day, positive preserved completed-core sequence, and absence of an existing token, prior completed sequence, or published factual read. It rejects active advance/runtime contexts and requires the mutation guard and Daily-v1 authorities. The existing protocol snapshot API enforces sealed owner/provider/operation inventories, bound-thread quiescence, current owner witnesses, stable shared epoch during capture, and an ordinally ordered copied owner vector. After capture, the method repeats the owner-thread, operation, health, publication, identity, day, and no-existing-boundary checks before creating the token.

The new token is bound to the candidate runtime identity, admission context, configuration, calendar, composition profile, exact WorldId, preserved day and sequence, captured shared epoch and owner vector, and `RestoredContinuation` provenance. Admission does not read `CurrentDay`, advance time, increment the preserved sequence, or accept/transfer the source token. The ordinary successful advance path replaces it with a `CompletedAdvance` token and increments from the preserved sequence. The provenance check in token validation accepts only those two defined boundary kinds.

The tests cover successful admission into a fresh runtime, candidate/source token separation, continued source-token validity, cross-runtime rejection, sequence preservation and next-advance behavior, wrong identity/day and invalid sequence, prior publication or advance, active operation, wrong thread, and unsupported profile. Failed cases assert that admission does not partially publish a token or alter sequence/publication state. No blocking implementation defect or scope expansion was found.

## Validation evidence

Retained validation is tied to the exact `Assets` tree above. The reviewer independently verified the manifest hashes against the files at validation tip `f9f2b1d`; the XMLs report passing counts with zero failed, skipped, or inconclusive tests:

- `SimulationRuntimeAdmissionTests`: 75/75. XML `E16C744FEC2271EB02D53415915B773BD638B003797B275C1C9BFF5CDF22A052`; compressed log `4B5ED7A496B65D3FE5CD89B7F10E3C77D659C7F6AB50A7C3DC6D754896292FEF`.
- ALL EditMode: 2738/2738. XML `D2D24950AE6515B03465DD8246A8E19A0E7A364D06005923C67FDA566832681A`; compressed log `30D87F0742202F114EDEC3A80365445FAA6F9C41910B672F4308FF6063B048A1`.
- Official Smoke: 5/5. XML `5A2CC3975CC3EE27816AA70F92CCD42EEE5F2BF8AA87ED7C014457F4D821A05E`; compressed log `1511E320BD4332BB7E103F23F63BAED7AEFC8CFB7506B56F410B93FEFC8F9EBF`.
- `SimulationBootstrapCompositionTests`: 26/26. XML `660A6A15F0D5BE42BD51021944A7E4D35FD5F92988E1F8D8C66AB1E7FE31814C`; compressed log `CD06F0CBC61A87FA1FE4D120B5B2EB0DE526E89F675A0AC377CF2CFE073309E4`.
- Base-to-code `git diff --check`: PASS.

No tests were rerun during this independent review; these results are the retained exact-tree evidence.

## Scope and remaining limits

This review supports the bounded restored-boundary admission capability only. It does not implement envelope parsing, complete owner hydration, staged target-owner exact-zero validation, active-session publication/swap, failure atomicity across whole-graph restoration, no-replay proof, or continuation parity. The focused tests do not claim those behaviors. P12-G and P12-A remain `WAIT_DEPENDENCY`, P13 remains `BLOCKED`, and Phase 12 remains open. This review does not promote the candidate to canonical or close a phase.
