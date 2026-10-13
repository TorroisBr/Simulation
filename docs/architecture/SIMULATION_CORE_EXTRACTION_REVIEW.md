# Simulation Core extraction — independent architecture and technical review

**Date:** 2026-10-13. **Disposition:** PASS for architecture/documentation promotion;
CORE-A technical design **PASS / READY_FOR_IMPLEMENTATION** after promotion.
This record transcribes independent reviewer findings; the General Architect
prepared the candidate and recorded the findings, and did not approve their own
technical design. Reviewers inspected read-only and authored no candidate files.
No runtime/test implementation or new test execution is represented here.

## Refs and independent context

- Architecture canonical investigation/base:
  `e940714bcc05c0d790e0cb9a7f614ee6b493e21a`.
- Production code/closed P12 evidence:
  `4cc31073f0ec6ba5c718084e4cebd6e914931d5d`.
- Reviewed cumulative architecture content:
  `f4e11df2645e5c1bc91fdc09bfdfad3f34e5c12e`.
- Independent technical reviewer: `core_first_slice_audit`, source audit and
  technical full review at `019ab6053786706ea1c1bd4c6ad51a287f888bf8`, then
  exact-tip applicability at `d91269c78fe8b817eb610b74163d1570f26256fe`.
- Independent architecture/inventory reviewer: `core_dependency_audit`, full
  cumulative PASS through `f4e11df2645e5c1bc91fdc09bfdfad3f34e5c12e`.
- External independently inspected at
  `0ce8403ba05f778db6850f566a974a4c56cf4edb`, separate Simulation-External repo.

The final f4e11df delta adds explicit file-co-location caveats and makes execution
authority narrower/clearer: this General Architect task ends at reviewed design,
architecture promotion and Master handoff. It does not start code migration or
turn readiness into an implementation grant. It changes no technical behavior,
source closure, required validation or P12 guarantees. The architecture reviewer
independently checked this final delta. Final review/State-only successors require
an exact-tip applicability check under the evidence policy, without a self-SHA loop.

## Findings and disposition

| Severity | Finding | Disposition |
|---|---|---|
| BLOCKER / MAJOR | None in the final reviewed contract. | PASS. |
| MINOR | Generic implementation Skill retained stale future-packager wording. | Resolved: current delivered/supported-scope conditional and original-report fallback are explicit in Skill/AGENTS/Execution Model. |
| MINOR | CSV paths can connect co-located types without a real type-specific reference. | Resolved by explicit file-level/candidate disclosure, concrete example and mandatory per-extraction compiled-closure proof. Inventory is advisory, never a portability certification. |
| MINOR | Initial wording could imply design readiness itself granted code execution. | Resolved in f4e11df: Master consumes technical readiness under its active implementation mandate; this assignment starts no migration. |
| NOTE | Referenced review file was absent during content review. | This record supplies that exact target; metadata applicability is checked before promotion. |

## Evidence actually checked

Reviewers verified 336 sorted unique inventory rows against exact production Git
blobs, categories 45 direct / 242 transitive candidates / 49 no lexical path,
142 Mathf calls on 125 matching lines in 20 files, 13 ScriptableObject classes in
12 files, 238 test sources, four additional Editor C# sources and three isolated
producer C# sources. Lexical/comment-removal and file-graph limitations are explicit.
Representative real transitive seams, the actual Assembly-CSharp boundary and
one existing engine-free producer assembly were inspected.

CORE-A closure is exactly CalendarDefinition (204 lines), SimulationCalendar
(238 lines, including CalendarAgeCalculator) and the **whole** SimulationDate
struct at SimulationTime lines 134–222. SimulationTime stays legacy. The calendar
file has 32 tests, with exactly 28 portable and four Unity/SimulationTime checks.
The three-file BCL closure, package/source uniqueness, SDK links/targets/pins/locks,
Unity import roots and assets/metas, serialized fields and no-engine assembly
boundary were independently checked against the pinned source.

The P12 factory's loaded-project assembly discovery includes Library/ScriptAssemblies,
so the Core MVID must be observed through that existing path, not hardcoded or
removed. New-build same-host continuation and old-descriptor negative rejection
are required; descriptor injection is explicitly distinguished from real old-save
execution. Closed Daily-v1 does not become cross-host/build compatible. Failure
atomicity, genesis/publication, completed-boundary admission and exact owners
remain intact; no profile expansion, P13 readiness or phase reopening.

The .NET runner contract checks complete TRX identity/results/counters, discovery,
input equality and toolchain, retains complete reviewed original results and has
bounded self-tests. Unity uses its delivered packager plus added-input sidecar;
unsupported reports are not forced into that parser. Current full Unity/Smoke
and causal LongRun gates are preserved, with exact two-host/P12/asset gates for A.
Lab, External read-only projection, future phase dependencies and measured versus
forecast disk benefits are separated. One Core authority and no duplicate rules.

Cumulative `git diff --check` passed. All 13 changed content files at f4e11df are
docs, operational Skills/AGENTS or source-inventory CSV; no Assets, Packages,
ProjectSettings or production Tools changed. Unity/.NET runs are **future
implementation gates**, not validation claimed for this documentation candidate.
No remaining product/architectural choice is needed for CORE-A.

## Approved handoff and limitations

Use [CORE-A Master handoff](SIMULATION_CORE_A_MASTER_HANDOFF.md) and
[architecture/migration contract](SIMULATION_CORE_EXTRACTION.md). The reviewed
first slice is implementation-ready after canonical architecture promotion;
actual implementation needs its active mandate, isolated ownership, current-base
refresh, all stated gates and independent exact-tip implementation review.
CORE-B/C are technical-design targets; full world-host and equivalent .NET P12
continuation remain WAIT_DEPENDENCY. No automatic code promotion or broader
save guarantee follows from this design PASS.
