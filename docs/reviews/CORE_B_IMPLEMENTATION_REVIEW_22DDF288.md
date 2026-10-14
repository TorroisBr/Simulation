# CORE-B Implementation Review — 22ddf288

## Verdict

**NEEDS_CHANGES** — exact-tip review of candidate `22ddf288789dbdff5e06640057042ef87e4a8932`, tree `c5b667b251837b0977db55f6ee208755d1074f9b`, against Core canonical base `c95752846a0ce0e1ebf985c4d79e0e17b02bb7af`.

This is an independent review. The candidate's prior review `9fb61a7b7669069c6a9c75310013719e643877b9` was superseded/qualified and is not relied on. Applicable design: `d20c2352887184ccc964d7f9586407d00a727d32`; original design review: `96a7df8c2ebad6cf5ce3bfe2bde140f98c6e46dc`; current-baseline design revalidation: `3eb18a6a09ee430e15b2d479c02031b221443fd8`. Architecture baseline cited by the design/revalidation: `32358d2a0faf511e098c4f295a5132a4bfb8cfb2`.

## Findings

1. **The required test-case identity mapping is absent.** Accepted design §4 requires the validation record to preserve an explicit old full-name → new full-name/case mapping for every retained case. The candidate's `docs/validation/COREB_DeterministicTemporal/VALIDATION.md` and `docs/CORE_STATE.md` report aggregate counts and run identities, but do not provide that mapping. The Unity result manifest lists current results; it does not map the old full names to their new full names. Add the complete mapping to the validation record before this checkpoint can pass review.

2. **The required durable per-case .NET evidence is absent.** Accepted design §7 requires the original per-case report bundle and privacy-checked evidence under the CORE-A evidence policy. The candidate records that CoreValidation cold/warm each passed 97/97 and gives run ID `83e8e0475f154ff6a531b1f5cf06550d`, input digest `d955f8a7084878a408312b91d27071d60e5fd9fbb88ef92055c9b7facfcace24`, and TRX hashes `08b6a10e2fc0a08e0eefdfbab52fd938183a2b96bf744170bbb432ae5cd1c88a` and `fb025dd74e851b24a1ddf8065fc7b45511029dd7869f33a81a94bb75e40e2380`; it also says the original reports/logs remain private. No durable .NET TRX bundle or validated normalized per-case result package for this run is present in the candidate. The Unity ALL/Smoke packages are Unity evidence and cannot substantiate these .NET results. The available CORE-A TRX package is bound to different source/test inputs and run identities, so it does not qualify for CORE-B. Hashes and aggregate summaries alone do not preserve the required per-case outcomes. A privacy-reviewed durable bundle for this exact CoreValidation run is still required.

## Reviewed evidence and boundaries

- Remote candidate ref resolved to the exact commit/tree above; the candidate is based on the current canonical base and `git diff --check c95752846a0ce0e1ebf985c4d79e0e17b02bb7af 22ddf288789dbdff5e06640057042ef87e4a8932` passed.
- The full implementation/source scope remains the reviewed CORE-B extraction. Production and test C# files are unchanged from code tree `906d2b8243c7f2b626b0edcc484a8fb83a826249`; this successor adds the four generated folder `.meta` files and updates documentation/evidence. The four committed folder metadata blobs match their recorded manifest sizes and hashes.
- The refreshed Unity ALL package is internally consistent: run `48298dd5a6e542f2a8b644730f6ade28`, 3,085/3,085; archive SHA-256 `c6b46224c4e504e965fa5c484ab27d4cda3ed5b2afeb44424bb561e4f3cb05d8`; committed LF manifest SHA-256 `e52f62c49d331ad41051652aa9abce2aea681ad11e232eab616716627e9c0069`; normalized XML SHA-256 `dabd1ec4e4f4667f6ac7f40b1ba04ea3a83835a4228572685f91346773b62d11`. The validation record reports official Smoke 5/5 and correctly explains why its filtered report is not packaged.
- Three unrelated, user-protected `.meta` files remain unmodified and uncommitted. Their hashes are recorded in the validation manifest and match the local inputs. They are external dirty inputs rather than part of the candidate commit; preserve them and do not stage them. This is a reproducibility boundary, not a substitute for the missing .NET result bundle.
- No Unity or .NET tests were rerun for this review. This review does not authorize canonical promotion or claim CORE-B readiness.

## Required follow-up

Add the complete §4 old-to-new test-case mapping and publish a privacy-reviewed durable per-case .NET evidence bundle satisfying design §7 and the validation evidence policy. Then request a fresh exact-tip review of the updated candidate.