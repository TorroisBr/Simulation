# Independent Review — CORE-F Selected Daily-v1 NPC Materialization

## Verdict

**VALIDATED_CANDIDATE** — no actionable implementation or test-integrity findings.

## Exact identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Candidate commit: `126998ab8c4030c6408bfeaf976104bc86c50ff5`
- Candidate tree: `109bb5528384240f5ae8e94dc398afa4740d78cc`
- Base: `357cf9357500b486224467aafb5635ca81898c5f`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Candidate remote ref was confirmed at the reviewed commit; Core canonical remote remained at the SHA above.

The independent reviewer inspected the complete candidate diff, approved selected Daily-v1 construction path, owner identity and binding behavior, P12 membership transaction, tests and evidence. The change uses the exact `NpcData` binding to resolve the admitted `DailyV1NpcDefinition` node, builds the existing Core NPC owner group before publication, and retains the legacy constructor path for mixed or unselected rosters. The starting City is not attached before the existing membership transaction publishes it. Rejection occurs before owner writes.

The focused test verifies rejected-input non-mutation (PersonStore revision, NPC roster, City presence count/revision and P12 mutation epoch) and successful exact asset, definition, effective-graph and Core-owner identity. The success path retains the existing one-epoch and selected census assertions. The reviewer found no weakened assertions or actionable defect.

## Validation evidence inspected

- Official focused EditMode: 1/1; run `496ab9a77d3944df99d1161bd07ebaa6`.
- Full EditMode: 3,316/3,316; run `3aa52e426d134a19827f9d78e74985b8`.
- Official Smoke: 5/5; run `7a79586d6b53422baf204d8e6fe62ebd`.
- Each run had stable before/after input inventory SHA-256 `4a037ce97fecc6c8595acebed7c4bb590ea93c0e4d630c220699a3ad33ebf908` and a stable Unity executable.
- The focused case and five Smoke cases' identities/outcomes are present in the complete EditMode report.
- Result-only full EditMode archive SHA-256 `2d74397d018b684bbe42a2218815880bf394ad45dc9f897cd483c80068b040ec`; manifest SHA-256 `ee191ee8168994c77b8891dc246d3a940d7251f6c75b4fecc29d00db967dda16`. Package verification against the protected full-run manifest passed under pinned PowerShell 7.6.5.
- The filtered Smoke XML was not separately packaged because its selected parent-suite count disagrees with its descendants; the original is unchanged and its cases are included in the complete report. This limitation is recorded in the validation record.
- `git diff --check`: PASS.

The Unity input inventory included the pre-existing local ProjectSettings asset state. That user-owned edit and the unrelated validation-document edit were not part of the candidate diff and remain untouched. The validation record makes this boundary explicit.

## Scope and limitations

This is a reviewed candidate only. It does not promote Core canonical or complete CORE-F, the full selected runtime membership boundary, `DailyV1Application.TryStart`, detached restoration, the .NET host, or cross-process continuation. Core canonical was not changed.