# CORE-F Shared Daily-v1 Application/Host candidate review

**Verdict:** `NEEDS_CHANGES` for the approved CORE-F Application/Host package.
This commit is a retained partial implementation milestone and is not ready
for canonical promotion as the package.

**Candidate:** `e5549253c43a39b002e39dc7f5bcf7c99b960529`

**Candidate Git tree:** `47243ba2cb326da78d6165ec7591c051c441c7b8`

**Direct base/current `origin/codex/core/canonical`:**
`a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

**Independent reviewer:** separate read-only Codex review agent; exact-tip review
requested by the Master Orchestrator.
**Review scope:** complete candidate diff against the direct base, current
CORE-F host/runtime/restore design and package acceptance requirements.

## Delivered in this partial candidate

- Moved the shared logger source into Core and preserved its Unity `.meta` GUID.
- Added a Unity logging adapter for the persistent-data root and output sink.
- Kept `SimulationModule` in the Unity authoring assembly and generalized the
  logger entry point so the shared source does not own that Unity enum.
- Added typed definition, identity, host-port and P12 compatibility groundwork.
- No duplicate `SimulationLogger` implementation was found.

These changes are retained. The candidate does not deliver the complete shared
application/host package.

## Blocking findings

1. **Shared application and executable host are missing.**
   `DailyV1ApplicationHost` currently stores host ports only. No
   `DailyV1Application` or `TryStart` implementation composes, advances,
   captures, saves, loads, restores or publishes a shared runtime. Runtime,
   active session, bootstrap, selected owners and P12 restoration remain in
   Unity-owned Assets sources, so the fixed .NET host cannot execute the same
   authoritative graph.

2. **P12 v2 groundwork is not detached .NET restore.** The current restore
   coordinator still takes `SimulationConfigData`, stages against Unity
   authoring definitions and creates its logger through the Unity adapter.
   The compatibility factory also consumes Unity authoring inputs. No shared
   application wires the host root, file operations, output sink and truthful
   identity into save/load/restore, and no fresh-process restore exists.

3. **Exact-tip validation evidence is incomplete and not durably reviewable.**
   The candidate contains no validation record/package. At review time,
   `Temp/ValidationResults` contained only the official Smoke XML/log pair; the
   stated .NET cold/warm 200/200 and full Unity EditMode 3186/3186 reports,
   complete input/toolchain manifests and a result package were not available
   in the candidate. Those counts remain user-reported and were not independently
   verified against this exact tip.

4. **Package exit gates remain outstanding.** These include the fixed .NET 8
   host and identity manifest; the complete selected owner/runtime and typed
   definition closure in the shared Core assembly; app-wired v1/v2 persistence;
   detached graph restore and atomic rejection coverage; preservation of the
   historical 49-case CoreValidation identities and 89-family/24-operation
   owner evidence; and fresh-process repeated T/T+1/T+2 parity with Unity
   regressions and applicable Smoke/LongRun gates.

## Focused logger and identity review

The moved logger source is unique and retains its existing Unity metadata
identity. The adapter maps the three severities and supplies
`Application.persistentDataPath`. `SimulationModule` remains declared in the
Unity authoring source, and the generic logger entry point preserves that
assembly boundary. Existing tests cover receipt replay, report text, explicit
storage-root handling and basic sink severities.

The candidate does not include a legacy-output oracle or verify sink delivery
semantics for receipt replay. A supplied sink that throws can escape after the
logger has committed its report, receipt and revision state. The accepted
contract does not define that failure case; resolve and test the behavior during
host integration before claiming complete logger parity or atomicity.

## Validation and promotion disposition

The user-reported validation is .NET CoreValidation 200/200, Unity EditMode
3186/3186, official Smoke 5/5 and `git diff --check` PASS. The reviewer could
confirm only the exact candidate diff check and the available Smoke artifact;
the missing durable .NET and full EditMode package prevents exact-input
independent evidence verification. No tests were rerun for this read-only
review. The package is **not promoted**; complete the shared application,
selected runtime/owner migration, detached host/restore, exact evidence package
and subsequent exact-tip review before promotion consideration.

The unrelated `ProjectSettings/EditorBuildSettings.asset` and
`ProjectSettings/ShaderGraphSettings.asset` working-copy changes were excluded
from the candidate and left untouched.
