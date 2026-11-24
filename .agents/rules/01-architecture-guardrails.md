---
description: Core architectural guardrails for LaunchMe-Win (WinUI 3, .NET 9, Pause-on-Hide GPU, zero placeholders)
---
# LaunchMe-Win Architecture Guardrails

1. **Contracts First:** Never modify `src/LaunchMe.Contracts/` from a worker subagent. Code strictly against the frozen interfaces.
2. **Pause-on-Hide GPU Law:** Every video wallpaper (`MediaPlayerElement` / MediaFoundation), GIF loop, and Win2D particle/shader timer MUST subscribe to window visibility state and immediately pause/suspend rendering when the Launcher window is hidden.
3. **Zero Placeholders or Stubs:** Never leave `throw new NotImplementedException()`, `// TODO`, or fake mock data in production projects (`src/**`). Mocks are allowed ONLY inside `tests/**`.
4. **Native Win32 Safety:** All P/Invoke calls must use `CsWin32` generated bindings or safe `SafeHandle` wrappers, check return codes/Win32 errors, and clean up hooks (`UnhookWindowsHookEx`, `SHAppBarMessage(ABM_REMOVE)`, `UnregisterHotKey`) in `IDisposable.Dispose()`.
5. **Privacy-First File Search:** Never scan arbitrary user directories without explicit folder scope approval persisted in local SQLite settings.
6. **Clean Compiler Output:** Every project must compile cleanly under `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` with nullable reference types enabled (`<Nullable>enable</Nullable>`).
