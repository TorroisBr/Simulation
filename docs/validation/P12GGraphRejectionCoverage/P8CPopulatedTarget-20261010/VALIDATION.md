# P12-G P8-C populated target-cardinality proof — validation

**Base:** `0acfd53d6a1090b7f40f9c55186babd7d281c527` (`codex/phase12/canonical`)
**Code commit:** `70f442f46898f80b61d581e360fed882eb579c11`
**Git tree:** `8b75d03f4f7a1e775a4b69fd847671ac0d66374c`
**Tested `Assets` tree:** `0581ee266cd0b9c2822aaa7f2ff9978a0f52ef08`

The candidate changes only `SimulationRuntimeAdmissionTests.cs`. The added
case selects a deterministic existing staged City, binds it to the sole
staged P8-A Location in the private restored candidate, and observes the
installed P8-C census provider at cardinality/revision `1/1`. The restore
coordinator then rejects the profile's explicitly-empty P8-C target through
the existing target-vector path. The shared harness checks that the original
active session, completed-boundary token, health, and graph remain intact,
that subsequent continuation matches the uninterrupted control, and that a
later valid restore succeeds. No production code or profile contract changed.

## Validation

Unity `6000.3.9f1` was used. All results below are from the tested Assets tree.

| Gate | Result | XML | XML SHA-256 | Compressed log SHA-256 |
|---|---:|---|---|---|
| Focused `DailyV1RestoreRejectsCorruptedRootOrOwnerVectorAtomically` | 20/20 PASS | `FocusedRaw/EditMode-20261010-193230-0b6cf77ea4774442b91ebd9a1e78fcc7.xml` | `7646A31CD300833FF74D45F79A1496EB03BB7CEE518BD6EA2A660FF33584A7CA` | `35390709A6CD70AC591195834B8602EB4DEF39DDF35659CDFBEA4D687A23AC27` |
| ALL EditMode | 2846/2846 PASS | `AllEditModeRaw/EditMode-20261010-193256-7f2556f55ccb49d98fd239a583ef90aa.xml` | `437B55BB644CDCA83A25B74843E30B014581F84C452D403DE56230176582FE26` | `6F2268F793791E44A1BC7877A112011498618D0931C04417EA099D665D64BC5F` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 PASS | `SmokeRaw/EditMode-20261010-193352-8a229963bda04c45919b4a10cfa02a37.xml` | `A45060DCBFBD1ED887E44A04B0683407FBAD055CB5FB6018EA07D22696E38A30` | `17BBCC819889B9D64A16A632F328F0B7F55170622A114FF2C992485050564298` |
| `git diff --check 70f442f^ 70f442f` | PASS | — | — | — |

The protected user-owned files remained outside the candidate commit. Their
observed hashes in the existing P12-G worktree remained `58BCBFB23DA7AAB5ACD696E7D83E9D75A86442ED39A582159D2493049361BA28`
(`ProjectSettings/EditorBuildSettings.asset`) and
`5C432C9C73FDF73FEE91C8823EA02AB98E3B7A8EDF341B5AE6901D699CCE52A7`
(`ProjectSettings/ShaderGraphSettings.asset`). Untracked `.meta` files were
also excluded and untouched.

This is bounded rejection evidence only. It does not establish complete
P12-G graph/rejection coverage, broader owner/epoch completeness, capture
eligibility, P12-A readiness, P13 readiness, or Phase 12 closure.
