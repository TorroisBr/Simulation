# CORE-F Cross-City BuyGoods Projectionless Dispatch

Status: validated candidate slice; this does not complete CORE-F or promote Core canonical.

## Identity and scope

- Base: `ef2007e274b47ed62b93d45d974802abd6d64c72`.
- Implementation commit: `954e265449a2e76e4c9c9d67395ff9dccb86df3e`.
- Candidate tip: `de566df204c571115592cb889fd767f60ed8b9cc`.
- Candidate tree: `896dcd9024e30497fc4f0a785aa18db7f83b4296`.
- Current Core canonical at validation: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Scope: reconstruct the exact Unity compatibility projection for an otherwise projectionless, supported cross-city `BuyGoods` Core slot. Existing Core eligibility, action execution, owner mutation, and P12 admission remain authoritative.

## Validation

- Focused cross-city dispatch test: 1/1 passed in official EditMode run `8390d710e548485ba1397c9c7d0f0a14`. This focused run preceded a later path-length-only test-fixture correction; the final full run below covers the exact candidate tip.
- Full official Unity EditMode: run `158d3c5528dc4b56a96a50282e615f93`, 3,294/3,294 passed, 0 failed, 0 skipped, 0 inconclusive; stable inputs. The exact report is in the result-only package below.
- Official Smoke: run `91a79c969680454d93a5e90f61808242`, 5/5 passed, 0 failed, 0 skipped, 0 inconclusive; stable inputs. Its input digest matches the full EditMode run. The complete EditMode report includes the Smoke tests; the separate filtered report is retained privately and is not part of the result-only package.
- Official package verification: passed against the exact full-run manifest. Package: `coref-cross-city-buy-projection-de566df2.zip`; archive SHA-256 `80d1acbe7c19f4cd700a3c16ee7234110de4e94c5cbc5f192020eba817004522`. Public manifest SHA-256 `89163de7b9b8c1dd779eb039ad066d94fc197e230f7f51b7272d0547c8c3fb35`.
- The package contains only the complete EditMode result XML and its result-only manifest; the official verifier confirmed it matches the protected original run manifest. A filtered Smoke XML was not separately packaged because the strict packager rejects its incomplete suite-level aggregate. The separately recorded 5/5 result remains supplementary; the full packaged report contains those Smoke cases.
- Artifact scan found no local user-directory paths or credential-like strings in the public manifest or packaged XML.
- `git diff --check`: passed before evidence capture; rerun at the candidate review boundary.

The final candidate also shortens one P12 continuation test's temporary fixture directory name to keep generated paths below the Windows path limit. This changes test setup only; assertions and production behavior are unchanged.

## Preservation and limits

Raw Unity reports and private run manifests remain in the local protected evidence area and are not committed. No production CoreValidation rerun is claimed for this Unity-adapter-only slice. User-owned ProjectSettings edits and local work artifacts remain unstaged and outside the candidate commits. An earlier invalid zero-test PlayMode invocation is excluded from the results; the correct Smoke gate is the EditMode-filtered official run above.

This candidate remains separate from `codex/core/canonical`. It does not claim the complete shared runtime, application/restore/host contract, or CORE-F acceptance.
