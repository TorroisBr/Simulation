# P12-G owner-vector row-fact sanitization

The package is generated from the retained raw validation outputs. Only exact literal local-root prefixes are substituted; no generic path or URL rule is used. Roots are the active candidate worktree, the Unity installation, the user-local source root, and the Unity build-output root. Each source slash spelling has its own stable marker and is exactly reversible.

| Exact root class and source separators | Marker | Replacements |
|---|---|---:|
| Unity build-output root (BS) | `SANITIZED_LOCAL_BUILD_BS` | 1 |
| Unity installation root (BS) | `SANITIZED_LOCAL_UNITY_BS` | 9 |
| Unity installation root (FS) | `SANITIZED_LOCAL_UNITY_FS` | 15 |
| user-local root (FS) | `SANITIZED_LOCAL_USER_FS` | 9 |
| active candidate worktree root (BS) | `SANITIZED_LOCAL_WORKTREE_BS` | 240 |
| active candidate worktree root (FS) | `SANITIZED_LOCAL_WORKTREE_FS` | 14 |

Reversing the markers restores each raw report byte-for-byte. XML suite totals, element order, and test-case name/result attributes are unchanged. The local roots and raw report contents are not embedded in this package. URLs and diagnostic suffixes are preserved.
