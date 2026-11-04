# CORE-F Restored Selected Owner Mutation-Guard Integration — Validation

## Scope

During restored Daily-v1 boundary finalization, Core now prebinds its mutation guard to the exact selected Core owner graph before the Unity host binds the remaining authorities and projections. The Core preflight checks all selected roots before binding, so an incompatible root rejects the operation without partial binding. This is a bounded restoration-authority integration; it does not claim completion of the shared runtime, the full detached host, or CORE-F.

Canonical base: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
Candidate base before this change: 6bac9d0bd26464372840c5f9807f4a5fc2e77944

## Core validation

Official CoreValidation run 7907828ffdb2445cb378e59e965b9f5c: cold 1,012/1,012 PASS and warm 1,012/1,012 PASS, stable inputs.

- Protected run manifest SHA-256: 9d14582008c531928452bf58ecdf3250e18a8a63eed8c64ba30296b46a6a8455
- Cold TRX SHA-256: fe7b606994eade7d181e9c5abdb78820d061e4b1c0c6245f13090460e829575e
- Warm TRX SHA-256: 863e7e71f559dcbb27a060df204eadb3b2ac1de795de7ff1532033f38714a49db

The original TRX and full run artifacts remain in the protected validation store. The run used a worktree with Unity Library present; this is not evidence of Library-independent validation.

## Unity validation

- Focused SimulationRuntimeAdmissionTests, run 46b200bff7604b50a2c3aeb5c09ea8a5: 243/243 PASS.
- Full EditMode, run 384c6cacbad44855b6ddfded47432397: 3,299/3,299 PASS; stable input snapshot c88e939396a2a4e728710ebe66028b68cf1f78282a6f2eb49a6bf2807d57b5f0.
- EditMode Smoke, run 634a817a4cbd4c729f80d66328ac2354: 5/5 PASS, same stable input snapshot.
- Final full EditMode package verifies against its original run manifest. Package manifest SHA-256: 5671ac0c0bb486db9e87a1db441b1a2fcf304967b59eb88a4dbce1411e612ae7. Archive SHA-256: df8e91349ddad888eabbbbc6ec7cd5a1831c12aadaf4b3bbf9f5ee5846b7650d.
- Full EditMode XML SHA-256: 85b2bb22ee5ec82558c02a8f0ac10ceee67d70cbcba8269b2b49b92fadc0a53.
- Full EditMode log SHA-256: 5f3f1faf1fa5ac38abcd6256d28bd2c8b67b7d0ddbe92885492ad84379ffcc7b.

The focused Core regression was 3/3 PASS for:
RestoredCoreBindsItsExactOwnerGraphBeforeHostProjectionBinding,
RestoredCoreMutationGuardPreflightDoesNotPartiallyBindAnInvalidGraph, and
RestoredExecutionBoundaryIsCoreComposedFromExactStagedOwnersAndRoots.

The official Smoke evidence is from the EditMode runner. A prior PlayMode filter attempt matched zero tests and is not counted as validation.

## Evidence boundary

The final Unity package is included here as a normalized, independently verifiable evidence bundle. Original run artifacts remain in the protected evidence store. No raw protected TRX or workstation-specific evidence directory is committed.

## Remaining work

- The Unity host still binds non-graph authorities and Unity projections.
- The full Core-owned runtime/application lifecycle and independent detached .NET host remain incomplete.
- Separate-process T/T+1/T+2 continuation remains outstanding.
- This candidate is not a Core canonical promotion, and CORE-F remains incomplete.