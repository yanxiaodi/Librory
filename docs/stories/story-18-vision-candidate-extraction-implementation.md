# Story 18 Implementation: MAF-Driven Book Recognition Workflow

## Goal

Implement a server-side recognition workflow that uses Microsoft Agent Framework to orchestrate image recognition, Google Books enrichment, and structured result generation behind a single scan upload endpoint.

## Architecture

Keep the workflow inside the API project and use Microsoft Agent Framework to orchestrate the AI steps.

This implementation should follow the MAF docs in three specific ways:

- Use function tools for Google Books and any other external lookup the workflow needs.
- Use structured outputs for the LLM step so the model returns typed book candidates instead of free-form text.
- Use a workflow builder to compose the recognition steps inside the API process, then execute the workflow in-process.

Recommended flow:

1. The API accepts the upload through a single scan endpoint and stores the image temporarily.
2. The job processor starts a Microsoft Agent Framework workflow inside the API process.
3. The workflow sends the image to Azure OpenAI gpt-5.6-luna.
4. The model returns structured candidates with title and optional author hint.
5. The workflow calls Google Books as a tool for metadata enrichment and disambiguation.
6. The workflow re-ranks metadata matches using title and author agreement.
7. The recognition job persists the final ranked list.

The workflow should keep the LLM output typed, for example with a `BookCandidate` model or equivalent JSON schema, so downstream steps do not need to parse ad hoc text.

## Tasks

- Define a structured candidate contract for the AI workflow
- Replace the current recognition pipeline with a Microsoft Agent Framework workflow
- Add a workflow step that passes the uploaded image to the LLM
- Expose Google Books as a workflow tool for metadata enrichment and disambiguation
- Add metadata re-ranking based on title and author agreement
- Keep the final recognition job response compatible with the existing UI
- Add logs for workflow step count, structured candidate count, metadata match count, and author agreement decisions

## Validation

Validate the slice with:

- unit tests for candidate extraction parsing
- unit tests for metadata re-ranking rules
- integration tests for the workflow with mocked LLM and Google Books tool calls
- a manual shelf-photo run that confirms the workflow returns fewer noisy candidates than the current flow

## Current Code Status

The recognition pipeline is wired end-to-end:

- `BookRecognitionAgentWorkflow` is registered as the recognition pipeline implementation.
- `IBookVisionChatClientFactory` / `AzureOpenAIBookVisionChatClientFactory` build a Microsoft Agent Framework
  `IChatClient` from `AgentFramework:AzureOpenAI` configuration (Azure OpenAI endpoint, API key, deployment name).
- The workflow creates an `AIAgent` from that chat client (`IChatClient.AsAIAgent(...)`), sends the shelf photo as a
  `DataContent` image attachment alongside a text prompt, and calls `agent.RunAsync<VisionCandidateExtractionResult>(...)`
  to get typed candidates (title, optional author, evidence text, confidence) directly — no manual JSON parsing.
- Each candidate is enriched via the existing Google Books search service and re-ranked by title and author
  agreement (`BookRecognitionAgentWorkflow.RankMetadataMatches`).
- When Agent Framework is not configured, the workflow logs a warning and returns zero candidates instead of
  fake/placeholder results.
- The old OCR and vision-fallback services, their config section, and the stale duplicate `BookRecognitionWorkflow`
  class have been removed.

The two steps (vision extraction, metadata enrichment) are wired as plain sequential async calls rather than a
`Microsoft.Agents.AI.Workflows` graph, since there is no branching or checkpointing requirement — see the class-level
comment on `BookRecognitionAgentWorkflow` for the rationale. `Microsoft.Agents.AI.Workflows` and `Azure.AI.Projects`
were removed from the Infrastructure project since they were unused.

Unit tests (`BookRecognitionPipelineTests`) cover: no-candidates-when-unconfigured, structured extraction with a fake
`IChatClient`, metadata-lookup-failure warnings, and title/author re-ranking.

## Risks

- The LLM may still return noisy candidates if the prompt is too broad
- Google Books may return multiple editions for the same title, so author matching must stay tolerant
- Workflow orchestration adds complexity, so the implementation should stay modular and asynchronous
- The final API contract should remain stable so the web UI does not need a large rewrite
