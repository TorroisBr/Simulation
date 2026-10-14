# Windows GitHub authentication for this repository

Use Git Credential Manager (GCM) with its normal Windows Credential Manager
store. Do not switch to plaintext credential storage, disable authentication
checks, or delete saved credentials to address an account-selection prompt.

## Multiple GitHub accounts

When a user has more than one GitHub login saved, a generic HTTPS remote such
as `https://github.com/OWNER/REPOSITORY` may leave GCM without a selected
account identity for an authenticated operation. Ask the user which login to
use; do not infer it from the repository owner, Git author, or Windows account.
Then pin that confirmed username in the local config of this repository clone:

```powershell
$login = Read-Host 'Confirmed GitHub username'
git config --local --replace-all credential.https://github.com.username $login
```

The setting contains only a username, not a token. A repository's linked
worktrees share this common Git config, so the setting applies to them too. A
separate clone has a separate `.git/config` and needs its own confirmed setting.
To remove the pin later:

```powershell
git config --local --unset-all credential.https://github.com.username
```

Keep the configured GCM helper and secure Windows credential store. Do not use
`credential.helper store`, embed credentials in a remote URL, or copy a token
between worktrees.

## Safe diagnostics

Inspect configuration and remote shape without requesting credential values:

```powershell
git config --show-origin --get-all credential.helper
git config --show-origin --get-all credential.https://github.com.username
git remote -v
```

Treat all diagnostic output as private. Never run `git credential fill` for
display, paste token-bearing output, or print environment variables that may
contain secrets. `git ls-remote` is a read operation and does not prove that a
push credential is available.

To check that Git can perform a noninteractive push negotiation without
creating a remote ref, use a unique diagnostic ref with `--dry-run`:

```powershell
$env:GIT_TERMINAL_PROMPT = '0'
$env:GCM_INTERACTIVE = 'Never'
$suffix = [guid]::NewGuid().ToString('N')
$ref = "refs/heads/codex/auth-diagnostic-dry-run-$suffix"
git -c credential.interactive=false push --dry-run --porcelain origin `
  "HEAD:$ref"
```

This must not create the named ref. A successful dry-run confirms that the
negotiation completed without an account picker, but it may not prove write
authorization because server behavior can vary. Do not make a real test push
or delete a remote ref unless that operation is separately authorized.

If GCM still prompts, stop and use the normal sign-in flow with the user's
confirmed account. If credential enumeration or retrieval returns an access
denied error, do not bypass Windows security or replace/delete credentials;
report the specific error and ask for the smallest required Windows account or
Credential Manager access action.

## Verification in the current clone

On 2026-10-14, the repository-local username pin was checked from the main
checkout and a linked worktree. Noninteractive dry-run pushes to a unique,
nonexistent diagnostic ref completed without a picker. Then, after confirming
each local HEAD exactly matched its existing remote branch, normal no-op pushes
to those same refs completed without a picker. No diagnostic ref was created
and neither remote branch advanced. This verifies authenticated push behavior
for this Windows account and clone; a separate clone must be configured and
verified independently.
