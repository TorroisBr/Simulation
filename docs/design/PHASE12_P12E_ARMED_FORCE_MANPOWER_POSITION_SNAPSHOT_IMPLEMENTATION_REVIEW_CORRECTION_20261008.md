# P12-E exact-tip review correction — 2026-10-08

**Status for candidate `b9f00f0b14f010931e5e37ca0b512e4ea1f43932`: `NEEDS_CHANGES`.** This note supersedes the `VALIDATED_CANDIDATE` verdict in review record `cc1d3a4373c9b09a6c66b14aed430487eeabdac5` for that candidate. The earlier PASS is inapplicable to the exact pushed tip and must not be used as promotion evidence.

The review was mistakenly written against the shared implementation worktree's uncommitted test contents. Direct inspection of the exact candidate object shows that `b9f00f0` contains test blob `3f88e6da4fddbc34fd3de12f760f8d78a79864d5`, and that committed file has no negative-amount or negative-owner/state-revision rejection cases. The accepted design requires this invalid-value coverage in §6. The missing test evidence is therefore a required change before this candidate can be considered validated.

No candidate files were changed by this correction, and no canonical promotion is authorized or implied. The candidate ref has since advanced; at the time of this note it resolves to `d929a57e3d173912666a452ad37987e714a9f8c6`. That newer tip is outside this correction's review scope and has no verdict here. It requires a fresh exact-tip review after its validation evidence is available.

Preserve both review commits. This correction is additive; it does not rewrite or delete `cc1d3a4`.
