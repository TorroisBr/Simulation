# CORE-F restored Person/NPC/Genealogy validation

## Candidate and scope

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Base: `b4c6be93e7038761ca1aa74f52db5a2be5af64c3`
- Implementation commit: `1182692aa2e4db7ae4e6154f48db8d87bfb7bfea`
- Implementation tree: `d48b98cbc638a7dc340d1454ee019c536ae39c05`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

The exact Core owner graph now validates restored PersonStore rows against its selected NPC owner roster, checks the materialized-NPC index cardinality, and rejects Genealogy edges whose endpoints are absent from that same PersonStore. `DailyV1Application` runs this domain check as part of restored-Core-target acceptance after the complete target owner vector has been captured. Unity continues validating host projection references and compatibility bindings.

This adds no owner state, writer, mutation, revision, epoch, identity, or persistence semantics. It does not complete detached .NET restoration, final Library-free Core validation, a fixed .NET host, T/T+1/T+2 continuation, or CORE-F.

## Validation

| Gate | Result | Evidence |
|---|---:|---|
| Official CoreValidation cold | 1,020/1,020 passed; zero failed, inconclusive, or not executed | Run `df7432faa28b430a83b28acf74f20087`; private manifest SHA-256 `8622d61a3bde599d3de2ee84e8c73ec531c98c43e8e5e9ce8aca415bfc96a5e1`; input snapshot digest `2239aa848b659c47b5aebb6db0b049fd7600724446e3e3679f13b0b370068498`. |
| Official CoreValidation warm | 1,020/1,020 passed; zero failed, inconclusive, or not executed | Same run and manifest. |
| Focused P12G EditMode | 33/33 passed | Run `1b506469d5a946beb88f48c64eb0bcf7`; original result retained privately. The filtered XML was rejected by the official packager because its root suite count does not match descendants; it is not published as a package. |
| Full Unity EditMode | 3,299/3,299 passed; zero failed, skipped, or inconclusive | Run `b43fecca14914fbf954b9846a5b1705a`; input stable; original XML SHA-256 `ec957634723370098df8f054d5a48c0b581872e462e303e214b70de7586395ee`. |
| Full EditMode evidence package | Verified against protected original run | Archive SHA-256 `2bed96b1336661f5575f17a2f9b4e136a42a18a46bd67710f525bf387cde4e7c`; package manifest SHA-256 `7d9b97e97523508e06685f74ba5b6314d13b5505446aeba0240f3d95f30b9261`. |
| `git diff --check` | Passed | Before the implementation checkpoint. |

CoreValidation found a Unity `Library` in the worktree. No public TRX package was created, and this is not the final Library-free .NET-host proof. An earlier invocation began with the previous checkout as process working directory and stopped during the Lab restore before any tests; it is excluded from these results. The successful CoreValidation run used the E: candidate as its working directory and recorded stable inputs.

No separate Smoke result is claimed. The full Unity package contains only normalized result XML; raw reports and logs remain protected locally.

## Review and remaining work

The preceding restored-Core-target invariant checkpoint received independent exact-tip `PASS / VALIDATED_CANDIDATE` review at `codex/review/coref-restored-target-b4c6be9`, review record commit `b51e16bb7956dc3933e893377b9cdc634108e352`. The current Person/NPC/Genealogy addition still requires its own exact-tip review. No Core canonical promotion is claimed.
