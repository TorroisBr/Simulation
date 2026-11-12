# CORE-F selected Steal Core operation boundary

**Status:** validated candidate; independent exact-tip review is pending. This
bounded integration does not complete or promote the full CORE-F package.

## Candidate

- Base: `23974377e532e3cab024171cded957f0ca4768b2`
- Candidate: `7563a7687158e44494c2d91be653bce9a5ca37a7`
- Candidate tree: `a8e93486c4185634084ea89f55859179205418c9`
- Core canonical at validation: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Changed source/test files: `SimulationRuntime.cs`,
  `DailyV1P12CrimeJusticeMutationCoordinator.cs`,
  `DailyV1SelectedCrimeActionExecutionStage.cs`, and
  `DailyV1SelectedCrimeActionExecutionTests.cs`.

The admitted Unity path now asks the Core boundary to construct the existing
selected Crime/Justice action-mutation scope from the exact perpetrator,
victim, Justice owner, and receipt sections. The non-admitted compatibility
path still uses the existing Unity adapter. The outer `runtime.advance-day`
operation and its identity are unchanged. Money transfers retain their
existing nested host operation, and the existing Crime/Social coordination
and compensation flow are unchanged. No operation ID, profile, or persistence
scope was added.

Focused Core tests cover exact participant and ordered section admission,
status-write restriction to the perpetrator, continued delegation of the
existing transfer operation, and rejection of stale action or replaced host
binding before opening the P12 scope.

## Validation

| Gate | Result | Evidence |
| --- | --- | --- |
| Official CoreValidation cold | 1,115/1,115 passed | Run `ce8bd56dedde4eeaa43f785b238e6c55`; cold run `4f8f5963-1323-40f3-8bf4-7f243c457f7a` |
| Official CoreValidation warm | 1,115/1,115 passed | Warm run `d9373e1c-1aae-44b5-bdbb-669a165fa7e8` |
| Focused selected-Steal DailyAdvance Unity EditMode | 1/1 passed | Run `6de0ffdb273041e2a437fc128159f220` |
| Full Unity EditMode | 3,318/3,318 passed | Run `816b78a422c24d979067b8567b27f5c0` |
| Official Unity Smoke (`EditMode`, filter `Smoke`) | 5/5 passed | Run `165cf410386e412d94ad089ab3bfc1d1` |
| `git diff --check` | Passed | Candidate source diff |

The CoreValidation input snapshot was stable with digest
`50f5f1dd99054f4142dc74c7a5a45dda24571a54c8e86523ff16cea98d7e4285`.
The coordinator, Core execution-stage, and Core test source SHA-256 values in
that snapshot match the files in candidate `7563a768`. The Unity full and
Smoke runs used the same stable 1,982-file input digest
`69a282ead9fa7a321e5844ccf3cfe13ce3fef3fee4e99e30c6ba506538421546`; both
captured candidate `7563a768` and tree `a8e93486c4185634084ea89f55859179205418c9`.
All five Smoke cases also appear as passing cases in the full EditMode report.

The verified normalized Unity result package is
[`coref-selected-steal-core-operation.zip`](UnityEvidenceFull/coref-selected-steal-core-operation.zip)
(SHA-256 `04a6901873b2e397c8956ce326cb9d420d5ae7260d37b2f35481b5dff839f27e`);
its manifest is
[`coref-selected-steal-core-operation.manifest.json`](UnityEvidenceFull/coref-selected-steal-core-operation.manifest.json)
(SHA-256 `276490d7d37e8bdc2e5d108ff666ca6650c7ca727b02882d2cc7b43c12db28f4`).
The official verifier passed against the protected original full EditMode
run. The full report contains all 3,318 test results, including the five Smoke
cases.

## Evidence limits

Original CoreValidation TRX files and logs remain in the protected local
evidence area. Their hashes are recorded in the private run manifest
`a6bf02eb4912123a0808b50d45fc80084f28e7823302c284bdb43cd7cee57093`; the cold
and warm TRX SHA-256 values are respectively
`25167ed6bcc49a80c5365ef562025e80cd1aabfa587ed0da63216b13471a3cb3` and
`caae5e5fbfe32639e6d621a2790f0631eeaf8ce3ce9689591a3713f81f978c16`. A
public normalized .NET TRX package is not claimed because the approved TRX
projection adapter is not yet available. No original report was modified.

An attempted Unity `TestCategory=Smoke` selection returned zero tests and is
excluded from validation. The accepted Smoke result is the separate five-case
EditMode run above; no zero-test result is counted. A prior CoreValidation
invocation from the wrong shell directory failed before restore could locate
the project and is likewise not counted as a build or test failure.

This candidate preserves current Core canonical history and remains
unpromoted. It does not claim full shared Daily-v1 runtime completion,
`DailyV1Application.TryStart`, detached .NET continuation, or CORE-F closure.
