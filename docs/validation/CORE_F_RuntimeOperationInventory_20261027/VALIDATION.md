# CORE-F Runtime Operation Inventory — validation

This is a candidate implementation checkpoint, not Core-canonical promotion or CORE-F completion.

## Source identity

- Candidate base: 61395a1438e0bf760e600ae09779a83f12c8d626
- Base tree: afc5664a3a17b4041ddfc2d95a1c4260e31f9cee
- Core input digest: 7298022e711fcd7a4e04b5ff54f984c330eadb1c3ec786baded1a4feac3468eb
- Unity validation input digest: 77ad5873d2f885ce6e8648c6f59d62376449d97b97776b5758c47595a4db7dc5
The official CoreValidation production/Core test inputs remained unchanged from its run through Unity validation. Unity validation input digest was stable between the final full EditMode and filtered Smoke runs.
## Results

| Gate | Result | Run identity |
|---|---:|---|
| Official CoreValidation, cold | 886/886 PASS | 37b19dc71c9a4d2ea2a2a40526f7a207; private manifest SHA-256 EE43B5C53E792B684AF1C1105F8F8AAED23013B871BFE98CA6820E1D767FC5E5 |
| Official CoreValidation, warm | 886/886 PASS | Same run; cold TRX SHA-256 bd0ae098579377534bb863a45ce56d82f42cbed56a8a046b5be4abbe8391124b; warm TRX SHA-256 8c35346d5f5d7d038f82acef3462612ff430f2c885e65c7d2538bd6f2ddc6809 |
| Unity ALL EditMode | 3271/3271 PASS, zero skipped/inconclusive | cce027d7f1b04d62ac60e5770ee4a9f4; private manifest SHA-256 5BDBD4C93C8C04FFC8762220856AADCD54DF5BCB5792B5423775B5CBD4F050E8 |
| Official filtered Smoke | 5/5 PASS, zero skipped/inconclusive | 8255d8f26b394e22924f3dd71a91f6fa; private manifest SHA-256 6BB83A7E1EE130E3E6C42D679ACA7954BDC75CDA96D55D3C2E1760D72773CF |
| git diff --check | PASS | Warnings only concern line-ending normalization on preserved user-owned ProjectSettings changes. |

Unity was 6000.3.9f1; the evidence wrapper used PowerShell Core 7.6.5 and .NET 10.0.11.

The CoreValidation checkout had a Unity Library directory. This run validates the Core suite; it does not claim Library-free or detached-host independence.

## Durable Unity evidence

The full EditMode report was normalized and packaged by the approved evidence packager. Original-to-package verification passed.

- Manifest: Evidence/coref-op-inventory-editmode-20261027.manifest.json
- Result archive: Evidence/coref-op-inventory-editmode-20261027.zip
- Archive SHA-256: 87de2754cdf36d21a597e2bcc07641626ac58b16ea4a252da54a25280b1d2a25

The official filtered Smoke run also passed and its unmodified original XML/log and run manifest remain in protected local retention under the run identity above. The result packager rejected that filtered XML because Unity leaves parent-suite testcasecount attributes at full discovery counts (3271/3271), while the selected report contains five descendant cases. The runner's root result is coherent at 5/5. No XML was edited or relabeled, and no normalized Smoke package is claimed here. The complete ALL EditMode package contains the same five smoke test cases with their passing results.

CoreValidation TRX originals, runner manifest, logs, and runtime traces remain in protected local retention under its run identity above. They were not published or rewritten.

## Scope limits

This checkpoint moves the selected runtime operation inventory and active operation identity checks into DailyV1ExecutionBoundaryState; ContinuationCensusProtocol delegates in shared-runtime mode. Standalone census-protocol instances retain their local test/kernel inventory. No daily loop, action execution, owner, persistence, or application lifecycle authority is newly claimed. CORE-F remains incomplete and unpromoted.