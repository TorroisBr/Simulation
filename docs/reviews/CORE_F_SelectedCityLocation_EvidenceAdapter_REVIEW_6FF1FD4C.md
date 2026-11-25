# CORE-F Selected City Location and TRX Adapter Review

**Verdict: `VALIDATED_CANDIDATE`**
**Review date:** 2026-10-23
**Independent reviewer:** separate Codex review agent `coref_exact_tip_review_36597787`
**Candidate branch:** `codex/core/CORE-F-core-owned-runtime-application`
**Base:** `0d487af641ba7854b87a116107dcf1afbc3d430c`
**Reviewed implementation:** `06a5ef0f86a124802f71485579981703fa007112`
**Candidate tip:** `6ff1fd4c262c4e05bb236aca29cf4373efb2d451`
**Core canonical:** `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

## Scope reviewed

The selected-location implementation derives the actor's current-city
eligibility from the exact admitted Core owner graph. It resolves the actor's
Person identity and position through the bound Person and spatial-position
owners, and the City's Location through the bound anchor owner and spatial
authority. Missing position preserves the existing no-position behavior;
transit, unresolved anchors, and mismatched locations fail closed. The change
does not alter daily ordering, RNG, or mutation sequencing.

The additive evidence-adapter correction binds the official cold/warm
`--settings` arguments to the protected runsettings files, verifies their
recorded byte lengths and SHA-256 values, and accepts only the closed XML
structure that names the corresponding protected runtime trace. The adapter
projects private settings paths in the public command record. Synthetic tests
cover a missing settings argument, trace redirection with an updated hash, and
a settings-hash mismatch.

## Validation evidence

- Unity EditMode: **3,254/3,254 passed**; official Smoke: **5/5 passed**.
  The exact Unity input snapshot is recorded in the validation manifest. The
  Unity code/test source is unchanged from implementation commit `06a5ef0`.
- CoreValidation: **669/669 cold and 669/669 warm passed** at source commit
  `dc1e81793a3805a36eb1dce0fa64984fec86adb2`, tree
  `a0f820e988e1d20308a28019350b443f783ce1c7`, over 340 inputs with digest
  `2e25c61be420c275b67cd547151cde5569c8293289219332e41ad938b8ad8e67`.
  The later `6ff1fd4` commit changes only the evidence adapter and README; no
  C# source or test files changed after those runs.
- Evidence-adapter tests at candidate tip `6ff1fd4`: TRX adapter **153
  assertions passed**; package adapter **65 assertions passed**.
- The official TRX packager verified the original-to-normalized comparison and
  archive integrity: two reports, archive SHA-256
  `8621a8c8226a2a7d1c7d20e78e03d18284ccc96fbdf18095faae6e554d5d976c`.
- Unity result archive SHA-256:
  `4515ba715fb02707b02c07c7f1b2721f2e1416e4da2551e28f936f937622c840`.
- `git diff --check` passed for the committed candidate changes.

The implementation author did not perform this review. The reviewer reported
no actionable defects. The review does not establish full CORE-F acceptance,
does not approve canonical promotion, and leaves shared `DailyV1Application`,
detached restoration, and .NET host integration as outstanding CORE-F work.

At review time the reviewer's remote observation preceded publication of
`6ff1fd4`. The exact reviewed commit was subsequently pushed without source
changes; a fresh `git ls-remote` check confirmed the remote candidate ref equals
`6ff1fd4c262c4e05bb236aca29cf4373efb2d451`. The implementation worktree also
contains unrelated ProjectSettings edits and protected/untracked evidence, all
excluded from candidate commits.
