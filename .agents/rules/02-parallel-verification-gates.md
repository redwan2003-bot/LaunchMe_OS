---
description: Mandatory verification gates before any subagent completes or merges a parallel workstream
---
# Parallel Execution & Verification Gates

1. **Plan Before Code:** Before writing code for any phase, produce an Implementation Plan artifact listing exact files to create, interfaces implemented, and unit tests to add.
2. **Isolated Worktrees:** Always run `core-engine-architect`, `win32-hooks-engineer`, and `ui-shell-builder` in parallel using isolated worktrees (`branch`) or disjoint directory scopes defined in `docs/PARALLEL_WORKSTREAMS.md`.
3. **Self-Verification Before Handoff:** No worker subagent may report "done" until it has executed `dotnet build` and `dotnet test` for its own project and included exact pass/fail output in its final summary.
4. **Independent Auditor Gate:** Before merging any parallel branch into `main`, invoke `qa-verifier-auditor` to verify:
   - Zero compiler warnings or errors across the full solution.
   - All unit tests pass.
   - Zero boundary violations (subagent did not touch files outside its ownership).
   - Zero `NotImplementedException` or `TODO` stubs in `src/`.
   - GPU pause-on-hide and `IDisposable` hook cleanup are present.
5. **Systematic Debugging:** If a build or test fails, capture the exact compiler/stack error, identify the root cause before editing, and fix the underlying defect rather than suppressing warnings or disabling tests.
