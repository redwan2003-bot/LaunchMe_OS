# Antigravity Setup & 1st Initial Training/Assignment Prompt

## Quick Setup (Before Pasting the Prompt)
1. Create a new empty folder (e.g., `LaunchMeWin`), initialize Git (`git init`), and extract `launchme-win-antigravity-kit.zip` into the root so `.agents/`, `docs/`, and `AGENTS.md` sit at the repository root.
2. Make an initial commit (`git add . && git commit -m "chore: add Antigravity workspace kit"`). Git is required so Antigravity can isolate parallel subagents into separate worktrees (`branch`).
3. Open the folder in **Google Antigravity** (App, IDE, or CLI) in **Review-driven** or **Agent-assisted** mode so you approve the Phase 0 contract plan before parallel coding begins.
4. Confirm `.agents/` is discovered (in Antigravity CLI, run `/agents` to verify `core-engine-architect`, `win32-hooks-engineer`, `ui-shell-builder`, and `qa-verifier-auditor` are loaded).

---

## 1st Initial Prompt (Copy & Paste Verbatim Into Antigravity)

```text
/plan

You are the Lead Orchestrator for **LaunchMe-Win**, a native Windows 11 (.NET 9 + WinUI 3 + Win2D + CsWin32) workspace manager achieving full feature parity with LaunchMe macOS v3.01.0.

Before writing any code, thoroughly read and internalize every workspace specification and rule:
1. `AGENTS.md`
2. `docs/PRD_LAUNCHME_WINDOWS.md`
3. `docs/ARCHITECTURE_AND_CONTRACTS.md`
4. `docs/PARALLEL_WORKSTREAMS.md`
5. `.agents/rules/01-architecture-guardrails.md` and `.agents/rules/02-parallel-verification-gates.md`
6. `.agents/skills/winui3-parity-build/SKILL.md`

### Your Assignment & Execution Protocol
1. **Audit & Plan First (No Implementation Yet):**
   - Produce a detailed Implementation Plan artifact for **Phase 0 (Contract Freeze)** and **Phase 1 (Core Grid, App/File Indexer, Unified Search, Clipboard, Hotkeys & Hot Corners)**.
   - Explicitly list every C# interface and DTO you will freeze in `src/LaunchMe.Contracts/` and confirm how `core-engine-architect`, `win32-hooks-engineer`, and `ui-shell-builder` will execute Phase 1 in parallel without touching overlapping files.
   - Wait for my approval on the plan.

2. **After Plan Approval — Execute Phase 0 Sequentially:**
   - Scaffold `LaunchMeWin.sln`, `Directory.Build.props` (`net9.0-windows10.0.22621.0`, `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`), `src/LaunchMe.Contracts/`, and empty project shells + xUnit test projects for `Core`, `Platform.Win32`, and `UI`.
   - Run `dotnet build LaunchMeWin.sln -warnaserror` and verify it passes with 0 errors and 0 warnings, then commit Phase 0 to Git.

3. **Execute Phase 1 in Parallel Using Custom Subagents:**
   - Spawn all three specialized subagents concurrently in isolated Git worktrees (`branch`):
     - `core-engine-architect`: Implement `ISpaceStore` (SQLite baseline), `IAppIndexer` (Start Menu `.lnk`, MSIX, Extra Sources, 128px icon cache), `IFileSearchService` (opt-in folder scope), Inline Calculator, Emoji index, and Clipboard classifier + unit tests.
     - `win32-hooks-engineer`: Implement `IGestureAndTriggerService` (`RegisterHotKey` + `WH_MOUSE_LL` Hot Corners with 150ms dwell) and `IClipboardMonitor` (WinRT Clipboard listener) + unit tests.
     - `ui-shell-builder`: Implement the WinUI 3 Full-Screen Launcher overlay (`OverlappedPresenter`, `WS_EX_TOOLWINDOW`, cursor-monitor placement), Classic View + Free Layout Edit Mode, Folders + embedded Windows folders, and Unified Search bar with `TAB` Emoji switcher.

4. **Mandatory Verification Gate:**
   - Do NOT declare Phase 1 complete yourself. Once all three subagents finish and their branches are integrated, invoke `qa-verifier-auditor` to run `dotnet build -warnaserror`, `dotnet test`, zero-stub checks, ownership checks, and `IDisposable` hook audits.
   - If `qa-verifier-auditor` returns FAIL, route the exact errors back to the owning subagent and re-audit until it returns PASS.

Start now by reading all files above and presenting the Phase 0 + Phase 1 Implementation Plan.
```

---

## Follow-Up Orchestration Commands (For Later Phases)

- **Start Phase 2 (Theming, Liquid Glass, Pause-on-Hide Live Wallpapers & Core Widgets):**
  `Run /swarm using core-engine-architect, win32-hooks-engineer, and ui-shell-builder to implement Phase 2 from docs/PRD_LAUNCHME_WINDOWS.md, followed by a mandatory qa-verifier-auditor gate.`
- **Start Phase 3 (Spaces, Workflows, `.launchmetheme` Packaging & Clean Uninstall):**
  `Run /teamwork to deliver Phase 3 (Spaces scheduler, Workflows runner, .launchmetheme double-click installer, and Recycle-Bin Clean Uninstall) with qa-verifier-auditor sign-off.`
- **Start Phase 4 (Dynamic Notch, Multi-Display AppBar Dock, Full-Screen Music Player & HID Pinch Gestures):**
  `Dispatch all three worker subagents in parallel branches for Phase 4 (Notch HUD + Pocket/Share, SHAppBarMessage Multi-Display Dock + Dock Widgets, GSMTC Full-Screen Music Player, and Raw Input HID 3/4/5-finger pinch gestures), then run qa-verifier-auditor.`
- **Deep Bug Fix (If a tricky Win32 hook or GPU issue appears):**
  `Use /boost to diagnose and fix [describe exact bug] while keeping dotnet build -warnaserror and all tests green.`
