# Independent exact-tip review — CORE-F resident-floor boundary

## Verdict

**`VALIDATED_CANDIDATE`** for this bounded selected resident-floor authority slice. No actionable implementation or test-integrity findings remain. This is not Core canonical promotion or CORE-F completion.

## Review identity

- Candidate branch: `codex/core/CORE-F-runtime-app-session-boundary-20261025`
- Candidate commit/tree: `04f998f10457b11d88b434b798a8b4308354705a` / `05c73df3a766e689d58b31c3901b69adcc9467d2`
- Exact base: `7f7632db6e1e2c451cc56a1cf34eb4f2659ce555`
- Current Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Review date: 2026-10-26

## Scope and findings

The exact nine-file base-to-candidate delta was reviewed. Core now counts living registered `PersonStore` residents for selected City owners and adds a residual supplied by the existing Unity full-roster presence query. The host residual is the former total minus those Core-counted rows, preserving existing legacy NPC and host-only Person projections. The public snapshot continues to expose the same total floor. Selection is by exact City runtime identity and residence, not physical presence; dead Persons and Persons outside the selected City graph are excluded. An already-bound owner graph rejects a different `PersonStore` instance. Missing, negative, inconsistent, or overflowing residual composition omits that City floor and retains the existing fail-closed `RepresentedResidentFloorMissing` diagnostic path.

The production runtime composition binds the exact PersonStore before selected demography runs, and the Unity adapter supplies the same runtime-owned store. No duplicate authoritative state, new writer, persistence owner, P12 section, mutation, revision, epoch, RNG draw, or ordering change is introduced. The existing ordinal City ordering and aggregate-demography policy remain in force. The Unity compatibility path retains the prior represented-resident total, including legacy NPCs.

The new Core test covers a living registered resident, a dead resident, an out-of-graph resident, and a host supplement. The Unity test verifies that one registered Person plus one legacy NPC retains a total of two while reporting a host supplement of one. Existing assertions were not weakened or removed; the interface rename only changes the meaning from total floors to residual floors.

No actionable source, scope, deterministic-ordering, persistence-compatibility, or test-integrity finding remains.

## Validation evidence reviewed

- CoreValidation run `4626475ebcbd4182a79e8fc0363b3cc3`: cold and warm **835/835 PASS**, zero failures; stable input digest `3e2943c73ea7697e3b93e1b4e9a77c8c4b8f6012b0be2ba95b41b730106346d0`; private manifest SHA-256 `7de790c5defbcfcfd3f357b5f2c01d99eda3834491045545546b749c5987e077`. The three changed Core source/test hashes match that manifest, and the retained cold/warm TRX counters and hashes match its artifact records. The runner records a Unity Library in the worktree, so this is not claimed as Library-independent or packaged portable .NET evidence; protected originals remain private.
- Full Unity EditMode run `de3cb8e8426a47c18e93c2064c3cc145`: **3271/3271 PASS**, zero failed, skipped, or inconclusive; stable input digest `7cad7612dfbd4427e2d442b9f4651f6caaec122252ac91336a2f158861eedb63`. The changed Unity runtime, adapter, test, and shared Core-stage hashes match the exact run manifest. Original XML SHA-256 `4b6851164cdf40856b70aa2af079d2935c31d6f01d731c670d29a6f85b1125e0`; private manifest SHA-256 `2471474af04a756c56e8cce3b061516dbe034cdb05bf69f016e6d274bc17fbec`.
- The approved Unity result-only package verified against the protected run manifest: archive SHA-256 `0747ddf439c2c5e7cc15b13f79c2899e34d81c61e755895c57fcb797672b9921`; public manifest SHA-256 `7101a453423a8849600e108ff50d965fadacb938a8b68ac581a152db6ac5d93e`.
- Filtered official Smoke was separately 5/5 on the earlier Unity snapshot. The complete current EditMode run and archive include those five cases; no separate current filtered Smoke run is claimed.
- `git diff --check`: PASS for the exact base-to-candidate delta.
- No private workstation paths were found in the public validation record or Unity package manifest. The two user ProjectSettings changes are absent from the candidate diff and remain untouched in the implementation worktree.

The reviewer did not rerun tests; the exact source hashes, stable run manifests, TRX counters, package integrity, and reported full-suite results were checked directly. The resident-floor change preserves the previous aggregate input by partitioning its existing represented-resident total, so it introduces no daily-output or long-horizon behavior change that would require a LongRun rerun for this slice.

## Limits

This verdict accepts only the resident-floor boundary candidate. Person-death writes remain host-owned. The full Core-owned runtime/session, DailyV1Application/TryStart, shared P12 capture and detached restoration, independent .NET host, and separate-process continuation remain outstanding. No P12 scope expansion, P12 readiness change, or CORE-F completion is claimed. Canonical promotion remains a separate workflow.