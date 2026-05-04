# AI Chat Workbench Redesign Design

**Date:** 2026-05-05

**Status:** Approved in conversation, pending written spec review

## Goal

Upgrade the frontend AI chat page from a utility-style management surface into a product-grade AI chat workbench with a cleaner information hierarchy, stronger reading rhythm, lighter control density, and output behavior that feels closer to ChatGPT plus DeepSeek.

The redesign preserves existing capabilities such as model selection, chat modes, knowledge base routing, web search, streaming output, session history, and export actions, while making chat and writing quality the primary experience.

## Product Direction

The target experience is a hybrid:

- The main chat surface should feel like ChatGPT: quiet, centered, readable, and low-friction.
- Capability status should feel like DeepSeek: present, trustworthy, and lightly visible without becoming a control panel.
- Advanced controls should remain available, but move out of the first visual layer.

This is not a feature expansion project. It is an information architecture and interaction quality upgrade.

## Success Criteria

The redesign is successful when:

1. The first screen emphasizes the current conversation and the input composer rather than settings.
2. The left rail acts as a conversation navigator, not an AI control center.
3. AI responses read like polished product output instead of card-styled admin messages.
4. Mode, model, knowledge base, and web capabilities remain available through lighter status surfaces.
5. Error and transient system states are rendered as system feedback, not as assistant prose.
6. The mobile layout preserves the same product identity without exposing the current bulky control drawer.

## Current Problems

### Information Hierarchy

- The sidebar currently mixes identity, model settings, mode selection, knowledge base selection, history, and destructive actions in one dense stack.
- The page reads like a tool console because high-level controls are louder than the conversation itself.

### Conversation Presentation

- Assistant messages are rendered as heavy cards with strong borders and panel treatment.
- Output feels boxed in instead of readable.
- System details such as route hints and retrieval status can visually compete with the answer itself.

### Composer Experience

- The input area feels like a large form field rather than the main AI work surface.
- Capabilities related to the current prompt are separated from the composer, increasing cognitive distance.

### Mobile Experience

- The mobile drawer reproduces too many desktop controls and overwhelms the core task of asking and reading.

## Design Principles

1. Default to chat first.
2. Preserve power without exposing all power at once.
3. Make answers feel authored, not panel-rendered.
4. Keep capability feedback visible but quiet.
5. Treat system errors, retries, and transient states as product feedback, not chat content.
6. Follow the repo's existing component structure where possible instead of introducing a full page rewrite.

## Target Information Architecture

### Left Rail

The left rail becomes a conversation navigation surface only.

Visible by default:

- Brand or assistant identity
- New chat
- Search conversations
- Conversation list grouped by time
- Bottom settings or account entry

Removed from the first layer:

- Model settings card
- AI mode grid
- Knowledge base select block
- Clear all button as a primary action
- Export as a primary action

Session row secondary actions such as rename, pin, export, and delete should move into a compact overflow menu or hover affordance.

### Top Status Bar

The top bar should describe the current conversation context, not act like a toolbar full of setup panels.

Primary content:

- Current model
- Current answer mode
- Knowledge base state
- Optional web search state

Secondary content:

- Share
- More actions

All state items should be rendered as compact text or light capsules. Clicking them may open a menu, popover, or panel for changes.

### Main Conversation Surface

The center column becomes the dominant visual surface.

- Content width should be constrained to an easier reading column, roughly 760px to 820px.
- Empty state should show a welcome block plus a small set of realistic prompt starters.
- Message flow should avoid heavy panel nesting.
- The composer should stay visually anchored at the bottom.

### Composer Surface

The composer should become the main working surface.

Required capabilities near the prompt:

- Knowledge base toggle or selector
- Web search toggle
- Mode entry
- Send action

These controls should be lightweight and contextual, not the primary content of the page.

## Interaction Model

### Desktop

- Left rail is visible by default and can be collapsed.
- Main content remains centered and readable when the rail is open.
- Advanced controls open from the status bar or composer toolbar.

### Mobile

- Left rail becomes a drawer focused on new chat, search, and history.
- Model, mode, and knowledge base controls move to top-bar or bottom-sheet interactions.
- Composer remains fixed at the bottom with compact accessory controls.

## Message Presentation Design

### User Messages

- Right aligned
- Dark rounded bubble
- Shorter visual footprint than assistant content
- Minimal metadata emphasis

### Assistant Messages

- Left aligned with assistant avatar
- No heavy card framing
- Prefer transparent or very light surface treatment
- Answer header may contain one or two lightweight state capsules
- Footer actions such as copy, retry, and continue should appear after the content and stay visually quiet

### Output Formatting

Assistant output should support adaptive reading:

- Short prompts should produce concise, clean answers with minimal visual overhead.
- Complex prompts should expand into structured markdown with healthy spacing.

Rendering rules should improve:

- Paragraph spacing
- Heading rhythm
- List readability
- Code block polish
- Quote treatment
- Table restraint

The output should feel closer to article reading than to chat card scanning.

## Capability Feedback

The chosen direction is light feedback rather than hidden feedback or highly explicit process visualization.

Visible examples:

- Smart mode
- Knowledge base connected
- Web enabled
- Thinking

Rules:

- Show at most the most relevant one or two states in the answer header or status bar.
- Retrieval and routing progress can appear while work is ongoing, but should collapse into a quieter state once content begins streaming.
- Avoid turning retrieval mechanics into the headline of the answer.

## Streaming Behavior

Streaming should feel stable and professional.

- Before content appears, show one calm system state such as `thinking` or `retrieving context`.
- Once text starts streaming, reduce status prominence.
- Avoid jumpy spacing changes as markdown content grows.
- Preserve scroll stability during long outputs.

## Error Handling

System failures must not appear as if the assistant is saying them.

Rules:

- Technical exceptions are rendered as system-level feedback.
- User-facing error copy remains friendly and generic.
- Common transient failures such as rate limits or provider instability should render as retry-oriented notices.
- Existing error sanitization should remain compatible with the redesign.

## Component Strategy

The redesign should prefer reshaping the existing chat module instead of introducing an entirely separate screen implementation.

Expected touch points:

- `weblog-vue3/src/components/chat/ChatPanel.vue`
- `weblog-vue3/src/components/chat/StreamMarkdownRender.vue`
- `weblog-vue3/src/components/chat/ModelSettings.vue`
- `weblog-vue3/src/pages/frontend/chat.vue`

Potential refactor opportunity:

- Split `ChatPanel.vue` into focused subcomponents for sidebar, status bar, message list, and composer if the file becomes too hard to reason about during implementation.
- Keep the split scoped to the chat feature and avoid broad design-system refactors.

## Testing Strategy

Verification should cover both behavior and presentational regressions.

### Functional Checks

- Session creation, switching, rename, delete, and export still work.
- Model and mode changes still affect requests correctly.
- Knowledge base and web search states still flow into requests correctly.
- Streaming output still renders incrementally.
- Error states still render sanitized feedback.

### UI Checks

- Empty state renders welcome content instead of a blank page.
- Sidebar contains only conversation-focused content in the default view.
- Assistant output no longer uses the previous heavy bordered card treatment.
- Composer remains fixed, usable, and visually central on desktop and mobile.

### Responsive Checks

- Desktop with sidebar expanded
- Desktop with sidebar collapsed
- Mobile drawer open
- Mobile active conversation with keyboard-safe composer spacing where applicable

## Out Of Scope

The following are not part of this redesign:

- Backend provider protocol changes
- New AI capabilities beyond current model, mode, knowledge base, and web support
- New persistence formats for sessions
- A new design system for the whole site
- Rewriting unrelated article-detail AI surfaces

## Recommended Rollout Order

1. Reshape the page skeleton and information architecture in `ChatPanel.vue`.
2. Rebuild the sidebar into history-first navigation.
3. Introduce the top status bar and move capability controls out of the sidebar.
4. Restyle the message stream and assistant output presentation.
5. Rebuild the composer into a product-style work surface.
6. Finish mobile behavior and responsive polish.
7. Verify streaming, error rendering, and request wiring after the visual rewrite.

## Risks And Mitigations

### Risk: Visual redesign breaks request wiring

Mitigation:

- Keep request payload logic close to existing state variables.
- Add targeted frontend tests in the existing frontend test setup and manual verification around provider, mode, and knowledge base propagation.

### Risk: `ChatPanel.vue` becomes harder to maintain

Mitigation:

- Split only along clear feature boundaries if the file grows more complex during implementation.

### Risk: Product styling drifts into marketing or dashboard aesthetics

Mitigation:

- Keep the page sparse, utility-driven, and chat-first.
- Avoid decorative cards, oversized hero treatment, or control-heavy chrome.

## Final Decision

Implement a hybrid AI chat workbench:

- ChatGPT-like layout clarity and reading comfort
- DeepSeek-like light status feedback and capability confidence
- Existing power features retained through lighter, contextual entry points

The key outcome is not more visible capability. The key outcome is better default conversation quality.
