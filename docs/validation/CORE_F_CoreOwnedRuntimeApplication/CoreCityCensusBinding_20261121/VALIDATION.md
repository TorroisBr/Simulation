# Validation - Core-owned City census binding

- Reviewed code commit: dbe865527e94297b89a89c024a4c985d4dca0b29
- Reviewed code tree: 92fd685d76419353532da6593ee163fd04c4538f
- Base: 29e84bb6f6cc50def84629e8edd59e36ffac5833
- Core canonical: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea

| Gate | Run | Result | Evidence manifest SHA-256 |
| --- | --- | --- | --- |
| CoreValidation cold | db6633cae9d54856bce05fc68ecd8592 | 1,184/1,184 PASS | 5b11b0c3e4b9cc630245fcb5785103b4b370ea04eb2524a9c15ec2cdf711e003 |
| CoreValidation warm | db6633cae9d54856bce05fc68ecd8592 | 1,184/1,184 PASS | 5b11b0c3e4b9cc630245fcb5785103b4b370ea04eb2524a9c15ec2cdf711e003 |
| Unity EditMode | 068686008b7e46b09aedc8d9007de7d8 | 3,325/3,325 PASS; input and executable stable | 3c14e065b9690380edbc47771dae5173b676b79f5bc5f7e954a547dd327b3dcd |
| Official Smoke | b33621995e0449aa98a37f744f151ada | 5/5 PASS; input and executable stable | 6621be4894e65f4346bb8fa31adae008a895140ed4ebe3493247eebf6e718139 |
| git diff --check | N/A | PASS | N/A |

Private original reports and logs are preserved outside the repository. This record contains no machine-local filesystem paths.
