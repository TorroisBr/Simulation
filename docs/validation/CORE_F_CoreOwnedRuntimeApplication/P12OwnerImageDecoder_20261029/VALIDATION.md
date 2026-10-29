# P12 owner image decoder source-closure validation

## Identity

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Base: `cbc9cd6f7d5a4476da53fe0342b3d7820bc4d2f9`
- Candidate: `eabc9457a78d5838d0c2cb5822dbc6d48518b1b8`
- Candidate tree: `dfeb97fa211a07cee2f728ae5b84ce14a6f8ce66`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

## Scope

Moved the existing P12-A typed image decoder and the P12 receipt, Justice, and CrimeSocial codecs into the shared Core source set, preserving their Unity metadata GUIDs. Updated only their section/schema constant references to existing Core-owned contract constants and added the existing ExplorableSite wire section identifier to the shared census contract. No codec rules, encoded values, owner identity, cardinality, or alias behavior changed.

This is a source-ownership migration for shared detached-image decode. It does not complete the P12-G coordinator migration, add `DailyV1Application.TryStart`, establish detached .NET hosting, or complete CORE-F.

## Validation evidence

- Official CoreValidation: cold **926/926 PASS**, warm **926/926 PASS**; run `c073b3d352fc4b7784edb906b13436e8`; protected manifest SHA-256 `03EBC9AA920672E3C9E7CD2A28D6089BE6B96E94F1BBD38AA7B2EFF170935C08`.
- Official Unity EditMode: **3279/3279 PASS**, zero failed, skipped, or inconclusive; run `83225dcfeaee43cc94b1fba24f8fd46f`; editor `6000.3.9f1`; protected manifest is the authority for the exact full input inventory.
- The Unity result-only package is `UnityEvidence/coref-p12-owner-image-decoder-editmode-20261029.zip` with manifest `UnityEvidence/coref-p12-owner-image-decoder-editmode-20261029.manifest.json`. Archive SHA-256: `739c7a34a7c630ed3ae3f402981ab5aa6172354970c7de80e369860e4a6efa99`; manifest SHA-256: `0D58770704AE53BD73C031202CB741612303C3FF7BEB6EE4E3FD77D0FD14F250`. The archive size and digest match its manifest.
- Exact-input review confirmed all Unity package input hashes and all CoreValidation input hashes match the candidate source; the moved source and project compile-list hashes are unchanged at the candidate.
- `git diff --check`: PASS.

CoreValidation original TRX/logs and Unity original XML/logs remain in the protected E-local evidence roots; they are not included in this public result-only package. The normalized Unity XML package preserves the complete passing test results without the removed output fields described by its manifest.
