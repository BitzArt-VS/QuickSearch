# UI-Tweaks Localization

## Project Facts

- Mod ID: `bitzartuitweaks`
- Lang directory: `resources/assets/bitzartuitweaks/lang/`

## Language Files

All 19 language files must be updated together. Never edit only a subset.

| File | Language | File | Language |
| --- | --- | --- | --- |
| `be.json` | Belarusian | `nl.json` | Dutch |
| `cs.json` | Czech | `pl.json` | Polish |
| `de.json` | German | `pt.json` | Portuguese |
| `en.json` | English | `ro.json` | Romanian |
| `es.json` | Spanish | `ru.json` | Russian |
| `fr.json` | French | `sv.json` | Swedish |
| `hu.json` | Hungarian | `tr.json` | Turkish |
| `it.json` | Italian | `uk.json` | Ukrainian |
| `ja.json` | Japanese | `zh.json` | Chinese Simplified |
| `ko.json` | Korean | | |

## Key Naming

Keys are kebab-case and grouped by feature or section prefix:

| Pattern | Purpose | Example |
| --- | --- | --- |
| `ui-tweaks-config` | Config dialog title | |
| `config-page-*` | Config dialog page names | `config-page-hud` |
| `config-<feature>-*` | Per-feature config option labels | `config-quicksearch-enable` |
| `config-tooltip-*` | Shared tooltip widget config labels | `config-tooltip-padding` |
| `config-*` | Global config labels | `config-back` |
| `<feature>` | Top-level feature display name | `quicksearch` |

Tooltip descriptions use `<base-key>-tooltip`, such as `config-quicksearch-enable-tooltip`.

## Workflow

1. Read `resources/assets/bitzartuitweaks/lang/en.json` first to confirm the existing key set and choose the final English keys and values.
2. For each target language individually, think through what the string needs to communicate in that language and context. Consider multiple possible phrasings when useful, compare clarity, length, tone, and game-UI naturalness, then choose the best option.
3. Determine translations for every target language. Use concise, idiomatic, game-UI phrasing.
4. Update all 19 files directly in one pass. Preserve flat JSON objects and existing two-space indentation.
5. Verify that every new key exists in every language file.
6. Verify that no non-English language file received English fallback text.

Use `apply_patch` to edit language files directly. Do not create helper scripts just to update localization files. Use structured JSON parsing for validation when practical.

## Translation Quality

- Never copy English into a non-English file as fallback.
- Prefer idiomatic phrasing over literal translation.
- Treat each language as its own localization problem. Do not translate from a single intermediate phrasing mechanically across all files.
- Keep UI labels concise.
- Use formal register where customary, unless the language's gaming convention differs.
- Respect Vintage Story terminology for healthbar, satiety, temporal stability, and similar concepts.
- If confidence is low, make a best-effort translation and flag the uncertainty to the user.

## Slavic Tooltip Terminology

For tooltip-related strings, use the loanword for "tooltip" rather than the native word for "hint" or "tip":

| Language | Use | Avoid |
| --- | --- | --- |
| Russian (`ru`) | тултип, тултипа | подсказка |
| Ukrainian (`uk`) | тултіп, тултіпа | підказка |
| Belarusian (`be`) | тулціп, тулціпа | падказка |
| Polish (`pl`) | tooltip, tooltipa | podpowiedź |
| Czech (`cs`) | tooltip, tooltipu | nápověda, popisek |

Apply this to all `config-tooltip-*-tooltip` strings and any other text referencing the tooltip UI element.

## Constraints

- Do not add nested JSON objects.
- Do not skip any language file.
- Do not leave English fallback strings in non-English files.
