# CORE-F selected P12 action-status Core-owner binding

Date: 2026-10-26

## Candidate identity

- Base: `582d3ca0440adbcc090d87d75a5cc737a994a6d2` (tree `623b87e232ae199d897e555ced8977fbde98017e`).
- Implementation: `4dadbe126875961a2e36f0e2ee651d82f9d662b0` (tree `fc5dc22bcd10399de471ea1c61d471104c3ba7e0`).
- Changed source/test files: the P12 status section helper/provider, Core owner binding/coordinator, Unity P12 invalidation adapter, and Core coordinator tests.

## Scope

For selected NPCs, Crime/Justice status mutation bindings now use the exact
`DailyV1NpcRuntimeOwners` composite as the P12 operation identity and read its
persisted `CrimeJusticeRevision`. The canonical status section ID is derived
from the Core owner-section contract and remains byte-identical to the prior
Unity provider ID. Legacy NPCs without a selected Core owner retain the
existing `NpcRuntime` binding path. The existing P12 census witness identity,
wire data, operation ordering, projection preflight, projection refresh, and
notification timing are unchanged. No action semantics or P12 profile scope
changed.

## Validation

- CoreValidation run `3638c46a560943cb9aedbec26070c3dc`: **823/823 cold and 823/823 warm**, zero failures. Input digest `12a6479591bac1457e8b1830d0469e0edace1c2d71de56b50acd58a6a6916033`; protected manifest SHA-256 `6656727de0f4c5d9a4b34694bb69cadcc79863e34e373fb630ce897779e02f54`. Cold and warm TRX SHA-256: `25c66fc899080ae288883c52cb51f8e79d29647da2e81db1b1f3f7d12c5fa1df` and `22acd38710d7cddc6f5ea5ed2ff8c23d3eee15a1ca9c08928997259a80f6a480`.
- Focused restored Core-local-topology/P12 regression run `a51dd97e085643f8b3aab19f86d8fe72`: **1/1 passed** after removing a graph-publication prerequisite that the existing detached-owner fixture does not require. Protected manifest SHA-256 `2c73e28463b948dacafb3d31d281ed8fd82d2062342fe8f672e803c6b4678b75`.
- Full Unity EditMode run `4bcbe905c7ff432fb4f71bd17a41a828`: **3269/3269 passed**, zero failures, skips, or inconclusive tests; input stable. XML SHA-256 `8128fec65964e36fc26f01739550d28e328d37544b899d028ab7eb710e3d96a9`; protected manifest SHA-256 `e2890b3d2b40c84d8a0bcfeb4a89bca9e356567c82a8963cb7bc2b4e5a80019e`.
- Official Smoke filter run `fc77c1246c934a4fbc6484a3de775579`: **5/5 passed**, zero failures; input stable. XML SHA-256 `3c1859201f908f3cf4a10e51cd34c280826b9493380adc0a173ba8fd915b4fd3`; protected manifest SHA-256 `9431295b58a61e2fc0ba5382afc0339a12c6b6f3ab0eb5139acefcf3a93a7b5e`.
- Unity's four changed production-source hashes match the final candidate source files. The later Core-only test assertion is covered by the final CoreValidation run and is not part of the Unity test assembly.
- `git diff --check`: PASS.

An earlier full EditMode attempt `e1a29f59bdbc4f3bab94965c613d61d3` had one failure because an additional graph-publication check rejected a valid detached owner fixture. That check was removed; the exact failing test and the subsequent full suite passed. Original failure evidence remains preserved.

Raw Unity reports/logs and CoreValidation originals remain in protected local evidence storage. This record publishes hashes and result accounting only; it contains no workstation paths or raw private metadata. No claim of full CORE-F completion or canonical promotion is made.
