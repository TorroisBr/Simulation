# CORE-F Core-owned restored-image owner-stage orchestration

## Scope and source identity

This is a recoverable intermediate CORE-F implementation checkpoint. The Core
application now owns the ordered private image-restore sequence after P12-C:
P12-D owner hydration, record/event authority creation, P12-E owners, P12-F
owners, then Core session/calendar composition. The Unity coordinator receives
only completed-stage boundaries and projects those exact instances at the
existing D/E/F host boundaries. The final session remains private for the
existing outer publication/commit gate.

The P12-D Core stage also owns the exact empty `ExplorableSiteStore` admitted by
the current Daily-v1 profile. The Unity projection reuses that store; nonempty
site snapshots are rejected. This preserves the P12 profile boundary and does
not add P10 site content.

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Base: `ec357a6ea833fb6965e92153b28ad3ce0bdd439d`
- Implementation commit: `f4c9c0260ad884856990f1909b04caf1b8b12196`
- Implementation tree: `eb6b97f9ee3e148c697e0bceae360032fd42d51c`
- Core canonical during validation: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

The eight-file implementation/test change is limited to the Core restore-stage
application and P12-D owner staging, the Unity D/E/F projection and restore
coordinator, and `DailyV1P12DSelectedCoreOwnerStagerTests`. No ProjectSettings,
untracked user files, or protected evidence files were included.

## Validation

All accepted runs below used the exact implementation commit and report stable
inputs for that run. Protected originals and private manifests remain in the
owner-only E: evidence store; only the sanitized full EditMode package is
published here.

- Official CoreValidation run `5d7a4200a74f4a0daa20b6ac421b419f`: cold and warm
  **1,010/1,010 PASS**; SDK `9.0.315`; source inputs stable; `Library` absent.
  Input digest: `16e48303482c5f198f29477204ae75028c8367564670f60c6ab7113ebaa5f613`.
  Private runner-manifest SHA-256:
  `4d6b7c8216d5864b7a61b42a8ba3983ea2aaed61a7ce3c7137327be16ec946ba`.
  Original cold/warm TRX files are each 1,424,058 bytes, with SHA-256
  `9fcead7cc5192963664c2522d9dcf88ed5629d9e7916f7f2fd6f0c478d4a8156` and
  `0e509979781d5111b7f4abad6f11ba4ad14cadfa8091291cd273b0c13a765bf6`.
  The original TRX and manifest remain private; no unsupported public TRX
  context was fabricated.
- Focused Unity P12 restore regression, run
  `eb52832b73534c5aabe062aac1ece8ca`, filter
  `CoreImageRestoreRejectsPrivateTargetRosterMutationAfterOwnerVectorCaptureAtomically`:
  **1/1 PASS**, zero failures, stable inputs. Private manifest SHA-256:
  `90b8e8e070bda8cff9fc604f7f6b6c64b904bf736618d979e71d088718f8814d`.
- P12 restore suite, run `94cca392ba7940008ed6de7cf82f02c8`, filter
  `P12GPoliticalBindingRestoreTests`: **16/16 PASS**, zero failures, stable
  inputs. Private manifest SHA-256:
  `cf84a86d070297b364c72387f1c1cb10bef66561daf1bd035ead862a8005467e`.
- P12 owner-vector suite, run `8916a30c512948fd89de92ac19d94295`, filter
  `P12GDailyV1OwnerVectorTests`: **16/16 PASS**, zero failures, stable inputs.
  Private manifest SHA-256:
  `8ae923a20afc885c08c7712a4a41b9ac0ede18769515b6c305c4fb411f73eedb`.
- Official EditMode Smoke, run `41769264b7dd48539f2461641b550a52`, filter
  `Smoke`: **5/5 PASS**, zero failures, skips, or inconclusive results, stable
  inputs. These cases are authored in the EditMode assembly and transition to
  Play Mode inside their fixtures. Private manifest SHA-256:
  `f8a04dc8f82d08c7373d7306b97a3abbc5f6cbb4b7c8ba476ffa965a34467cf2`.
- Full official Unity EditMode run `bd6c77e785d143bf97abe5a29381a1b6`:
  **3,299/3,299 PASS**, zero failures, skips, or inconclusive results; exit 0;
  `inputStable=true`; Unity `6000.3.9f1`. Input digest:
  `8432187396397b906fdf44e08b14b2f56c2fe33cd8b32c4d8db51e71ebed37c4`.
  Private run-manifest SHA-256:
  `525caba420ea19086c146c4b5318ddc1784fbdc0fe379a46a5e264cd5ba544cb`.
  Original XML: 3,290,107 bytes, SHA-256
  `c35b33328aa818b4649bb437aaf3ef4c74879a05cb6000bc540c515868eef2be`.
  Original log: 249,339,914 bytes, SHA-256
  `bac8ef61d7beb758cbe3ac6bbb985e9f6a055f371c65e323b61e04d76c5a4a8f`.
- Sanitized full EditMode evidence package: [archive](coref-restore-stage-f4c9c-all-editmode.zip)
  and [manifest](coref-restore-stage-f4c9c-all-editmode.manifest.json) contain
  one normalized full-suite XML report. The approved
  verifier passed with `runs=1 entries=1`. Archive SHA-256:
  `c01a9346898933a4d72d4b5bb2ce9efc9179aa608bcc25e784eea0f22a809e10`;
  manifest SHA-256:
  `0a5bc630de90f1fa98a779f459f053fb015748338cd6b3b93fc429c4b493bf80`.
  Filtered XML and raw logs remain private; the full report contains those
  selected cases and avoids publishing subset XML with stale suite aggregates.
- `git diff --check`: PASS before the implementation commit; rerun after this
  validation record is committed.

## Superseded attempts

- CoreValidation run `024f4da7714a4ba1862397d8a6c7095a` stopped at cold build
  because the direct Core stager test calls had not yet supplied the newly
  admitted site snapshot. The call sites and exact-empty regression were
  updated; the accepted exact-commit cold/warm run above passes.
- Unity run `6437cc3538584254b8ae051317a6fe40` produced no XML because the first
  projection edit referenced a stage local outside its method. The reuse was
  moved to the image-projection path, the Core and Unity suites then compiled,
  and the accepted reports above pass.
- First import run `bf6a7011c72646f3914c8d8b083ccb6f` passed its one selected test
  but reported unstable inputs after Unity initialized two ProjectSettings
  files. It is excluded. Later accepted runs report stable input inventories.
- Run `04ee9d3fe7ea4009b1443fbe6ec433af` requested the PlayMode platform for
  fixtures declared in the EditMode assembly and selected zero tests. It is
  excluded. The correct EditMode `Smoke` run above passed all five cases.

The implementation worktree's two ProjectSettings files changed by the first
Unity launch were restored to their clean pre-run state before the implementation
commit. No ProjectSettings change was staged or committed. The disposable Unity
validation worktree and its generated Library remain separate from the
implementation worktree.

## Status and limitations

This commit is a recoverable, validated implementation checkpoint, not
independent review acceptance or canonical promotion. CORE-F remains incomplete;
the full detached .NET host and separate-process T/T+1/T+2 continuation are not
claimed here.
