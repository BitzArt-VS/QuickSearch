## Project Facts

- Mod name: UI Tweaks
- Mod ID: `bitzartuitweaks`
- Namespace: `BitzArt.UI.Tweaks`
- Stack: C# 14, .NET 10, VintagestoryAPI, Harmony, Newtonsoft.Json, cairo-sharp, xUnit v3
- Target: Vintage Story v1.22.0+
- Actual game DLLs: `resources/lib/`
- Tests: `tests/UI-Tweaks.Tests/` with xUnit v3.

## Common Tasks

Build:

```powershell
dotnet build src/UI-Tweaks/UI-Tweaks.csproj
```

Test:

```powershell
dotnet test tests/UI-Tweaks.Tests/UI-Tweaks.Tests.csproj
```
