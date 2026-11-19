# CORE-F detached Core host ports — exact-tip review

## Identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Candidate commit: `f30fc0b4a7f79b653f1e869168f43fdedce6b6be`
- Candidate tree: `b1f8c8a669cc0339156fae6e0bc79b10ce97c32b`
- Reviewed base: `46684c1c095caba36c947b06017162ab3dfd56f5`
- Reviewed payload: `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs`, `DailyV1ApplicationHost.cs`, `DailyV1SelectedDailyExecutionStage.cs`, `DailyV1SelectedDemographyStage.cs`, and `tests/Simulation.Core.Tests/DailyV1ApplicationHostTests.cs`.

## Independent review

Verdict: **PASS** for the exact candidate commit and tree above.

The review verified the committed tree against the reviewed five-file diff. Genesis failure terminalization is bound to the exact retained preparation and NoSave admission; subsequent preparation and both publication paths reject the terminal application. Unexpected exceptions are preserved while the consumed Genesis attempt is marked terminal. The IOException regression covers that path. No lock-safety, retry, or publication bypass finding remains.

## Validation

- Focused IOException regression: 1/1 passed.
- Official CoreValidation: 1166/1166 cold and warm passed; run `0dbde0d5ff79402fab6ff8466e023e3b`.
- `git diff --check`: passed.
- Unity EditMode and Smoke: not run for this Core-only checkpoint.

## Scope and limits

This is a bounded, fail-closed detached-host integration checkpoint. It does not provide the production live 89-family census builder, does not make the selected application start operational, does not expose public `TryStart`, and does not complete or promote CORE-F. The protected ProjectSettings change, validation-document edit, and untracked private evidence remain outside this candidate payload.
