# LaunchMe-Win — Master Agent Constitution

You are building **LaunchMe-Win**, a native Windows 11 workspace manager achieving full feature parity with **LaunchMe v3.01.0 for macOS** (Full-Screen Launcher, Dynamic Notch, Multi-Display Dock, Full-Screen Music Player, 11 Widget Families, Unified Search, Workflows, and portable `.launchmetheme` Spaces).

## Tech Stack (Frozen)
- **Language & Runtime:** C# 13 / .NET 9 (`net9.0-windows10.0.22621.0`)
- **UI Framework:** WinUI 3 (Windows App SDK 1.6+) + Win2D (`Microsoft.Graphics.Win2D`)
- **Native Interop:** CsWin32 (`Microsoft.Windows.CsWin32`) + WinRT (`GlobalSystemMediaTransportControls`, `Clipboard`, `DataTransferManager`)
- **Local Storage:** SQLite (`Microsoft.Data.Sqlite`) + `System.Text.Json` (strictly local-first, zero cloud telemetry)
- **Testing:** xUnit + Moq / FluentAssertions

## Mandatory Execution Order
1. **Phase 0 (Orchestrator Only — Sequential):** Scaffold the `.sln`, `src/LaunchMe.Contracts/` (frozen C# interfaces & DTOs), and verify `dotnet build` passes before spawning any parallel subagents.
2. **Parallel Phases (1–4):** Spawn custom subagents (`core-engine-architect`, `win32-hooks-engineer`, `ui-shell-builder`) in **isolated Git worktrees (`branch`)** strictly respecting the file-ownership boundaries in `docs/PARALLEL_WORKSTREAMS.md`.
3. **Mandatory Verification Gate:** After every subagent finishes, invoke `qa-verifier-auditor` to run `dotnet build`, `dotnet test`, and the architectural checklist before merging any worktree. Never mark a phase complete without a passing audit report.

## Reference Documents (Read Before Coding)
- `docs/PRD_LAUNCHME_WINDOWS.md` — Complete LaunchMe v3.01.0 feature specification
- `docs/ARCHITECTURE_AND_CONTRACTS.md` — Solution layout, shared interfaces, and `.launchmetheme` JSON schema
- `docs/PARALLEL_WORKSTREAMS.md` — Strict directory ownership map for zero-conflict parallel execution
