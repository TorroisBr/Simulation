# P12-G Political Binding and Evidence Packaging Validation

Date: 2026-10-12

## Candidate identity

- P12 canonical base: `eef376d51f36641918a13b2e937e704051814820`
- Candidate branch: `codex/phase12/P12GPoliticalBindingCompletion`
- Validated code commit: `a01d73297228c6ee466bb01a636ca0bcdecc9f68`
- Candidate Git tree: `de049a4ff5efb78845ce643080ee056787f2cf20`
- Validated `Assets` tree: `a61dad9e1755c28c87fc3f7e01e6e30bb1f9964f`
- Coordinator source blob: `e1ccf80c05fc3e3b8e2ec739376bd78b1f208ea2`
- Political fixture test blob: `2960505666ce4e1451120c9ef1d010059bb0a217`
- Every Unity run below records candidate `a01d7329`, Git tree `de049a4`, and input snapshot `56d5c53bc6b8f9a30a6c9f3b2054a3929dcfcbade36abde6fcef17753b4b7196`.
- Unity Editor: `6000.3.9f1`

## Validation results

All seven gates passed with zero failed, skipped, or inconclusive tests. The focused results are retained in the protected local evidence store; their exact run IDs, complete result counts, and original XML hashes are recorded here. Raw focused XML and logs are not published.

| Gate | Run ID | Result | Original result XML SHA-256 |
|---|---|---:|---|
| `P12GPoliticalBindingRestoreTests` | `a3abf8b45b7f454fb8e778907f70080e` | 11/11 | `38b43efb6261723d3bfbf0099c72a9bc04113adca1915667d895a14629100543` |
| `P12` | `45572cacc86147deba6182f84160bf73` | 363/363 | `2c539e803ce224173f540269f4ff786d11ab5536fd6b8275d4a9b0fd52827d66` |
| `Political` | `8dcc3e5e754849678a92bb4f66eda798` | 111/111 | `24c981fe960f68c330fa407d97a6f0062385f00f049ff9bcdeb64c7cbd3e6275` |
| `SimulationRuntimeAdmissionTests` | `e7ff2d3598864a9ba5110a715e13d3d9` | 200/200 | `fa58da9137f30716f8980b65d7bc5a72081e7ca262aafad066742222ee7ff932` |
| `SimulationBootstrapCompositionTests` | `3dc7a4b30403425e947c4a294a870a7f` | 27/27 | `0f813fabad2dfa3a02dcb42dfc4cc9c3bc14393d10f84a6a4c428ae80f3b5be8` |
| All EditMode | `b09638de5d7149f7a17de25c9c92be99` | 2890/2890 | `3a90bee3d77e7bed88faa98f1413289a5a7f88ce3ec17058771acef80514e238` |
| Official Smoke | `0e147dd00d1340e28afa73027f6eaa1b` | 5/5 | `53196ea495ee51e938f7e5cf8f76777bd261c2b32b20b3508219e6591ff5bcb6` |

The Unity validation harness self-tests passed 33/33. The evidence-packager self-tests passed 61 assertions. `git diff --check` passed for the staged packager correction; the final candidate diff is checked again after documentation and review records are added.

## Evidence package

The package contains only the complete, unfiltered All EditMode result XML. Filtered Unity XML reports retain suite case counts that describe the unfiltered assembly, so they are retained privately and summarized above rather than packaged as incomplete standalone result hierarchies.

- Package manifest: `p12g-political-binding-closure-20261012-a01d7329.manifest.json`
- Manifest SHA-256: `3b7176f41961ee0cd6597c087aa542f044f3312a714b8dfc753450cb1b2b17a4`
- Deterministic result-only archive: `p12g-political-binding-closure-20261012-a01d7329.zip`
- Archive size: 150477 bytes
- Archive SHA-256: `6fa85db87b116f8baad0711c89739acd4b00ed76324bdb7d6116467da7e40107`
- Packager runtime: PowerShell Core 7.6.5, .NET 10.0.11, compression assembly 10.0.0.0
- Declared output policy: `remove-output-elements`
- Declared path policy: `redact-absolute-local-assembly-suite-fullnames`
- Archived normalized XML: 1,496,774 bytes; SHA-256 `d7c3ec54b6e89bfe4862af3e12579652d01a5012e34ce7583a199bcd5369c76b`
- Package verification against the protected source run manifest: **PASS** (`runs=1`, `entries=1`).

Only the machine-specific root of the single supported NUnit Assembly suite `fullname` was projected to `<PROJECT_ROOT>`; assembly-relative identity and test hierarchy were preserved. The source-to-normalized semantic comparison and archived counts passed. The new privacy regression tests reject quoted, parenthesized, and exception-style absolute POSIX paths while accepting repository-relative paths that contain a component named `root` or `agent`.

The archive excludes raw logs and the focused XML files. Historical validation artifacts were not rewritten.

## Evidence boundary

This record validates the bounded synthetic PoliticalDecision/PoliticalKnowledge source fixture and the evidence package on the exact candidate tree. The fixture is not a supported Daily-v1 political writer, does not simulate a pre-fixture political history, and does not establish P12-G completion. See [`CLOSURE_MATRIX.md`](CLOSURE_MATRIX.md) for the retained aggregate obligations and current Phase status.
