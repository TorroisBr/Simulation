# Validation — Core-owned session projection publication

Candidate code identity: commit 0c5e23772b39d55479f803b80c69e95ff6234836, tree 9126340c24996b8ba52d3dbcecdcfd8e0930e7e8, parent a1b4cf23ee8ab9aeb117db7faa5b0ba0576c96bf. Core canonical was a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea.

This checkpoint moves host-projection registration and publication into the Core application gate. It changes no authoritative simulation behavior and does not complete the CORE-F application/host contract.

## CoreValidation

The official CoreValidation runner completed against the exact candidate commit in a detached validation checkout before Unity created a Library there.

- Run: 39749c7e2f1645f6a59b09d1f6972595
- Overall status: Passed; source commit matches this candidate.
- Cold: 993/993 passed, zero failed.
- Warm: 993/993 passed, zero failed.
- Cold and warm build, demo, and lab gates passed.
- Focused session-gate tests passed in both runs:
  - ApplicationPublishesGenesisAndExactHostProjectionTogether
  - CoreRestoreCommitRechecksCandidateCensusBeforeInitialPublication
  - CoreRestoreReplacementAcceptsRestoredSourceTokenAndRejectsStaleSourceCensus
- The runner reported LibraryAbsent=True.

The protected native runner manifest SHA-256 is a8f79f9ddbf57c31ec2cd6ddeeb10c8048c3c681c0e7deb731d8a0e81ff308f1. Original cold and warm TRX SHA-256 values are bdb4c37df8f5e6b85ee468c9803142777624671e563f0bce77f0e2f04654e637 and ede4f3557c2a26228ec5c2a9fb1ce8666a7b1029524c792c8e07371dd1d88844. They remain in protected local evidence unchanged.

No public .NET TRX package is claimed. This run emitted simulation-core-validation/v1, while the approved DotNetTrx packager requires a separately captured dotnet-trx-source-context-v1 context. The adapter rejected the native manifest as unsupported; no context was fabricated and no original was rewritten.

## Unity EditMode

The official Unity runner completed against the exact candidate commit and tree.

- Unity: 6000.3.9f1
- Run: b9c71d28d5d74215b8105e18d12e77d4
- Result: 3296/3296 passed, zero failed, skipped, or inconclusive.
- inputStable=True before and after the run.
- Captured input digest: 886fe6c031643bb50a5f876d5fc397d04aeff77d7796bbe1348e83657b67a182

The successful isolated validation checkout used stable local overlays of the three protected ProjectSettings files from the implementation checkout. A first clean-import attempt rewrote two defaults and was excluded because inputStable was false. The successful run records the stable overlay before and after execution. These local files were not staged or committed; the user’s originals remain in the implementation checkout.

The official evidence packager created and verified the full EditMode package:

- Manifest: coref-session-projection-0c5e2377.manifest.json
- Manifest SHA-256: 59c34510650016a99e52b1efe9ef925063c10fedc44f607b77c2d982950cf0ae
- Archive: coref-session-projection-0c5e2377.zip
- Archive SHA-256: fb1a98542a1bf4cd1be626f47692394c1a48b48183f46272f8626239560eeea3
- Archive size: 174229 bytes
- Official Verify: VALID, one run and one normalized XML entry.

## Official Smoke filter

A separate official Smoke-filter run also passed 5/5 with stable inputs and the same input digest. Its five exact case identities are present as Passed cases in the complete 3296-case EditMode report above.

The packager rejected a standalone package for the filtered report because Unity’s filtered XML has a parent-suite testcasecount inconsistent with its descendants. The filtered original was not altered or reinterpreted. No standalone Smoke package is claimed; the complete packaged EditMode report contains all five Smoke cases with passing outcomes.
