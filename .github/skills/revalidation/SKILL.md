---
name: revalidation
description: 'Revalidate an agent or skill configuration. Use when the user requests an "agent revalidation", "skill revalidation", or "revalidation". Performs structural validation of the config file against the codebase, then logical coherence analysis, applying approved changes one at a time.'
---

# Agent/Skill Revalidation

A structured quality-check workflow for keeping agent and skill configuration files accurate and coherent with the actual codebase.

## When to Use

The user has asked for an "agent revalidation", "skill revalidation", or simply "revalidation" in the context of working with an agent or skill.

## Procedure

### Step 1 — Structural Validation

Re-read the target agent or skill configuration file in full. Validate every documented fact against the current state of the codebase — including changes made in the current session or prior sessions. Check:

- File and directory paths (do they still exist and match what's documented?)
- Referenced tools, reference docs, and linked files (do they exist?)
- Named types, classes, dialogs, and namespaces (are they still accurate?)
- Documented conventions (are they still followed by existing code?)

For any discrepancies found, apply fixes **automatically** (no approval needed) — structural corrections are objective and non-controversial.

### Step 2 — Logical Analysis

Go beyond mechanical path checks. Analyze the overall coherence of the configuration:

- Are the documented responsibilities still aligned with the tools and reference files available?
- Do the conventions still make sense given the current codebase patterns, or have practices drifted?
- Are there sections that overlap, contradict, or have become redundant?
- Are there gaps — areas routinely worked in that aren't covered by the instructions?
- Do the self-maintenance rules catch all the side-effects that tasks typically produce?
- Are reference files structured in a way that scales, or do they need restructuring?

Present findings **one at a time**, ordered by importance and impact. For each finding:

1. Describe the issue and your concrete suggestion.
2. Use the ask-questions tool to let the user approve, reject, or modify the suggestion before proceeding.
3. Apply approved changes immediately, then move to the next finding.

Do **NOT** batch all findings into a single response. Do **NOT** apply logical-analysis changes without user approval.
