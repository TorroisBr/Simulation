# CORE-F selected solo-travel P12 boundary

## Candidate

- Base: `3e55a18010812df93d528b29efd3072573e1564a`
- Implementation: `e699cc6b135059c9a395eaaa1babb5ab059a5108`
- Implementation tree: `d62f62d37b1205c859c3c9aebfe29b7a1b485afe`
- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`

The change makes the Core-selected solo-travel boundary own the operation
lifetime and exact-session checks. The Unity adapter supplies the existing
host census preflight and projection callbacks. The P12 host section builder
is shared with the compatibility path, preserving the money, travel, plan,
spatial-knowledge, event-counter, record-sequence, and reciprocal City-presence
sections. Legacy/mixed-roster dispatch retains its existing provider and host
operation path. No action resolution, random draw, owner identity, writer,
revision, epoch, or persistence semantics were changed.

## Validation

| Gate | Result | Evidence |
|---|---:|---|
| CoreValidation cold | 1,103/1,103 passed | run `121fe5b957d64dde8c789454109bc75e` |
| CoreValidation warm | 1,103/1,103 passed | run `121fe5b957d64dde8c789454109bc75e` |
| `P12SoloTravelStartOperationTests` | 11/11 passed | Unity run `dfc3d88a178f4bf7a004b58749f5eace` |
| `SimulationRuntimeAdmissionTests` | 246/246 passed | Unity run `f212e6da831b49aeb69b06c04c157346` |
| Full Unity EditMode | 3,318/3,318 passed | Unity run `75e6c88058d940878382fa031ca66c71` |
| Official EditMode Smoke filter | 5/5 passed | Unity run `f068e0a19f75486ea50f97824dea846b` |
| `git diff --check` | passed | implementation commit input |

All passing Unity manifests report stable inputs. The code/test files were
hashed against the passing manifests after the implementation commit and match
the committed source. CoreValidation used pinned SDK `9.0.315`; its cold and
warm reports each contain the same 1,103 test identities and no failed,
skipped, aborted, or inconclusive cases.

Private manifests and original XML/log artifacts remain under the local
protected `.private-coref-solo-travel-*` evidence roots. They are not published
by this record. Manifest SHA-256 values:

- CoreValidation `manifest.json`: `0f7d83bed581bdfd4419a4e67b65260bf4ebed145d09974005590cff71ace424`
- P12 focused Unity run: `f0fc0090602d5774e17942df9d218ab73a4b360dfa6c76a2127761cca4231790`
- Runtime admission Unity run: `339f8e63e1742b8fe86d91120c106539c54bf938335ca3e41ab42b12babea275`
- Full EditMode Unity run: `6f68ac39fb16cdbc0eee31d542ccca0a1d50247c73f33968ba789d76909daf12`
- Official Smoke Unity run: `b5637d6b81f7f8d4977526bd7008491fa5203a4c6e215c1cf647251515a2dbbb`

An attempted PlayMode filter for `GMConsolePlayModeSmokeTests` selected zero
cases and is not counted as Smoke. The repository's established Smoke gate is
the EditMode `Smoke` filter above. Earlier runner attempts also did not count:
one was launched outside the repository root, and a subsequent compile exposed
the Core test protocol double that was then updated. The final official run
completed all gates successfully.

## Scope limit

This is a recoverable CORE-F implementation candidate only. It does not claim
the complete shared runtime, `DailyV1Application.TryStart`, detached .NET host,
or T/T+1/T+2 continuation, and it does not promote Core canonical.
