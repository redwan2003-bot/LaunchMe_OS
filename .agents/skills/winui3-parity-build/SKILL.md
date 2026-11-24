---
name: winui3-parity-build
description: Orchestrates phased parallel builds of LaunchMe-Win using frozen Contracts, isolated subagent worktrees, and mandatory QA Auditor verification gates.
---
# WinUI 3 Parallel Parity Build Runbook

Use this skill whenever starting or advancing a build phase of **LaunchMe-Win**.

## Step 1 — Phase 0 Contract Freeze (Orchestrator Only)
1. Read `AGENTS.md`, `docs/PRD_LAUNCHME_WINDOWS.md`, and `docs/ARCHITECTURE_AND_CONTRACTS.md`.
2. Create `LaunchMeWin.sln`, `Directory.Build.props` (enabling `net9.0-windows10.0.22621.0`, `Nullable=enable`, `TreatWarningsAsErrors=true`), and `src/LaunchMe.Contracts/` containing all 9 frozen interfaces and `.launchmetheme` DTOs.
3. Run `dotnet build src/LaunchMe.Contracts` and confirm 0 warnings / 0 errors.

## Step 2 — Parallel Subagent Dispatch (Per Phase)
Dispatch up to 3 worker subagents concurrently in isolated Git worktrees (`branch`):
- `core-engine-architect` → `src/LaunchMe.Core/` + `tests/LaunchMe.Core.Tests/`
- `win32-hooks-engineer` → `src/LaunchMe.Platform.Win32/` + `tests/LaunchMe.Platform.Tests/`
- `ui-shell-builder` → `src/LaunchMe.UI/` + `tests/LaunchMe.UI.Tests/`

## Step 3 — Independent Verification & Merge
1. After all 3 workers return, invoke `qa-verifier-auditor` on the integrated branch.
2. If **FAIL**, route each defect back to its owning subagent with exact file/line references and re-audit.
3. Only when `qa-verifier-auditor` returns **PASS** (0 warnings, 100% tests green, 0 stubs), commit the phase milestone and proceed to the next phase.
