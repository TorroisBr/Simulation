# CORE-F incremental review — 9f6ce110

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Base: `6c7243da2f7993a25c13202d16cee03b56ae0236`
- Reviewed code commit: `9f6ce110`
- Scope: Core application session-lineage publication guard and Unity test-fixture setup for the captured admission context.
- Independent review: **PASS** at exact tip `9f6ce110`.

The review verified that initial publication fails closed until the application captures its bootstrap admission context, and that both replacement publication paths check candidate lineage while holding the same session-gate lock used for publication. Tests reject same-thread sessions from a different application for initial and both replacement paths, and preserve the valid same-application publication/replacement case. The Unity fixtures now obtain the exact context from their `DailyV1Application`; their thread and thread-ID assertions remain, and no references to the removed duplicate bootstrap-thread fields remain.

CoreValidation cold and warm passed **688/688** for the Core production and test sources in this commit. The independent reviewer verified that the validation manifest hashes match the current files and that the lineage case is Passed in both TRX reports. `git diff --check` passed.

Unity EditMode was **not run** for these fixture changes. The prior automated attempt stopped in Unity Licensing Client reconnection and emitted no test XML. This record does not claim Unity validation or full CORE-F acceptance. It is a recoverable candidate checkpoint only; Core canonical remains unchanged.
