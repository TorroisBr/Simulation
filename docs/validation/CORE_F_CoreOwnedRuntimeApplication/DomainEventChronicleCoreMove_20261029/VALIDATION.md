# Domain events and chronicle Core move — validation

Candidate source commit: 5a97b913900fb76403f9506b0dd3118411ec140c
Candidate source tree: 1e4e004155cd5d4acdbd9a1a1dd5d2a33fb66547
Base: 8c92173221ffdd30d8192338a22cd6f3a2793c7a

## Exact-source results

- Official CoreValidation: cold 928/928 PASS; warm 928/928 PASS.
- Runner source identity: exact candidate commit/tree above; inputs stable; clean source snapshot; no Unity Library present during CoreValidation.
- Selected CLR: .NET 8.0.28 for both test runs.
- Official Unity EditMode: 3279/3279 PASS, exact candidate commit/tree.
- Official Unity Smoke filter: 5/5 PASS, exact candidate commit/tree.
- git diff --check: PASS.
- Independent exact-tip code review: PASS; no actionable findings.

## Public and private evidence

The exact-tip Unity EditMode run is packaged at coref-domain-events-chronicle-editmode-3279-5a97b913.manifest.json and .zip. The repository packager's private original comparison and Verify passed for one run/one report entry. Archive SHA-256: e6b7bcc6ac333432cbb64a3de6af21bccd1fccd7a09da1c0949b638f21e397aa.

The exact Smoke original remains in protected local evidence. Its filtered NUnit XML was rejected by the package tool because its suite aggregate count does not match descendant counts; no source or result was rewritten. The full EditMode package independently covers the complete suite.

The CoreValidation original manifest, cold/warm TRX reports, logs, and runtime-selection traces remain protected locally. The current DotNetTrx adapter is a closed profile for 11 logs and 3 generated project-assets records, while this runner version's exact run records 15 logs and 4 project-assets records (the extra records are the Lab gates). Packaging rejected that incompatible shape. The originals remain unchanged and private; this is an evidence-tool profile mismatch, not a test failure. No normalized Core package or original-comparison claim is made.

This checkpoint records only the validated source-boundary change. It does not complete or promote CORE-F.
