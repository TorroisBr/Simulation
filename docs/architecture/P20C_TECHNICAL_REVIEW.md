# P20-C independent technical and architecture review — 2026-10-07

**Verdict:** PASS — READY_FOR_IMPLEMENTATION for the refreshed technical design.
**Exact reviewed content tip:** `461a045cbdc81c1e34faedc56e73b0c6878d3213`.
**Architecture base:** `e16796014d348e3b59da7ed848101c4c03926ba5`.
**Independent reviewer:** delegated read-only Luna reviewer
`/root/p20c_independent_review`, independent of the retained design author
and the General Architect who authored the refreshed record. No files or code
were written by the reviewer. The final review-record/readiness assembly is a
separate additive delta and requires applicability confirmation before push.

## Review scope and evidence

Read AGENTS, current architecture/Execution Model/Roadmap, Phase Briefs/States,
the complete immutable P20B_TECHNICAL_DESIGN and historical review, PR #1
handoff and Phase20 candidate revalidation. Inspect current canonical P8/P18/
P20/P12 code and relevant tests against the exact refs in
[P20C_TECHNICAL_DESIGN_RECORD.md](P20C_TECHNICAL_DESIGN_RECORD.md).
Review the full documentation candidate diff against the architecture base;
`git diff --check` passed. No Unity tests were run or claimed for this
documentation-only task.

The old design is substantively compatible but **not approved as-is** for
current dispatch. A refreshed P20-C record is required for the new identity,
current APIs/bases, already-promoted P20-B profile boundary and the bounded
FailedStart correction. That record and the retained source now constitute
the reviewed contract. The verdict approves design readiness, not existing
code, domain delivery, canonical promotion or Phase closure.

## Findings and resolution

**MAJOR — FailedStart reconstruction gap: closed in design; open in code.**
P18 increments lifecycle revision, commits Cancelled/FailedStart and releases
both commitments; current P20 `CommitFailedStart` is empty, but private
restore requires exact lifecycle-token equality. Existing tests exercise failed
start without a corresponding failed-start restoration regression.

The refreshed contract selects a specific correction: prebuild P20 replacement
row/root with expected lifecycle revision n+1 for either start outcome; install
that token in the same P18/timeline failed transition, without P8 installation,
new assent revision or new causal order. Preserve strict restore equality;
handle P18 validator rejection even if P8 preparation succeeds; distinguish
genuine rejection from stale/malformed captured state before publication.
Required reconstruction regressions close implementation evidence before
P20-C delivery. There is no remaining worker-invented semantic choice here.

**MINOR — Roadmap checkpoint conflict: resolved in candidate.**
Current planning names C as travel, B as promoted admission and A as synthetic.
Historical 2026-10-03 B travel language, historical review and owning State/
promotion evidence remain identifiable. Canonical publication retains its
normal approval gate; that is not an unresolved consumer product choice.

**MINOR — tree-kind ambiguity: resolved.**
The handoff distinguishes repository tree `62f8f3f...` from Assets tree
`8b579d9f...`. Existing admission validation cannot be promoted by relabeling
as P20-C travel evidence.

**NOTE — current implementation differs from design readiness.**
Relevant existing APIs support the chosen P18/P8 transaction shape: one
lifecycle transition participant, P18 open-ended pair reservation, prepared
due-start/consumer terminal transition, P8 per-store batch replacement and
distinct per-Person movement context. Required atomicity/failure evidence
remains a Master code-review/validation duty. Current P12 and P20 lines need
explicit integration comparison and rejection tests; one branch's census hook
does not establish complete integrated profile composition.

## Approved constraints

Two Persons are only the fixture. P18 owns lifecycle/reservations/timeline;
P20 owns proposal/independent decisions/abort; P8 owns individual travel.
Each retains PersonId, Knowledge, position/progress and outcomes. One coherent
required start, explicit per-person progress, staggered arrival and
second-arrival completion/AbortAfterLeg settlement remain bounded. No persistent
Group/Party, generic role solver/dispatcher, new scheduler or gameplay manager.

Hard promoted capability edges are P18-A/B/C, P20-A and P8-E.
P20-B/P12 is the existing composition/rejection constraint. P18-D, full P12,
P13, P19 and Group membership are not prerequisites; P11 is conditional on
separately selected external commands. P12 scope remains unchanged.
The Lab demonstration is a follow-up, not a new hidden delivery prerequisite.

## Exact readiness and handoff

- Technical contract: READY_FOR_IMPLEMENTATION after independent design PASS.
- Architecture candidate: documentation/planning only; normal human canonical
  promotion gate remains.
- Master scheduling: MUST WAIT for that gate and a safe serialized hotspot
  window, then audit/correct/validate existing code rather than duplicate it.
- P20-C delivered capability: NOT PROMOTED; FailedStart code correction and
  fresh P20-C/current-P12 integration evidence remain required.
- Product/semantic decisions: none outstanding within the selected scope.
- Durable handoff: `docs/architecture/P20C_JOINT_CIVIL_TRAVEL_MASTER_HANDOFF.md`.

No P20-C code, P12 file/scope change, numbered-phase State rewrite or active
Master implementation checkout change is included.

## Final packet applicability — independently confirmed

The same independent reviewer reviewed the full canonical-base diff and final
assembly delta at `1ef5e9c336ad9338c526c0df3dc8c55f1171afd8`.
**Final packet verdict: PASS.** The approved technical content at `461a045`
remains applicable: intervening changes add review/State evidence and clarify
readiness/history; no technical boundary changed. Both diff-checks passed.
No unrecorded durable boundary remains. This appendix records that verdict
without changing scope, readiness, code or canonical publication authority.
