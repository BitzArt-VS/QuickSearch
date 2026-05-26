## Architecture

- `UI-Tweaks/ModSystems/`: Vintage Story `ModSystem` entry points. Use `ClientModSystem` or `ServerModSystem` from `UI-Tweaks/ModSystems/Base/ModSystem.cs` rather than extending Vintage Story's `ModSystem` directly.
- `UI-Tweaks/ModFeatures/`: feature logic outside mod systems. Extend `ModSystemFeature`, `ModSystemFeature<TModSystem>`, or `ModSystemFeature<TModSystem, TConfig>` from `UI-Tweaks/ModFeatures/Base/`.
- `UI-Tweaks/Services/`: stateful services used by features.
- `UI-Tweaks/ModConfig/`: user-facing configuration models, grouped by feature. Config classes use the `BitzArt.UI.Tweaks.Config` namespace.
- `UI-Tweaks/HarmonyPatches/`: Harmony transpilers, prefixes, and postfixes for game internals.
- `UI-Tweaks/HudElements/`: custom HUD elements rendered outside the dialog system.

## Coding Conventions & Code Quality

Writing good, readable code is a difficult and complex task — and it is a hard requirement for this codebase.

- **Descriptive names are mandatory.** Avoid abbreviations and single-letter names except trivial loop counters. Use full descriptive names such as `ClientApi`, not `Capi` or `capi`.
- **Use primary constructors** when the constructor only passes dependencies through simple assignment or forwarding. Avoid them when the constructor body does meaningful work.
- **Split namespaces** by meaning, not directory structure. A file path does not need to mirror the namespace.
- **Always use full curly brace blocks** for `if`, `for`, `foreach`, `while`, and similar control flow.
- **Order methods by call hierarchy:** callers above the methods they call, high-level entry points above implementation details.
- **Order same-file classes according to inheritance hierarchy:** derived classes above base classes.
- **Comments are a code smell:** add comments only when logic is genuinely complex or intent cannot be inferred from the code. Code must be self-describing.
- **Keep methods and classes short and focused:** treat classes above roughly 100-150 lines, long methods, deep nesting, and mixed responsibilities as refactoring signals.
- **Apply refactoring actively** — invert conditionals, extract methods, extract classes, use early returns, and prefer clearer responsibility boundaries.
