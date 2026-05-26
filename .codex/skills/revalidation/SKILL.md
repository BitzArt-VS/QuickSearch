---
name: revalidation
description: "Perform a comprehensive revalidation of an agent's instructions files against the current codebase, including structural checks and logical analysis of coherence, alignment with tools, and coverage of responsibilities. Update the instructions file to reflect any discrepancies or improvements found."
---

# Agent Revalidation

Perform the following steps for all files under the `.codex` directory, all files under `.agents/references/`, then for each of the `AGENTS.md` files in this project. Pick a good candidate file to start with, then proceed through the rest systematically. Process skill files using a top-down approach, starting with the highest-level files first (`SKILL.md`) and then proceeding to any reference files linked from that skill. For each file, expand on your personality instructions' `Self-maintenance` section and perform a more thorough analysis. Present an update to the user as soon as you find one. Wait for the user's response, execute based on the user's input immediately, then proceed with the rest of the revalidation. Do NOT wait to present all your findings at once.

1. **Structural validation** — Re-read the instructions file in full and validate every section against the current state of the codebase — including any changes made in the current session or in prior sessions. Check project structure paths, conventions, entity references, and any other documented facts. If discrepancies are detected (e.g., renamed paths, new conventions, outdated references), follow the steps in the `Self-maintenance` section of your personality instructions to suggest updates to the user.

2. **Logical analysis** — Go beyond mechanical checks. Analyze the overall coherence of the agent configuration:
   - Are the documented responsibilities still aligned with the tools and reference files available?
   - Do the conventions still make sense given the current codebase patterns, or have practices drifted?
   - Are there sections that overlap, contradict, or have become redundant?
   - Are there gaps — areas the agent routinely works in that aren't covered by the instructions?
   - Do the self-maintenance rules catch all the side-effects that tasks typically produce?
   - Are reference files structured in a way that scales, or do they need restructuring?

Present findings as described in the `Self-maintenance` section. For each finding:
1. Describe the issue and your concrete suggestion.
2. Present your finding to the user and let them approve, reject, or modify the suggestion before moving on.
3. Apply approved actions immediately or skip those that were not approved, or work with the user on clarifying details until the item is exhausted.
4. Proceed with the rest of the analysis.
