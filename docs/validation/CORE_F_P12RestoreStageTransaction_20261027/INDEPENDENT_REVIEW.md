# Independent review — CORE-F ordered P12 restore-stage transaction

## Verdict

**VALIDATED_CANDIDATE** — no actionable implementation or test findings.
This review is limited to the ordered Core-owned P12 C→D→E→F staging
transaction and restored TravelParty binding validation. It does not complete
P12 or CORE-F, and it does not authorize Core canonical promotion.

## Reviewed identity

- Repository: `TorroisBr/Simulation` (`origin` verified).
- Candidate branch: `codex/core/CORE-F-initial-restore-orchestration`.
- Candidate implementation commit: `f8a0ca21d3cb05385b73a34efd2dfacd012e8b92`.
- Candidate tree: `137b8e5528172c86a7310b0e26a3d134177b7936`.
- Exact implementation base/parent: `c2109dd9eb7efbcae4fcebbfde5562a35e9e791b`.
- Current remote Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Candidate remote tip was verified at the reviewed commit. The detached review
  checkout was clean before this record was created.

## Review findings

No actionable implementation or test findings. The full parent-to-candidate
diff was reviewed. Core creates a single transaction for the admitted
application restore attempt and binds it once to the detached P12-C root and
the exact application, expected session, and replacement operation. C→D→E→F
advancement is monotonic; only one stage may be active. The D step retains the
exact staged PersonStore and ordered selected City/NPC owner references. E
requires those exact D owners and the image day. F requires the exact D/E owner
references used by the selected application graph, the same definitions and
ordered NPC owners, identity index, spatial/site owners, and matching image
day/time. Session composition checks the retained C roots, selected owner
references, effective definitions, day and last-economy boundary before Core
creates the unpublished candidate.

A failed or disposed stage clears only its active reservation and can be
retried at the same stage. Completed stages remain private to the detached
attempt. The transaction does not mutate or publish the existing session.
The existing P12-G path still performs its final cross-section validation,
recapture, restored-boundary admission, and single publication after Core
composition. The non-application compatibility path remains outside this new
transaction; this review makes no claim that it has moved into Core.

The new Core graph validation checks the restored TravelParty census against
the exact selected NPC, identity-index, spatial-location, and route owners. It
rejects unresolved or mismatched route endpoints, duplicate/missing party
members, invalid travel state, unsynchronized member progress, inconsistent
destination City bindings, and nonreciprocal NPC party IDs. Existing P12-F
staging and P12-G image-level validation remain in place. New tests assert
application/root identity, single-claim root binding, stage ordering,
retry after an aborted private step, and valid versus mismatched TravelParty
bindings. Existing assertions were not removed or weakened.

## Source and evidence identity

The validation runs occurred while the candidate implementation was still
uncommitted on top of base `c2109dd`; their manifests identify that base and
record the dirty-worktree inputs. I independently compared those input hashes
with the committed candidate. All changed Unity-compiled production inputs
match the full EditMode manifest. CoreValidation changed-source and test inputs
match its private input manifest; the newly added Core test has the same
LF-normalized content as its recorded input, with only the expected CRLF
checkout projection in this review worktree. No implementation/test content
change separates the validated inputs from the candidate tree.

Recorded validation:

- CoreValidation run `c5b430d9b1ae4554be8ab74524eaa6c6`: cold and warm
  906/906 passed. Its private manifest SHA-256 is
  `580078e4f2e0f994a24f10cecf41a56d3b521df8213cb3694bb2112c42a640bb`; cold
  and warm TRX SHA-256 values are `e1236e7f79b0120774197fc8502304d5508f61ac4318b3015b2cb5d5480d858`
  and `bdb10de3bab420b83e00da0db0bcf1aed1b677b7facd8f5f0270fcc5ffddedba`.
  Raw .NET reports/logs remain private; no public DotNetTrx package is claimed.
- Focused Unity P12 restore run `813d1f0fe8e2460bb36c6c0fcf03067b`:
  15/15 passed.
- Full Unity EditMode run `9fefc02e4dd645b3b59c804ee49b054d`:
  3274/3274 passed. The normalized public package contains one full result
  XML. Its manifest SHA-256 is
  `25ed78c7385a5de764446184d000ab1b70ef9cb286a7f1cf1498881e5e4cce11`; its
  archive SHA-256 is
  `a341560554a9813d428a99dce97be963c0f7c269166be6c42f90ec7bddf53767`.
  Both hashes were independently verified, and the packaged result reports
  3274 passed, zero failed.
- Official Smoke run `86fca371a1214464a54bc3123030e5e2`: 5/5 passed.
- `git diff --check` on base-to-candidate: passed.

The evidence tool rejected publishing the focused and Smoke filtered XML
because their parent-suite testcase counts remain those of the unfiltered
suite. Their original reports/logs remain protected and unchanged; the full
EditMode package is the only published Unity result package. This review does
not claim those filtered originals were publicly packaged or independently
reproduced.

## Scope limits

This checkpoint establishes an ordered Core-owned staging transaction for
P12 C→D→E→F within the application restore path and adds Core TravelParty
binding validation. It does not claim full P12-G closure, detached .NET host
restoration, public `DailyV1Application.TryStart`, T/T+1/T+2 parity, CORE-F
completion, or canonical promotion. Existing Core canonical remains
`a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
