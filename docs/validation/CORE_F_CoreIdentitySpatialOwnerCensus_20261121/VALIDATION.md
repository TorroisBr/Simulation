# Core identity and spatial census providers

## Candidate identity

- Candidate branch: codex/core/CORE-F-restore-stage-transaction
- Implementation commit: 487f491beba6267ea4f5502f2a3838b6674751a7
- Implementation tree: 3e16b0091e0bde3ec2b635130fe79650869c5b33
- Parent/base: bac90bed3e953d1b1228ddd9eaeb1e74d60980e1
- Core canonical: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea

## Delivered boundary

Core now constructs the 19 selected identity/spatial census providers from its selected owner graph and live state. The Unity compatibility census compares section, schema, owner instance, cardinality, and revision before replacing the matching Unity projections with the Core providers. It retains the rest of the sealed section inventory and faults closed when the comparison fails. The P8-C exact-zero binding row remains constrained.

The change touches:

- Assets/_Project/Scripts/ContinuationCensusProtocol.cs
- Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs
- Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs
- tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs

This is a partial census ownership migration. It does not complete the 89-family census, make the complete selected runtime start independently in .NET, or establish DailyV1Application.TryStart, a detached .NET host, or process-separated continuation.

## Validation

All validation inputs for this candidate remained stable. The exact source commit/tree above was validated.

### CoreValidation

- Official CoreValidation, cold: 1,186/1,186 passed.
- Official CoreValidation, warm: 1,186/1,186 passed.
- Exact-source package run: Library absent; Lab was explicitly skipped for the normalized three-project TRX package profile.
- Runner run ID: dd8aae575c0740578f6cc9834ceeb443
- The cold/warm original reports were preserved and compared against the normalized public archive.
- Package archive: DotNetTrx/coref-identity-spatial-corevalidation-20261121.zip
- Archive SHA-256: f56871c8c8a7c9a8e04ad3f60de2e2bf79d49b922947ee36d4cf92d993b654ce
- Package manifest: DotNetTrx/coref-identity-spatial-corevalidation-20261121.manifest.json
- Manifest SHA-256: 252b12b3a434f441bc7cf5cf16a7a70d6f5f256c347eff88b89f648124f78c06
- Official package verification: PASS; originalComparisonVerified.

### Unity EditMode and Smoke

- Official Unity EditMode: 3,325/3,325 passed; 0 failed, skipped, or inconclusive.
- EditMode runner ID: f1c39fcaee224bb4bd8447f0fb2eb97a
- Public package: Unity/coref-identity-spatial-editmode.zip
- Archive SHA-256: bc947425335495c0f7d26477ba1c12db17516b37c46fa1c6915f2c7f301ba7a2
- Official Smoke: 5/5 passed.
- Smoke run ID: 51b5a5d6c3514001ab8fa387f483ceea
- The Unity worktree contained pre-existing protected ProjectSettings and validation-document changes. They were preserved, excluded from this candidate commit, and were not altered as part of the implementation.

### Review and integrity

- Independent exact-tip implementation review: PASS.
- Independent public DotNetTrx artifact review: PASS.
- Unity and .NET evidence were bound to the candidate source identity; public packages contain no C:/Users path.
- git diff --check: PASS.

The private original runner manifests, XML reports, logs, runtime traces, and retention copies remain in owner-protected E: evidence storage. Public packages contain normalized reports only.