# CORE-owned P12 header and causal roots

## Scope

The continuation-save reservation now captures and retains the P12-C causal-root snapshot once. Its owning `DailyV1Application` uses that exact snapshot to build the continuation header and P12-C causal-root sections, verifies the host compatibility descriptor against the Core P8/P9 fingerprints, and seals those sections with the reservation's completed-boundary owner vector. The host continues to create its existing v1/v2 compatibility descriptor and supplies the existing `PoliticalWorldRevision` scalar unchanged. Owner-row encoding is unchanged. The no-operation legacy capture path retains its former header/root/owner encoding order.

This is an incremental capture-boundary milestone. The Unity coordinator still owns source/owner encoding and detached restore orchestration. This does not claim full P12 Core ownership, detached .NET hosting, full EditMode/Smoke validation, or CORE-F acceptance.

## Source identity

- Base: `f2cf0db17d753117f48df77061b22bd8249dc078`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Current candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- The exact changed source inputs tested on this base are identified by SHA-256 below.

| Input | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` | `4b7410a8002c2837b57022dae4a811d01a29f53cf81cd1543a134298ad0d1968` |
| `Packages/com.simulation.core/Runtime/Persistence/DailyV1P12GContinuationSourceWitness.cs` | `84173d9b8b270e7f54d89c30d60f1272dec3e2644d92cb1581937848385ebee5` |
| `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.cs` | `9c68a5f12a4963742fc9f2837e63ca481293f81852c421110ea35a0a5917ae7f` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | `2384f41aa49f1bcda4d7b4268b6fe896dfc2a570f50e24f89ac20f657232ea36` |

## Validation

- Official CoreValidation run `931df7aaf1bd4cd0914a975e9aa318a3`: cold 936/936 and warm 936/936, no failures or skips. Input snapshot was stable; digest `0dc384023a0bf8e64ee2b05e29331478f6dac4174e13f40f95fa1d55e2ac20fd`; protected manifest SHA-256 `a1f905e72561c9234b4d5aee33634e1b29c3e31cbb0cc73ed0a08e2cfcf56971`. This checkout contains a Unity Library, so no Library-free .NET claim is made.
+- Official Unity EditMode persisted-image restore/continuation, run `e28c65aa61084427a39a3bc8e7590053`: 1/1 passed; stable input; manifest SHA-256 `fd755b329cc0ac7e099e7f68416945d44f4e3b66de1b638e2794a31c7cee8007`; original XML SHA-256 `d1d55fb9034ac00484d0de39e29fdbbd8e04b032d7e0bf81469458846daa252c`.
+- Official Unity EditMode save-operation closed-boundary regression, run `c95ff16483ed482bacd8d1679fa7ffb9`: 3/3 passed; stable input; manifest SHA-256 `d0ca0504e9bd73ea26642893963932a26ace9d02be0a7eed6672cca67d6aa020`; original XML SHA-256 `ad14e9754da38bcc0c17ca2993e4503bbb40ff7e00c9e2204cdef1949045325a`.
+- The persisted-image report exercises the current P8/P9 descriptor-mismatch rejection assertions; the closure report verifies rejection after the save operation closes. `git diff --check` passed.
+
+Raw reports/logs and manifests containing machine-local metadata remain in protected evidence storage; they are not included in this record. No full Unity EditMode, Smoke, or permanent privacy-safe result bundle is claimed for this incremental checkpoint. Broader integration validation and evidence packaging remain required before eventual CORE-F acceptance.
+
+## Review and promotion status
+
+Independent exact-tip review for this follow-on candidate is pending. This implementation remains on the candidate branch; no canonical promotion or CORE-F completion is claimed.