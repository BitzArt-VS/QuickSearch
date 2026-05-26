# Vintage Story Reference

Use this reference when work requires checking local Vintage Story reference material, API behavior, vanilla content examples, or game-version-specific details.

When a sibling `../BaseGame` is present, follow the instructions in `../BaseGame/AGENTS.md` and use it as optional local reference material. If unavailable, rely on `resources/lib/` and public references.

While working with any files related to the `../BaseGame` reference, validate reference material against actual implementation details you encounter and propose reference updates to the user using the active workflow's required format when one applies, otherwise using the normal `Findings` format. This includes important specifics on implementation details, curious quirks, relevant examples, and anything else that may be helpful for future reference.

Maintain and update the reference proactively as new information is discovered. There is no separate dedicated maintainer role for this task.

## Checked Facts

- Vanilla `Vintagestory.API.Client.GuiDialog.DrawOrder` defaults to `0.1` in `../BaseGame/vsapi/Client/UI/Dialog/GuiDialog.cs`. `GuiManager.RequestFocus` only reorders dialogs within the same `DrawOrder` rank, so cross-rank stacking remains controlled by each dialog's `DrawOrder`.
- `ClientEventManager.TriggerRenderStage` iterates each render-stage renderer list directly by index in `../BaseGame/vintagestorylib/Vintagestory.Client.NoObf/ClientEventManager.cs`. Avoid unregistering a renderer from the same stage during its own `OnRenderFrame`; removing the current list item can shift later entries and skip a later renderer for that frame.
