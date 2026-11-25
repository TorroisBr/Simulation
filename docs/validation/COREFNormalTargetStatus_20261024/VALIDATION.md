# CORE-F targeted Normal-action status execution

## Scope

This candidate extends the existing Core-owned selected action path to apply
Normal-action status effects to the exact NPC owner identified by the Core
action-slot payload. The path does not require a Unity `NpcActionRuntime`
projection when the action definition, actor slot, target `RuntimeId`, and
status definitions all resolve to the exact admitted Core graph objects.

The existing Unity host still supplies the current P12 mutation-operation
boundary. The implementation does not move session ownership, daily execution,
P12 capture/restoration, or the complete `DailyV1Application` into Core. It is
not CORE-F package completion or canonical promotion.

## Source identity

Candidate parent: `a8f96d4e6d87aa3f304aae72d8b84e7c40a5b5c1`.

SHA-256 of the validated source inputs:

- `DailyV1SelectedActorActionExecutionStage.cs`: `08113d88c0186355dd9d1a23e2fe12883bfab509de6b1fe38f8a21feaedd5e61`
- `SimulationRuntime.cs`: `f854df221e2bd4fc8336e7d9bd1a7a948934f11a09bb98867f99df69ee577aea`
- `SimulationRuntimeAdmissionTests.cs`: `ccfeb0a8109899ba3c20669b729fe60a2a212ef702211d05849873449e403374`

The privacy-safe Unity input snapshot is `ec8f4c9b5cd7d086570674aedca808efe9717b6f2d0c8a7934f5096111b41858`.

## Validation

- Focused exact-target regression: 1/1 passed (`4c122a4bbb9e4a5d93c98c73e8942f1b`).
- `SimulationRuntimeAdmissionTests`: 223/223 passed (`34320d289cec4fc5a5b523ee4ed55a0f`).
- Full Unity EditMode: 3258/3258 passed, zero failures/skips (`3e312161bfb4484389e201bef3a3a4f4`).
- CoreValidation: 682/682 cold and warm passed (`720698f2c8f34cc8a07de0f58cca1967`, `-SkipLab`).
- `git diff --check`: passed.

The full EditMode run is packaged and independently integrity-verified:

- Manifest: `EditModeEvidenceShortPath/coref-normal-target-status-editmode-20261024-shortpath.manifest.json`
- Archive: `EditModeEvidenceShortPath/coref-normal-target-status-editmode-20261024-shortpath.zip`
- Archive SHA-256: `cb57751ba592440338bc81e4db7d7f35fe2a74f395cccab01083406d8d9e8baa`
- Unity: `6000.3.9f1`
- Evidence verification: one passing run, one report entry, archive verification passed.

Two earlier evidence captures used a result/log path at the Windows 260-character
limit and exited before producing XML. Those private diagnostics remain
preserved and are excluded. The successful capture used a shorter private
evidence root and the approved runner and packager; it matched the three source
hashes above. The protected original XML/log and CoreValidation evidence remain
in their private local retention directories.

## Review boundary

Independent exact-tip review is pending for the implementation commit. Existing
review evidence for the parent candidate does not cover this change.
