# CORE-F selected census source integration — exact-tip independent review

**Verdict: `VALIDATED_CANDIDATE`**
This is an implementation review only. It does not promote Core canonical or mark CORE-F complete.

## Reviewed identities

- Repository: `https://github.com/TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Actual remote candidate tip: `552966366307bde2fe15764a6dfcb77856d7eb38`
- Candidate final tree: `037180ff68cb31de5426c284a1da190fb4030317`
- Reviewed implementation commit: `bab7f816cedcdbb4ba7e61990ad8d864a3477663`
- Implementation tree: `b6a464a450fd7a5816c012f36649cbbdfebb9d20`
- Implementation parent / review base: `40a687780f2cb1648f44524ccdc1e9390bc231c1`
- Base tree: `efba9e4e9335885d41969c97673455980fe8f79e`
- Current Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

The remote candidate still points to the reviewed final tip and Core canonical still points to the stated baseline. The final candidate commit has the implementation commit as its parent. The base-to-implementation diff is limited to the five declared code/test files; the implementation-to-final-tip diff adds only the validation record and the two Unity evidence-package files. The implementation tree is unchanged after validation.

## Review scope and result

I reviewed the complete `40a68778..bab7f816` production/test diff and the exact-tip validation material at `55296636`, against the current candidate-review procedure, CORE-F Runtime/Restore Integration Design, CORE-F closure contract, and Phase 12's accepted `UnityBootstrap-Daily-v1` continuation boundary.

The change removes Unity-side filtering of selected Core-owned census rows. Unity now returns its full quiescent census and mutation epoch; Core validates the selected graph and composes the exact selected vector. This is an appropriate source-boundary move: the host no longer decides which of the Core-owned rows to hide before Core admission or restore validation.

The admission and restored-capture paths both validate the exact Justice projection identity and P18 receipt revision, derive the selected P12-F witnesses from the admitted Core graph, and then pass the composed vector through the existing complete-owner coverage checks. P12-F composition checks exact owner instance, role, schema, cardinality, and revision for host-supplied rows; it rejects partial P12-F sets and substituted facts. When those rows are absent, the Core graph is the source used to derive them. The exact selected NPC Inventory/MoneyAccount and related owner rows continue to be validated/rebound by the existing Core helpers.

The mixed-roster branch remains all-or-whole-host for compatibility NPCs. Inconsistent Inventory/MoneyAccount family counts fail closed; a mixed roster keeps its full legacy census and does not bind a partial Core NPC D/F source set. The existing mixed-roster Unity regression still verifies selected and unselected NPC rows and the legacy Crime/Justice writer behavior.

Deterministic section ordering remains ordinal. Owner identity, exact graph binding, local revisions, receipt revision checks, and the host-provided quiescence/mutation-epoch contract are retained. This change does not claim that Core owns Unity's host quiescence mechanism or shared mutation epoch.

The test diff strengthens the Unity integration assertions to require a complete host vector and compare each section's role/schema/owner identity/cardinality/revision with the completed token. The Core session-gate fake now supplies the full vector instead of simulating Unity filtering. I found no weakened assertions or test-only production behavior. Existing Core composition coverage also exercises omitted, partial, duplicated, stale, and substituted census facts and complete selected-owner-vector reconstruction.

No actionable implementation or test-integrity finding remains for this bounded checkpoint.

## Exact validation and evidence

The validation record identifies:

- Focused Core session-gate tests: 77/77 PASS.
- Official CoreValidation cold and warm: 1,017/1,017 each, zero failures, skips, inconclusive, or aborted results. The original private TRX hashes match the protected CoreValidation manifest; the three changed Core source/test files match the manifest's stable input hashes.
- Focused Unity admission: 1/1 PASS.
- Full Unity EditMode: 3,299/3,299 PASS, zero failed, skipped, or inconclusive cases.
- Official Smoke: 5/5 PASS; the five case identities are also present in the full EditMode run.
- `git diff --check`: PASS.

The Unity source snapshot is recorded against base commit `40a68778` and reports a stable input. All four Unity-scoped files changed by the implementation commit have hashes matching that snapshot. The three Core source/test files changed by the implementation commit have hashes matching the private CoreValidation input manifest, which also reports stable input. Thus the validated inputs match the exact implementation tree even though validation preceded its commit.

The official Unity evidence package was independently archive-verified: one run, one entry, archive SHA-256 `208d85958a2bdffdbd3e09ff37bcb1598819036c9da2d6fb17c94103c147e0a4`; manifest SHA-256 `6c9c05e0b97382b5a1ae64cff8469c6803b48a2c87cc1be485f000328554559b`. The public validation record and package manifest contain no private workstation paths.

The CoreValidation raw reports and native manifest remain protected/private. The public TRX adapter rejected the native `simulation-core-validation/v1` schema, so no public Core evidence package is claimed. The reported Core run had a Unity Library present; no Library-free validation claim is made. The standalone filtered Smoke package was rejected due to aggregate-count inconsistency; the original evidence was not edited, and Smoke is represented by its five passing identities in the complete EditMode run.

## Scope limits

This checkpoint validates the Core-owned selected census-source composition boundary only. It does not complete CORE-F, deliver a detached .NET host, prove separate-process T/T+1/T+2 continuation, or promote any code to Core canonical. Phase 12's accepted closure remains limited to its documented same-build/current-host Daily-v1 continuation profile; this review makes no broader profile, host-quiescence, or universal owner-coverage claim.
