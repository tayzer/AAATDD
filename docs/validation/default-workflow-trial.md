# Ordinary-request acceptance trial

Status is **REQUESTER ACCEPTANCE PASSED**. On 2026-11-09 the requester confirmed that ordinary use passed, including installation, automatic discovery, human-approved Gherkin, verified API development and ordinary maintenance/failure recovery. The requester subsequently confirmed both the disabled-plugin test and re-enabled fresh-chat continuation test passed. This records the requester's experience verdict. The procedure below remains available for repeat trials.

The requester corrected the earlier coached trial on 2026-11-07: install and enable TADA, then ask for the project as normal. The enabled project must supply the workflow. A prompt that tells the agent to read workflow files, use Gherkin or follow RED/GREEN supplies the very behaviour being tested.

## Setup and request

Install the [local TADA plugin](../plugin.md) and enable it through the host. Installation must not request an application folder or run project preparation. Open a fresh application directory without manually invoking the setup helper. The enabled skill should discover requirements and install the matching verifier in that workspace when business verification needs it. Keep SDK/feed details in agent setup and context. The host's plugin state controls automatic workflow selection.

Open that application repository in a genuinely fresh Codex chat. Describe a useful project in your own words, as you would without this trial. For example, a reading-list API is a possible project, not an approved specification. Do not name TADA, tell the agent which files to read, prescribe Gherkin/tests or paste a workflow prompt. Record the exact request and the session's initial actions.

The agent should recover enabled-project guidance itself, discover the business requirements with you and ask consequential questions. It should propose success/failure examples and executable acceptance before implementing business behaviour. You decide and approve the expectations. Installation, file generation or a self-reported intention to use TADA does not establish this result.

## Observe the full experience

- For the first business increment, review meaningful independent bindings and the captured baseline. Record human approval yourself. Observe intended behavioural RED where behaviour is missing, then GREEN after implementation and verification of retained evidence. Broken setup is INDETERMINATE; already-satisfied behaviour needs no artificial failure.
- Request a bug fix in ordinary language. Existing accepted scenarios should be reused where adequate; missing expectations need your agreement. The agent must preserve prior obligations and failure history.
- Request a behaviour-preserving refactor and a small chore normally. Appropriate existing checks should run without invented business scenarios or repeated process instructions.
- Start another genuinely fresh chat and request useful continuation normally. The agent must find Git-native context and authoritative features itself, check the referenced evidence and recover unresolved work. Do not tell it to read a handoff or supply baseline/run IDs in the request.
- Record concrete friction and your verdict on whether the workflow was useful and intuitive. Keep initial failures visible. If you must remind the agent to use TADA, record that initial attempt as an adoption failure; a coached retry is diagnostic evidence only.
- Check that installation completes without a project-folder question. During project work, setup should use the active repository without asking you to paste its known path. A genuinely ambiguous target can require clarification when verification setup becomes necessary.
- Disable TADA in the host's plugin controls and start a fresh session. Check that its skill is absent and automatic TADA orchestration has stopped. Re-enable it, start another fresh session and request useful continuation normally. Check recovery of the accepted project behaviour and evidence.

Automated file/CLI tests establish activation wiring, byte preservation and repeatability. Scripted Echo/Orders controls establish bounded verifier behaviour. Neither substitutes for this ordinary-request trial. Other agents must use their normal repository-instruction discovery; custom discovery settings, conflicting/nested instructions and truncated instruction chains require investigation. Keep local advisory L1 limitations explicit. Merge and release publication remain separate gates.
