# 00: Workstation prerequisites

**What to build:** A workstation that can build and run the platform, and a git repository that excludes the local reference material.

**Blocked by:** None (can start immediately)

**Status:** ready-for-human

**Requirements:** P-6, NFR-28

Found on the current machine (2026-11-11): git 2.52 present; .NET 9 runtime only, no SDK; Docker not installed; WSL default version 2.

- [x] .NET 10 SDK installed: `dotnet --list-sdks` shows `10.0.x`.
- [x] Docker Desktop installed with the WSL 2 backend; `docker info` succeeds (Aspire runs SQL Server, Kafka and tooling as containers).
- [x] Docker Desktop resources allow at least 10 GB of memory.
- [x] `docker run --rm hello-world` succeeds.
- [x] Aspire templates available: `dotnet new list aspire` shows the AppHost template (installed with the .NET 10 SDK; Aspire CLI optional).
- [x] Repository initialised with `git init`; `git status` does not list anything under `specs/prerequisites/`.
- [x] First commit contains the documentation set (README, AGENTS, GLOSSARY, docs, specs without prerequisites).
- [x] Optional: remote created and pushed.
