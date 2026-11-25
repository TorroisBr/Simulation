# Independent exact-tip review — restored TravelParty Core bindings

Date: 2026-11-02 (local)

## Verdict

`VALIDATED_CANDIDATE`

## Reviewed identities

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Exact reviewed tip: `f6fbb47fc1d85e2d5b3f20bf688bd2411eca166b`
- Candidate base: `424b2029ab57f64b22bf13a0806fa6908de6c42a`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Implementation source commit: `0df84c30c4000081d30a3e09bb9697e620c5cc7c`
- Implementation source tree: `e8cdea9b48a30eb3ba12af43509f136453fa4008`
- Evidence-only successor before this review: `ec79b69bb70844a5d78fd444fa2460ad5fc4ead5`

The independent review was read-only and examined the complete candidate diff
against the stated base, current architecture, CORE-F runtime/restore design,
P12 contracts, and the exact source and validation records. The evidence-only
successor `f6fbb47` changes only `VALIDATION.md` relative to `ec79b69`; source
and test blobs remain unchanged.

## Findings

No actionable implementation findings.

The Core owner-graph validator is fail-closed, read-only, and deterministic. It
checks the party store and identity index, exact spatial route endpoints and
travel-day identity, member NPC owner state, reciprocal party IDs, synchronized
member progress/start-day state, and unique destination City binding where a
City exists. The Unity restore path first verifies the same Core session and
owner instances, then validates the corresponding Unity object projections.
The legacy non-Core restore path retains its previous validator. Both selected
Core restore call paths use the Core graph validation.

The validator does not mutate authoritative owners or bypass P12 admission.
The new Core tests cover synchronized two-member state, mismatched progress and
party identity, and a route with inconsistent endpoints. A non-blocking test
coverage note remains: the new unit test does not directly vary
`TravelStartedToday`; the invariant is checked in implementation, and the
existing P12-D staged-graph regression covers mismatched start-day flags.

## Validation evidence checked

- Official CoreValidation: cold and warm `997/997` PASS, run
  `db2b9b6fc7a24697a3d1046a0c860a10`; private manifest, Core input digest, and
  original TRX hashes match `VALIDATION.md`.
- Official Unity ALL EditMode: `3296/3296` PASS, run
  `1c75eb4db5e141cc8da8f81c243bffeb`; exact input digest and original XML/log
  identities match the validation record. All five Smoke-named cases pass in
  this full result.
- Official filtered Smoke: `5/5` PASS, run
  `cc49bb61fdb54bf9ae2a8ff7dedb7b09`; source `ec79b69` and tree
  `894a1626c545b31ff3a99b418591774dd967883c` were unchanged before and after.
  Protected original XML/log hashes and private manifest hash match the record.
- The normalized Unity package verifies against its protected original and
  contains one complete result entry. The filtered Smoke original remains
  privately retained; no separate normalized Smoke package is claimed because
  the supported packager rejects that filtered report's inconsistent parent
  suite aggregate. The five identities are present in the packaged full
  EditMode report.
- `git diff --check` passes for the implementation candidate and the
  documentation-only successors.
- Evidence and source records contain no detected private workstation paths or
  credential-like values.

No tests were rerun by the reviewer. The implementation source hashes and
validation evidence identify the same source tree.

## Integration limits

This candidate delivers one restored TravelParty/Core-owner binding check. It
does not complete public `DailyV1Application.TryStart`, full shared P12
restoration lifecycle, detached .NET hosting, separate-process T/T+1/T+2
continuation, or CORE-F acceptance. It does not change Core canonical or expand
the admitted P12 profile.
