# CORE-F runtime boundary review — 202223ec

## Review identity

- Candidate branch: `codex/core/CORE-F-runtime-boundary-3ec0aa`
- Reviewed code tip: `202223ec2214f4d229a28a932720ccb069d74d3c`
- Reviewed tree: `b523688f0d486f8a00dacba3f16c7f14c9d8a61d`
- Candidate base: `8b9ed2976c9a871c5261ea04cb606a08575dcd00`
- Intermediate candidate: `4b4dbb7c71dec64120adbd0036d552f434d70f32`
- Independent review verdict: `VALIDATED_CANDIDATE` for code review

## Scope reviewed

The review covered the Core-owned Justice record rows and ordered owner,
Unity-facing wanted/sentence projections, P12 mutation binding and revisions,
the selected `Normal` action dispatch, and the related Core and Unity tests.
This remains partial CORE-F integration; it does not claim a shared complete
Daily-v1 runtime or host.

The first exact-tip review found that the P12 invalidation tests still cast the
Justice projections to `List<T>`. Commit `202223ec` changed those tests to the
supported `IList<T>` projection and verifies that duplicate insertion is
rejected before the owner census changes. The reviewer confirmed that the
finding is resolved and found no remaining Justice projection `List<T>` casts.
The reviewer also confirmed that new wanted and sentence rows now enter through
the Core owner's insertion methods, which attach their P12 callbacks as part of
insertion; the host no longer pre-binds a row that the owner would then reject.

The reviewer noted that the selected `Normal` dispatch bypasses the existing
random success roll. This matches the six checked authored Normal actions,
which are non-failable in the accepted proving profile. No broader action
profile or failable-Normal compatibility is claimed.

## Validation evidence and limits

- .NET `Simulation.Core.Tests`: 492/492 passed on the reviewed production and
  Core test source; the later `202223ec` change affects only Unity test source.
- `git diff --check`: passed.
- Focused Unity P12 Justice invalidation validation: pending. It was not run
  because active Unity Editor processes were using the shared project Library.
- Full Unity EditMode, official Smoke, and full CORE-F integration validation:
  not established by this candidate.

The independent code review is not a promotion approval. Canonical promotion
is not ready until required Unity validation and the complete CORE-F package
gates pass.
