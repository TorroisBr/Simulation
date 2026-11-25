# CORE-F Core-only Hide actor-turn checkpoint

## Candidate identity

- Candidate commit: `9ba83f777e8b502ca89f2ccb3e73f95942aa7572`
- Candidate tree: `85eeb47ada8318eca1ae3a18170ce37e63fd0dce`
- Parent: `fa8d8e0f4dd1bb8167fe5be503dc0830697339f6`
- Scope: execute the admitted, exact, targetless Hide action through the existing Core crime/justice action and P12 mutation boundaries when no Unity host effect ports are present.
- This checkpoint does not establish a complete Core-owned daily runtime, public application startup, full P12 restoration, or CORE-F completion. It is not promoted to Core canonical.

## Implementation review

Independent exact-tip review: **PASS** for the candidate commit above. The reviewer found the hostless dispatch restricted to the exact admitted actor, Hide definition, current action slot, and neutral targetless payload. Other unsupported dispatch paths remain closed. Crime/Justice owner mutations continue through the existing Core census/P12 boundary. The regression retains Normal and mismatched-identity coverage and adds the Hide case with status, duration, Justice revision, and census-revision assertions.

## Validation

| Gate | Result | Evidence identity |
| --- | --- | --- |
| Official CoreValidation cold | 1,142/1,142 PASS | Run `be4edc8855a3447182cd8e213f3bee6a`; manifest SHA-256 `443d6b8ba72320e936fcb97dcb03f4aad5bf1adefe25616158e3d0c34c9d2a21` |
| Official CoreValidation warm | 1,142/1,142 PASS | Same run. The actor-turn source and test file bytes match the candidate. The selected-daily source differs only by CRLF versus LF line endings between the runner inventory and normalized Git blob; normalized text matches. The runner captured the code immediately before commit creation. |
| Official Unity EditMode | 3,321/3,321 PASS | Run `8fafbec0da4b4acab5a84e25dd2be04a`; stable input digest `35d6c5b8a6c22739de060ec4dc7ebe7fb29a670cccd950b02a2b55fa28dff2ed` |
| Official Unity Smoke selection | 5/5 PASS | Run `d4e6a2f7ac01428980d8c2afffd483ac`, EditMode filter `Smoke`; stable input digest `fa220d3333c0d7b712546a09013b7fd6ab32484255264cea2f4f403f6903ce77` |
| Evidence package verification | PASS | Official verifier: one full EditMode run; archive SHA-256 `d8e82a8bd8760fefd8869338a15eccd2aca1aeb78f44ead03799323ef0e74f6a` |
| Diff check | PASS | `git diff --check` on the candidate change |

The full EditMode package contains the same five Smoke test identities as passed cases. The separate earlier `TestCategory=Smoke` attempt returned process status Passed but executed 0 tests; it is explicitly excluded as invalid Smoke evidence. The exact `TestFilter=Smoke` run above is the accepted result.

CoreValidation recorded that the Unity Library was present in this checkout. Therefore these results do not claim a Library-independent .NET environment. The full EditMode report was captured on unchanged source bytes immediately before commit; the exact-tip Smoke run was captured after the commit. Independent review verified the relevant code/test file identities. The CoreValidation TRX/log originals remain in the worktree-local private evidence area and are not represented as a public package here.

## Public Unity evidence

- [Normalized full EditMode manifest](Unity/coref-core-only-hide-editmode-20261116.manifest.json) — SHA-256 `fea2f6ce65c1a1371ec2dbb0dc37799ce38e6fcd64b866cc56d6c7446299a460`
- [Normalized full EditMode archive](Unity/coref-core-only-hide-editmode-20261116.zip) — SHA-256 `d8e82a8bd8760fefd8869338a15eccd2aca1aeb78f44ead03799323ef0e74f6a`
- Official verification confirmed one run and one normalized report entry. Original Unity reports remain preserved in the private evidence area.

## Changed source identities

| File | Git blob | SHA-256 |
| --- | --- | --- |
| `Packages/com.simulation.core/Runtime/Application/DailyV1ActorTurnExecutionStage.cs` | `5d6e1170025445cf6bea62591f118ee4fd8c74d9` | `4b0875b3f82c8dc38e4dba90702b88b696abb734c2ce3be846972c8046c3f93f` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedDailyExecutionStage.cs` | `9f47f8d75a80b082830169799794d1839cbfd9bb` | `9bd7d8da056173177e656be833a29d58f9f912d06fc45793142a103b987ca28e` |
| `tests/Simulation.Core.Tests/DailyV1SelectedDailyExecutionStageTests.cs` | `4a604e8612b354f7c2561fcd3ba010a560ba5f37` | `225cee89e28c77e1714ab29a151c338e78eb8aab14c1147ae698207c2dd4e0fb` |
