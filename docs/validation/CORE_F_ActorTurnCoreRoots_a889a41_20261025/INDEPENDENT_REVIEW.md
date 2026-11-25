# CORE-F Actor Turn Core Roots - independent review

## Verdict

**VALIDATED_CANDIDATE** - bounded implementation review and evidence applicability passed. This is not canonical promotion or CORE-F completion.

## Review identity

- Branch: `codex/core/CORE-F-p12d-core-hydration`
- Candidate commit/tree: `a889a41c6451afc8c13fd2e3289487b5dc7259d0` / `bb651f573a4368dfeca80a622d97ed8d097c68a5`
- Base: `334c386abe5cce8ed68ab72a907a0a2424c3acbb`
- Current Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Review date: 2026-10-25

## Review result

The complete 10-file delta was reviewed against its base: Core actor-turn, actor-choice, action, Merchant, and daily-stage code; the Unity adapter; and four focused Core test files.

- `DailyV1ActorTurnCoreRoots` takes RNG, allocator, record sequence, and decision store from the bound Core execution state. Identity checks bind those exact roots to the state, owner graph, PersonStore, and ActorChoiceStore.
- The admitted daily stage validates roots and selected Core boundaries before logger/daily owner callbacks. Missing or mismatched roots fail closed.
- Admitted autonomous actions, scheduled actions, actor choices, and Merchant redirect decisions use Core roots; host decision owners/RNG are not consulted for these admitted operations.
- Directive, choice, Merchant, and autonomous action paths cannot fall back to legacy host callbacks after admission. Actor-choice preflight requires the exact same Core action/choice boundary.
- Existing action/Merchant identities, P12 admission/projection ports, roster order, and decision origins remain intact. The unadmitted path retains its host-boundary behavior.
- Updated tests prove Core-owned roots and no fallback after selection. The changed host-owner-lookup tests now prove state-bound roots are authoritative; fail-closed tests for unbound state and host-owned fallback remain.

No actionable implementation or test-integrity finding remains.

## Evidence reviewed

- CoreValidation `731a8294326a4df9b4b652e3baa54b17`: cold/warm 804/804; private manifest SHA-256 `15b11ca642f2d9c84aaad9d2805404fc9f7c8f7a33ad98c40af48cadbd1c1f25`. Nine changed Core source/test hashes match the exact candidate worktree. Unity adapter is outside this runner inventory.
- Unity EditMode `122d30905b1a4325a53a23ebcb08d0bf`: 3264/3264, stable snapshot bound to exact candidate HEAD/tree. Archive SHA-256 `e6ea6e80a3d19a769b6645b12edbdb2a0bd4b8a83cfa57f5990535fb052306fe`; public manifest SHA-256 `9fbe03e7510a0345367d894d44d223d5090151ef4138fd1f63ba596c8c878229`. Changed Unity/Packages production input hashes match the candidate worktree.
- Private Smoke filter `0844feacb60446ab9c12e5fc8a0f0656`: 5/5. All five case IDs, names, and passing outcomes are in the full public EditMode archive; no separate public Smoke package is claimed.
- `git diff --check`: PASS.

The reviewer did not rerun tests. Original .NET TRX/logs remain protected and were not independently inspected; no public .NET package is claimed. The Unity run records three pre-existing modified `ProjectSettings` files as stable inputs; they remain outside the candidate diff and untouched. Earlier failed/operationally blocked attempts remain preserved separately and are not counted as passing evidence.

## Limits

This verdict accepts only this bounded candidate as `VALIDATED_CANDIDATE`. It does not promote Core canonical, complete P12 or CORE-F, or establish the full DailyV1Application, detached host, or continuation contract.
