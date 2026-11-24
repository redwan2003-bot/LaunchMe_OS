# Parallel Subagent Ownership & Merge Protocol

To prevent file conflicts when running Antigravity subagents in parallel (`invoke_subagent` with isolated Git worktrees or `/swarm`), every agent has **exclusive write ownership** over its assigned directories.

| Agent | Exclusive Write Scope | Read-Only Dependencies | Verification Command |
| :--- | :--- | :--- | :--- |
| **Parent Orchestrator** | `LaunchMeWin.sln`, `Directory.Build.props`, `src/LaunchMe.Contracts/**` | Entire repo | `dotnet build src/LaunchMe.Contracts` |
| **`core-engine-architect`** | `src/LaunchMe.Core/**`, `tests/LaunchMe.Core.Tests/**` | `src/LaunchMe.Contracts/**`, `docs/**` | `dotnet test tests/LaunchMe.Core.Tests` |
| **`win32-hooks-engineer`** | `src/LaunchMe.Platform.Win32/**`, `tests/LaunchMe.Platform.Tests/**` | `src/LaunchMe.Contracts/**`, `docs/**` | `dotnet test tests/LaunchMe.Platform.Tests` |
| **`ui-shell-builder`** | `src/LaunchMe.UI/**`, `tests/LaunchMe.UI.Tests/**` | `src/LaunchMe.Contracts/**`, `docs/**` | `dotnet build src/LaunchMe.UI` |
| **`qa-verifier-auditor`** | **None (Read & Test Only)** | Entire repo | `dotnet build -warnaserror && dotnet test` |

## Contract Change Rule
If any subagent discovers a missing method or property in `src/LaunchMe.Contracts/`, it MUST **stop and report the exact C# signature proposal to the Parent Orchestrator** rather than editing `src/LaunchMe.Contracts/` directly. The Orchestrator updates the contract once and broadcasts to all agents.
