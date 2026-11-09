# Independent review — CORE-F selected NPC census fragment

Verdict: PASS — VALIDATED_CANDIDATE

Date: 2026-11-09

## Reviewed identities

- Candidate tip: 5aa70ed19003a6d32b4d63497123065aea92adfe.
- Implementation commit: e1629cad26addf64d6b149dd474de313316eee79.
- Implementation tree: eeb99ea6579eb7725b75c6686636ec42ac56d548.
- Base: 9768ad27b2a2362064a33d701752056075317fc7.
- Core canonical: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea.

The independent reviewer verified the complete base-to-candidate diff and the exact candidate source and validation identities. No source or test changes occurred during review; the candidate tip after implementation changes only the validation record.

## Review findings

The implementation captures the complete host census first, then filters rows only when the exact selected Core NPC provider witnesses match role, schema, owner-instance identity, cardinality, and revision. Core recomposes that fragment from the admitted owner graph before returning complete capture evidence. Mixed-roster and stale/substituted rows retain the complete host vector. P12-D receipt rows remain host residuals.

The regression coverage verifies exact owner identities and row facts, the zero-cardinality/zero-revision LocalTopology owner, P12-D receipt retention, successful full-vector recomposition, and whole-host fallback for mixed and stale inputs. The existing 89-family formula and accepted P12 profile are unchanged.

The initial CoreValidation run was not accepted for exact-source applicability because the working-tree test file had mixed LF/CRLF bytes. This was corrected by normalizing line endings to the committed Windows checkout form without changing the Git blob or source text. The replacement cold/warm CoreValidation run matches the exact test-source bytes and is recorded in VALIDATION.md. The earlier run and reports remain preserved and are explicitly marked preliminary. Unity evidence remains applicable because no Unity source changed during this evidence correction; its package and input snapshot were verified.

## Exact validation

- CoreValidation cold/warm: 1,071/1,071 each; exact-source run 3f0f84cd59554fc3bb9560e8db43ba61.
- Focused Unity owner-closure suite: 1/1; run a3ff700617a742b9b976e80cbed1bca9.
- Full Unity EditMode: 3,302/3,302; run f67023929cee4b589fedbd14efb0ba20.
- Official filtered Smoke: 5/5; run 714839992d2d4369a23ff2deb94743c3.
- Full EditMode evidence package verification: PASS; archive SHA-256 143e44ef79ccdb8d9b017652e445abb042da3b9895dbc0c55c338658a1454f1c.
- git diff --check: PASS.

## Scope limits

The full ContinuationCensusProtocol, quiescence admission, and shared-epoch validation remain host-owned. This candidate does not establish Core ownership of the full 89-family protocol, complete P12 restoration, detached-host readiness, or CORE-F completion. No canonical promotion is made.
