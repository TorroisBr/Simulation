# CORE-F restored Core target invariants

## Scope

This checkpoint adds a Core-owned restored-target validation gate for the exact P11 ActorChoice authority and conditional P8 position/anchor owners. When a P8 owner is bound into the selected Core graph, its current census must match an exact zero-cardinality ExplicitlyEmpty owner-vector row. When the Core graph does not own that P8 instance, its row remains host-owned and is checked by the complete P12 owner vector.

P11 remains Required and may have supported populated ActorChoice rows. The restored owner vector must identify its exact instance, schema, cardinality, and revision. The excluded P18 ActorChoice temporal input remains exact-zero at the same owner identity and revision. Tests cover valid exact ownership plus wrong P11 role, wrong identity, missing P11 section, and the existing Expedition cardinality rejection.

This is a bounded restore-invariant slice. It does not claim complete P12 owner coverage, P12-A readiness, full CORE-F application/host completion, or migration of host-owned P8 owners.

## Source identity

Validation used the candidate base commit 1468b8cfa6ee3802c8e8bb92821310aeabbe8d45 (tree 692303815bb0ad58dd83f5595f6f1dc3c5155f3d) with the two dirty source inputs below. Their byte identities match between the candidate worktree and the isolated E: .NET validation worktree:

- Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs — SHA-256 4a5d0203382c39182796363adbd34b785bd9427bfb7f2f4a65fcb8279b7536b4.
- tests/Simulation.Core.Tests/DailyV1ApplicationHostTests.cs — SHA-256 047fa86074ee3d2531acca38d414531d6c9aace38ed9bbdfd05677a24aa5445d.

The .NET CoreValidation input digest was c3655d8bd038e24bcb323526d1290457e7d428b22aa2ecb3f825d180ad1a12cf. The Unity full-suite input digest was 2426ad9e44bc3d9d617ceffe25c5d2b2ed210ee06ece00ac7a2a916cf01b989b. Both source snapshots were stable.

## Validation

- Official CoreValidation Core-only profile: 1,020/1,020 cold and 1,020/1,020 warm; exit codes zero, stable inputs, and no Unity Library in the isolated validation worktree. The official DotNetTrx package passed verification with originalComparisonVerified. Run ID: 64d520774b14458185fe19ef901f6586. Package archive SHA-256: d368923e8f410ce77b285134a1235cf25c5b10a88236fcf51f6b36d31073394b. Package manifest SHA-256: d0955a30d15445b12b8742977617e1bf9d353f0cde16675c496bd2d73621c1c5.
- Unity 6000.3.9f1 / Test Framework 1.6.0 unfiltered EditMode: 3,299/3,299 passed, zero failures, stable inputs. The full report contains all five Smoke-named results; all five passed. The official Unity evidence package verified against its protected original. Run ID: 87adc6d8bb2e43dbbfe789540b865a87. Package archive SHA-256: e11e0ef0450147f27518efb3576f2fc67bc83d4cb139902eec6ff593aaacffac. Package manifest SHA-256: 617196a1eb4e3860194eb31c407bf18acafc090ae79310c84f47270cfecdb771.
- A separate filtered PlayMode attempt returned zero selected tests and is not counted as a passing gate. The five Smoke-named results above come from the unfiltered EditMode report.
- git diff --check: PASS.

## Evidence handling

The public Unity and TRX packages contain normalized result XML and privacy-safe manifests only. The TRX verifier confirmed the original cold/warm reports, source inventory, test identities, runtime-selection traces, logger identity, and retained assembly identities. Original XML and logs remain in protected evidence storage under the run IDs above; they were not modified or committed. Package verification passed for both packages.

The pre-existing ProjectSettings modification, unrelated untracked files, metadata, and other protected evidence were left untouched and excluded from the candidate commit. The isolated .NET validation worktree contained only the two source inputs listed above.
