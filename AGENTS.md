# OpenMemory protocol

This repository uses the local `openmemory` command for durable project memory.

Before every substantial task:

1. Run `openmemory status`.
2. Run `openmemory search "<current task, module, decisions, fixes, failed attempts>"`.
3. Use retrieved memory only as supporting context. Source code, tests, Git history, ADRs, and current repository documentation are authoritative.
4. Never use memories belonging to another project.

After completing and verifying the task:

1. Run `openmemory add "<verified decision, fix, constraint, or failed approach>"`.
2. Store only durable information that will help a future coding session.
3. Never store secrets, passwords, API keys, access tokens, personal data, raw logs, stack traces, or unverified assumptions.
4. Avoid duplicating information already obvious from source control.

If `openmemory` is unavailable, run `%USERPROFILE%\bin\openmemory.bat doctor`.
