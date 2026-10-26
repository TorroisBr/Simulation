# CORE-F selected P12 status Core-owner binding — independent review

**Verdict: VALIDATED_CANDIDATE**
**Review date:** 2026-10-26

## Exact identities

- Core canonical: `codex/core/canonical` at `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Candidate base: `582d3ca0440adbcc090d87d75a5cc737a994a6d2` (tree `623b87e232ae199d897e555ced8977fbde98017e`).
- Implementation: `4dadbe126875961a2e36f0e2ee651d82f9d662b0` (tree `fc5dc22bcd10399de471ea1c61d471104c3ba7e0`).
- Final candidate: `64a63a90beff495ccefdee135f1f90e1b6c92399`, published at `origin/codex/core/CORE-F-runtime-app-session-boundary-20261025`.
- The canonical tip is an ancestor of the candidate base. The implementation is the parent of the final candidate; the final successor adds only `docs/validation/CORE_F_P12ActionStatusCoreOwnerBinding_4dadbe12/VALIDATION.md`.

## Review scope and verdict

Reviewed the complete base-to-final-candidate diff: the P12 Crime/Justice census provider and invalidation adapter, the Core mutation coordinator and owner-section contract, the Core coordinator test, and the validation record.

For selected NPCs, the P12 mutation binding uses the exact `DailyV1NpcRuntimeOwners` composite and reads its persisted `CrimeJusticeRevision`. That revision is carried through the existing P12-D owner hydration path. The status section ID remains byte-identical: the new Core helper applies the same status prefix and RuntimeId composition as the previous Unity helper.

The selected path fails closed if the runtime projection and Core owner RuntimeIds differ. When no selected Core owner exists, the previous `NpcRuntime` identity and legacy revision binding remain intact. Existing P12 census witness identity and wire data are unchanged.

The existing callbacks still enforce exact projection preflight and mutation admission before the Core leaf write; after commit they refresh the projection, advance the persisted owner revision, and notify the P12 coordinator once for that leaf. Action ordering, no-op behavior, mutation guards, operation scopes, and gameplay semantics are unchanged. The focused fixture supports a detached Core owner and does not require runtime-graph publication.

No actionable findings. The two unrelated ProjectSettings edits observed in the candidate worktree are not in the candidate commits and were left untouched.

## Validation evidence

Validation evidence is retained privately; the following IDs, results, and hashes were checked against the committed validation record and protected artifacts.

- CoreValidation `3638c46a560943cb9aedbec26070c3dc`: cold and warm **823/823**, zero failures. Stable input digest `12a6479591bac1457e8b1830d0469e0edace1c2d71de56b50acd58a6a6916033`; manifest SHA-256 `6656727de0f4c5d9a4b34694bb69cadcc79863e34e373fb630ce897779e02f54`. Cold and warm TRX SHA-256: `25c66fc899080ae288883c52cb51f8e79d29647da2e81db1b1f3f7d12c5fa1df` and `22acd38710d7cddc6f5ea5ed2ff8c23d3eee15a1ca9c08928997259a80f6a480`.
- Focused Unity EditMode `a51dd97e085643f8b3aab19f86d8fe72`: **1/1**. Manifest SHA-256 `2c73e28463b948dacafb3d31d281ed8fd82d2062342fe8f672e803c6b4678b75`; XML SHA-256 `a54c299ec5caf694135ac61cac55f39f5fdb119a79e6ef4c6ba2009fd76dd011`.
- Full Unity EditMode `4bcbe905c7ff432fb4f71bd17a41a828`: **3269/3269**, zero failures, skips, or inconclusive tests. Manifest SHA-256 `e2890b3d2b40c84d8a0bcfeb4a89bca9e356567c82a8963cb7bc2b4e5a80019e`; XML SHA-256 `8128fec65964e36fc26f01739550d28e328d37544b899d028ab7eb710e3d96a9`.
- Official Smoke `fc77c1246c934a4fbc6484a3de775579`: **5/5**, zero failures. Manifest SHA-256 `9431295b58a61e2fc0ba5382afc0339a12c6b6f3ab0eb5139acefcf3a93a7b5e`; XML SHA-256 `3c1859201f908f3cf4a10e51cd34c280826b9493380adc0a173ba8fd915b4fd3`.
- Unity input-before/input-after digests match at `8f773d60ee745707f7ab48b0070cc64bf3d9109c7e160dbecd36ecf42402014b`. All four changed production-source hashes in the Unity manifests match the candidate. CoreValidation was captured with its implementation inputs as worktree edits atop the base; the affected Core source and test hashes match the implementation commit.
- `git diff --check`: PASS. Tests were not rerun during review.

## Integration boundary

This is an intermediate CORE-F candidate only. It does not complete CORE-F and is not a canonical promotion. Canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
