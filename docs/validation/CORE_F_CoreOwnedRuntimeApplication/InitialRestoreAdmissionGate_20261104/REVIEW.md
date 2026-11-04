# Independent exact-tip review — initial restore publication gate

**Verdict:** `PASS` for the source/test diff at the exact code candidate. This does not pass the candidate's Unity validation gate.

| Identity | Value |
| --- | --- |
| Reviewed candidate | `c906c2eb7983b89a0f838236fdaa0d5a0e8b46b7` |
| Reviewed tree | `642e7dd221a2cb83a947c247d0f13a6195b8f59f` |
| Base | `d68940ebf7f2f3eb2679cb5ef5b7d83bfa4a6f01` |
| Core canonical | `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea` |
| Reviewer | Independent agent `/root/action_execution_core_review` |

The reviewer verified the full three-file source/test diff and confirmed that all tokenless initial restore routes reject publication. The dedicated initial-restore API checks the same application's exact admission and image before publication. Replacement with a non-null current session remains supported and is covered by tests. The Unity test sends a foreign application's token directly to the publication gate. No actionable source/test findings remain.

The reviewer did not inspect the private CoreValidation reports or Unity log. CoreValidation is separately recorded as passing in `VALIDATION.md`; the focused Unity run produced no XML and remains blocked. This source/test review is not an overall validated-candidate verdict, does not complete CORE-F, and does not promote Core canonical.
