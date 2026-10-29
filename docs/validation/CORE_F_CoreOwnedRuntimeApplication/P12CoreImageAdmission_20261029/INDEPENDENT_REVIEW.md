# Independent implementation review — P12 Core image admission

## Reviewed identity

- Candidate branch: codex/core/CORE-F-core-owned-runtime-application
- Exact code commit: f41df5791dd31643b00c9edad042b21f523f6046
- Base commit: 3434120b1262c61e33516c4c92b9fb6ca955fa31
- Code tree: 35c4dca1f6ed1b21c7e41d4c28dd31bb4a89321c
- Core canonical at review: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Scope reviewed: the three implementation/test files in the candidate commit.

## Verdict

**PASS — no actionable findings.**

The reviewer verified the exact candidate/base/tree and confirmed the commit contains only the requested three files. The pre-existing ProjectSettings edits and private/untracked evidence are excluded.

The Core application performs the selected host-identity and complete census admission before invoking typed image decoding. The Unity restore coordinator preserves distinct admission and decode failure classification. The legacy path without an application remains unchanged. The regression covers successful application-owned decoding and rejects another application's bootstrap context as WrongThread before exposing census or snapshots.

The review found no changes to P12 schema, capture, mutation, or owner authority semantics. It did not claim full restore orchestration migration, public TryStart, a detached .NET host, or CORE-F completion.

## Evidence checked

- CoreValidation: cold and warm 926/926 PASS.
- Focused Unity restore regression: 1/1 PASS.
- Full Unity EditMode: 3279/3279 PASS.
- Official Smoke filter run: 5/5 PASS. The earlier category-filter invocation that selected zero tests was excluded.
- All three changed-file hashes match the stable Unity input manifests.
- The result-only Unity package verifies against the exact full EditMode run manifest. Archive SHA-256: f0123f67653cc7f416fa22f89120b5f1d9077f74453ff3d86629cce70a24196a.
- The package manifest SHA-256 is 778a136ad0646184d81827507ed323539b8f69df8d7a9df589ce3d25509db5b1.
- Review was read-only; no source or test files were changed by the reviewer.

## Boundary

This is an exact-tip review of the bounded P12 image admission/decode change. It does not approve canonical promotion or claim completion of the broader CORE-F package.