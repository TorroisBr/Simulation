# CORE-F selected runtime advance boundary — independent review

**Verdict: VALIDATED_CANDIDATE — bounded implementation checkpoint only.**

## Reviewed identities

- Candidate branch: `codex/core/CORE-F-runtime-app-session-boundary-20261025`
- Candidate commit: `251afabc1cd198bd9e9898e405d983f469b3e0c4`
- Candidate tree: `3d60b4a6c43b6a28c977f1f993ffa6b2ae38b08c`
- Candidate base: `315a3913da3b1818d688a93d2d1732c311f6014a`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Candidate and canonical refs were fetched immediately before review; the remote candidate matched the reviewed commit. This review did not change the candidate.

## Review result

I reviewed the complete base-to-candidate diff, the current approved CORE-F runtime/restore design, the owning Core State, and the test and validation records. I found no actionable implementation or test-integrity defects in this bounded checkpoint.

The selected session now binds one Core-owned advance-boundary object to the exact Core execution-state instance and immutable host-port instance before publication. The selected application route only accepts that Core boundary, verifies the session/state identity, reserves the application operation, and calls the Core selected advance path. That path has no day or intraday host-advance member; the legacy host advance remains on the legacy boundary. A missing selected stage context fails closed for an admitted all-selected roster. Compatibility fallback remains only when Core's own ordered roster contains an unselected live member.

The existing selected stage continues to invoke Core-owned deterministic operations with the exact admitted graph and allocator/random/sequence roots. Unity supplies current selected-stage, actor-action, projection, and P12 evidence/census adapter ports. The candidate does not duplicate the Core state object.

The P12 Crime/Justice path derives NPC status owners, section identities, and the Justice revision source from the exact Core graph. It checks the active Crime/Justice receipt census identities by reference, checks selected admission and owner-thread state before baseline validation or mutation, reserves the affected sections and epoch before the daily mutation, and closes the protocol on rejected/faulted work. Each existing Crime/Justice phase remains a separate operation. The added tests cover exact identity, unadmitted state, wrong-thread rejection, and no mutation before rejection.

The bootstrap change removes the Unity allocator from the selected Core genesis path while retaining the legacy Unity hook. The typed definition graph, canonical definition-node references, and P9 fingerprint assertions remain covered.

Test edits do not weaken assertions. The selected bootstrap assertion now expects Core rather than Unity to allocate the selected WorldId. The roster-compatibility test constructs a real Core roster entry for the unselected member and proves the host compatibility flag cannot select that path. The missing-context test proves that a fully selected admitted path does not delegate a day to Unity.

## Exact-source validation evidence

The official CoreValidation run passed 833/833 cold and 833/833 warm. Run ID: `bd04d496f68848208aa39c5205307f9b`; input digest: `d5f603b82ff5ad6d80b0c8fd24340611bb4d69354b723206a397eac5fcbb3de6`; protected runner-manifest SHA-256: `930d6eb212406d9e05a85c5d5354a8d68364affc1295248336986b1945566d09`. Both original TRX files were present in protected evidence and their hashes matched the runner manifest.

The official full Unity EditMode run passed 3270/3270 with no skipped or inconclusive tests. Its exact run-manifest SHA-256 is `6742ac7b26c6dbcb474dabbd4023c59c663612dbaebdf19322b35cb449c8f5be`. All seven `SimulationRuntimeLongRunTests` cases in the full result passed. The official Smoke run passed 5/5 with no skipped or inconclusive tests; its run-manifest SHA-256 is `6c7fc256b558f3db3aa159c08e27fd2c8471279800f4b6f4adc41f942a01db65`, and the original report and log hashes matched that manifest.

All 18 changed C# source/test files match their corresponding official CoreValidation or Unity input-snapshot hashes. The full EditMode package was rechecked with the repository's official verifier against its protected run manifest: `VALID`, one run, one entry, archive SHA-256 `66c8523bccb836b991728698f63e360f46e25e0aadf8fa16dcdf4274fabc6f96`. Package-manifest SHA-256: `45a8735ee09cc303f488cb5e58ac7ecf53f10180b8127422d353ccacf8bb70ec`. `git diff --check` passed for the exact base-to-candidate commit.

Validation ran on the candidate source changes before the candidate commit. The two pre-existing local ProjectSettings edits were part of the Unity input snapshot and were intentionally not included in the candidate. The changed C# source hashes match the validated inputs. Revalidation is required if a later integration changes relevant Unity settings, tools, source, tests, or scope.

Evidence limitation: the normalized full EditMode package is included in the candidate. The .NET TRX originals and filtered Smoke originals remain protected locally; this candidate does not include normalized .NET or Smoke packages. Their results and hashes are recorded above and in the validation record, but this is not a complete permanent evidence package for final CORE-F acceptance.

## Scope and remaining integration

This review approves only the exact implementation candidate as a bounded checkpoint. It does not approve Core canonical promotion or claim that CORE-F is complete. The public `DailyV1Application.TryStart`, Unity-independent selected owner/runtime construction, full P12 detached restore and descriptor-v2 application integration, fixed .NET host, and separate-process T/T+1/T+2 continuation remain undelivered. Unity still supplies selected-stage/action projection and P12 evidence/census ports; the admitted mixed-roster compatibility fallback still uses the established whole-day host path. These are explicit package limitations, not claims satisfied by this checkpoint.

Before any later CORE-F package promotion, complete those approved integration obligations, produce the required durable evidence packages, validate the final integrated tree, and obtain a fresh exact-tip review.
