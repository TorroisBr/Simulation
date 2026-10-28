# Selected Capture Census Boundary — Independent Exact-Tip Review

## Verdict

**PASS — no actionable findings.**

## Reviewed identity

- Candidate commit: `f594dfec117a4db2d6f9471b8337adf7f4bdd1c4`
- Candidate tree: `eb1e64b3921cb391d56905d9751c24e438dff7db`
- Parent: `db28d143c51f14ae32d1f079a3402fac08d055f8`
- Core canonical comparison base: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Validation record: [`VALIDATION.md`](VALIDATION.md)

## Findings

The Core pre- and post-callback gates correctly require the exact admitted UnityBootstrap-Daily-v1 profile, owner thread, runtime and configuration binding, selected execution, mutable guard, and the correct advance-lease/outer-scope pairing. Callback failure, a non-None failure returned with success, a negative mutation epoch, and an incomplete selected owner vector are rejected with output values cleared.

The complete-snapshot contract retains fixed section identity, family key/cardinality parity, relation joins, alias identity, and duplicate rejection. The existing Unity protocol remains responsible for live factual owner reads and world-publication validation. For DailyAdvance, the runtime-protocol operation closes before completed-token census while the outer advance scope and lease remain held; idle, continuation-save, and session-replacement paths do not require an advance lease.

The regression covers mismatched lease/scope, an idle census with an advance lease held, complete-vector acceptance, and incomplete-vector rejection. No assertions were weakened. Validation manifests matched the changed source/test hashes, and all required validation passed as listed in `VALIDATION.md`.

This review accepts only the bounded selected-census admission slice. It does not claim full live Core ownership of the 89-family census, a complete shared runtime, P12 readiness, CORE-F completion, or canonical promotion.
