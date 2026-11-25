# Independent exact-tip review — CoreValidation generated-output containment

**Verdict: `VALIDATED_CANDIDATE` for this bounded CoreValidation tooling correction.**

This review validates the generated-output path-containment correction only. It does not approve or promote the incomplete CORE-F Runtime/Host Integration package.

## Reviewed identities

- Repository: `TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-runtime-host-integration`
- Review base: `f3ea1b36d4aef7f5f3a9e508fdbc52b6af67c62b`
- Reviewed implementation commit: `3b93196461c3388bf66a386b758216dd9c7f75b9`
- Reviewed implementation tree: `e2755cac3a301a8f0993375956272eef6a26bef7`
- Exact final candidate tip: `7a671a46daa04667a2f033b93b46485be731fc87`
- Final candidate tree: `15382ba6b2df2f318b53c91d60b627c78185602a`
- Current remote Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

The implementation commit changes only `Tools/CoreValidation/CoreValidation.psm1` and its PowerShell self-test. The exact final candidate commit is documentation-only (`Tools/CoreValidation/README.md` and the validation note). No production simulation source or Unity test source changed.

## Review findings

The prior containment gap is closed for the reviewed call path. `Get-CoreProjectCompileInputs` now checks repository containment and each existing path component for reparse points before excluding generated `bin`/`obj` compile outputs. Missing leaves are permitted only for paths classified as generated; any existing component is still inspected, so a missing generated leaf does not hide a junction in its parent path. External paths fail the root-prefix check before generated-output exclusion.

The synthetic self-test exercises both the path helper and its caller. It creates an in-repository `bin` junction to an external source, evaluates a synthetic SDK project whose `Compile` item uses that path, and asserts that `Get-CoreProjectCompileInputs` rejects it. The preserved self-test log is 361 bytes, SHA-256 `600576d2362379c6bddae9ad75d61278aa342c1611fc4291ba1bf8468de8d7bb`; its contents record the self-test PASS including reparse-point rejection.

No actionable findings remain in this bounded change. The test setup removes its junction before deleting its temporary root. The README describes the same synthetic caller test and the evidence note records the implementation and evidence identities without workstation paths.

## Validation evidence reviewed

The official CoreValidation run is bound to implementation commit `3b93196461c3388bf66a386b758216dd9c7f75b9`, tree `e2755cac3a301a8f0993375956272eef6a26bef7`:

- Run ID: `b074a1c56a4a4290ae7fb4b134bcdee8`
- Protected manifest SHA-256: `c751822e90ccc2c6aa95583720efde953c7f07c4551bbf9c3222297b8d425a21`
- Stable 337-input digest: `2675e48c4e09ee553400f2a99a9f50300baf71b50c682bd78cb97b178e3232e2`
- Test discovery: 640; cold: 640/640 passed; warm: 640/640 passed
- Cold TRX SHA-256: `fd97091ed0161440870866b64a73772332817052b83642a45a802b45a7ae55cf`
- Warm TRX SHA-256: `c4ce012a873d09c82e41e31a6341063426c21efbf2921cb067cdcbaeec8b6f34`

The actual protected manifest and report files were available and their hashes and TRX counters match the record. The manifest includes the changed CoreValidation module and self-test source. The CoreValidation run used the Unity worktree, so it does not establish Library-independent .NET execution.

The retained Unity EditMode evidence remains applicable to simulation code and Unity tests because the changes from its tested commit `6248829fee6dd6dd10fd2836d8973afc0d9e5bb0` through implementation commit `3b93196461c3388bf66a386b758216dd9c7f75b9` do not change `Packages`, `Assets`, `src`, or simulation `tests` inputs:

- Run ID: `b51c8ae0c1784fcfacc87fe4460fa7ff`
- Tested tree: `4cb77b76401eedbe6113eb1175902732b74c7f2a`
- Protected run-manifest SHA-256: `868254cfc0e580fd25c0b5efac7d13a5100405a3843a6a3b719842330f724096`
- Stable 1,846-file digest: `54feb141c0212cdb17610216af0cfaedec70818b0800cc2b6e628d7ae70f4a67`
- EditMode: 26/26 passed, zero failed, skipped, or inconclusive
- Original XML SHA-256: `ab5acacda135b022f65955fe65a83118062209b3494e5e379d76dc9d86f4fa0c`
- Original log SHA-256: `4b55bbc0706948873dbf03288fe53c67bde3ea20c0a734f9d3568e8195c2dcf1`

The final candidate tip `7a671a46daa04667a2f033b93b46485be731fc87` adds documentation only; no tests were rerun for that change. The CoreValidation manifest therefore remains specifically attributable to implementation commit `3b93196461c3388bf66a386b758216dd9c7f75b9`, rather than claiming a test run on the documentation successor.

## Integration limits

This review covers only source-path containment in CoreValidation. It does not establish the complete CORE-F host, detached restoration, Library-independent validation, or full-package acceptance. No canonical promotion is requested or implied.
