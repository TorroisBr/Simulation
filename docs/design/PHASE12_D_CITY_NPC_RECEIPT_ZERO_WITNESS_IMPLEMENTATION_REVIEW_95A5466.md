# P12-D City/NPC receipt-owner exact-zero witness — implementation review

**Verdict: NEEDS_CHANGES.** The source implementation is consistent with the
reviewed bounded design, but two required validation cases are missing. Do not
promote this candidate on this review. The implementation candidate remains
unchanged.

## Exact identity and authority

- Candidate branch: `codex/phase12/P12DCityNpcReceiptWitnessImplementation`
- Exact remote candidate: `95a5466a78432b245ff2ec835c726c06e795cfc7`
- Candidate tree: `e587c981c0152cbf22f251424a8802b9922fc193`
- Exact base / current remote P12 canonical: `04e7c3f49a7c9690ebc091fcfc50f05ef67009d6`
- `merge-base(base,candidate)`: `04e7c3f49a7c9690ebc091fcfc50f05ef67009d6`
- Candidate is one commit ahead of base and is a clean fast-forward from it.
- Remote candidate ref still resolves to the exact candidate SHA. The P12
  canonical ref still resolves to the exact base.
- Architecture baseline reviewed: `47eff220c7ce00f6e7c759bdc2b76780bb46f628`
- Accepted technical design: `5b10a58a680af9adcf56a585f6f2ac50f8e6b172`
- Exact-content design review PASS: `d4d2e343d3297d1dfc61b9ff52c9db25aba464e3`
- Repository `AGENTS.md`, candidate-review Skill, current Execution Model,
  Phase 12 Brief, and Phase 12 State were read. The current State leaves D
  incomplete and this unpromoted candidate does not change that status.

The complete base-to-candidate diff was reviewed. It adds the two exact-zero
per-NPC receipt-owner families and their roster reconciliation, the required
raw read accessors, focused/composition test changes, and committed validation
artifacts. Unrelated `ProjectSettings` and `.meta` user changes are outside the
candidate diff.

## Source review

No implementation defect was found in the reviewed exact-zero path:

- Both P18 owner read methods inspect the serialized backing list and revision
  directly; they do not use the lazy `ReceiptList` accessor or expose receipt
  contents. Null lists and negative revisions fail without initializing state.
- Providers retain the exact embedded owner and parent `NpcRuntime`, verify
  those references on each read, and reject every nonzero count or revision.
  Required section role is not mistaken for an exact-zero rule.
- The factory produces two deterministic schema-v1 rows per current NPC and
  rejects null/duplicate NPCs, duplicate RuntimeIds, missing child owners, and
  aliased receipt owners.
- The protocol binds the family into the existing owner-section vector and
  checks exact family/roster identity during assessment and capture. Reconcile
  stages the receipt family with the other owner families, checks epoch
  capacity before publishing the staged maps, and folds membership changes
  into the existing single shared-epoch decision.
- An unchanged RuntimeId cannot silently replace its NPC or child owner. The
  exact same unregistered NPC object can be re-added with its original child
  owners; the selected identity registry rejects a different object reusing
  that RuntimeId.
- The changes do not add P18 receipt export/replay behavior, a receipt-specific
  epoch or notification, new P12-B operation semantics, or P12-A/P13 claims.

The review also checked the exact selected-profile inventory integration,
owner-thread/quiescence path, and unchanged-section validation. No new external
or adversarial security boundary is introduced.

## Required changes

1. **Exercise LocalObservation through completed-token validation.**
   `SelectedRuntimeVectorIncludesRequiredExactReceiptRowsAndSurfacesLiveInventory`
   captures a token and then commits a MerchantTradeState receipt before
   asserting that token validation rejects it. The separate
   `EveryProviderReadRejectsACommittedReceipt` test proves direct provider
   rejection for both owner kinds, but does not prove that a LocalObservation
   write invalidates an already-issued `DailyCaptureEligibilityToken`. The
   accepted design's validation plan requires the receipt-owner change to be
   detected through the same capture vector. Add the LocalObservation
   post-token case (preferably parameterize over both kinds) and assert token
   validation fails for the changed row.

2. **Prove failed receipt-family reconciliation publishes no partial rows.**
   `CommittedRosterAddRemoveAndSameObjectReAddReconcileBothRowsOnce` covers
   successful add/remove/re-add and a `DuplicateRuntimeId` rejected before
   commit. It does not exercise a committed roster addition whose new receipt
   owner is malformed or nonzero, causing the receipt-family rebuild to fail.
   The accepted design requires a failed provider rebuild/reconciliation to
   leave the receipt-family maps/provider inventory unpublished, leave the
   epoch unchanged, and fault-close the existing census boundary. Add this
   case and assert those outcomes. The design also identifies epoch-capacity
   failure as an atomicity boundary; existing generic coverage is not specific
   evidence that the new receipt family remains unpublished on that path.

These are validation gaps rather than an observed source-level semantic defect.
The candidate is therefore retained for correction, but is not a
`VALIDATED_CANDIDATE` yet.

## Validation evidence checked

The committed manifest is
`docs/validation/P12DCityNpcReceiptOwnerCensus/VALIDATION.md`. It reports 12
focused suites at 137/137, ALL EditMode at 2578/2578, official EditMode Smoke
at 5/5, and `git diff --check` PASS. The 14 retained XML and compressed-log
SHA-256 pairs match the manifest. The eleven implementation source-file
SHA-256 values checked in the manifest also match the exact candidate
worktree. No Unity tests were rerun during this source review.

The full validation evidence belongs to the exact candidate tree, but it does
not cover the two missing assertions above. After correction, the changed code
tree requires affected focused suites, ALL EditMode, official Smoke, and
`git diff --check` again, followed by a fresh independent exact-tip review.

## Scope and integration constraints

This review concerns only the two D-owned exact-zero NPC receipt-owner census
families and dynamic roster reconciliation. It does not deliver the City/NPC
snapshot assembler or complete P12-D, alter P12-B's bounded status, or imply
complete owner/epoch coverage, capture eligibility beyond the existing
validated boundary, export, hydration, P12-A readiness, P13 readiness, or
Phase closure. P12-A remains `WAIT_DEPENDENCY`; P12-B remains incomplete;
P13 remains blocked.

Candidate promotion remains a separate later step. Revalidate the actual
candidate tip, current canonical ancestry, exact reviewed tree, and all
required evidence after the requested test fixes.
