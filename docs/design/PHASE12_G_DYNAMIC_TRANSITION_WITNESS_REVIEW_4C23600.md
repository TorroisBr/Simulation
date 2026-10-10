# P12-G DynamicTransitionWitness — independent exact-tip review

**Result:** `VALIDATED_CANDIDATE` — the bounded existing-NPC Person-binding manifest witness passed review.

**P12 canonical base:** `a7810cd149abbc1bcdadca9073db9001162431ad`

**Candidate:** `4c23600a5424cf3955fb774f8a03b201f8956573`

**Candidate Git tree:** `1255f32ce4594a072afa8ffbcb464c3023acdd93`

**Reviewer:** `actor_choice_impl_review`, independent of the candidate author.

## Review findings

The complete candidate diff from the named base adds one test to `SimulationBootstrapCompositionTests.cs` and validation documentation/artifacts. No production code or unrelated project files changed. `git diff --check a7810cd..4c23600` passes.

`SelectedDailyV1ExistingNpcPersonBindingReconcilesDynamicOwnerManifest` starts the authored Daily-v1 profile and deterministically selects an installed City and one of its existing NPCs. It invokes the independent `BuildSelectedDailyV1SectionInventory` through `AssertSelectedDailyNpcOwnerFamilies` before mutation, after registering a Person, and after binding that Person to the existing NPC. That helper compares the independently built manifest against both protocol expected and registered ID sets; it checks role/schema/section identity, live owner identity, cardinality and revision, stable repeated readings, and the documented fixed/NPC/Person/City formula. It does not rely solely on the census protocol's own expected dictionary.

The test separately demonstrates the two transitions: Person registration changes the Person row while leaving NPC roster count and selected City-presence count/revision unchanged; binding the Person to the existing NPC changes binding ownership while keeping the NPC roster count, the NPC's City, and that City's presence count/revision unchanged. A final roster-census assessment passes. This supports the test-only dynamic membership/cardinality claim without asserting every owner transition or exhaustive live inventory.

The scope and State wording correctly retain the limits: this adds evidence only, does not make P12-G ready, and does not establish whole-graph validation/atomicity, capture eligibility, export/hydration, P12-A readiness, P13 readiness, or Phase 12 closure.

## Validation evidence

I parsed each retained NUnit XML and recomputed its SHA-256. All report `Passed` with zero failed, skipped, or inconclusive tests; hashes match `VALIDATION.md`:

- Focused `SelectedDailyV1ExistingNpcPersonBindingReconcilesDynamicOwnerManifest`: 1/1; XML `91A78551DB9D334582798744397A75DA58969691E99065E63BE34FC88A064DA9`.
- ALL EditMode: 2740/2740; XML `C3581DAD88AA80065ED20DF46DCA2C534FD670C230935DC8E5B77621BAFA93D8`.
- Official Smoke: 5/5; XML `2FFD1A54121E9E2E03A94680940DBBC4C59574E5863E25663C74ECA74C89A9F8`.
- Raw log archive `RawLogs.zip`: `DA5BEE33BDA286BBF8406E0EE17A072576CD8569F778602B49D8F226FDCF13C1`; it contains the three matching run logs.
- `git diff --check`: PASS.

No tests were rerun during this independent review; the listed results are retained exact-candidate evidence.

## Status boundary

This review validates only the dynamic Person-registration and existing-NPC binding witness. P12-G remains `WAIT_DEPENDENCY`; P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains open. No canonical promotion or phase closure is implied.
