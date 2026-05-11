---
name: uitweaks-localization
description: 'Add, edit, or review translation keys in UI-Tweaks lang files. Use when introducing new user-facing strings (config labels, tooltips, feature names), renaming keys, or auditing translation coverage across supported languages. If the user mentions a new string that needs localization, or asks to produce one, or GUI work requires modifying localization strings, use this skill.'
---

## Project Facts

- **Mod ID:** `bitzartuitweaks`
- **Lang directory:** `resources/assets/bitzartuitweaks/lang/`

## The 19 Language Files

All files **must always be updated together**. Never edit a subset.

| File | Language | File | Language |
|------|----------|------|----------|
| `be.json` | Belarusian | `nl.json` | Dutch |
| `cs.json` | Czech | `pl.json` | Polish |
| `de.json` | German | `pt.json` | Portuguese |
| `en.json` | English | `ro.json` | Romanian |
| `es.json` | Spanish | `ru.json` | Russian |
| `fr.json` | French | `sv.json` | Swedish |
| `hu.json` | Hungarian | `tr.json` | Turkish |
| `it.json` | Italian | `uk.json` | Ukrainian |
| `ja.json` | Japanese | `zh.json` | Chinese (Simplified) |
| `ko.json` | Korean | | |

## Key Naming Conventions

Keys are **kebab-case** and grouped by feature/section prefix:

| Pattern | Purpose | Example |
|---------|---------|---------|
| `ui-tweaks-config` | Config dialog title (standalone, not a prefix) | — |
| `config-page-*` | Names of config dialog pages | `config-page-hud` |
| `config-<feature>-*` | Per-feature config option labels | `config-quicksearch-enable` |
| `config-tooltip-*` | Shared tooltip widget config labels | `config-tooltip-padding` |
| `config-*` | Global config labels | `config-back`, `config-requires-restart` |
| `<feature>` | Top-level feature display name | `quicksearch` |

**Tooltip pattern:** the description for any base key is `<base-key>-tooltip` (e.g. `config-quicksearch-enable` → `config-quicksearch-enable-tooltip`).

## Workflow

1. **Read `en.json`** first to confirm the existing key set and finalize the new keys/values.
2. **Determine translations** for every target language. Produce idiomatic, natural phrasing appropriate for a game UI — concise, player-facing, never word-for-word.
3. **Update all 19 files in one pass.** Use `multi_replace_string_in_file` to append new keys efficiently. Match existing 2-space indentation.
4. **Verify** that each new key exists in every lang file and that no English string was left as a non-English fallback.

## Translation Quality Standards

- **Never copy English into a non-English file** — always provide a real translation.
- **Idiomatic over literal.** Favor natural phrasing per language.
- **Concise.** These are UI labels; they must fit buttons and short fields.
- **Formal register where customary** — e.g. formal "you" (`Sie` / `vous`) unless the language's gaming convention differs.
- **Respect game terminology.** Vintage Story has specific terms (healthbar, satiety, temporal stability) — keep them recognizable per language (transliterate or translate as the local gaming community does).
- If confidence is low for a translation, make a best-effort attempt and flag the uncertainty to the user. Do **not** fall back to English.

## Slavic Tooltip Terminology

For **tooltip-related strings**, use the loanword for "tooltip" rather than the native word for "hint" / "tip":

| Language | Use | Avoid |
|----------|-----|-------|
| Russian (`ru`) | тултип, тултипа (gen.) | подсказка |
| Ukrainian (`uk`) | тултіп, тултіпа (gen.) | підказка |
| Belarusian (`be`) | тулціп, тулціпа (gen.) | падказка |
| Polish (`pl`) | tooltip, tooltipa (gen.) | podpowiedź |
| Czech (`cs`) | tooltip, tooltipu (gen.) | nápověda, popisek |

Applies to all `config-tooltip-*-tooltip` strings and any other text referencing the tooltip UI element.

## Skill Self-Maintenance

When changes to the project affect this skill's scope, **update this SKILL.md** to keep it accurate. Specifically:

- If a language file is added or removed: update the **The 19 Language Files** table and the count in the description frontmatter
- If key naming conventions change: update **Key Naming Conventions**
- If the workflow or tooling changes: update **Workflow**
- If translation quality standards change: update **Translation Quality Standards**

## Constraints

- DO NOT add nested JSON objects — strictly flat key/value pairs.
- DO NOT copy English values into non-English files as a fallback.
- DO NOT skip any of the 19 language files — all updated together, every change.
