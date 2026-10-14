# CORE-C — finite-source Core extraction validation

## Candidate and scope

- Current Core canonical base: `bb82446c279ef47cf0d18a371f4728b2be4090e3`.
- Candidate branch: `codex/core/CORE-C-P14B-finite-source`.
- Final candidate reviewed: `114e3a7ec9f794b2996305eb1d640a8a58ab1083`, full
  tree `2359e06c25dc9f244fe48b653eb5ace9d9930583`.
- Core implementation commit/tree: `6355d37f416f88f90eb11ce6c8283c23d8acfb51` / `b5ad86fd6114db106d7223e6e70da3bd5ff2b62c`.
- Test/input commit/tree: `b33a302f9b78e8f35ba0ea94bf7168e6ee94f86f` /
  `170d95b3fa7fc73a069a472f029261e880680415`. The later commits to the final
  candidate add validation/evidence and review records only; executable source
  and test inputs are unchanged.
- Technical design: `e80ae27cdf597e9f551fe94dd22827f59636712a`; independent design review: `cb98d3e99c9491ce2c34278516272763333b271e`.
- Current-baseline applicability review: `8fa0408e481f457c153d7552eaed8ed5551cc612`.

The extraction is limited to the existing Market price calculation and the
prepared finite-source production value kernel used by the approved P14-B
profile. Unity retains live owner authority and performs admission/install.
P14-C remains Unity-side regression behavior; no claim is made that it calls
the Core kernel.

## .NET cold/warm validation

The promoted `Tools/CoreValidation` runner completed both cold and warm gates
on test/input commit `b33a302f9b78e8f35ba0ea94bf7168e6ee94f86f`:

- Cold: 104/104 passed; native VSTest run `25a4fd2e-e12b-4e50-87b8-72cb31e28114`.
- Warm: 104/104 passed; native VSTest run `e1bc6a7e-0dbe-4bcb-af70-1ce3056a2c47`.
- Both selected `Microsoft.NETCore.App` 8.0.28.
- Exact case discovery and cold/warm identity comparison passed. The input
  inventory digest is `ef1b5d8e4481ad81fd5113bec0d58101683ae35d9e00c0541894f2235a051324`;
  it includes the new CORE-C runtime sources, shared price vectors, and linked
  Core tests. Input and generated dependency snapshots remained stable.
- Private runner manifest SHA-256:
  `34ac78e7cfbf8a362c73c210ee36ab06e64595675420d298ceb8659f530cb503`.
- Protected cold TRX SHA-256:
  `8e3d07ff0c3578ee3d13bab74b810cc15b7b9dbb5deb7973e28a55006368fcac`.
- Protected warm TRX SHA-256:
  `a2609ce447e81de515407574212e9996f4835cbecf6af4ff6e7420627c2e3d65`.
- The normalized cold/warm package is
  [`corec-dotnet-coldwarm.zip`](Evidence/corec-dotnet-coldwarm.zip), archive
  SHA-256 `30ca105c38a2c529333fb7e50a0a46569a4601838d74ac6285b58beb8be2b763`,
  manifest SHA-256
  `1d147fea52e9b57e9c8c272e60203b53bb313e546de69cc1bc0637d5a8eb305d`.
  Package verification returned `originalComparisonVerified`; the archive has
  two result entries and does not contain the private source context, runner
  manifest, original reports, or raw logs.

The CoreValidation evidence directory and all 13 retained report/log files
passed the owner-only protected-storage checks. Original reports and logs
remain private and unchanged. The validation ran in a sparse .NET worktree
without a Unity `Library` directory.

## Unity validation

All evidence wrapper runs reported `InputStable=True` on test/input commit
`b33a302`.

| Suite | Result | Run ID |
|---|---:|---|
| `MarketPriceUnityBaselineTests` | 1/1 | `69b2c768736a4b5b8680c31bb0043b3d` |
| `FiniteSourceProductionTests` | 19/19 | `ad09685aa28a4c4dbb7715cad4ea579e` |
| `P14CMixedSourceProductionTests` | 9/9 | `cef9b8cfdc4f47e593961cc2b425dae7` |
| `EconomyTransactionTests` | 45/45 | `b2abdff179ad41dca2d0dcd6a2b00a56` |
| `SettlementStockOwnershipTests` | 14/14 | `0bb01a8c3ed644a1bd41f4ec875e9d24` |
| `CityDailyEconomyContinuationTests` | 19/19 | `063574a8540b484b91a6fec8e330e21f` |
| `SimulationRuntimeAdmissionTests` | 205/205 | `84d5f571c7f14fba99252cbdcfc11eeb` |
| ALL EditMode | 3110/3110 | `945776681ba944e6ba4023265bbb25e4` |
| Official Smoke | 5/5 | `1ce9aa0c13714c0ba87d1b5183ec3cb0` |

The ALL EditMode evidence package is
[`corec-all-editmode.zip`](Evidence/corec-all-editmode.zip) with archive
SHA-256 `d9bd54dd07a86e5d9fb626240bdcf9894feed6b1f835e76734e62d6b3b03ae00`.
Its manifest SHA-256 is
`03a65535ae248e520832b77d3cf94626f467f199b429df684e56c246ceaef2b0`.
The package was verified against its protected source run; the packager
reported `VALID`, one run and one report entry.

The separate filtered Smoke run passed in the official runner. Its packaging
attempt was rejected because the XML's aggregate `Simulation` suite
`testcasecount` does not match its descendants. The original report was left
unchanged and remains in protected local evidence. The packaged ALL EditMode
report contains the complete five passing Smoke cases; no altered or
reinterpreted Smoke XML is published.

`git diff --check` passed for the implementation and validation-tool changes.
Independent exact-tip implementation review passed with no actionable findings
for final candidate `114e3a7ec9f794b2996305eb1d640a8a58ab1083` and tree
`2359e06c25dc9f244fe48b653eb5ace9d9930583`; durable record
`c04ec332eef29387463937c7153387d9b8728c75` is on
`codex/core/CORE-C-implementation-review-114e3a7`. The reviewer verified the
recorded evidence but did not rerun the suites. This validation record alone
does not claim CORE-C promotion by itself. The reviewed candidate was promoted
to `codex/core/canonical` by fast-forward from
`bb82446c279ef47cf0d18a371f4728b2be4090e3` to
`0e400bcf333ae7bfdb2d28287d2d40929b5774cf`; the promotion-record tree is
`cd0dbb289b21bcf6edb1782494f7e18536e93a0a`. The reviewed implementation and
test-input trees remain unchanged. This promotion does not claim P12 or P13
readiness, equivalent .NET P12 continuation, full world hosting, or broader
simulation hosting.
