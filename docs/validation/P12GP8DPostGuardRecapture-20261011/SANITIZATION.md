# Sanitization record

This package contains sanitized XML reports and compressed Unity logs for the P12-G P8-D post-guard target-owner recapture test.

Only incidental local filesystem roots and the local account label embedded in Unity licensing IPC identifiers were replaced. The local account value is not recorded in the repository. Test source, production code, executable tree, test results, failure counts, assertions, source identities, and validation semantics were not changed.

| Replacement token | Occurrences |
| --- | ---: |
| SANITIZED_LOCAL_PROGRAMFILES_BS | 8 |
| SANITIZED_LOCAL_PROGRAMFILES_FS | 20 |
| SANITIZED_LOCAL_USERPROFILE_FS | 12 |
| SANITIZED_LOCAL_WORKTREE_BS | 288 |
| SANITIZED_LOCAL_WORKTREE_FS | 16 |
| SANITIZED_LOCAL_ACCOUNT in LicenseClient identifiers | 24 |

Each of the four logs had six local account labels. Reversing the account-label replacement in memory reproduced the prior path-sanitized log content and its recorded SHA-256 exactly; the original account value was not written to any repository file. Reversing the path replacements from that prior content reproduced the original pre-publication log bytes and matched the original SHA-256 in VALIDATION.md. The path-only hashes above describe an intermediate sanitation pass. The published package contains only the final account-redacted gzip artifacts listed in VALIDATION.md and MANIFEST.md.

For source XML, reversing path replacements reproduced the original pre-publication bytes and matched the original SHA-256 in VALIDATION.md. The published XML blobs use LF line endings in Git; their exact published blob hashes are recorded separately in VALIDATION.md and MANIFEST.md.

The final sanitized XML and decompressed logs were checked for absolute drive paths, UNC paths, the local account label in licensing identifiers, bearer-token patterns, private-key markers, and credential assignments. No such values remain. XML totals and test outcomes were checked against the original reports.

No test was rerun for artifact-only sanitation. The code and test-source tree remains the exact validated tree identified in VALIDATION.md.