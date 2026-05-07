# Agent Revalidation

When the user requests an "agent revalidation":

1. **Structural validation** — Re-read agent instructions file in full and validate every section against the current state of the codebase — including any changes made in the current session or in prior sessions. Check project structure paths, conventions, entity references, and any other documented facts. If discrepancies are detected (e.g., renamed paths, new conventions, outdated references), update this file to reflect the actual codebase state.

2. **Logical analysis** — Go beyond mechanical checks. Analyze the overall coherence of the agent's configuration:
   - Are the documented responsibilities still aligned with the tools and reference files available?
   - Do the conventions still make sense given the current codebase patterns, or have practices drifted?
   - Are there sections that overlap, contradict, or have become redundant?
   - Are there gaps — areas the agent routinely works in that aren't covered by the instructions?
   - Do the self-maintenance rules catch all the side-effects that tasks typically produce?
   - Are reference files structured in a way that scales, or do they need restructuring?

   Present findings **one at a time**. For each finding:
   1. Describe the issue and your concrete suggestion.
   2. Use the ask-questions tool to let the user approve, reject, or modify the suggestion before moving on.
   3. Apply approved changes immediately, then proceed to the next finding.

   Do NOT batch all findings into a single response. Only apply structural-validation fixes (step 1) automatically.
