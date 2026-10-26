# CORE-F selected Core runtime advance and P12 boundary

Status: validated implementation candidate on the existing CORE-F candidate branch. This is not canonical promotion or CORE-F completion.

## Scope delivered in this candidate

- Core genesis allocates the selected Daily-v1 `WorldId`; the Unity identity allocator remains on legacy paths, with only an internal deterministic injection seam for Core tests.
- Core derives the admitted selected roster compatibility decision from its exact ordered runtime roster. Mixed rosters retain the existing whole-day compatibility path; a fully selected admitted execution fails closed if its selected day-start context is missing.
- The selected P12 Crime/Justice daily mutation coordinator derives section identities and revision sources from the exact Core owner graph. Core now checks the exact active Crime/Justice identities against the separately bound census identities, verifies admission and owner-thread state, and owns the mutation fault/rejection decision before protocol reservation or mutation.
- A published selected session stores a Core-owned runtime advance boundary bound by reference to the exact `DailyV1ExecutionBoundaryState`. Core owns selected day/day-batch operation admission, the single advance lease, selected stage invocation, completed-boundary accounting and token publication. The selected boundary has no host day/intraday advancement members. The Unity adapter supplies the remaining P12 evidence/census and selected runtime projection ports.

This does not yet provide the public `DailyV1Application.TryStart`, a Unity-independent owner/runtime constructor, detached .NET host, end-to-end descriptor-v2 persistence, or separate-process T/T+1/T+2 continuation. Those requirements remain open in the approved CORE-F design.

## Validation

The validation input was the E: worktree with Git `HEAD` `315a3913da3b1818d688a93d2d1732c311f6014a`, committed tree `be5691cf8f7302b1d9b6338c0fb142022462b72b`, plus the implementation/test changes in this candidate. The two existing local `ProjectSettings` edits remained present, were included in the Unity input snapshot, and were not staged or committed.

- Official CoreValidation: 833/833 cold and 833/833 warm; run `bd04d496f68848208aa39c5205307f9b`; working-tree input digest `d5f603b82ff5ad6d80b0c8fd24340611bb4d69354b723206a397eac5fcbb3de6`; manifest SHA-256 `930d6eb212406d9e05a85c5d5354a8d68364affc1295248336986b1945566d09`. Original cold/warm TRX SHA-256: `67bacf19cd7cf7a67666182dbdceb241065581292c31b6aca6e85fdacf577486` and `4a915fe9a9bd524fe1e1404739e66026c67c7f9cdb4c4b7257f96f32f9360c58`. They remain in the protected E: evidence store; no normalized .NET TRX package is claimed.
- Official full Unity EditMode: 3270/3270 passed; run `fb7b4665d3724a888f9646129039362d`; stable input digest `bc3247e7db4662e85aed005d2592d87eb0a1487d1180d24059ea4b9cbab82059`; original XML SHA-256 `6f62414ab75032cc26e50b96abfebb43f7b167dc7a44fb31c17109ced76ab012`; runner manifest SHA-256 `6742ac7b26c6dbcb474dabbd4023c59c663612dbaebdf19322b35cb449c8f5be`.
- The normalized full EditMode result package is [CORE-F-Core-SelectedRuntimeAdvance-315a391.zip](Evidence/CORE-F-Core-SelectedRuntimeAdvance-315a391.zip), 172,964 bytes, SHA-256 `66c8523bccb836b991728698f63e360f46e25e0aadf8fa16dcdf4274fabc6f96`. Its package manifest is [CORE-F-Core-SelectedRuntimeAdvance-315a391.manifest.json](Evidence/CORE-F-Core-SelectedRuntimeAdvance-315a391.manifest.json), SHA-256 `45a8735ee09cc303f488cb5e58ac7ecf53f10180b8127422d353ccacf8bb70ec`. The official verifier returned `VALID: runs=1 entries=1` against the protected run manifest.
- Official Smoke filter `Smoke`: 5/5 passed; run `0ca38dd49cdd47279def4a46bb1684c4`; it used the same stable Unity input digest. Original XML SHA-256 `df0a6acb22ee11e4bd5195d483593d0f07028d056eb16ea6ec338fc4b0c7e109`; raw log SHA-256 `f29cf2c90420d1db5c0be8376789bc432e96c6104b08da2e0ad97bd6045f7de7`; runner manifest SHA-256 `6c7fc256b558f3db3aa159c08e27fd2c8471279800f4b6f4adc41f942a01db65`. The originals remain protected and unchanged. No normalized Smoke package is claimed; the existing packager rejects this filtered report's suite aggregate counts, as recorded by the prior exact-profile packaging check.
- An earlier official Unity invocation, run `6fd1ec0292c14a80b8ab7d5583c0daec`, returned `NoResultXml` because the in-progress source named inherited evidence members as members of the legacy interface. The compiler reported those explicit-interface declarations; the source was corrected to bind the members to `IDailyV1RuntimeAdvanceEvidenceBoundary`. The later full EditMode run above passes on stable input. The failed original log/manifest remain in protected evidence.
- `git diff --check`: passed after the implementation changes.

The CoreValidation originals are not normalized into a distributable package, so this evidence is sufficient to retain the candidate's recorded validation but does not satisfy a future full CORE-F evidence closure by itself. No test report was edited or reinterpreted.

## Preservation and limits

`ProjectSettings/EditorBuildSettings.asset` and `ProjectSettings/ShaderGraphSettings.asset` remain unrelated user changes and were not staged. This candidate is not Core canonical, does not close CORE-F, and does not claim the Unity projection/census ports have been moved into Core.