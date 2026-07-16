# OpenMemory protocol

This repository uses the local `openmemory` command for durable project memory.

Before every substantial task:

1. Run:
   `openmemory status`
2. Search for relevant architecture decisions, previous fixes, constraints, known issues, and failed attempts:
   `openmemory search "<current task, module, decisions, fixes, failed attempts>"`
3. Treat source code, tests, Git history, ADRs, and current documentation as more authoritative than retrieved memory.
4. Do not mix information from another project.

After a task is completed and verified:

1. Save only durable information:
   `openmemory add "<verified decision, fix, constraint, or failed approach>"`
2. Do not save secrets, passwords, tokens, personal data, temporary logs, stack traces, or guesses.
3. Do not save routine progress that is already obvious from Git.
4. When a previous memory is obsolete, clearly save the replacement decision and mention what it supersedes.

If the `openmemory` command is unavailable, run:
`%USERPROFILE%\bin\openmemory.bat doctor`
