# Independent review — P12-D Core relationship validation

## Verdict

**PASS — VALIDATED_CANDIDATE** for the relationship-validation delta at implementation commit `c2109dd9eb7efbcae4fcebbfde5562a35e9e791b`, tree `1b8b422aecebdc37c49d3b31bc5fd231980b5f29`.

- Base: `7f12a9f34e0535a79d480c9c6353e756d01fd737`
- Prior reviewed implementation: `aa0295514c33b14ae89604e594f48f3bd293c77e`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Candidate branch: `codex/core/CORE-F-initial-restore-orchestration`
- Findings: no actionable defects.

## Scope reviewed

The delta moves P12-D decoded-image relationship validation into `DailyV1P12DRelationshipValidator.TryValidate(PersonStoreOwnerSnapshot, GenealogyOwnerSnapshot, IReadOnlyList<DailyV1P12DCityCoreOwnerHydrationInput>, IReadOnlyList<P12DNpcDRow>, SpatialNetworkOwnerSnapshot)`. The selected Application restore path runs this validator after restore-authority/input checks and before creating staged spatial, Person, Genealogy, site, or selected City/NPC owners. The Unity adapter skips its duplicate relationship preflight only on that Core-owned selected restore path. Existing compatibility and fallback paths retain the prior Unity preflight.

The new overload projects the same values used by the prior Unity preflight: Person IDs, residence and materialized-NPC links; parentage endpoint IDs; City IDs, legacy Location IDs and ordered important-NPC IDs; NPC IDs, Person/residence/current/destination links; and the canonical spatial Location set. It preserves ordinal identity comparison and null-row rejection. Duplicate Person/NPC/City identities and duplicate person-to-NPC bindings are rejected. City resolved Location IDs must equal the legacy City Location and exist in the spatial set. The mapped values pass through the same existing relation rules for missing/mismatched cross-owner links, destination references, and City membership. No authoritative owner is created before this check succeeds.

The added Core regression accepts a consistent decoded image graph and rejects a mismatched NPC Location. Existing validator tests continue to exercise malformed and duplicate relationship cases. No product semantics or P12 profile scope changed.

## Validation evidence

All evidence runs report stable inputs. The CoreValidation run used the candidate's direct parent as HEAD with the candidate source changes in its stable working-tree input snapshot. The hashes for the changed Core files match that manifest. The Unity snapshot likewise matches the changed Unity adapter, Core Application, validator, and test inputs.

| Gate | Result | Evidence |
|---|---:|---|
| CoreValidation cold and warm | 904/904 each | Run `68546588f855434b8a7b98bde34cb882`; stable input digest `691655366ffca714de68b513dde4d25530ee23f0fcf9db0b90a3f990bf2d74c7`; manifest SHA-256 `079ead40621c02004c28341f9547a9b8f5b4c636ff86da70efc31fcef5ddc751`. Changed source/test hashes: `DailyV1Application.cs` `75876d330ee10837aff19a021ea6e11705072990fb3fde72f711cee3d536b05d`; `DailyV1P12DRelationshipValidator.cs` `cdf74bdae99f1b44c1794b3c3e0d7060a9d0d06987b2b998efd9a1bf6f013a82`; `DailyV1P12DRelationshipValidatorTests.cs` `8932ae83ef3c8f9814c4122ffeecebae6eede091bc85b5c585e79c270580214b`. |
| Focused P12-G owner-vector regression | 16/16 | Run `9a45a6920c17426da7e43e106791a4ab`; stable input before/after `b8eac97d7027e0cfc6959ef7010fe6682c7964af0860f5e2935e27e0de709f8c`; XML SHA-256 `4c6ab9c87028ccf4c32d9e7ac55050cd0317e0fc3cabb2f18325056bdaa9f212`. |
| Full Unity EditMode | 3274/3274 | Run `75c290b20bd14f2cbde81654a0e080d6`; verified package archive SHA-256 `27d0df1f870c16761f6cd42b1a861c086625c9534d67efcf36276a3b50a863de`; package manifest SHA-256 `a1872882d26cf1c38fa4fc2944749f4db611a2639b103637648e1dff639efab0`. Changed Unity adapter hash: `P12DDailyV1OwnerPackage.cs` `e39edc785d97c2b054284adc971990aec94f7b071d431d4b903546d66cdb1e50`. The package input snapshot is stable and its archive hash matches the manifest. |
| Official Smoke selection | 5/5 | Run `6706fc51bc7e4f5b9acae38dd04178c7`; stable input before/after; original XML SHA-256 `72fd65c0039719d6aae4e36a2f54bae6130560a2858f92655714ef3d788e8b22`; log SHA-256 `b1efc707b66cc0f9084157033fe19da1dab47a117e4a42280ce3f8faedbd97cd`. |
| `git diff --check` | PASS | `git show --check` on exact implementation commit. |

## Limits

This record reviews only the P12-D relationship-validation delta. It does not claim CORE-F completion or canonical promotion. Existing ProjectSettings edits, untracked metadata and protected validation evidence were not modified.
