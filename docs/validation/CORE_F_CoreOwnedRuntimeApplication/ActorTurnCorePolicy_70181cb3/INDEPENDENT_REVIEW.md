# CORE-F Core-owned actor-turn policy boundary — independent review

- Candidate: `70181cb3a2ee8a774365a2de7bd831a8f50f3130`.
- Code tree: `414e871b125401bc0e249f40bebc4acf979ad436`.
- Base: `e0ee2bf1d0602345c4d3db587da6dfcc00faf6d5`.
- Candidate branch and remote tip were both verified at the candidate SHA.
- Review verdict: **PASS**.

Two independent read-only reviews passed. The implementation review confirmed that Core creates the admitted actor-turn policy boundary from the exact execution state, owner graph, stores, roots, and roster; Unity supplies effects/projection/P12 ports without selecting actor policy or decision roots; and returned actor/target/sentence operations remain bound to exact Core graph owners. The test-integrity review confirmed the changed tests replace assertions about obsolete host-selected policy flags with assertions for Core-created policy, while retaining host-only rejection, exact owner/roster identity, mixed-roster compatibility behavior, operation ordering, and P12 clock/revision/token/fault atomicity checks.

The committed source/test blobs exactly match the previously reviewed working tree and the remote candidate tip. Reviewers did not modify files or rerun tests; validation is recorded separately in `VALIDATION.md`.

This is an unpromoted CORE-F candidate checkpoint. Full application startup, detached restoration, .NET hosting and continuation remain outstanding.
