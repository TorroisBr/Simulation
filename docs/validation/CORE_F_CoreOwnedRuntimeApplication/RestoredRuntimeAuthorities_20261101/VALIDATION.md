# Core-owned P12 restore runtime authorities

This candidate moves creation of the restored decision store and event/history/decision-receipt authority into the bound `DailyV1Application` restore-stage operation. The Unity coordinator still supplies its configured logger/output adapter and consumes the exact Core-created authorities. The stage order remains P12-C admission and roots, P12-D, Core runtime authorities, P12-E, then P12-F. No active session is published by this slice.

This is an intermediate CORE-F implementation checkpoint. It does not complete the shared runtime, detached host, or CORE-F acceptance and is not a canonical promotion claim.

## Tested source and inputs

- Candidate base: `9db8da1dcf27978bc96c7ce086546a58d4353099`; committed tree `0037ce2d77f9b740ec5cf9470e6ce572bff9d6e8`.
- CoreValidation input digest: `9594b3c6ee73c1cbfec9e72ecd943b558ce837dd0dc9dd943cb59f7ff023dac4`; stable before/after, including generated dependencies.
- Unity validation-input digest: `5f44304c5dd8e114fb1abccd2e6cf67da01e5ed75142c4214a1a676130d22a18`; stable for the focused restore suite, Smoke filter, and ALL EditMode run.
- Changed source files: `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` and `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.ImageRestore.cs`.
- The protected ProjectSettings inputs were captured by the official Unity runner. They and the separate user `TesteSimulacao.cs` change remain unstaged and untouched.

## Results

| Gate | Result | Evidence |
|---|---:|---|
| CoreValidation cold | 988/988 passed; 0 failed | Private run `80aef68b23424cdab6a95b8e3c1bc4cf`; TRX SHA-256 `eb53bb641dc84b8a2bc98db8512159b91f3cdb56f044fd9ce9c479084b45e505` |
| CoreValidation warm | 988/988 passed; 0 failed | Same private run; TRX SHA-256 `f87c46ff5e88831407685f9a43d1181a65425d75403db9dbf45bd2faebdc256e` |
| Focused P12G restore suite | 16/16 passed; 0 failed/skipped/inconclusive | Private run `084e6c801c674ffabfbdd96e00ecd222`; report SHA-256 `6921741e8fc7664496704f3e5f0ff7b4d7502db385f2fc36ccde1c2d6030b472` |
| ALL Unity EditMode | 3294/3294 passed; 0 failed/skipped/inconclusive | Public package below; original report SHA-256 `ce079e511109fd897ee5e102700520fc16e63350093b6048bc2bd366e55e77ca` |
| Official EditMode Smoke | 5/5 passed; 0 failed/skipped/inconclusive | Private run `cc6e9264b3764a8581b8ac1fcc7b1437`; report SHA-256 `998629e13ac708a8260251fc15cb5840b6ad0441276ef4bd5f491dad3ebd9518`. All five cases also appear in the complete EditMode report. |
| `git diff --check` | PASS | Candidate diff |

Unity used Editor `6000.3.9f1`, Test Framework `1.6.0`, and the official repository runner from the E: worktree. The result-only package is `coref-restored-runtime-authorities-all-1a4ff9d.zip`, SHA-256 `0f326e0928f97ba337f2526b26784259d6bbacce5ac80a83c12f06c61de144a5`; its manifest SHA-256 is `62538c920e1fe6a915f36d9b18389eec8b013a457640895490900965286b41e2`. The approved verifier reports `VALID`; the archive contains one normalized complete ALL report with 3294 cases. The full report includes all five Smoke identities and outcomes.

CoreValidation used pinned SDK `9.0.315` with `-SkipLab` because `tools/Simulation.Lab/Simulation.Lab.csproj` is absent from this checkout. Core tests and CalendarDemo cold/warm gates ran. The worktree contains a Unity `Library`, so this evidence makes no Library-independent host claim. The runner emitted `simulation-core-validation/v1`, not the approved `dotnet-trx-source-context-v1`; accordingly, no public .NET TRX package or original-to-public comparison is claimed. Its private manifest SHA-256 is `1563029cc03638b0650f1c85033256f1671a9d7e3274925c5ba14bbced0f283`. The original reports, logs, and manifest remain unchanged in owner-only local retention; no source context was fabricated.

## Evidence boundary

The Unity result-only archive and manifest are permanently reviewable. Protected focused, Smoke, and CoreValidation originals remain in owner-only local retention. This checkpoint does not satisfy the full CORE-F acceptance or canonical-promotion gate; detached .NET host, restored session composition, runtime advancement, and continuation parity remain outstanding.
