# WX-D v2 implementation candidate review

**Result: `VALIDATED_CANDIDATE`**

## Candidate identity and current canonical

- Simulation canonical base and current canonical at review:
  `e76e50bfefc68b827d54ceb74e0b25293e0ad7b8`
- Current remote ref: `origin/codex/phase12/canonical`
- Candidate branch: `codex/wxd/WXDv2ProducerCurrentBase`
- Reviewed production code commit:
  `bc3d4a31e45549fd91cd22a88022c93d39f2707f`
- Reviewed production code tree:
  `088f383db5b4b5fc51e8db0f8687c413709a5796`
- Candidate/evidence documentation tip reviewed:
  `3641e20b9ae92580febd56fa9cabab631fc75eee`
- External contract live ref: Simulation-External `origin/main` at
  `0ce8403ba05f778db6850f566a974a4c56cf4edb`
- External schema source blob:
  `5619013647c31d969a7cd49e0563ff68c78cdde3`

The candidate is a descendant of the exact current canonical base. The live
Phase 12 canonical and External main refs matched those SHAs when checked for
the review. The full implementation delta and candidate validation artifacts
are recorded in
[`WXD_V2_IMPLEMENTATION_CANDIDATE.md`](WXD_V2_IMPLEMENTATION_CANDIDATE.md).

## Independent review

The independent read-only reviewer was `/root/wxd_exact_review`, separate
from the implementation author. The reviewer inspected the complete candidate
against the exact base, checked its code/tree identity, checked the current
canonical and External refs, and confirmed all documented XML/log and fixture
SHA-256 values against the retained evidence.

The initial review returned `NEEDS_CHANGES` for one documentation-only
inaccuracy: two notes said coherent boundary evidence was passed into the
producer package. The implementation validates boundary, revision,
capability, and source-version evidence in the Unity host and passes only
`WorldId` and copied Faction ID/display-name values to the package. That
wording was corrected in
[`WXD_V2_IMPLEMENTATION_CANDIDATE.md`](WXD_V2_IMPLEMENTATION_CANDIDATE.md)
and [`WXD_V2_CURRENT_BASE_REVALIDATION.md`](WXD_V2_CURRENT_BASE_REVALIDATION.md)
by docs-only commit `3641e20b9ae92580febd56fa9cabab631fc75eee`. The reviewer
confirmed the exact docs-only diff, found no code, test, package, or evidence
changes, and returned `VALIDATED_CANDIDATE` with no remaining findings.

## Findings

The reviewer found no blocking implementation issue. The exact code review
confirmed:

- one coherent FR-C capture for `simulation.faction-truth/v1`, with
  fail-closed handling of unavailable, malformed, or incoherent results;
- the host validates logical boundary, store revisions, capability, and
  source-version evidence before creating the package projection;
- truthful complete-Faction coverage and all nine schema-v2 arrays and
  coverage entries, with the other eight collections empty and
  `UNSUPPORTED`;
- copied immutable factual output, strict UTF-8/base64url Faction identity
  mapping, deterministic serialization, and atomic file publication;
- no edits to `SimulationRuntime`, bootstrap composition, World Identity,
  factual stores, FR-B, FR-C, P12 runtime/census hotspots, or
  Simulation-External.

## Validation and remaining test gaps

The reviewed production code SHA/tree is exactly the code SHA/tree on which
validation ran. The retained evidence passed:

- WX-D focused tests: 7/7;
- FactualRead foundation: 9/9;
- FR-C Faction factual reader: 7/7;
- Simulation bootstrap composition: 21/21;
- ALL EditMode: 2212/2212;
- official EditMode Smoke: 5/5;
- External `validateWorldExchange`: valid schema v2, no issues, two Factions;
- `git diff --check`: PASS.

Evidence XML/log/fixture paths and SHA-256 values are listed in
`WXD_V2_IMPLEMENTATION_CANDIDATE.md`. The reviewer ran no tests and changed no
files. The docs-only correction after the first review does not affect
executable code; all required tests remain tied to the unchanged reviewed code
tree. No required gate is outstanding for this bounded candidate.

## Scope and integration constraints

This review covers only the first World Exchange v2 producer described in the
promoted design. It does not establish broader FactualRead completeness,
P12-B completion, save/load, capture eligibility, P12-A readiness, P13
readiness, or World Exchange production readiness. WX-D does not decide or
implement the Simulation-External collection-coverage contract.

This is a validated candidate, not a canonical promotion. Do not move or
rewrite `codex/phase12/canonical`; the Phase Master remains its sole writer.
The candidate is based on canonical `e76e50bfefc68b827d54ceb74e0b25293e0ad7b8`;
if that ref advances before consumption, classify/reintegrate and revalidate
before promotion. No canonical ref was moved by the candidate or review work.
