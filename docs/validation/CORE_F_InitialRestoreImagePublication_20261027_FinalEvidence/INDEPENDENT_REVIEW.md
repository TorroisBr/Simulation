# Independent Exact-Tip Review — CORE-F Initial Restore-Image Publication

**Verdict: VALIDATED_CANDIDATE**

## Reviewed identities

- Repository: https://github.com/TorroisBr/Simulation
- Candidate branch: codex/core/CORE-F-initial-publication-admission
- Candidate tip: 615b744264d0f7fe40469a3fa94d927eaf75e3fe
- Candidate tree: 5a6f8bc93d6e7ff279fb3d3f1531cad1ae8dfa9c
- Exact review base: 82c3b8feb42a0d96bdf6734c2c9dfbb0a90dcd18
- Tested implementation and validation-tool tip: bf4b2ac9fe52c3a4abd25a02d83c4f0a2c9f761c
- Tested tree: c90c9868c86f21411fc3ef832c54e7dee07a52e5
- Current Core canonical: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea

The remote candidate and canonical refs were refreshed and matched these identities. The candidate tip is a direct child of the tested code/tool tip. The complete diff from the exact review base contains the initial image authorization, corrected fixture reflection signature, zero-test Unity runner rejection, and validation records/packages. The tested source/tool tree is unchanged between bf4b2ac and the candidate evidence tip. The candidate diff contains no ProjectSettings changes, unrelated metadata, or raw protected validation reports.

## Implementation and test review

The Core load path retains initialContinuationLoadedImage only when the same application successfully loads a non-null image from its previously inspected present slot. TryAuthorizeInitialRestoredCandidate requires that exact image reference, the observed-present/loaded state, the bootstrap owner thread, an unused application, a restored descendant candidate, and a healthy idle completed-boundary runtime. Core initial publication additionally requires the exact candidate reference authorized by that application and consumes the image/candidate authorization only when publication succeeds. The direct Core initial-restore path without this proof now fails without publishing a session.

The selected Unity Daily-v1 startup passes the Core-loaded image instance to the detached restore coordinator and passes that same instance with the returned candidate into initial publication. The candidate runtime boundary is bound before authorization; the projection is registered under the same session gate as Core publication and is removed if publication fails. Replacement restoration continues through its existing reservation path. The restored-session reflection fixture now resolves the four-parameter method and retains its successful replacement assertion.

Existing direct-initial-restore expectations were tightened to require the application-loaded image authorization; no gameplay or replacement assertions were weakened. The Core test setup for a synthetic initial session now publishes through the existing genesis path after an explicit NoSave proof. Full exact-source Unity validation covers the supported initial restore path.

Bounded trust-path note: the internal Core authorization method receives the image and candidate as separate arguments; it checks that the image is the exact image loaded by that application and records the exact candidate reference, while the trusted Unity adapter establishes their derivation relationship by passing the same immutable image through the restore coordinator and publication call. This review confirms that sole production call path. It does not claim protection from manually forged internal calls or establish a new security boundary.

The Unity runner now treats coherent zero-test XML as NoTestsDiscovered, never Passed and never an operational retry. The added runner self-test preserves syntactic validity while asserting the gate rejects zero cases. I independently ran the exact candidate runner self-tests: 34 passed, zero failed, including ZeroDiscoveredCasesAreNotAValidationPass.

## Validation and evidence

- Protected official CoreValidation run 3050f1233507460da962638a83d88172 recorded the exact tested commit/tree, stable source and generated-dependency inputs, and 902/902 cold plus 902/902 warm. I inspected its protected manifest and original TRX reports in place: both report hashes match the manifest; both contain 902 passed, zero failed/skipped/aborted/inconclusive. The modified Core test source hash and byte count match that protected input manifest. The raw TRX, logs, and private storage paths were not copied or added to Git. The validation note correctly claims no public DotNetTrx package because the required dotnet-trx-source-context-v1 is absent.
- Official full Unity EditMode run e17b1f05bb0e4233a55f3be47fe6ca3f records 3274/3274, zero failed/skipped/inconclusive, stable inputs, and exact tested commit/tree. The changed Unity adapter, admission-test, and Core application source hashes and lengths match its input snapshot after Windows CRLF normalization.
- The final public package manifest identifies the exact tested tree and input digest 65b1af751c6b64cfb0efb16685249ff99f77cc70d51ca23d34763b31f43708f8. Its archive is 173247 bytes, SHA-256 480f04bb2613533ee09b314b9f014e9ece590129d1a33fff21074c43f1320c11, matching the committed archive and manifest. Its single normalized XML is 1714325 bytes, SHA-256 cadf721830fd3811883c7d813ac85811c4feb3efe0a0bf176607df32a902623a; it contains 3274 cases, all passed, with zero failures, skips, or inconclusives. The public package manifest and XML contain no detected absolute workstation paths. The documented manifest SHA-256 is for its Windows CRLF form; the committed LF Git blob has a different hash, and CRLF normalization reproduces the documented hash.
- Protected official filtered Smoke run 6531b16b8aab4879ae7831aabae74043 records EditMode/Smoke as 5/5 with stable inputs and the same source snapshot digest. Its raw report hash matches the retained validation record. Unity leaves unfiltered parent suite counts in this filtered XML, so the packager refusal is documented; the report was not rewritten. The five cases are covered by the complete public EditMode report/package.
- The CoreValidation manifest records unrelated ProjectSettings edits and an earlier local evidence directory as working-tree changes; they are explicitly preserved and are absent from the candidate diff. This review did not modify them or any protected evidence.
- git diff --check passed for the exact base-to-candidate range.

## Limits

This is a validated candidate for initial restored-image publication admission only. It does not complete P12-A, the detached .NET host, the shared DailyV1Application/TryStart, or CORE-F, and it is not canonical promotion or Phase closure. Core canonical remains a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea.
