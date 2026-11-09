# CORE-F Core P12-F Census Remainder — Validation

Date: 2026-11-09

## Scope

This bounded integration projects the five existing Core-owned P12-F census
rows from the Unity host remainder only when the raw source contains the exact
complete set:

- ActorChoice;
- ScheduledDirective;
- TravelParty;
- Expedition;
- PoliticalKnowledge.

Core creates the same owner witnesses used by P12-F composition and restoration
rebinding. It removes those source rows only when each exact section ID, schema,
Required role, owner-instance identity, cardinality, and revision matches.
ActorChoice remains a Required row when its cardinality is zero.

Partial, stale, and foreign P12-F rows remain in the host remainder by object
identity so the existing exact P12-F composition gate rejects them. If the raw
source omits all five P12-F rows, projection fails closed. The bound ActorChoice
store is captured under the same operation lock as the census source and owner
graph. The mutation-epoch check still occurs before projection.

This does not change the selected P12 owner contract, claim complete ownership
of all 89 census families, or establish P12 readiness or CORE-F completion.
Unity continues to supply the raw quiescent census and epoch.

## Exact implementation identity

- Candidate branch: codex/core/CORE-F-restore-stage-transaction
- Core canonical at implementation: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Candidate base: 2a408689ad6429155e13eb3866691b8d1efee69e
- Implementation commit: f6d1e2a86b273dbc0e889ed9198269ba8e47e1a7
- Implementation tree: 2d2a91cdb8fb5335bd9686a546f9a5ffbf404f18
- DailyV1ExecutionBoundaryState.cs SHA-256: 6ee101b16889fd7704ae4bf91b1541ba46e7cbd6830fdff59bf95ad7e2a50066
- DailyV1RuntimeAdvanceStage.cs SHA-256: 9f52a597030f6ed9a8c1ad4d801d89915d15a1905cd7b25164ba057ee02a41c5
- DailyV1ApplicationSessionGateTests.cs SHA-256: c67f3acbc7dff35f0cb2b855ef554255098409c2bfe823374a26dba14b6c2cfe

## Validation

- Official CoreValidation run e4d4c92f95244c5a97c1847631fc1c3f: PASS.
  Pinned .NET SDK 9.0.315; 1,078/1,078 cold and 1,078/1,078 warm; zero
  failed, timed out, aborted, or inconclusive tests. Cold build, CalendarDemo
  cold/warm, and Simulation Lab build/cold/warm gates passed. Stable input
  digest: 57e77d5bd63709d49867f7dc602890fcb601e95a369ebc337d0fda5e58b5b5bb.
  Cold TRX SHA-256:
  3013288414aa25f9e6e739911162dd7f446056f49d9548ef9c6124f9e640b54a.
  Warm TRX SHA-256:
  c222f609881753ae89dd6578713eeccd62d608e0d4d977a9b76802e6a81079e6.
  This worktree contains a Unity Library; this run makes no Library-independence
  claim.
- An earlier CoreValidation run 968898ca33ea4adba80fed3694ce1d75 failed
  three test assertions while the new fixtures still omitted City recomposition
  and the expected remainder sets still included projected P12-F rows. Those
  fixture expectations were corrected. The original failed report remains
  preserved and is not counted as passing evidence.
- Official full Unity EditMode run c42c9c6a721f4ef998d6c93246a781c3: PASS,
  3,303/3,303; zero failed or skipped. Unity Editor 6000.3.9f1. The 1,978-file
  input snapshot was stable at
  06d4ae4bce18387a85be2dab2f4d567a29004981333a22e33e98db26e4db6b0d.
  XML SHA-256:
  8d1c61d727e3646237940179b770bc6da90dc6cb0a65f2d38a5750d9dadbfedd;
  log SHA-256:
  e8924d5d59f06c4941de525a1d1659da3a22c6ae81361dd70cd39f7c67f87c85.
- Official EditMode Smoke-filter run bd2d38acc4154f51a307cd27af8ba4c9:
  PASS, 5/5; zero failed or skipped. It used the same stable 1,978-file
  Unity input snapshot. XML SHA-256:
  c896c3ed79f1fa22de8d3964a98f9e269c883b87a697b8449c5c513fe6319c28;
  log SHA-256:
  4ff9f0a688328f91e2b949416f8d86457e1238a9fdca69f95cfca2935eb13ae3.
- git diff --check: PASS after the final source/test edits and before the
  implementation commit.

Raw XML and logs remain in the official private evidence store. This record
contains run identities, counts, input digests, and artifact hashes; it does
not copy private workstation paths. The Unity source snapshot excludes the
.NET test project; CoreValidation independently compiled and executed the
changed test source.

## Review and integration boundary

The implementation and focused regressions are committed and validated at the
identities above. Independent exact-tip review remains pending. No canonical
promotion has occurred. This is a recoverable bounded CORE-F integration only.