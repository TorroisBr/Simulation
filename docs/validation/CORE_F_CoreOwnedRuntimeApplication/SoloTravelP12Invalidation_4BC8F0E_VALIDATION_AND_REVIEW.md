# CORE-F solo-travel P12 invalidation validation and review

## Candidate identity

- Candidate branch: codex/core/CORE-F-core-owned-runtime-application
- Implementation commit: 4bc8f0e8fa452bd065ffd8acebfa6872c2a65e0f
- Implementation tree: 35260257c515f6a4625002fff9ee1c1e7d8c6a07
- Parent: 45a6f4ed368a6485a094a7b28a352f62f1c5646e
- Current remote Core canonical at review: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Candidate merge base with remote Core canonical: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Changed files: Packages/com.simulation.core/Runtime/Application/DailyV1SelectedTravelDayStage.cs; Assets/_Project/Scripts/SimulationRuntime.DailyV1TravelStage.cs.
- This is an implementation candidate checkpoint only. It was not promoted to Core canonical and does not complete CORE-F.

## Validation

Validation was run against the candidate's exact two implementation-file contents. The Unity manifests record source hashes matching the committed blobs. The run snapshots also include local ProjectSettings inputs that were intentionally excluded from the commit and remain user-owned working changes; this record does not claim those settings are part of the candidate tree.

- CoreValidation: cold 907/907 and warm 907/907; run ID 0dcefbb1b96341219ed7081d66f32a08.
- Focused Unity EditMode SelectedSoloTravelProjectsEachArrivalBeforeP12CensusInvalidation: 1/1, run ID 4e16ac135106477abde598795c6c9026, manifest SHA-256 e68eefb4057b34b751f26b590af13e9905ded05ec5055d3a9ef2ed076ca51ccf.
- Focused Unity EditMode DailyV1RestorePreservesActiveTravelPartyWithoutReplayingEffects: 1/1, run ID 9ffd830909514ae8a8818a0cf4e70977, manifest SHA-256 1224b98552864c8bd98bcccf7e7152c4a61c1e23b7af0f121ee03642b53024ef.
- Focused Unity EditMode SelectedDailyV1OwnerVectorAndSupportedNpcTransitionStayBoundToTheLiveProtocol: 1/1, run ID 45eb1165db7243e6a5fe3854d1f70de6, manifest SHA-256 07d65e0dd892f184b0186b70cd1a51fe253e673057576c37fe45376db43e6d09.
- Full Unity EditMode: 3275/3275, run ID 3820586e869643c1b878d851a2cbef52, manifest SHA-256 258b282d0f2cf0c4d6a495e4fc98701e9dee501a0a1dd45d9f56e8bc9b1b229a.
- Official Unity Smoke: EditMode filter Smoke, 5/5, run ID 87e1866229824bea9b2ea9225b44b987, manifest SHA-256 7d61f4ddf974e202b4d137d337fd4e08a6f32e24ab408c228c104231c396f8b0.
- git diff --cached --check: PASS before implementation commit. No source edits followed validation.

A separate PlayMode invocation using the class-name filter GMConsolePlayModeSmokeTests returned a valid XML with zero tests executed. It is not counted as Smoke evidence; the required EditMode Smoke run above is the passing gate.

Original XML and raw logs remain in the protected local validation store. The run IDs and manifest hashes above identify the retained records without publishing local filesystem paths or raw logs.

## Independent exact-tip review

Verdict: VALIDATED_CANDIDATE — PASS; no actionable findings. The independent reviewer inspected commit 4bc8f0e8fa452bd065ffd8acebfa6872c2a65e0f against its parent and current remote Core canonical, and verified the relevant validation manifest/source identities. Review completed without rerunning tests or editing candidate files.

The review confirmed:

- P12 section IDs are derived from the exact prepared graph-bound NPC travel row and each exact changed City presence owner; duplicate or foreign owners reject.
- Party transitions validate against the full admitted roster, aggregate the changed section set in the Core session, and notify in deterministic sorted order when the party operation closes. TravelParty completion retains its existing store mutation callback.
- Solo transitions retain the prior per-transition notification behavior.
- Unity projection is published before the Core/session invalidation notification. Rejection before commit and post-commit failure paths remain fail-closed.
- Full-roster validation and mixed-roster compatibility behavior are unchanged by this commit.

## Scope limits

This checkpoint moves selected solo-travel P12 invalidation into the Core execution state after the Unity projection callback and retains the existing party batching behavior. Unity still provides the travel projection/event adapter, and the shared application, detached host, full P12 restoration, and T/T+1/T+2 continuation remain incomplete. No CORE-F completion, canonical promotion, or broader runtime-ownership claim is made.
