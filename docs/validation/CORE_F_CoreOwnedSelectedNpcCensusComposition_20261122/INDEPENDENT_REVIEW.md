# Independent exact-tip review — selected NPC census composition

Date: 2026-11-22
Verdict: `VALIDATED_CANDIDATE` / PASS

## Reviewed identities

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Candidate base: `eccb7af0d1eb98c6955c5eb0daf65f442d350c8d`
- Reviewed implementation commit: `b612168c177107890021da0454a45886cd9be8ed`
- Reviewed implementation tree: `9ab1582d20e39e9a8f0941697c2bf0e56977573d`
- Candidate documentation/evidence tip reviewed: `91af78d9326e1bc9b353fcbd1742b0a28957fdfe`
- Core canonical observed during review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

The reviewer was independent of the implementation author and reviewed the complete change against its recorded base, the census authority boundary, test coverage, and exact-source validation records.

## Review result

The first review of implementation commit `0359e165bf2023e2ac29b3fa3bdd5f5677834ad4` identified one actionable documentation defect: a Unity source comment described the older Inventory/MoneyAccount-only binding set. The comment was corrected additively in `b612168c177107890021da0454a45886cd9be8ed`, and focused plus full Unity validation was rerun against the resulting source tree.

The final exact-tip review of `b612168c177107890021da0454a45886cd9be8ed` passed with no actionable findings. It confirmed:

- Core composes selected NPC census registrations from the existing authoritative Core owner graph and provider builders.
- Family coverage, live owner and alias identities, schema, revisions, cardinality, and deterministic section ordering are checked.
- An empty selected NPC graph creates a valid empty provider set, distinct from an absent provider set; non-empty completeness requirements are unchanged.
- Unity compatibility providers remain for unselected or mixed-roster NPCs. P18 local-observation and merchant-trade receipt rows remain host-projected until their authorities join the Core graph.
- Existing P12 census requirements and test expectations were not weakened.
- The reviewed validation package and manifest identities match the recorded artifacts. The packaged Unity XML contains no local workstation paths or token-like strings.

No Core canonical promotion or CORE-F completion is claimed by this candidate.

## Exact-source validation verified

| Gate | Result | Source/run identity |
| --- | ---: | --- |
| CoreValidation cold | 1188/1188 PASS | run `f3ab7e8d1cc14aac83423b40d724dac6`, tree `9ab1582d20e39e9a8f0941697c2bf0e56977573d` |
| CoreValidation warm | 1188/1188 PASS | run `f3ab7e8d1cc14aac83423b40d724dac6`, tree `9ab1582d20e39e9a8f0941697c2bf0e56977573d` |
| Focused Unity bootstrap composition | 41/41 PASS | run `39d80d36e34d4196a009445438668368`, input stable, tree `9ab1582d20e39e9a8f0941697c2bf0e56977573d` |
| Full Unity EditMode | 3325/3325 PASS | run `8b24fd3de0bd47b48e3a3332403d2508`, input stable, tree `9ab1582d20e39e9a8f0941697c2bf0e56977573d` |
| Official Smoke | 5/5 PASS | run `33169343262143a9bb3a2b625374820a`, input stable, tree `9ab1582d20e39e9a8f0941697c2bf0e56977573d` |
| Full EditMode evidence package | verifier PASS | archive SHA-256 `760df0b0b07ede981f8c57ef2080c0381ee329721f80efb438945d51c205f280`; manifest SHA-256 `919546e8d7d603db92ecf00df59ba7f460348afed5685fed8c974ad87577046e` |
| Candidate diff check | PASS | `eccb7af0d1eb98c6955c5eb0daf65f442d350c8d..b612168c177107890021da0454a45886cd9be8ed` |

The CoreValidation original TRX and logs remain in protected local retention. This review does not claim a public DotNetTrx package because the native run did not emit the required source-context profile. Filtered Unity XML was not rewritten or included in the full-run package.

## Scope and limitations

This is a bounded selected-NPC census composition checkpoint. It does not claim complete P12/Core owner coverage, complete CORE-F runtime composition, detached .NET execution, or Phase/Core completion. Core canonical remains unchanged.
