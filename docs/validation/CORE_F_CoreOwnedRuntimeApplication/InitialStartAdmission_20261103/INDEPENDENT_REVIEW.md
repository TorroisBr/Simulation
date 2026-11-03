# Independent exact-tip review — initial start admission token

Verdict: **PASS** for the bounded change at `52dfcdcc62a1544f33bf643e201cb6ebd45744e6`, tree `25fed4035badb043dfcdc524dc4f3b822eda85b0`, against parent `31df5581e3d0727ef7f2db541838f4e37d25c0b1`. Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

The independent reviewer confirmed the admission token binds the exact application, slot result, and loaded image. Only the exact `NoSave` result permits genesis; invalid, present, or unreadable slots remain blocked. Restoration checks the exact application-owned loaded image immediately before publication, preserving the existing private-candidate and atomic-publish boundary.

The test changes add required admission-token setup for direct initialization fixtures and strengthen Core `NoSave` identity assertions. No assertions were weakened. The complete candidate diff had no actionable implementation, authority, identity, or test-integrity finding.

Review evidence binds the candidate sources to CoreValidation 1005/1005 cold and warm, RuntimeAdmission 243/243, and BootstrapComposition 39/39. The two Unity manifests report stable inputs and zero failed, skipped, or inconclusive cases. These results are detailed in the adjacent `VALIDATION.md`; no tests were repeated during review.

This is not a canonical promotion, a public `TryStart`, a complete P12 restoration, or CORE-F acceptance.
