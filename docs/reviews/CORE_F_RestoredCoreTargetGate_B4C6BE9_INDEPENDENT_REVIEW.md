# CORE-F restored Core target invariant — independent exact-tip review

**Verdict: PASS — VALIDATED_CANDIDATE (bounded target-invariant slice).**

## Exact identities

- Repository: https://github.com/TorroisBr/Simulation
- Candidate branch: codex/core/CORE-F-restore-stage-transaction
- Exact candidate and review base: b4c6be93e7038761ca1aa74f52db5a2be5af64c3 (tree 16ab4aebd7d710650dc17274c2a5034819c30db8)
- Implementation commit: 21cccfeb0e92aed5fad0c08224d1befe99ac8205 (tree dfdb6d756d5df83bca942d34c78cde9bfb3d88fc)
- Implementation base: 5e2ffeb27df97be00101ae3ce18f4b36b92ed4ed
- Current Core canonical: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- The implementation commit is the candidate's parent. The tip adds only the validation note, manifest, and full EditMode archive. The candidate and canonical refs were confirmed from origin; this review changed neither.

## Review result

The new Core target gate is placed after complete target-vector capture and image-to-target comparison, and before Unity sentinel/projection checks, guard binding, post-bind recapture, restored admission, and publication. The gate requires the candidate session to retain its exact execution state, selected owner graph, effective definitions, allocator, and record/event authorities. It then validates Core TravelParty bindings, checks allocator counters against all registered and retained allocated identities, and requires exactly one Required Expedition census row for the same live ExpeditionStore, with schema, zero cardinality, non-negative revision, and matching owner identity/revision.

The Unity application-backed path delegates the Expedition and allocator checks to Core and skips only the duplicate Unity checks. The legacy route keeps both checks and calls the same Core validation helpers. Unity still performs its host projection, sentinel, spatial, NPC, PoliticalKnowledge, and projected TravelParty checks. The change introduces no new authoritative Unity copy and does not move publication ahead of the Core gate.

A Core-gate rejection returns through the target-owner-vector failure path; the candidate remains private and cannot reach guard binding, admission, or the one publication gate. Existing post-bind vector/epoch recapture and publication checks remain in order.

Tests were additive: a focused test accepts the matching live Expedition witness and rejects nonzero cardinality; a second test rejects a stale allocator counter and accepts the next sequence. No existing assertion was removed or weakened. git diff --check passed on the full candidate delta.

No actionable implementation finding was identified.

## Exact-tip validation and evidence limits

- The public full EditMode package is run 5ecafc38f81b4115a0a6fdb7417ae40d: 3,299/3,299 passed, with zero failures, skips, or inconclusive results. I independently checked the committed archive bytes: 174,779 bytes, SHA-256 1a0d6dc99481852bd0fc7fc88260503cdd9ca6c8bd1fe07987c218f752244943. Its single normalized result XML reports the same counts and contains no drive-absolute path.
- The public manifest's source hashes match the implementation's Git blobs for all four Unity production inputs: restore image coordinator 12cb295833e9297e1f5d4849c01676dde22c1ae2 / SHA-256 eb2cd1d3f405d6b605aba4af06912616ef002ae0501151ccfe8937a0b8e716d3; Unity coordinator 72fe63b4811cd8822e1dbc38bf0664d435b29d93 / 734efed27eb6626d550bbd4923ab25f31f6c7ea88111a07a9f4af0fb29ae2902; Core application 3289e4116504b056a459b152e7d04616fcffae72 / d277742b295dcd36e4704a96e93d242f57319c4f2c62ed9659c324b09d9e30eb; owner-vector contract 4a318760b86d629e446484c96f021d0c3fb268f9 / dd2497a786cbda7c6f6f97bc8c62433995f463701f04c172f9c824d6d4372537. The Core test input blob is d439b8751c5bc40bb6b18b5f70bcb5ee9b2b4080, matching the validation note's tested-source row.
- The archive SHA matches the manifest. The manifest's documented Windows CRLF SHA-256 is 755d71fee3324ea24dc3a9e9898e4f56a184ec9277fa66fe22bc460745312a9c; its Git-normalized LF blob has SHA-256 5c98d9e462bede9d832b75daa19701b91ba936d28c061b99be662fa48e734925. The line-ending representation explains the difference.
- The protected CoreValidation run is reported as aac6561b487749acaa0b3cf97134929c, with manifest SHA-256 6615b9d9216008d65be4d8bd14ef7d9a120c882e44919a4fa1185d6a0cf3bd2d, and 1,019/1,019 cold and warm. The owner-only manifest on E: was not accessible to this reviewer, so its contents and TRX reports are not independently claimed as inspected. The record reports worktreeLibraryAbsent=false; no public TRX package exists. This is not the final Library-free .NET proof.
- The filtered Smoke run is reported as 5/5. Its separate package was rejected by the result packager because the filtered suite counts are inconsistent; no separate Smoke package is claimed. All five Smoke cases also occur in the verified full EditMode package.

## Scope limit

This review validates only the bounded restored-Core-target invariant implementation. It does not establish standalone .NET restore or DailyV1Application.TryStart, a fixed .NET host, T/T+1/T+2 continuation, full CORE-F acceptance, or readiness for canonical promotion. Exact-tip canonical preflight and the separate promotion workflow remain required.
