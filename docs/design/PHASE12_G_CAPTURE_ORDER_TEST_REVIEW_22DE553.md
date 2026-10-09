# P12-G capture-order test change — independent exact-tip review

**Result:** PASS — bounded test-code review
**Candidate code tip:** `22de553fb48507c71041a9dfb401c74ac83735d3`
**Candidate tree:** `79bf3ac5365fbf653b2cd513f7edaa1c9a590ddb`
**Candidate base:** `db1943766755f80bae7866f836f3c6f53409f9de`
**P12 canonical:** `02009f9063dd252bd4b177fd6aef1e74dcd947f5`
**Reviewed design seam:** `65a16e0cae85aa0b8fbc29bd596b05e0f06c07df`
**Review date:** 2026-10-09

The independent review inspected the exact one-file candidate diff. The test opens a `DailyCaptureStagingAttempt`, invokes F `TryCapture` with the source runtime, bootstrap, token, and owner-section vector, and only then calls C `TryStage`. It later stages the retained detached F capture through `capturedF.TryStage` with the same attempt and asserts the resulting F package references that attempt. Opening the attempt establishes identity/context and does not allocate a staged root. The sequence matches the reviewed P12-G design seam and the source APIs' capture/staging split. The reviewer found the API calls and assertions consistent.

The reviewer did not run Unity and could not access the local validation run artifacts. Separate focused validation evidence is recorded in [`P12GRestoredBoundaryOrder/VALIDATION.md`](../validation/P12GRestoredBoundaryOrder/VALIDATION.md); it is tied to the exact code tip above. No production runtime code or API changed.

This PASS applies only to the test-order change. It does not establish P12-G implementation readiness, complete live-profile inventory, restored admission/publication, P12-A readiness, P13 readiness, or Phase 12 closure. P12-G remains `WAIT_DEPENDENCY`, P12-A remains `WAIT_DEPENDENCY`, P13 remains `BLOCKED`, and Phase 12 remains open.
