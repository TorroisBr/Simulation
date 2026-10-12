# Sanitization record

The four XML reports and four compressed Unity logs were sanitized by replacing incidental local filesystem roots and redacting Unity licensing IPC account labels. The original account value is not retained. Test source, production code, test results, assertions, failure counts, source identities, executable tree identities, and validation semantics were unchanged.

| Replacement token | Occurrences |
|---|---:|
| SANITIZED_LOCAL_ACCOUNT | 24 |
| SANITIZED_LOCAL_PROGRAMFILES_BS | 8 |
| SANITIZED_LOCAL_PROGRAMFILES_FS | 20 |
| SANITIZED_LOCAL_USERPROFILE_FS | 12 |
| SANITIZED_LOCAL_VALIDATION_TEMP_BS | 12 |
| SANITIZED_LOCAL_WORKTREE_BS | 276 |
| SANITIZED_LOCAL_WORKTREE_FS | 16 |

The four original XML and log SHA-256 values and the sanitized XML/decompressed-log hashes are recorded in `VALIDATION.md`. The published XML blob hashes, compressed-log hashes, Git blob IDs, and byte counts are recorded in `MANIFEST.md`. The final XML/log package was scanned for absolute drive paths, UNC paths, local account labels, private identity strings, bearer-token patterns, and private-key markers; no findings remain. The focused test reports and semantic counts were checked before and after sanitation. Unity was not rerun for artifact-only sanitation.
