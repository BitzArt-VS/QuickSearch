# UI-Tweaks Documentation

Use this reference for documentation, player-facing copy, `README.md`, `mod-description.html`, and mdBook work.

## Subagent Use

When the user explicitly asks for a docs subagent, docs agent, or delegated docs work, use the project-scoped `uitweaks_docs` custom agent from `.codex/agents/uitweaks-docs.toml`. Tell the subagent to read [this docs reference](docs.md) before acting.

Good subagent responsibilities:

- Verify feature behavior, config names, and UI labels against source.
- Review `mod-description.html` against docs changes.
- Draft or edit one documentation surface while the main agent works on another.
- Perform a final docs accuracy pass before delivery.

Keep small typo fixes local only when a subagent would add no useful review.

## Documentation Facts

- Docs site: built with mdBook from `docs/src/`, published to `https://bitzart-vs.github.io/UI-Tweaks`
- Mod portal description: `mod-description.html`, rendered directly on the Vintage Story mod portal
- README: `README.md`
- Key links: docs site, GitHub `https://github.com/BitzArt-VS/UI-Tweaks`, Discord `https://discord.gg/eZUFCVWWtK`

## Documentation Structure

```text
docs/
  book.toml
  src/
    SUMMARY.md
    01.overview.md
    02.features.md
    03.configuration.md
    04.contributing.md
    01.overview/
    02.features/
  assets/
    carousel.css
    carousel.js
    tabs.css
    tabs.js
  extras/
    auxiliary source and preview assets for documentation imagery
mod-description.html
README.md
```

Always update `SUMMARY.md` when adding or removing mdBook pages.

## Current Features

Keep docs accurate to the source:

- Environment Widget: HUD overlay showing in-game date/time, temperature, and coordinates; configurable position, size, format, and background opacity.
- Status Tooltips: numeric overlays on health, satiety, hunger rate, and temporal stability bars; each independently configurable.
- Custom Tooltips: user-defined additional tooltip overlays via config.
- Quick Search: in-game item/block search UI.
- Calculator: arithmetic expression evaluation built into Quick Search.
- Zoom: disabled-by-default hold-to-zoom camera hotkey, default `Z` when enabled. Enabling requires a game restart.
- Minor Tweaks: small quality-of-life corrections to vanilla behavior, such as Calendar Year starting at 1 instead of 0.
- In-Game Config Dialog: GUI dialog for configuring mod settings without editing JSON.

## mod-description.html

- Keep it a self-contained HTML snippet without `<html>` or `<body>` tags.
- Use inline styles only; no external CSS classes except Vintage Story portal spoiler classes.
- Use this font stack: `Inter, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif`.
- Use text color `#333`, links/accent `#3d6594`, highlight background `hsl(42 100% 97%)`, highlight left border `4px solid hsl(45 51% 54%)`, section header background `hsl(42 60% 93%)`, and dividers `#ddd` or `hsl(42 60% 85%)`.
- Feature sections use collapsible spoiler `div` elements with `spoiler`, `spoiler-toggle`, and `spoiler-text`.
- Links open with `target="_blank" rel="noopener"`.
- The file starts with an `<hr>` separator and a top-level links bar.

## mdBook And Markdown

- Page filenames follow `NN.slug.md`, such as `03.compatibility.md`.
- Image directories match the page prefix, such as `01.overview/` for `01.overview.md`.
- The screenshot carousel in `01.overview.md` uses raw HTML `<div class="carousel">` blocks.
- Single-image showcases use `<div class="image-showcase">` blocks.
- `SUMMARY.md` uses mdBook's `# Title` plus `- [Page](file.md)` list format.
- Use mdBook-supported Markdown or raw HTML. Do not introduce unsupported syntax.

## Writing Style

- Audience: Vintage Story players, not developers.
- Tone: friendly, concise, practical, and playful where the surface allows it.
- Use simple language that a non-native English speaker can understand.
- For UI Tweaks player docs, especially mod portal copy, jokes, emojis, light memes, and community-flavored asides are acceptable and often desirable when they fit the feature.
- Write in natural language. Avoid common AI-polished phrases such as "at a glance", "seamless", "designed to", "robust", "transparency", "practical improvements", "more than just", "not just", and similar generic marketing filler.
- Avoid negative parallelism ("it is not X, it is Y"), gerund fragments, and overly dramatic rhetorical questions. Use direct, positive statements.
- Do not use em dashes in docs. Use commas, colons, parentheses, semicolons, or shorter sentences instead.
- Lead with what the player gains, not internal implementation details.
- Avoid C# code, mod architecture, and internal type names in player-facing docs.
- Verify feature names, config option names, and UI labels against `src/UI-Tweaks/ModConfig/` and `resources/assets/bitzartuitweaks/lang/en.json`.
- When documenting a config option, include what it does, valid values or range, and default value when known.
- Do not invent behavior. Read the source when unsure.

## Synchronization Rules

- Keep compatibility tables in `docs/src/01.overview.md` and `mod-description.html` in sync.
- `mod-description.html` mirrors key docs site content: features, compatibility, installation, and links. Review it whenever those change in docs.
- Do not edit `SUMMARY.md` without creating or verifying the referenced files.
