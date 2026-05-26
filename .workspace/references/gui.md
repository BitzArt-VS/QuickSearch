# UI-Tweaks GUI Reference Map

## Scope

In scope:

- Everything inside `src/UI-Tweaks/Gui/Framework/`: framework internals, components, renderers, services.
- Dialogs that extend `GuiDialog`, such as `ModConfigDialog` in `src/UI-Tweaks/Gui/Dialogs/`.
- New `GuiComponent` or `GuiNode` subclasses, extensions, and GUI framework reference docs.

Out of scope:

- Dialogs that extend `VanillaGuiDialog`, such as `QuickSearchGuiDialog`; use the general project instructions.
- Code outside `Gui/`, such as features, services, config models, and Harmony patches.
- Localization files; use [localization.md](localization.md).

## Design Principles

- Blazor-without-XML: component trees are declared in C# through `BuildRenderTree(IGuiRenderTreeBuilder)`, `RenderFragment` subtree declarations, and CSS-inspired styling.
- Performance is a first-class constraint because this runs on the render thread. Minimize allocations, prefer structs where appropriate, reuse buffers, and avoid boxing. Faster but less readable code is acceptable only on proven hot paths.

## Detailed References

Read the relevant reference file before working in each area. Links are relative to this file:

| File | Covers |
| --- | --- |
| [gui/01.overview.md](gui/01.overview.md) | Concept glossary and file map |
| [gui/02.reconciliation.md](gui/02.reconciliation.md) | Blueprint/diff/patch, lifecycle order, scoped rebuilds |
| [gui/03.rendering-pipeline.md](gui/03.rendering-pipeline.md) | Frame loop, Cairo surface, bounds propagation, dialog bootstrap |
| [gui/04.component-model.md](gui/04.component-model.md) | `IGuiComponent` contract, configuration persistence, public API surface |
| [gui/05.layout-parameters.md](gui/05.layout-parameters.md) | `GuiComponentLayoutParameters`, `GuiThickness`, `GuiComponentBounds`, layout pass |
| [gui/06.tooltips.md](gui/06.tooltips.md) | Floating tooltip layer: `GuiTooltip`, `GuiTooltipBackground`, `TooltipHost`, `FloatingLayerRenderer` |
| [gui/07.mouse-events.md](gui/07.mouse-events.md) | Mouse input routing and event handling |
| [gui/08.keyboard-events.md](gui/08.keyboard-events.md) | Keyboard input, focus model, caret blink, slot-level handlers, virtual hooks, Escape handling |
