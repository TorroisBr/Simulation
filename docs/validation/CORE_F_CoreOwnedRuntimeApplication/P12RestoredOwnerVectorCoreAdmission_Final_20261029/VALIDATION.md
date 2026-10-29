# P12-G Restored Owner-Vector Core Admission — Validation

This record covers the bounded owner-vector comparison and application staging-authority slice. It does not close CORE-F or Phase 12.

- Candidate implementation: `955f3ee066e8377860b7a0d2bb61bc9d67e92b14`
- Candidate tree: `cf679c0bc673acc8a0bbf733f0f9e786c04974db`
- Base: `9c197a7d86d0ac563bff77457ac8d0771743909e`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

## Results

- Official CoreValidation run `c5b5275ab3e54f14bec6a500a728d180`: **928/928 cold and 928/928 warm**, zero failures. The protected runner manifest SHA-256 is `4d6eeebaf582427b8dac4123e0c370c28fb0100e83a48e2ae03630f54ba604d8`. Original reports and logs remain in protected local retention.
- Official Unity EditMode run `1ceeafc4855e45b284d8d313886d9824`: **3279/3279**, zero failed/skipped. Its 1,972-file input snapshot was stable before and after execution, digest `9d3615c4d166fffff21d7df00ba00b0016180a0b9cb3c7a4f113937d82c76999`.
- Official Smoke-filter run `be4f7e4d24b04e789b59a2fc45cb0864`: **5/5**, zero failed/skipped, same stable input digest. All five Smoke cases are also present in the full EditMode result package.
- `git diff --check`: PASS.

The privacy-safe full EditMode bundle is [here](UnityEvidence/coref-p12-restored-owner-vector-admission-final-20261029.manifest.json) with [archive](UnityEvidence/coref-p12-restored-owner-vector-admission-final-20261029.zip). The archive SHA-256 is `c3e43d5767a38b4a93910e7a5c3d5eef3f5efc673778a302ff4076d955d95812`; the official verifier returned `VALID: runs=1 entries=1` against the original run manifest.

The original CoreValidation TRX and logs, and the separate filtered Smoke XML/log, remain protected. No raw report or log is included in this public record. A public DotNetTrx bundle was not produced for this run because the evidence adapter requires a verifiable `dotnet-trx-source-context-v1`; its runner manifest alone is not that context. No packaging pass is claimed for the CoreValidation reports.