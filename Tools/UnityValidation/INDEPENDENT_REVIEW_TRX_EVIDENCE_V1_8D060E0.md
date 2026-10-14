# Independent review: TRX-EVIDENCE-v1

Date: 2026-10-13

## Exact target and authorities

- Candidate branch: `codex/tools/trx-evidence-v1`
- Candidate commit: `8d060e0500828e5725322b018d1645c011416e18`
- Candidate tree: `7f590c711fb753e2bacac71ab5ce56e3fac57b41`
- Candidate parent: `c5ffa77faa68c522d3065eadc27e8b50242f01e7`
- Direct P12 base: `4cc31073f0ec6ba5c718084e4cebd6e914931d5d`
- Current architecture baseline reviewed: `58157f9ac7bf573e9a4ed1b2d485a2db190e7c4f`
- Promoted TRX contract: `63d3b8352a450b27b9a5f593c4ea166c0343c59e`, `docs/operations/DOTNET_TRX_EVIDENCE_CONTRACT.md`
- Applicable evidence policy: `58157f9ac7bf573e9a4ed1b2d485a2db190e7c4f`, `docs/operations/VALIDATION_EVIDENCE_POLICY.md`
- Independent reviewer: Codex subagent, not the candidate author. Review was read-only against the candidate; this record is additive.

The candidate changes six files under `Tools/UnityValidation`. Its second commit adds runner-manifest identity checks, current source/input inventory binding, report/log/logger identities, runtime trace binding, recovered assembly/PDB retention, cold/warm cardinality checks, and preservation of absent or empty optional metadata. Those changes address several earlier review findings. The full cumulative diff and exact tree were inspected.

## Verdict

**NEEDS_CHANGES.** Focused synthetic and legacy tooling checks pass, but the implementation has remaining contract mismatches and cannot be validated against the protected original run in this environment.

## Findings

1. **Native `Error` results are counted as native failures.** `TrxValidationEvidence.psm1:202-203` sets native failed/executed expectations from `Failed + Error`. The promoted contract §5 requires native `failed` to equal the `Failed` case count and `executed` to equal `Passed + Failed`, while keeping `Error` distinct in the histogram. The new test at `TrxValidationEvidenceTests.ps1:34` changes the native failed/executed counters to make an `Error` result pass this implementation. Align the parser and fixture with the accepted counter semantics.

2. **Valid nonpassing reports cannot pass through the package path.** `TrxValidationEvidence.psm1:399` requires runner status `Passed`; lines 549 and 673 reject nonzero test/command exits. The contract §5 and §10 permit complete, well-formed failing evidence to be packaged with `gatePassed=false`; incomplete failure records must still fail closed. The current code rejects even a complete context for a valid failing TRX, so parser-only nonpassing tests do not establish the promised packaging behavior.

3. **Public command provenance does not preserve or prove the effective command paths.** At `TrxValidationEvidence.psm1:674-677`, the `--results-directory` value is projected to `<EVIDENCE>`; the contract requires `<EVIDENCE>/<runnerRunId>`. `workingDirectory` is hard-coded as `<WORKTREE>`, while the private context schema at lines 388-394 has no actual/recovered cwd field. The code does not inspect the captured `project.assets.json` `restore.projectPath` relationship or otherwise bind cwd. Contract §§4 and 9 require the effective path suffix and private cwd/recovery evidence; these are source claims, not display-only fields.

4. **Captured dirty-input facts are erased.** `Assert-TrxPublicRunSourceClaims` builds `Run.source.influencingChanges` from the captured/current worktree status (around line 709), but `New-DotNetTrxEvidencePackage` then unconditionally overwrites it with `@()` at line 876. A stable run with a captured influencing change would therefore publish a false clean-input claim. Preserve the derived records or reject unresolved influence; only a clean run may publish an empty list.

5. **A second package attempt with the same retention handle fails.** `Get-TrxRetainedAssemblyInventory -Create` at lines 604-606 rejects any existing `validated-output/<runnerRunId>` directory. A successful first packaging call creates that directory, and the contract requires reuse of the same retention handle for repackaging. The retry path needs to verify and reuse an existing matching protected receipt/copies, while continuing to reject mismatched or partial contents.

6. **Discovery log validation is not exact.** At line 683 the code checks only that every manifest case appears as a substring in `list-tests.log`. It does not parse and compare the complete ordered discovery output or reject extra cases. Thus an extra discovered case can be omitted from the manifest and from cold/warm comparisons while this check still succeeds. The contract requires exact ordered discovery strings/multiset and selection reconciliation.

7. **Archive-only manifest validation accepts inconsistent claims.** `Assert-TrxPublicManifest` (lines 330-382) validates many object keys but leaves several values/relationships unchecked. For example, it does not reject `source.inputStable=false` alongside passing attempts, nor validate the declared fixed archive format. `Test-DotNetTrxEvidencePackage` can consequently report archive validity and `GatePassed=true` for a manifest contradicting the contract's stable-input gate requirement. Archive-only verification must still validate the public schema's scalar forms and internally checkable claim consistency, while continuing to label original comparison as not performed.

## Validation and evidence limits

Fresh exact-tip runs completed successfully:

- TRX synthetic tests: 67 assertions passed.
- Existing evidence packager suite: 64 assertions passed.
- Legacy Unity validation runner suite: 33/33 passed.
- `git diff --check 4cc31073f0ec6ba5c718084e4cebd6e914931d5d 8d060e0500828e5725322b018d1645c011416e18`: passed.

These tests do not exercise complete source-context packaging, a real retained Unity package, ACL rejection/secure-copy cases, or original-to-normalized comparison. The expected local CoreValidation evidence root did not pass the existing owner-only ACL check, so I did not inspect its private contents. The candidate contains no real evidence package or durable validation record. Therefore I could not independently confirm the 27 captured inputs, recovered assembly/PDB identities, the 45 cold plus 45 warm source-to-output comparison, or the original-copy retention/privacy attestations required by contract §§8-10. I do not infer PASS from the synthetic checks or contract narrative.

## Integration constraints

Keep this candidate unpromoted. Resolve the findings, add end-to-end synthetic/context and ACL cases, verify an existing Unity package retains its behavior, then perform the required protected original comparison and durable validation/review attestation. No Unity domain code changed in this candidate; its lack of a Unity EditMode/Smoke rerun does not close the separate required package/evidence gates.
