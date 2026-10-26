# Independent exact-tip review — CORE-F P12-E Faction Core extraction

**Verdict:** VALIDATED_CANDIDATE — PASS
**Reviewed base:** 64c005051bccea2cda6197ae03632ba106d8d1c5
**Reviewed implementation commit:** ae2a426cf4525407c5246bbd9396518e12d9b4d3
**Reviewed tree:** d6ba79dd22e0d99c869cbe31353d92b2132a7f4a
**Current Core canonical:** a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
**Candidate branch:** codex/core/CORE-F-shared-daily-v1-app-host
**Review date:** 2026-10-26

## Scope and review

I independently reviewed the complete base-to-candidate diff and confirmed the remote candidate branch still points at the exact reviewed implementation commit. The current remote Core canonical also matches the stated SHA. The candidate is a clean direct descendant of the reviewed base.

The candidate moves the existing Faction census provider, detached owner snapshot, and codec into Simulation.Core. It extracts capture-current checking behind an internal Core interface and keeps the runtime/staging-attempt bridge in a Unity-side adapter. The P12 owner-package call site is redirected through that adapter. Explicit .NET compile entries and the CoreValidation test registration are added.

The complete diff is limited to the Faction capture/source boundary, source registration, tests, and Unity metadata for those files. No ProjectSettings, unrelated user files, or untracked data are in the candidate commit. The moved codec and census provider source blobs are byte-identical to their prior Assets versions. The snapshot differs only to replace its direct Unity-runtime dependency with the internal capture-source contract. The three pre-existing .meta files for the moved provider, snapshot, and codec have identical Git blobs and preserve their GUIDs. The two newly introduced .meta GUIDs are unique.

The Core sources contain no UnityEngine/UnityEditor dependency, and the Core Unity assembly declares noEngineReferences. The Unity adapter retains the only runtime-specific freshness operation. No new public API or second owner authority is introduced; the existing public census-provider surface moves with its source, and the new capture-source interface is internal.

## Contract findings

- Capture requires the exact token.OwnerSections list instance, admitted Daily-v1 context, positive completed Core sequence, nonnegative day and mutation epoch, and one required schema-v1 witness per Faction section.
- Each witness must refer to the exact same FactionStore object and match its own cardinality plus the shared local revision. The snapshot rechecks both cardinalities, revision, and witnesses after copying; the adapter then verifies that the completed capture token remains current, including the supplied staging-attempt identity when present.
- Affiliation rows must resolve to the Faction's exact PersonStore root. Existing validation preserves IDs, historical tenure values, active-pair constraints, row counts, and one exact shared revision. Staging remains private and restores through the existing owner hydration seam.
- Codec payload/schema, deterministic encoding, and validation logic are unchanged from the moved source. Existing Faction codec coverage is now compiled by CoreValidation.
- Existing Unity snapshot assertions were preserved; edits only redirect the capture calls to the adapter. Political-binding restore and runtime-admission test changes likewise change only the invoked capture boundary. No assertion or expected result was weakened.
- P12 mutation operations, writer ordering, owner admission, revision/epoch behavior, census identities, and atomic restore publication are unchanged.

No actionable implementation or test-integrity findings remain.

## Exact-source validation reviewed

The CoreValidation manifest identifies implementation commit ae2a426cf4525407c5246bbd9396518e12d9b4d3, tree d6ba79dd22e0d99c869cbe31353d92b2132a7f4a, and a stable input inventory. Cold and warm runs each passed 850/850 with zero failed, skipped, inconclusive, or unexecuted cases. The Faction census and codec test cases were present and passed in both reports. Their recorded report hashes match the retained originals.

Official Unity manifests identify the same commit and tree. Before and after input digests match at 3c91462d3f60761ce6971fcf9f27c561ffbb8904295defa277f16558d3dd0a9f for each run:

- P12 filter: 562/562 passed; run b2dc2af69419411f99181791dc3db2e8; result XML SHA-256 333d3cc7bf6f166b7a36921d2b05d5f30dd98f4e19a89e9f9d320f4755428494.
- Full EditMode: 3271/3271 passed; run dac002c24bbb4f6f8c4b791edfe410e8; result XML SHA-256 f6a698aeddc8429bacdeb8a0c06148f87853b8de659a36cb4c26c6b9a91ff57a.
- Official Smoke: 5/5 passed; run a338bba57a144524bfcd8e5af4b37b70; result XML SHA-256 7fdd7e86a46d99968b13cfe27547e5ea242e274f16706fdac47a496ebe5dfb0c.

The official git diff --check passes. The first input-unstable Unity import run was excluded; it is not counted as validation. No tests were rerun during this review because the candidate code tree and exact validation inputs were unchanged.

## Evidence and promotion boundary

The exact XML/TRX reports and logs reviewed here remain in the owner-only protected local evidence retention; this review record does not publish raw reports or machine/workstation paths. The .NET cold/warm TRX SHA-256 values are 26381b3be7c64b0faf460488334b97f323ce0ebba433e6f757f068071679ee3c and b7a9622f4f0b9c0d494972da21ae99d2ed081895d13b702f163612b8c161b968. The protected run manifests and report hashes were checked against the retained originals.

This is an exact-tip implementation review, not canonical promotion or CORE-F acceptance. The validation evidence remains protected local evidence rather than a permanent result package in canonical history. Before any later canonical promotion, satisfy the current Validation Evidence Policy's durable evidence requirements with a privacy-safe package/record that preserves complete decision-bearing results. No publication of protected raw XML or logs is authorized by this review.
