# CORE-F candidate exact-tip review — 84d4db08

**Verdict: `NEEDS_CHANGES` — required exact-tip Unity validation is unavailable.**
The independent static review found no actionable code defects. This record
does not approve canonical promotion and does not claim CORE-F completion.

| Identity | Value |
|---|---|
| Candidate branch | `codex/core/CORE-F-core-owned-runtime-application` |
| Candidate commit | `84d4db084f5aa3c7ba8ebff66aef4459226d8297` |
| Candidate tree | `3076e601b1cd49685470fd0abadba42435b490a1` |
| Review base | `19f5877e766039d4dc671db3ae253769ded36cdf` |
| Review-base tree | `e532252e6b864bdc5fd513d7d8de2c9d51b92456` |
| Core canonical | `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea` |
| Independent review | Luna read-only exact-tip review; no code findings |

The reviewed delta from the stated base changes only
`DailyV1CompatibilityDescriptorFactory.cs` and its Core test file. The factory
rejects zero, negative, and unparsable authored geography distances, returns a
null descriptor with a nonblank diagnostic, and preserves valid positive-scale
descriptor framing. The tests assert the rejection result and diagnostic.

## Evidence

- CoreValidation: cold and warm **674/674 PASS**. The preserved manifest names
  source commit `980567478998ab5f97ccf9992fff70c38aacabd0`; the independent
  reviewer verified all 342 listed inputs, 3 generated dependencies, and report
  hashes against candidate `84d4db08` and found them unchanged.
- Unity focused EditMode: **not executed for this exact tip**. The official
  runner returned no test XML because Unity Licensing Client reconnection and
  license initialization failed before tests could execute. The earlier
  focused Unity pass predates this candidate correction and is not exact-tip
  evidence.
- `git diff --check`: PASS.
- No Unity EditMode, Smoke, or full-package acceptance claim is made for this
  exact tip. The validation gap is environmental; it is not evidence of a
  source-code failure.

The independent reviewer confirmed the candidate branch points at the reviewed
commit and the review base is its ancestor. The source correction is retained
on the candidate branch, but the checkpoint remains unaccepted until the
required Unity validation is available and produces inspectable results.

## Disposition

Preserve the candidate and its private original validation evidence. Do not
promote CORE-F. Continue independent implementation work on the shared runtime
integration while Unity validation is unavailable; rerun only the required
exact-tip Unity validation after the licensing environment has materially
changed.
