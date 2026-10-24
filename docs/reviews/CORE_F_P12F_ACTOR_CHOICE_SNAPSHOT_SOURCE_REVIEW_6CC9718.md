# CORE-F P12-F ActorChoice snapshot source integration review

- Verdict: VALIDATED_CANDIDATE (PASS); no actionable findings.
- Reviewer: independent Codex review agent, task /root/p12d_hydration_exact_review.
- Reviewed candidate branch: codex/core/CORE-F-p12d-core-hydration.
- Candidate/docs tip: 86759ee0f6a9b3a32e8e4e3e474ab9165755efcc (tree 971b7bd166ea36dff91730cf5ac77d9f1a5d484c).
- Implementation commit/tree: 6cc9718f19ff46b026354fa48d89bc07f16c701f / d4a95197eab08725d83cd06c2b6f153c95f8cf60.
- Implementation base/parent: 950bce0b3650b38d69b4f7d6159183ed46f5b184.
- Current Core canonical at review: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea.
- Remote preflight: candidate branch at 86759ee0f6a9b3a32e8e4e3e474ab9165755efcc; Core canonical at a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea. Code is a clean descendant of the named base; base and candidate descend from current Core canonical.

## Scope reviewed

The commit moves the unchanged P12FActorChoiceSnapshot and ActorChoiceCensusProviders source files and their Unity .meta files from Assets/_Project/Scripts to Packages/com.simulation.core/Runtime/Persistence, adds explicit Core compile entries, links existing P12FActorChoiceSnapshotTests into CoreValidation, and changes one test argument from the Unity result wrapper to equivalent Core enum NpcActionResultType.Success.

The source and .meta Git blob identities are unchanged across the move:
- P12FActorChoiceSnapshot.cs: 9a10a4d21dba448e21c9814f7560c2721c201009.
- P12FActorChoiceSnapshot.cs.meta: 2a07b0d8b3f689a6e777af8c8a43a5ae0463499d.
- ActorChoiceCensusProviders.cs: bf19003af4c333d6488c4caf18115b16659d005f.
- ActorChoiceCensusProviders.cs.meta: 7b91dd236bf438990f97fb58536744c242eec679.

The snapshot remains terminal-history-only: it validates the completed Daily-v1 boundary and exact owner census, rejects pending/in-flight/unsupported temporal state, stages against the staged PersonStore, and does not invoke or replay actor choices. Census providers remain passive; no domain writes, identity/revision behavior, assertions, snapshot semantics, profile scope, or production behavior changed. The Core asmdef has noEngineReferences=true; neither moved source references Unity APIs. Existing test assertions are unchanged. The test argument is equivalent because NpcActionResult.Succeeded() returns NpcActionResultType.Success, and TryRecordAttemptReturned consumes that enum. No duplicate authority was introduced.

## Validation and evidence

CoreValidation private manifest:
- Run: a1b6544d1d724d45b2f2fe4a3ed91251.
- Tested as a source overlay on base 950bce0b3650b38d69b4f7d6159183ed46f5b184; input digest e1738c4bc65599a93c2d4b22af4c5484ce62ae7dc0b491f43dc0512ea2d21d93.
- Private manifest SHA-256: 1227ab7d18bef691698ea912437d7aedffef1aa9084e791efb38368278675f1e.
- Cold 3cafe5c2-a290-4ee1-a0fc-ef71b12ed15b: 730/730 PASS. Warm 5410fbcd-30ed-4150-8ba4-1c49dd472c76: 730/730 PASS. All 369 recorded input file hashes and sizes independently matched the current candidate source checkout; zero missing or mismatched entries, and the CoreValidation inventory contained no ProjectSettings paths.
- Original cold/warm TRX SHA-256: bd99e5ddfa823557ece0841023a7a8276b1d665cc7a002695ee8a45e1f0c152c / 05f936c40fd6cb760a320bbaec0d26b483f0dd69751c74cec42ccfc0604833dd.

Unity private run manifests bind code commit/tree, Unity 6000.3.9f1, and stable 1,874-file input digest 3854484304e8e20c2d15db2f17eb12005d7297476324e81dff0c919ad678c88a:
- Focused P12FActorChoiceSnapshotTests, run 3f6a0128384f4d58875655c1bf8348ab: 7/7 PASS; original XML/log SHA-256 260c3e470159eddca5807eef1403016e355f2df06afe19492bb514077e126baf / 73244c18ebe7ff96ea57283ac795b500ef8cf726796770418a0db18247a5cb72.
- Full EditMode, run 2f55993fa1f3497f8b28f054a5ab49da: 3263/3263 PASS, zero failures/skips/inconclusive; original XML/log SHA-256 b67cd247d75820dda2cc9724d65071472d619d53a42621851a1ead9e7b8379c7 / 43241fd2db6243a257b214be3d46b9dc80ceb133347a523191dcce8809e428b2.
- Official Smoke filter, run c5bbe7a50d014f4fb5e35b80f99d8f7c: 5/5 PASS; original XML/log SHA-256 277e4c2c37777c699917bae4059a2bce5cb308569423ab63fd796c8c5a54b370 / c9b25ca42ec7f5ed97caf1c4bc6f853dbb537d69454f906f042976a399b60559.

Filtered focused and Smoke reports remain private and unchanged because the packager rejected Unity's inconsistent parent-suite testcase counts. All seven focused identities and five Smoke identities/outcomes were independently matched to passing entries in the complete report.

Full-suite package: archive SHA-256 7128afeea247d68d96b01214f5a774c2b3cbd45e0141b2760c2dd22cc21ee3fc; manifest SHA-256 364cc6088d8f544052249a937b95f1d5e8d9651f398093c07c871bb95e2576ad; normalized XML SHA-256 988fb39a0cf548ad8e6b980f52314c3d68a8a7fa2d5caf5996549b56483befbb. I independently recomputed the archive/manifest/XML hashes, verified its single full-suite XML has 3,263 passes and zero failed cases, and scanned the public manifest/XML for workstation paths and credential patterns (none found). git diff --check passes for base-to-code.

The first Unity import attempt was correctly marked unstable because Unity rewrote two protected ProjectSettings files. Later disposable validation worktrees were pre-seeded with byte-identical copies of those existing user files. The source worktree was not modified and no ProjectSettings file is in the candidate diff. Their contents were not inspected. Successful Unity input inventories were stable before and after.

## Limits

This is a Core source-ownership slice only. It does not complete P12-F, P12-D, the shared DailyV1Application, detached .NET host, CORE-F, or Phase closure, and it does not promote Core canonical. CoreValidation remains a private simulation-core-validation/v1 result, not a public TRX-EVIDENCE-v1 package, and makes no Library-independence claim. Filtered reports remain private; the complete public normalized Unity report contains their case-level results.