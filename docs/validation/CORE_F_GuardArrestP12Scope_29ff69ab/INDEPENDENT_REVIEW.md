# Independent exact-tip implementation review

Verdict: **PASS — no actionable findings.**

Reviewed implementation commit: `29ff69ab4f954e17202ffe26fae1ae0b2ac698c0`
Reviewed tree: `f3f1c98f920a1b9fd8a9160e25e66024501ed404`
Reviewed parent: `b44185a47683d98de70af220671b64e324f90208`

The review confirmed that Core derives the Guard Arrest scope from the exact graph NPC owners and Justice owner. Sections match the current P12 helper: guard and target status, Justice records and receipt, and the Crime receipt when composed. Exact-owner/admission failures fault closed. The legacy path remains when there is no selected Core graph.

The review checked the existing P12 contract in `docs/design/PHASE12_P12B_CRIME_JUSTICE_INVALIDATION_DESIGN.md` and the exact implementation delta. The focused Unity assertion preserves the two actual selected NPC status writes and leaves shared epoch semantics monotonic. Core scope tests passed in cold and warm CoreValidation; the exact Unity integration test passed 1/1 with stable inputs. Validation artifacts and their hashes are recorded in `VALIDATION.md` and remain in the private evidence store.

The review covers only this bounded code tree. It does not approve canonical promotion, public `TryStart`, the complete P12 detached restore/host startup path, or full CORE-F acceptance. Three unrelated local `ProjectSettings` changes were outside the reviewed commit.
