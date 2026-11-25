# CORE-F selected Crime action P12 boundary — independent exact-tip review

## Verdict

**VALIDATED_CANDIDATE** for the ordinary selected Hide/Escape P12 operation-boundary slice. No actionable implementation findings remain. This verdict does not promote Core canonical or complete CORE-F.

## Exact identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Reviewed candidate/evidence tip: `f226da49b5390cf5dce0cdd4c8321303b288a308`
- Reviewed tip tree: `bcf7cfe7f4861c39b93873522907d712bbc6725a`
- Code commit: `33704f60d0a940b53a77bc63f5820f84fa8159ef`
- Code tree: `5200c2b3f904569940ea4ab18b6d041893a28e3e`
- Base/previous candidate: `fe566597ea63d8cfde1fd7779970cf0877bf3ab2`
- Core canonical during review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

The reviewed code commit is the parent of the evidence tip; the evidence-tip additions are the validation record and normalized Unity result package. The remote candidate branch matched the reviewed evidence tip before this review record was created.

## Review scope

Reviewed the complete base-to-code diff in:

- `Assets/_Project/Scripts/SimulationRuntime.cs`
- `Packages/com.simulation.core/Runtime/Application/DailyV1P12CrimeJusticeMutationCoordinator.cs`
- `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedCrimeActionExecutionStage.cs`
- `tests/Simulation.Core.Tests/DailyV1SelectedCrimeActionExecutionTests.cs`

Also reviewed `VALIDATION.md`, the committed Unity evidence manifest/package, private Core and Unity run manifests/reports, the current Crime/Justice architecture semantics, Phase 12 closure contract, CORE-F execution contract, and the applicable repository review instructions.

## Findings

Core binds the selected operation to the admitted execution state, current owner thread, exact graph, actor, action, and payload. It pins the host census protocol and opaque Crime/Justice census identities to that session, derives the actor/Justice owner bindings and receipt sections, and opens the immediate P12 scope. Unity retains exact projection checks and publication callbacks. Replaced host bindings or stale action slots fail closed before the P12 operation starts.

The operation preserves the existing action roll order and Hide/Escape domain behavior. Forced Escape retains its separate legacy boundary and same-day ForceOutcome handling; non-admitted execution retains the host compatibility boundary. The new tests cover exact section/scope lifetime, host-binding replacement, and stale-action rejection. The test diff only adds coverage and helpers; it removes or weakens no existing assertions.

No actionable code finding was identified. A source-grounded next bounded boundary to review is selected Guard Arrest: the current coordinator already derives its actor/target/Justice/receipt sections, while the Unity adapter still initiates the operation.

## Validation evidence

- CoreValidation run `10e424b7d0e34dccb7bf4e1611658d95`: cold and warm **1,106/1,106**, zero failed or skipped, identical test identity sets. The pinned SDK was `9.0.315`.
- Unity `SimulationRuntimeAdmissionTests`, run `89a8e1d1654c4e7793415596a389c0a4`: **246/246**.
- Full Unity EditMode, run `c9ab17887bc449998ff54761a78dabd2`: **3,318/3,318**.
- Official filtered EditMode Smoke, run `e50bb903d1f74fb1b09dd84b901ff7e1`: **5/5**.
- Unity input digest: `a8a308481b2c307994c5f33c5c9dc995b15c54aa0ed38790dcf8aac370ba73f5`; snapshots were stable.
- Core input digest: `694f1d1e8b4267a98e9b376a99b3518eba4107cfa34810a9df3097c0109569f4`; snapshots were stable.
- Independent source-hash audit found zero mismatches across all 541 Core inputs, 4 generated dependencies, and all 1,982 full-EditMode inputs. Changed-file SHA-256 values: `SimulationRuntime.cs` `6d617169a64ffbbd92b31ecd4bf537625290a285bd34e0db7b9060cb6874e1c1`; `DailyV1P12CrimeJusticeMutationCoordinator.cs` `109a9c1003386a5759df299a120240785036f39d5aa19c54f0e0e12a07f8bc2f`; `DailyV1SelectedCrimeActionExecutionStage.cs` `60929872eed219ed7dade95e432ed10ad03eb3276f36d47122bdb95489c7b5a9`; `DailyV1SelectedCrimeActionExecutionTests.cs` `9f95b7567c7a939605aa549d5471254fc7051bbb287a8e84e824a426d7daa865`.
- Original Unity XML hashes matched their private manifests: admission `024d9d9aba5c30ea3a40fc2824538b1e9782a52c16dc4bfca0c913cb81f003e7`; full EditMode `ba7f0c93f592000b2bea30400b9690e34b47c796e101d07a1c31371b9a830ca5`; Smoke `9410fb2ae0d2ef39516d539494f756c5cd06044816da5444fd1c53a22cd4826d`.
- The normalized Unity results ZIP is `c0037bc59de262255ec3a126a117e49bb0d371b4a163f9b5e0c03ad8837ddf1d` (175,530 bytes); its manifest is `77d8caef2fb2d1e07327539ed9311c0664136cf79b62f53d3f8a99317ff8a31b`. The ZIP contains the full 3,318-case EditMode report.
- `git diff --check` passed for the code commit.

## Evidence and promotion limits

CoreValidation TRX reports, the Core manifest, and original Unity reports/logs remain in protected local E: evidence retention. No public normalized TRX package exists: the approved adapter requires a separate `dotnet-trx-source-context-v1` record, which was not fabricated. The public Unity package contains only normalized full EditMode XML; focused and filtered Smoke originals remain private.

The runs recorded base HEAD `fe566597ea63d8cfde1fd7779970cf0877bf3ab2` with candidate working-tree inputs. The full input hash audits above matched those inputs to the committed candidate. Unrelated modified/untracked local files were present during review; they were not part of the candidate diff and were left untouched. The candidate is not promotion-ready and makes no full CORE-F, detached-host, or Core-canonical claim.
