---
name: uitweaks-gui
description: 'Read this skill whenever the request involves any GUI work, such as working on the GUI framework, GUI components, rendering pipelines, dialog event dispatching, or implementing dialogs using the framework, except for dialogs that extend VanillaGuiDialog (legacy GuiComposer path).'
---

# UI-Tweaks GUI Framework

## Scope

**In scope:**
- Everything inside `src/UI-Tweaks/Gui/Framework/` — framework internals, components, renderers, services
- Dialogs that extend `GuiDialog` (the Cairo path) — e.g. `ModConfigDialog` in `src/UI-Tweaks/Gui/Dialogs/`
- Adding new `GuiComponent`/`GuiNode` subclasses, new extensions, new reference docs

**Out of scope — use uitweaks-code directly:**
- Dialogs that extend `VanillaGuiDialog` (the legacy `GuiComposer` path) — e.g. `QuickSearchGuiDialog`
- Code outside `Gui/` entirely (features, services, configs, Harmony patches)

**Out of scope — use uitweaks-localization skill:**
- Any changes to `resources/assets/bitzartuitweaks/lang/` files

## Design Principles

- **Blazor-without-XML** — component trees declared in C# via `BuildRenderTree(IGuiRenderTreeBuilder)`, `RenderFragment` as first-class subtree declarations, CSS-inspired styling.
- **Performance is a first-class constraint.** This runs on the render thread. Minimize allocations, prefer `struct` over `class`, reuse buffers, avoid boxing. Faster-but-less-readable code is acceptable on proven hot paths.

## Framework Knowledge Base

Read the relevant reference file **before** working in each area. All files are in `./references/`:

| File | Covers |
|------|--------|
| [01.overview.md](./references/01.overview.md) | Concept glossary, file map |
| [02.reconciliation.md](./references/02.reconciliation.md) | Blueprint/diff/patch, lifecycle order, scoped rebuilds |
| [03.rendering-pipeline.md](./references/03.rendering-pipeline.md) | Frame loop, Cairo surface, bounds propagation, dialog bootstrap |
| [04.component-model.md](./references/04.component-model.md) | `IGuiComponent` contract, configuration persistence, public API surface |
| [05.layout-parameters.md](./references/05.layout-parameters.md) | `GuiComponentLayoutParameters`, `GuiThickness`, `GuiComponentBounds`, upcoming layout pass |
| [06.tooltips.md](./references/06.tooltips.md) | Floating tooltip layer: `GuiTooltip`, `GuiTooltipBackground`, `TooltipHost`, `TooltipRenderer` |
| [07.mouse-events.md](./references/07.mouse-events.md) | Mouse input routing and event handling |
| [08.keyboard-events.md](./references/08.keyboard-events.md) | Keyboard input, focus model (`FocusManager`), caret blink, slot-level `OnKey*` handlers vs virtual hooks, Escape handling |

## Skill Self-Maintenance

When changes to the project affect this skill's scope, **update this SKILL.md** to keep it accurate. Specifically:

- If a new type is added anywhere in `Gui/Framework/`, add it to the file map in `./references/01.overview.md`.
- If a new reference doc is added to `./references/`, add it to the **Framework Knowledge Base** table.
- If in-scope or out-of-scope files change (new legacy dialogs added, new framework subdirs), update the **Scope** section and the `description` frontmatter.
- If design principles or code quality standards change, update the relevant sections.
