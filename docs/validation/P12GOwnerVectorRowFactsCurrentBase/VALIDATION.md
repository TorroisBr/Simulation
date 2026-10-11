# P12-G owner-vector row-fact validation

- Focused suite: P12GDailyV1OwnerVectorTests, 15/15 passed.
- ALL EditMode: 2873/2873 passed.
- Official Smoke: 5/5 passed.
- These reports are retained outputs; this packaging correction did not rerun Unity.
- Sanitization substituted only exact local-root literals; all raw reports reverse-substitute byte-for-byte, URL counts match, and no absolute drive path remains.
- Manifest SHA-256 values and byte counts use exact Git blob bytes. Text XML and Markdown are LF-normalized in Git; raw-source hashes below use the retained raw files.

| Run | XML totals | Test cases | Original XML SHA-256 (raw source) | Sanitized XML SHA-256 (Git blob) | Original log SHA-256 (raw source) | Sanitized log content SHA-256 | Sanitized gzip SHA-256 (Git blob) |
| focused | 15/15, failed 0, skipped 0, inconclusive 0, assertions 0 | 15 | e0e858b4db14acdce3ebdd59d3343566df36d73f73f95072cde1d17c29fddfcb | b5556a38a21d9c4afb985eacd50d6c86e0dfe99cc2438d35cd775aaa853a6119 | d2d83f26a789c34057c06bf1fe18ebd09431c5b4572bb51a00077b5d84538c6d | 67c94c6e3ebeaedb506cfd78aff88e5425a59288b76be66ac81f58ed05c14b4e | 46b921acd40db18cbb3e9cbad9a25a90b1c41aa3722c7fc0c90fed30610b82c1 |
| editmode-all | 2873/2873, failed 0, skipped 0, inconclusive 0, assertions 0 | 2873 | f98f2272953ab6ae733901cd238a9dc36aae8c5a7227cd4ffdc7cd79074d25c8 | 7be03a917e603696eb500a571222e9a50c82cd28fe103639823b46e733153139 | 2a075af09a1d73d8ed90325fe6b17b55ba1fdbb1380d7465d9d2b7060c005c33 | 27457a1be9777ee16f6d7f23f40c796cf5e13ea37fd9805a7a472da741833c4c | cddebb5141710fd451b845410a612d5e94c20dc108b15c55d7c086ddf9bdc7bd |
| official-smoke | 5/5, failed 0, skipped 0, inconclusive 0, assertions 0 | 5 | e8a07b1f91d070e5c26c5fcfc21d7bec484650799ce15631e9712f8977371b62 | bb93f1d52ba9e25d41786401a8f52af0ec5a1116194d64414b085dd2c7b3fa95 | 0cdf8ea3950d1c1b99e42b2cda049499aa181ec743236d81d73e9d2da556a331 | d027572fbbe6251b8e973774869788cc5757b4a903362ee9cfc2b8cd5e278bab | 448c4e9bdb1be1e10fdf103c6c683621c7a1fe4612c9bc2028d40b87863b5d6d |
