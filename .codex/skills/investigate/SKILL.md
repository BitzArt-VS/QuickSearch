---
name: investigate
description: Use when the user requests to perform an investigation process via explicit requests (not casual mentions of the word 'investigation'), or to review an existing investigation file.
---

# Investigate

## Reading existing investigations

Investigation folders live under `.workspace/investigate/`, with one folder per {investigation-id}. Each investigation may contain a root `overview.md`, a current `temp-log.md`, and a `findings/` folder. Each finding folder contains an `overview.md` and the archived investigation `log.md`.

When asked to read or continue an investigation, use this structure to find the relevant logs and findings. Do not create or modify investigation files for read-only requests.

## Workflow

1. Review `.workspace/investigate/` for existing investigations and assign an {investigation-id}, consisting of a two-digit incrementing number and a short name, like `01-topic-focus`. Output it to the user immediately before proceeding with the next steps.
2. Create `.workspace/investigate/{investigation-id}`.
3. Create `.workspace/investigate/{investigation-id}/overview.md` when starting a new investigation. It should state the user's intent and the agent's thoughts on the overall idea of this new investigation.
4. Create `.workspace/investigate/{investigation-id}/findings/` if missing.
5. Create or continue `.workspace/investigate/{investigation-id}/temp-log.md` for the current investigation. If it already exists, review its contents carefully before proceeding. Consider whether it is relevant to your current investigation and should be kept and continued, or if it should be cleared/recreated before continuing. Clear/recreate it right away if necessary.
6. Update `.workspace/investigate/{investigation-id}/temp-log.md` incrementally while investigating.
7. For each finished finding:
   1. Create a new folder under `.workspace/investigate/{investigation-id}/findings/`. Give it the next two-digit incrementing number and a short name, for example `01-example-finding`.
   2. Create `overview.md` inside the finding folder.
   3. Move the current `.workspace/investigate/{investigation-id}/temp-log.md` into the finding folder.
   4. Rename the moved log to `log.md`.
   5. Create a new `.workspace/investigate/{investigation-id}/temp-log.md`.
   6. Present the finding to the user using the `Findings` format, and include a link to the finding's `overview.md` and `log.md` in the message as markdown links.
   7. Ask if the user wants to continue iterating.
   8. If yes, continue investigating. If no, stop.
