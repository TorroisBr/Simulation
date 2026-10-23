# CORE-F runtime advance failure contract review

**Verdict:** `VALIDATED_CANDIDATE`

**Reviewed candidate:** `d16c89f1716b1861f9ba7bac434d4bdddc1a7c63`

**Candidate tree:** `e87fcfd783e1d581bb18f7e82a5db19d98d3eec0`

**Candidate base:** `286208b83e84d8cbf1d74e84d2dae642241b5e3b`

**Core canonical at review:** `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

**Independent reviewer:** `/root/coref_action_boundary_review` (separate read-only exact-tip review)

## Scope reviewed

This seven-file candidate moves the public `SimulationRuntimeAdvanceFailure`
contract into `Simulation.Core`, adds the source to the explicit Core compile
list, and has the Unity runtime consume the shared enum directly. It preserves
the public enum name, default `int` backing type, and values `0` through `6`;
the duplicate Unity declaration and boundary byte casts are removed. Existing
behavioral assertions remain, with a contract test locking the backing type and
numeric values.

The review checked the complete candidate diff against its exact base, source
and project ownership, API compatibility, test assertions, Unity metadata,
and the boundary between Core and the Unity adapter. It found no actionable
defects. No advance ordering, lease, admission, owner mutation, capture, or
restoration behavior changed.

## Validation evidence

- Official CoreValidation: **681/681 cold and 681/681 warm passed**. Private
run ID: `469ad7f5e1214212905be7021d2b6fc6`.
- Focused official Unity `SimulationRuntimeAdmissionTests`: **220/220 passed**.
  The result is retained under the worktree's `Temp/ValidationResults` run
  directory; raw evidence and private runner data remain local.
- `git diff --check`: **passed**.
- Independent exact-tip review: **PASS**, including source-tree and evidence
  applicability checks.

## Integration limits

This is a partial Core API-contract slice. It does not establish a Core-owned
active runtime/session, application composition, `DailyV1Application.TryStart`,
detached P12 restoration, a fixed .NET host, separate-process T/T+1/T+2
continuation, or full CORE-F acceptance. It does not promote Core canonical.
