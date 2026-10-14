# Bounded .NET Core validation

Run the synthetic parser/input-integrity checks:

```powershell
pwsh -NoProfile -File .\Tools\CoreValidation\Invoke-CoreValidation.ps1 -SelfTest
```

Run the locked Core test, CalendarDemo and Simulation.Lab gates from the repository root:

```powershell
pwsh -NoProfile -File .\Tools\CoreValidation\Invoke-CoreValidation.ps1
```

The default run also executes the fixed Simulation.Lab scenario and compares
its cold and warm output. To produce the standard three-project Core TRX run
for the existing `DotNetTrx` evidence packager, omit only the separate Lab
restore/build/run gates:

```powershell
pwsh -NoProfile -File .\Tools\CoreValidation\Invoke-CoreValidation.ps1 -SkipLab
```

Validate the Lab independently from a clean committed candidate worktree:

```powershell
pwsh -NoProfile -File .\Tools\CoreValidation\Invoke-LabValidation.ps1
```

Both runners bind the Lab source, project and locked dependency file in their
source inventory. The standalone Lab runner retains its own protected manifest
and command logs; it does not create or replace TRX evidence.

Before writing run output, the runner creates a unique run directory with the
shared `UnityValidationEvidence` owner-only ACL helper, verifies its exclusive
current-account ACL, and checks each retained file. It leaves the caller's
evidence parent untouched. Pass `-EvidenceRoot` to place the protected run
directory under a suitable private location; the default remains the current
user's LocalApplicationData evidence root.

The runner requires the pinned SDK in `global.json`, locked NuGet restores,
non-empty test discovery, coherent TRX run-local test IDs/entries/results and
exact discovered-case identity. It rejects malformed or missing reports,
unknown outcomes, counter mismatches, skipped/not-executed cases and input
changes. A valid failed test report remains failed; the runner does not retry.

Original TRX reports and command logs are written under the current user's
LocalApplicationData `Simulation/CoreValidation/<run-id>` directory (or an
explicit private evidence directory supplied by the operator). Review their
actual contents before publication. This tool does not strip or normalize TRX.
The manifest records relative source identities, hashes, command arguments,
test cases, outcomes, available durations, toolchain, elapsed times, build
storage and generated dependency-input hashes. Keep the private source copy and
the exact input-identifying manifest together during review.

The cold and warm test gates also retain separate private .NET host-trace files.
Per-process `DOTNET_HOST_TRACE` output isolates the testhost trace so the selected CLR
is bound to each test assembly runtime configuration.
Trace paths stay inside the protected run directory and are never published.

For the final .NET-only proof, invoke the Core runner from a sparse isolated
worktree that contains the explicitly listed package/project/test/demo/tool
inputs and has no `Library` directory. Use its default mode for the combined
Core-and-Lab run, and `-SkipLab` when the standard Core TRX package shape is
required. Invoke the standalone Lab runner from the same committed source tree
for its locked restore, Release build, and cold/warm exact-output comparison.
Unity validation remains a separate gate in the implementation worktree using
the unchanged Unity harness and evidence tool.
