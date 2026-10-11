# P12-G post-graph P8-C Person position sanitization

The package is generated from the retained raw Unity outputs. Sanitization replaces only exact local-root literals; it does not rewrite test names, result attributes, assertions, counts, diagnostics, or URLs.
The active worktree, Windows user/runtime roots, and installed-program roots are represented by stable markers. Marker reversal restored every raw XML and log byte-for-byte before packaging. The raw results remain in the ignored local Logs directory and are not included in the candidate.

| Local root class and source separators | Marker | Replacements |
|---|---|---:|
| PROGRAMFILES (BS) | `SANITIZED_LOCAL_PROGRAMFILES_BS` | 8 |
| PROGRAMFILES (FS) | `SANITIZED_LOCAL_PROGRAMFILES_FS` | 20 |
| USERPROFILE (FS) | `SANITIZED_LOCAL_USERPROFILE_FS` | 12 |
| WORKTREE (BS) | `SANITIZED_LOCAL_WORKTREE_BS` | 288 |
| WORKTREE (FS) | `SANITIZED_LOCAL_WORKTREE_FS` | 16 |

The raw and sanitized XML parse to identical result/count/test-case metadata. No absolute drive-root or UNC path remains in any published XML or compressed log.
