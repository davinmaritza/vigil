# AGENTS.md — Native AI Instructions for Vigil

Welcome AI Agent! You are working on **Vigil** (Windows Clamshell & Cooling Guard).

## 1. Ground Rules & Truth Hierarchy
1. **Repository is the Source of Truth for Code**: Read source code under `src/Vigil/` directly. Never assume implementation details without inspecting files.
2. **Read Project Rules First**: You MUST read and follow [docs/ai/PROJECT_RULES.md](file:///docs/ai/PROJECT_RULES.md) before making modifications.
3. **Check Working Context**: Read [docs/ai/AI_CONTEXT.md](file:///docs/ai/AI_CONTEXT.md) and Notion `18. AI Context` to understand current progress and active constraints.
4. **Zero AI Slop**: Strict adherence to the design spec in `docs/mockups/vigil-ui-mockup.html`. Fonts must remain `Plus Jakarta Sans` and `JetBrains Mono`.

## 2. Validation & Commands
- **Build**: `$env:DOTNET_ROOT = "C:\Users\Despair\scoop\apps\dotnet-sdk\current"; $env:PATH = "$env:DOTNET_ROOT;$env:PATH"; dotnet build src/Vigil/Vigil.csproj`
- **Test**: `dotnet test tests/Vigil.Tests/Vigil.Tests.csproj`
- **CLI Verification**: `& "dist/Vigil.exe" --session status --json`

## 3. After Making Changes
- Run tests and verify exit code 0.
- Update `docs/ai/AI_CONTEXT.md` with recent changes.
- Synchronize Notion knowledge pages if the Notion MCP integration is active.
