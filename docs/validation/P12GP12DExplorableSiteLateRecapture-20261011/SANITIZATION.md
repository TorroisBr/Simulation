# Sanitization record

The four XML reports and four compressed Unity logs were sanitized by exact local-root substitutions and replacement of local account labels embedded in Unity licensing identifiers. Original account values are not retained. XML result attributes were compared before and after sanitation. Inverse token substitution reconstructs the raw XML/log bytes in memory. Git stores XML as text and normalizes CRLF line endings to LF; published XML hashes are recorded in `VALIDATION.md`. This line-ending normalization does not change XML content or test results. Compressed logs are binary and retain their exact sanitized bytes.

| Replacement token | Occurrences across XML and logs |
|---|---:|
| SANITIZED_LOCAL_ACCOUNT | 24 |
| SANITIZED_LOCAL_APPDATA_FS | 12 |
| SANITIZED_LOCAL_PROGRAMFILES_BS | 8 |
| SANITIZED_LOCAL_PROGRAMFILES_FS | 20 |
| SANITIZED_LOCAL_VALIDATION_TEMP_BS | 12 |
| SANITIZED_LOCAL_WORKTREE_BS | 276 |
| SANITIZED_LOCAL_WORKTREE_FS | 16 |

The sanitized artifacts preserve test names, result/count attributes, assertions, failure counts, source identities, and validation semantics. A final scan found no absolute drive paths, UNC paths, local account labels, bearer-token patterns, GitHub token patterns, or private-key markers. Unity was not rerun for artifact-only sanitation; the validated code and test source remain unchanged.
