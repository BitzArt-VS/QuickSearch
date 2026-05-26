### Update block:

Only list the items that reference files (items that contain markdown link blocks) if it is an existing file and not when the suggestion is to create a new file.
Only add the applicable fields for each update, for example for an update that suggests adding a new skill file, you would only add the "New file" field, and not the "Agent", "Skill", or "Reference file" fields. Only list the **affected** files and not just *relevant* ones.
Merge items based on the concept complexity, rather than on how the files are structured. For example, if you notice a minor update that spans across multiple files but all the changes are conceptually simple, merge these into a single item.
Squashing multiple update items into a single one is allowed if the items deal with closely related concepts in a single file. Split the items otherwise — even if they deal with the same file but separate logical concepts.

For details blocks, present a brief overview of the proposed change, the rationale on why you arrived to a conclusion these changes are necessary, and any relevant context. Be concise but list items comprehensively.

Make sure to only output a single logical item at a time, you can present the next item after the current one is successfully executed upon.

If not currently executing `revalidation`:

```md
### Agent self-maintenance warning {iterate like 1, 2, 3, etc. within the current session, do not add an iterator number if this is the first and only such update in this session}: {brief description (title) of the update}
```

If executing `revalidation`:

```md
### Revalidation update {i}: {brief description (title) of the update}
```

And then:
```md

- **Agent:** [{agent-name}]({file-path}) (only add if applicable, for working on agent files (`.codex/agents/{agent-name}/{agent-name}.toml`))
- **Skill:** [{skill-name}]({file-path}) (only add if applicable, for working on skill files (`SKILL.md`))
- **Directory reference:** [{agent/skill name}/{file-name}]({file-path}) (only add if applicable, for working on `AGENTS.md` files located in project directories)
- **Reference file(s):** [{file-name}]({file-path}) (only add if applicable, for working on reference files under skill or agent configuration, multiple files can be referenced here if they are related to the same update item)

- **Add file:** {file-path}
  - {brief description of the new file and its purpose (2-10 sentences)}
- **Add section:** {section-name}
  - {A short overview of the new section (2-5 sentences)}
- **Update section:** {section-name}
  - {section update details (2-10 sentences)}
- **Remove section:** {section-name}
  - {A short explanation on why you suggest removing this section (1-3 sentences)}
- **Relevant source content:**
  - {list any source code files highly relevant to the change in markdown link format relative to the current working root, try to keep this list concise by only listing the most relevant files, and not all related files, to avoid overwhelming the user with too much information.}
```

After listing this information, propose the suggested change and request user's feedback on how to proceed.
Use the built-in `request_user_input` tool, not a normal chat question.

Ask me exactly one structured question with the format below, preserving the wording provided and without omitting anything. The question must align closely with the proposed change. It must never cover multiple logical items simultaneously. You may have used complex language beforehand, but it is not allowed in the question block. The question block's wording must be designed as if you were speaking to an uninitiated user, so it must be concise and clear, and avoid any technical jargon or complex language. The options provided in the question block must be clear and descriptive enough for the user to understand the implications of each choice without needing further explanation. Complex/difficult/technical wording in the question block is extremely bad UX and must be avoided at all costs.

```md
- header: revalidation
- id: revalidation_update_{i}[_{n}] (`i` is the current revalidation update, `n` is the current count of iterations on the same update - such as follow-up questions, omit the `_{n}` part if this is the first and only question for this update)
- question: {A concise question about how to proceed with the proposed change, covering the change briefly and asking for the user's decision on how to proceed with it.}
- options:
  1. 
    - label: Fix (Recommended)
    - description: {brief description of the proposed change, 10-30 words}
  2.
    - label: Skip
    - description: Irrelevant for now? I will skip this issue and not make any changes regarding it.
  3.
    - label: Ignore
    - description: If this is not an important issue, let's mark it as such in the agent instructions for future agent iterations, so it won't be flagged again.
```

`Ignore` is an option for the user to select and **not** an instruction for you to ignore this option.
If option 3 (`Ignore`) is selected, propose where and how it can be marked for future agent iterations to ignore it and ask the user to approve it in a follow-up question using the same format.

After the user answers, acknowledge the selected option.
If the `request_user_input` tool is not available, say that instead of emulating it in plain text.

After receiving the user's answer, execute on their decision immediately before proceeding with any further tasks.

After this is done, proceed with the process of revalidation, until all requested files have been revalidated and you are absolutely sure there are no deeper logical layers to uncover in the relevant or related files.