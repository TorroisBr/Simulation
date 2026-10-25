# CORE-F P12-A .NET-v2 host admission validation

## Candidate and scope

- Repository: `https://github.com/TorroisBr/Simulation`.
- Candidate branch: `codex/core/CORE-F-p12d-core-hydration`.
- Code candidate: `fc6e6ac93389503e2a100c085eecd71aff622659`.
- Code tree: `040a8ab5efea01d7350797bc97f42cb37a3a9485`.
- Code base: `f342b1e8a236660eff4f67cb4fe8a10d01360ebc`.
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

The change makes a continuation header valid for encoding only when its typed
.NET-v2 descriptor uses the supported .NET host kind (`1`). Host kind `2`
remains a recognized foreign descriptor identity and is rejected by the .NET
admission path before owner staging. Unity-v1 encoding, descriptor bytes,
filename, envelope version, atomic slot algorithm, and explicit-root store API
are unchanged. Unity and .NET callers retain their host-owned explicit roots;
the implementation does not add a second slot or cross-host load behavior.

This is a bounded persistence-admission correction. It does not complete the
shared application, P12 restore graph, detached .NET host, CORE-F, or any
cross-host continuation guarantee.

## Validation results

| Gate | Result | Run |
|---|---:|---|
| Official CoreValidation cold | 744/744 PASS | `dcf5297f55b44225a595ab94ce378cb7` |
| Official CoreValidation warm | 744/744 PASS | `dcf5297f55b44225a595ab94ce378cb7` |
| Focused Unity `P12AContinuationCodecTests` | 37/37 PASS | `9fbb44fe6bf84b8982336c419fd73630` |
| `git diff --check` | PASS | code candidate `fc6e6ac9` |

CoreValidation's protected manifest records stable input digest
`1ca754779340ad0758f5508cacd2c19656151461601d35580c52d2b4b573b5bc` and
373 source/test inputs. The final candidate's Core production source and Core
test hashes match the runner inputs. The run occurred in a worktree with a
Unity `Library`; it does not establish Library independence. The cold and warm
reports and runner manifest remain in owner-only evidence retention. The
runner emits its `simulation-core-validation/v1` manifest; no public TRX
package is claimed.

The Unity run used Unity `6000.3.9f1`; its protected manifest reports 1,878
stable inputs. The exact Core production file and Unity P12-A test hashes match the
candidate. The filtered XML contains 37 passed cases, zero failed cases, and
has SHA-256 `a4745e3884fbd61e957c305d81214069f7a3eb85952fc720d8ab9b2093607512`
(26,019 bytes). Its matching log has SHA-256
`b831a09e83d4bfa8d6dd7555b3560269e29f2c45dbdfc8d67cc12e3bb8a04e3a`
(47,024 bytes). XML, log, and the private run manifest remain in owner-only
evidence retention; no filtered report is published.

The exact candidate file hashes are:

| File | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Persistence/P12AContinuationImage.cs` | `3349727dff46f688f3ab66e18cf2d8d43decd4960e8633e5237eeda950bb8b8d` |
| `Assets/_Project/Tests/EditMode/Editor/P12AContinuationCodecTests.cs` | `6747575d1237517d87ea19d4c4e97875bbf3c91cd169ed8aaa19c4c64db0932a` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationHostTests.cs` | `718db02c308e0c1a19fea4470fb8f618e1e651cef5b10a12705f80ea8ed2d6c9` |

## Review and limits

Independent exact-tip review passed with no actionable findings against code
`fc6e6ac93389503e2a100c085eecd71aff622659` and tree
`040a8ab5efea01d7350797bc97f42cb37a3a9485`; see the durable
[review record](../../reviews/CORE_F_P12A_DOTNETV2_HOSTKIND_REVIEW_FC6E6AC9.md).
No canonical promotion has been performed. Core canonical remains
`a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`; P12-D and CORE-F remain open.
