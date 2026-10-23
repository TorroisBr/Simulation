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
storage and generated dependency-input hashes. Before each run, the runner asks
the pinned MSBuild SDK to evaluate the `Compile` and `ProjectReference` items
for every project it builds, recursively hashes the repository-contained source
inputs, and verifies the same snapshot afterward. This binds linked Core files
and transitive project source that are not present in a hand-maintained source
list. Generated `bin`/`obj` compile outputs are excluded; project files,
tracked build configuration, locked dependencies and generated asset snapshots
remain bound separately. A compile input that resolves outside the repository,
traverses a reparse point below the repository root, or is missing causes the
run to stop rather than produce incomplete source
evidence. Keep the private source copy and the exact input-identifying manifest
together during review.

The self-test evaluates a synthetic SDK project whose compile item reaches an
external source through an in-repository `bin` junction. This verifies that
generated-output exclusion cannot bypass source-path containment checks.

The cold and warm test gates also retain separate private .NET host-trace files.
The runner writes SDK-specific host-trace settings into each isolated VSTest
testhost through a per-gate runsettings file. TRACEFILE names a file inside the
protected run directory. The runner binds the unique trace containing the exact
test assembly runtime-configuration paths; process filenames vary by SDK launch
mode. The settings file and selected trace hashes are retained in the private
manifest. Neither is published.

For the final .NET-only proof, invoke the Core runner from a sparse isolated
worktree that contains the explicitly listed package/project/test/demo/tool
inputs and has no `Library` directory. Use its default mode for the combined
Core-and-Lab run, and `-SkipLab` when the standard Core TRX package shape is
required. Invoke the standalone Lab runner from the same committed source tree
for its locked restore, Release build, and cold/warm exact-output comparison.
Unity validation remains a separate gate in the implementation worktree using
the unchanged Unity harness and evidence tool.
