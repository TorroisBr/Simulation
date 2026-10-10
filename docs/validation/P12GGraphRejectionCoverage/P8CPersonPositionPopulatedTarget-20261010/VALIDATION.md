# P12-G populated P8-C Person-position target rejection — validation

**Base:** `4c3371a1aa5838067acb5859742694ac6d653a3c` (`codex/phase12/canonical`)
**Code commit:** `2b0e42db528266cd2411dafca6ef544d881a3a56`
**Git tree:** `189bfc21e9df294007d79d7bf7eb6e6d91996359`
**Tested `Assets` tree:** `903aecaecf94dd52f2c70845c8b53ac7ad77b0e2`
**Unity:** `6000.3.9f1`

The test-only case registers the same stable Person in the source and
uninterrupted control before their completed daily boundary. In the private
restored candidate, it assigns that existing Person to the already staged
P8-A Location through `PersonSpatialPositionStore.TrySetAt`. The installed
P8-C census provider observes the exact target owner at cardinality/revision
`1/1`; normal target-vector validation rejects the explicitly-empty Daily-v1
owner before publication. The shared harness checks the source session,
completed-boundary token, health and graph remain unchanged, later source
continuation matches the uninterrupted control, and a subsequent valid restore
succeeds. No production code, P12-D Person set, or profile contract changed.

## Validation

Each result is tied to the tested `Assets` tree above. Test-run XML is the
authoritative result; compressed logs are retained alongside it.

| Gate | Result | XML | XML SHA-256 | Compressed log SHA-256 |
|---|---:|---|---|---|
| Focused `DailyV1RestoreRejectsCorruptedRootOrOwnerVectorAtomically` | 21/21 PASS | `FocusedRaw/EditMode-20261010-194607-b34dfb081ba84974b4416beaaa433352.xml` | `04935381E0B30E227036E215AD32CEDDABE6798D10FD3CE8FA52C32CECAFEE13` | `D1EDC9A6B5EC16DD7111E2A2F2AB430726C1FB14036ECF137906398691D4E60B` |
| ALL EditMode | 2847/2847 PASS | `AllEditModeRaw/EditMode-20261010-194625-f644665a4bca449c930504bac39a8a89.xml` | `B363D58382724649789FABA2C6A9737096E4BA967197868654C903E93A4DA23F` | `753244ABC07A305B7B604F18149BE9973651A169BB885986C2E37CBA6F49142F` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 PASS | `SmokeRaw/EditMode-20261010-194454-100bfb5cdeaa48888cf8bf528c709a3b.xml` | `737F9B34CAEBD7BE2FBE98EA03A9451B8FD64FFA2CC9C457402CB875B61A1232` | `EECF92F43AC7394C6858176DE3B20333725B27006B5956CD778B8645CAD8242D` |
| `git diff --check` | PASS | — | — | — |

The manifest's hashes were recomputed from the retained local artifacts. The
focused and full XML results both include the new populated Person-position
case. Protected user-owned files retained their recorded hashes:

- `ProjectSettings/EditorBuildSettings.asset` —
  `58BCBFB23DA7AAB5ACD696E7D83E9D75A86442ED39A582159D2493049361BA28`
- `ProjectSettings/ShaderGraphSettings.asset` —
  `5C432C9C73FDF73FEE91C8823EA02AB98E3B7A8EDF341B5AE6901D699CCE52A7`

Untracked `.meta` files were excluded from the candidate and left untouched.
This closes only the populated P8-C Person-position target rejection case; it
does not establish P12-G/P12-A readiness, complete owner/shared-epoch
coverage, capture eligibility, export/hydration, P13 readiness, or Phase 12
closure.
