# CORE-F P12-D Genealogy Core Codec — Validation

**Status:** validated candidate implementation; no Core canonical promotion. This slice moves the existing P12-D Genealogy census provider, immutable snapshot wrapper, and wire codec into the shared `Simulation.Core` assembly. The existing capture/stage orchestration and profile semantics are unchanged.

## Source identity

- Repository: `TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-p12d-core-hydration`
- Tested implementation commit: `05410e2653d64dc36a8b8553c8c91906b7ddcf33`
- Tested tree: `6e0c55c46a5a85bf1aba2baadc89accdce8cfe2e`
- Base: `ad0faf05751c9dcc038dc4c4f21c9450423dd6c4`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

The moved source and `.meta` files preserve their existing GUIDs. The candidate changes only those three source files, the explicit Core/test compile lists, and focused Core tests. Existing `ProjectSettings` edits, untracked user data, and protected evidence were not staged or modified.

## CoreValidation

Official `Tools/CoreValidation/Invoke-CoreValidation.ps1` run `a35e875e5cd648c6a05e0dff5140cfbb` passed:

- Cold CoreValidation: **790/790**.
- Warm CoreValidation: **790/790**.
- Locked build, CalendarDemo cold/warm, and Lab build/cold/warm: PASS.
- All 434 source/test and generated-dependency inputs were rehashed against the committed candidate; **0 mismatches**. Input digest: `c55738caa17007b63a62c771967bfbf711d444271b11e2f3be97de8a8ab5a855`.
- Protected private manifest SHA-256: `c5eb7891bec03521963ad5fad7ed2e4fbc56c02480b1279cad242c3a7be68852`.
- Original cold/warm TRX SHA-256: `5b529e602f0357adeefcee6074e59e180ffde9a30dbdee392dcb272ef2345995b` and `c0b7c7b39663374cc38fc053190f36a562020576a176e81f5306f5337b7f9ba5`. Original report bytes and the private run manifest remain in owner-controlled E: retention; no public normalized .NET TRX package is claimed.
- A Unity `Library` was present. Library-independent .NET execution is not claimed.

The runner started while the candidate changes were in the working tree based on the stated base commit. After commit, the complete recorded input inventory was compared with the candidate checkout and matched exactly.

## Unity validation

Official Unity validation used Unity **6000.3.9f1** and the unchanged approved runner. The input snapshot was stable before/after each run; all **1,912** recorded input hashes were rechecked against the current checkout after commit, with **0 mismatches**. Input digest: `0c7ba6dbf4b7fde715bd23f58efbdf35eba62ce9b0362b48da892050f65193ec`.

- Focused `P12GPoliticalBindingRestoreTests`, run `61d30757f947413ca02830ebc113d659`: **15/15 PASS**, original XML SHA-256 `185ae0215ac0a45e52ec3a29f8cac6015a3a4f02e09125199e26f0bedb36e14d`.
- Full EditMode, run `ce2bb55a6b064c68a4aef0b9313d5509`: **3264/3264 PASS**, zero failed/skipped/inconclusive. Original XML SHA-256 `617bbd8bb29b49bdddfb3ee10a2e801042031ae28df854197b8b989abe4cd711`.
- Official Smoke (`TestFilter Smoke`), run `bb0fab4227e141c29b471a152139d94b`: **5/5 PASS**, zero failed/skipped/inconclusive. Original XML SHA-256 `d5fab0d547de3197dbe8737a87418cc26d0516b7777c1b3f866054650e271eb2`.

The approved packager produced and independently verified the result-only full EditMode package:

- Archive: `coref-p12d-genealogy-core-codec-20261025.zip`, 172,346 bytes, SHA-256 `c07539ba037e62215c207d183e919395de458bdcb28d2b2f8059db06c8aab622`.
- Public manifest SHA-256: `778d69d069a794ab6293a2f475b55b3eb5bc5616e52e9f2fca2c7516c085f5a1`.
- Packer verification against the protected original run passed. The public manifest has no local workstation path; the only drive-prefix scan match is the `https://` repository URL.

The recorded Unity inventory included existing user-edited ProjectSettings inputs; they remained uncommitted and unchanged. No daily-loop change was made, so no LongRun gate applies. `git diff --check` passed on the implementation commit.

## Scope and evidence limits

The Core census/provider and genealogy image codec are now compiled from the shared Core package. The owner capture/staging adapters remain as they were; this does not move the entire image-owner decoder or P12-G coordinator to Core. This slice does not complete P12-D, detached restoration, P12-A, the .NET host, or CORE-F. No canonical promotion or Phase closure is implied.
