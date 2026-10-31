# AI-Powered Applications with .NET Fundamentals, Second Edition: Companion Code

Companion code for *AI-Powered Applications with .NET Fundamentals, Second Edition* by Adam Seebeck: Contoso Support Copilot, built up chapter by chapter with .NET 11 and Microsoft.Extensions.AI. Every `ChapterNN` folder is the complete solution at the end of that chapter, and every chapter runs without an AI account.

## What You Need

- The .NET 11 SDK, version 11.0.100 or later. The `global.json` file in this folder selects it and accepts later .NET 11 feature bands; check with `dotnet --version`.
- Visual Studio or Visual Studio Code with C# Dev Kit. The .NET CLI commands below work in any terminal.
- Nothing else for the default offline mode: no AI account, no database server, and no network access after the NuGet restore.

Optional, for real model output: [Ollama](https://ollama.com) for free local models, a Microsoft Foundry resource with one chat deployment and one embedding deployment, or an OpenAI API key. SQL Server, including LocalDB, can replace the default SQLite database.

## Run a Chapter

From a chapter folder:

```powershell
cd Chapter03
dotnet run --project ContosoSupportCopilot.Api -- --check
dotnet run --project ContosoSupportCopilot.Api
```

`--check` runs the chapter's main path once, prints the results, and exits; keep it as the last argument. The second command starts the API at `http://localhost:5101`. From Chapter 2 on, `http://localhost:5101/health` reports the AI provider's status, and `/health/live` answers without checking the provider. From Chapter 11 on, `POST /api/workflow/tickets/{ticketId}/process` runs the multi-agent workflow and `POST /api/workflow/{workflowId}/resume` continues a stored run; Chapter 12 adds `/health/ready`.

Activity solutions live in `ChapterNN/Activity`. From Chapter 6 on, every chapter folder also has a `ContosoSupportCopilot.Tests` project. Run the tests from the folder that holds the solution:

```powershell
cd Chapter06
dotnet test
```

## Offline Mode

`AI:Provider` defaults to `Offline`. The offline chat client in `ContosoSupportCopilot.Infrastructure/AI/Offline` answers locally and deterministically: the same request always gets the same answer.

- Text answers start with a label such as `[Offline sample reply]`. They come from fixed templates and keyword rules, not from a model.
- JSON answers follow the format the code asks for, so parsing and validation code runs for real.
- Token counts are estimates (about four characters per token), so the cost lines are illustrations.
- Offline embeddings are hashed word vectors. Texts that share words get similar vectors, but synonyms do not, and the vectors cannot be mixed with a real model's vectors.
- From Chapter 8 on, offline grounded answers quote the first sentence of each source and cite it, so citation checks run without a model.
- From Chapter 10 on, the offline client calls the tools that an agent's instructions name, in that order, with the user's message as every text argument, so function invocation runs for real; only the decisions are scripted. A draft that receives a knowledge answer restates it without the citation markers.
- In Chapter 9, the LLM judges need a model. Offline, the deterministic metrics run: retrieval precision and recall, citation usefulness, and source overlap. The check also runs `GroundednessEvaluator` against a scripted stand-in for the judge model, which returns fixed verdicts for two example answers.

Offline output shows how the application works. It says nothing about how well a model answers; switch to a real provider for that.

## Use a Real Provider

The `AI` configuration section selects the provider:

| Setting | Ollama | Microsoft Foundry | OpenAI |
| --- | --- | --- | --- |
| `AI:Provider` | `Ollama` | `Foundry` | `OpenAI` |
| `AI:Endpoint` | Optional; defaults to `http://localhost:11434` | Required: `https://<resource>.openai.azure.com/openai/v1/` | Optional; set it for an OpenAI-compatible server |
| `AI:ChatDeployment` | Chat model name | Chat deployment name | Chat model name |
| `AI:EmbeddingDeployment` | Embedding model name | Embedding deployment name | Embedding model name |
| `AI:ApiKey` | Not used | Required unless `AI:UseEntraId` is `true` | Required |
| `AI:UseEntraId` | Not used | `true` for keyless access | Not used |

`AI:TimeoutSeconds` (default 120) limits each network attempt. In Chapters 2 and 3 the OpenAI SDK retries failed requests on its own; from Chapter 4 on, the resilience pipeline is the only retry layer, and the SDK sends each request once. Keep keys out of files: on a development machine, use user secrets, which every chapter shares through the user secrets id `contoso-support-copilot`; in CI pipelines and containers, use environment variables such as `AI__Provider` and `AI__ApiKey`.

**Ollama.** Install Ollama and pull a chat model and an embedding model, for example `ollama pull llama3.2` and `ollama pull nomic-embed-text` (good choices at the time of writing). Then, from a chapter folder:

```powershell
dotnet user-secrets set "AI:Provider" "Ollama" --project ContosoSupportCopilot.Api
dotnet user-secrets set "AI:ChatDeployment" "llama3.2" --project ContosoSupportCopilot.Api
dotnet user-secrets set "AI:EmbeddingDeployment" "nomic-embed-text" --project ContosoSupportCopilot.Api
```

Quality and speed depend on the model and your hardware. llama3.2 runs the checks of Chapters 2 to 7. From Chapter 8 on, use Microsoft Foundry or OpenAI: those chapters check citation formats, scores from a judging model, and agent tool calls, which small local models such as llama3.2 and qwen2.5:7b do not follow reliably at the time of writing.

**Microsoft Foundry.** Create a Foundry resource, deploy a chat model (a GPT-5-series model such as gpt-5.4-mini at the time of writing) and an embedding model (such as text-embedding-3-small), and copy the endpoint and key from the resource:

```powershell
dotnet user-secrets set "AI:Provider" "Foundry" --project ContosoSupportCopilot.Api
dotnet user-secrets set "AI:Endpoint" "https://<resource>.openai.azure.com/openai/v1/" --project ContosoSupportCopilot.Api
dotnet user-secrets set "AI:ChatDeployment" "<chat-deployment-name>" --project ContosoSupportCopilot.Api
dotnet user-secrets set "AI:EmbeddingDeployment" "<embedding-deployment-name>" --project ContosoSupportCopilot.Api
dotnet user-secrets set "AI:ApiKey" "<api-key>" --project ContosoSupportCopilot.Api
```

For keyless access, assign your account the Cognitive Services OpenAI User role on the resource, sign in with `az login`, skip the key, and set `AI:UseEntraId` to `true`. A deployed app uses its managed identity instead. `CachedTokenProvider` keeps each token until shortly before it expires, so the Azure CLI runs about once an hour, not before every model call.

**OpenAI.** Set `AI:Provider` to `OpenAI`, `AI:ApiKey` to your key, and the two model names. To use an OpenAI-compatible server, also set `AI:Endpoint` to its base address.

Microsoft Foundry and OpenAI charge per token. A `--check` run sends a handful of short requests, and with a real provider `/health` sends one more at most about once a minute, because the AI check reuses its result, also when the provider did not answer within the check's 8 seconds; `/health/live` sends none. Chapter 3's cost line uses the placeholder rates in `AI:Pricing`; replace them with your deployment's current rates. The resilience, validation, and fallback demonstrations in the Chapter 4, 5, and 6 checks always use the offline client, so they cost nothing with any provider.

From Chapter 4 on, the `KernelResilience` section sets the timeouts, retries, and circuit breaker for model calls, and every chat call made through the application's `IChatClient` goes through them, because the pipeline runs as middleware of that client. Embedding calls share them from Chapter 7 on, because the embedding client runs the same pipeline, and so do the Chapter 9 judges, whose own client gets the same middleware. Each model call passes the pipeline once. Chapter 5 asks OpenAI and Microsoft Foundry models for strict structured output, which current GPT-5-series models support; a model or OpenAI-compatible server without strict schema support rejects those requests, so set `AI:StrictStructuredOutput` to `false` for it. For a reasoning deployment, `AI:StructuredOutputReasoningEffort` (`Low`, `Medium`, or `High`, empty by default) sets the reasoning effort of those structured calls; leave it empty for a model without reasoning, which can reject the setting.

## Vector Store

From Chapter 7 on, the `VectorStore` section selects where knowledge chunks are stored and searched. The default `InMemory` store needs nothing: the app fills it from the database at startup. `VectorStore:MinScore` (default 0.2) is the cosine similarity floor for search results. It suits the offline embeddings; every embedding model has its own range, so calibrate it for yours, as Chapter 9 shows. `VectorStore:TopK` (default 5) limits the results.

**Azure AI Search.** Create a search service, then, from a chapter folder:

```powershell
dotnet user-secrets set "VectorStore:Provider" "AzureAISearch" --project ContosoSupportCopilot.Api
dotnet user-secrets set "VectorStore:Dimensions" "1536" --project ContosoSupportCopilot.Api
dotnet user-secrets set "Search:Endpoint" "https://<service>.search.windows.net" --project ContosoSupportCopilot.Api
dotnet user-secrets set "Search:ApiKey" "<admin-key>" --project ContosoSupportCopilot.Api
```

`VectorStore:Dimensions` must match the vectors of your embedding deployment, for example 1536 for text-embedding-3-small at full length. The app creates the index named by `Search:IndexName` (default `contoso-knowledge`) on first use, and the `--check` runs of Chapters 7, 8, and 9 index the sample articles. The app needs an admin key because it creates the index. For keyless access, set `Search:UseEntraId` to `true`, enable role-based access on the search service, and assign your account the Search Service Contributor and Search Index Data Contributor roles. Azure AI Search has its own pricing tiers; check them before you create a service.

## Retrieval and Evaluation Settings

From Chapter 8 on, `Rag:ProductAreasByEngineer` assigns product areas to engineer IDs for the authorization filter; the check answers as `engineer-ana` (Authentication and API) and `engineer-ben` (Payments). In production, take the assignments from your identity provider. `Rag:MaxOutputTokens` (default 1500) caps every grounded answer, and `AI:Pricing:EmbeddingPerMillion` prices the embedding tokens in the cost lines.

From Chapter 9 on, the `Evaluation` section names the identity the evaluation runs as (`evaluation-runner`), which reads every product area. Only a run with `--check` grants it those areas, from `EvaluationAccess.cs`, so the web host never accepts it as an engineer; the keys follow `Evaluation:RequesterId`, and an engineer who already has areas keeps only those, so a check run as `engineer-ben` fails. `JudgeDeployment` names the deployment for the LLM judges: use a different model from the chat deployment, ideally a stronger one; empty uses `AI:ChatDeployment`, and the evaluation logs a warning. The judges' client removes the temperature, top_p, and penalty settings that the quality evaluators send, because reasoning deployments such as the GPT-5 series reject them, and raises the judges' output limit to `JudgeMaxOutputTokens` (16000), because reasoning tokens count against it. A judge call that fails stops the run with the judge's error. `UseJudges` switches the judges on or off; empty runs them with every provider except Offline. Results and cached judge responses go to the `evaluation-results` folder in the ContosoSupportCopilot.Api folder, which `.gitignore` excludes. With a real provider, the Chapter 9 check makes up to 80 chat requests (four variants of the four answerable test cases, each with an answer and four judges, the fourth variant with the revised prompt; the out-of-scope case gets the fixed refusal and no judges), and the reporting cache answers repeated judge requests from disk. To build the report with the command-line tool, install the version that matches the libraries, because a newer tool can read a different storage format:

```powershell
dotnet tool install --create-manifest-if-needed Microsoft.Extensions.AI.Evaluation.Console --version 10.11.0
dotnet tool run aieval report --path ContosoSupportCopilot.Api/evaluation-results --output report.html
```

## Agents, Workflows, and Operations

From Chapter 10 on, the agents use Microsoft Agent Framework (`Microsoft.Agents.AI`). With a real provider, use a chat deployment that supports tool calling, such as a GPT-5-series deployment in Microsoft Foundry at the time of writing; see the Ollama note above for local models. Agents keep one session per conversation, agent, and engineer, with a version check: a run that saves after another run adds its turn to the stored session. Every run writes a record with its model and token counts.

From Chapter 11 on, a step that fails for a transient reason stops the workflow with its state saved, and the endpoint answers 503 with a `Retry-After` header; resuming the run retries that step. The `Workflow` section sets the wait, `RetryAfterSeconds` (default 60, and never shorter than `KernelResilience:CircuitBreakerBreakSeconds`), and `MaxStepRetries` (default 2, at most 5): after that many stops, the step's next transient failure goes to a person. The process and resume endpoints take the engineer from the `X-Engineer-Id` header; the sample has no sign-in. Only the engineer who started a workflow can resume it, and a resume that another request beat answers 409. The Chapter 11 activity adds an `Idempotency-Key` header, scoped to the engineer; the same key sent again with another ticket or input answers 422 with a Problem Details body.

Chapter 12 adds these sections:

| Setting | Default | What it does |
| --- | --- | --- |
| `Telemetry:Exporter` | `None` | `Console` prints spans and metrics; `Otlp` sends them, and the logs, to `OTEL_EXPORTER_OTLP_ENDPOINT` |
| `Telemetry:EnableSensitiveData` | `false` | Puts prompts, answers, and tool arguments in the spans; keep it off outside a debugging session |
| `Health:AIProbeIntervalSeconds`, `Health:AIProbeRetrySeconds`, `Health:AIProbeTimeoutSeconds` | `300`, `30`, `10` | How often the background monitor probes the AI provider while it answers and while it fails, and how long one probe may take; readiness only reads the result |
| `Budget:DailyLimitUsd`, `Budget:AlertThresholdPercent`, `Budget:MaxTokensPerConversation` | `100`, `80`, `50000` | The daily budget, the alert threshold, and the token limit that flags a conversation |
| `EmbeddingCache:MaxEntries` | `10000` | Size limit of the embedding cache |
| `Database:InitializeOnStartup` | `true` | `false` leaves the schema to `--initialize-database`, as the Kubernetes manifest does |
| `Database:SeedSampleData` | `true` in Development, `false` elsewhere | Adds the sample tickets and articles to an empty support database; a production database never needs them |
| `Authentication:Schemes:Bearer` | not set | Authority, v2.0 issuer, and audience (the API's client ID) of the Microsoft Entra ID access tokens; the deployment sets them |

Outside the Development environment, Chapter 12 takes the engineer from the `oid` claim of a validated access token that grants the API's `Copilot.Use` scope, and ignores `X-Engineer-Id`; a request without a valid token gets 401, and a token without the scope, such as an app-only token, gets 403. `Rag:ProductAreasByEngineer` then maps object IDs to product areas. Readiness reports an unreachable AI provider as `Degraded` (status 200), and a database or the vector store as `Unhealthy` (503).

With a real provider, the checks of Chapters 10 to 12 make a few dozen chat requests each; Chapter 12's benchmark alone runs the workflow five times, with seven chat requests and one embedding request per run. While the Chapter 12 API runs, its AI provider monitor sends one short chat request every five minutes.

## Containers and Kubernetes

Chapter 12 has a `Dockerfile` on the .NET 11 SDK image and the chiseled-extra ASP.NET Core runtime image, which runs as a non-root user and includes the ICU data that SqlClient needs. From the `Chapter12` folder:

```powershell
docker build -t contoso-support-copilot:1.0.0 .
docker run --rm -p 8080:8080 contoso-support-copilot:1.0.0
```

The container runs the offline provider in the Production environment: `http://localhost:8080/health/ready` reports every dependency, the workflow endpoints expect a token, and the database holds no sample data. On a local machine only, add `-e ASPNETCORE_ENVIRONMENT=Development` to call them with the header and the sample tickets. `ci.yml` is a GitHub Actions workflow that builds, tests, runs the check, and builds the image; copy it to `.github/workflows/`. `deploy/kubernetes.yaml` holds the service account for workload identity, the deployment, the service, and the autoscaler, and `deploy/initialize-database-job.yaml` runs the image once per release with `--initialize-database`. Replace the `<...>` placeholders before you apply them, and keep secrets in a Kubernetes secret, never in the files.

## Database

From Chapter 2 on, the API uses SQLite: `contoso-support.db` appears in the `ContosoSupportCopilot.Api` folder on the first run, with three sample tickets and three knowledge articles. From Chapter 6 on, conversation state and run records go to a second file, `contoso-copilot.db`, next to it. From Chapter 7 on, the knowledge base has five articles, one of them an unpublished draft; Chapter 8 adds an article with a prompt injection attempt and one with addresses, a token, and an email address to mask. Delete the files to start over; each `--check` run in Chapter 6 adds three turns to the password ticket's conversation and one to the API ticket's. To use SQL Server instead, set `Database:Provider` to `SqlServer` and `ConnectionStrings:SupportDb` to your connection string, for example `Server=(localdb)\MSSQLLocalDB;Database=ContosoSupportCopilot;Trusted_Connection=True;TrustServerCertificate=True`. From Chapter 6 on, also set `ConnectionStrings:CopilotDb` to a different database, such as `Database=ContosoCopilotState`; each context creates its tables only in a database that has none.

Later chapters add tables to the copilot database: `AgentSessions` in Chapter 10, `WorkflowStates` in Chapter 11, `WorkflowRequests` in the Chapter 11 activity, and `ModelUsage` in Chapter 12. From Chapter 10 on, the app creates the tables that an existing database lacks at startup and logs `Added tables to the copilot database: ...`, so a database from an earlier chapter keeps its data; it never changes an existing table. From Chapter 12 on, `dotnet run --project ContosoSupportCopilot.Api -- --initialize-database` creates the databases and missing tables and exits without starting the server; it adds the sample data only in Development or with `Database__SeedSampleData=true`.

## Chapter Map

| Chapter | Folder | What it adds | A real model shows |
| --- | --- | --- | --- |
| 1 | `Chapter01` | Domain model, application contracts, `SupportChatClient` over `IChatClient`, in-memory sample data. Activity: escalation drafts with xUnit and Moq tests. | Nothing extra: this chapter runs offline only. |
| 2 | `Chapter02` | Provider switch, SQLite or SQL Server, Semantic Kernel on the shared `IChatClient`, options validation at startup, a cached AI check on `/health` and a probe endpoint `/health/live`, Key Vault in production. Activity: a JSON health report. | The health check against your deployment. |
| 3 | `Chapter03` | `IntentExtractor` and `DraftingService` on `IChatClient`, JSON output, streaming with token usage, cost from per-million-token rates, logging middleware. Activity: a confidence-based fallback with tests. | Real classifications, streaming speed, and token counts. |
| 4 | `Chapter04` | Semantic Kernel behind `IKernelOrchestrator` on the shared `IChatClient`, a native plugin with a scoped registration, escalation drafts, and a Polly 8 resilience pipeline (total timeout, retries with jitter, circuit breaker, attempt timeout) that every chat call runs through as chat client middleware. Activity: a customer history plugin, detailed fallback messages, and tests. | Real escalation drafts and real latency against the timeouts. |
| 5 | `Chapter05` | Prompt templates, structured output with `GetResponseAsync<T>`, strict JSON options, validators, and retries for invalid output under one deadline, with every model call still in the Chapter 4 pipeline. Activity: conversation history in the escalation prompt, with tests. | How often a real model needs a retry, and strict structured output on models that support it. |
| 6 | `Chapter06` | Conversation state in its own database with a concurrency check, run records with model and token counts, correlation IDs with middleware and a trace tag, drafts inside the Chapter 4 pipeline, a fallback decorator for complete and streamed drafts, and the `ContosoSupportCopilot.Tests` project. Activity: an intent fallback decorator, paged history, and more tests. | Real token counts and model names in the run records. |
| 7 | `Chapter07` | `IEmbeddingClient` over the Chapter 2 embedding generator, chunking at paragraph and sentence boundaries, an in-memory vector store with cosine similarity or Azure AI Search with escaped filters and the embedding model recorded with every chunk, and `VectorKnowledgeRepository` behind `ISupportContext`. Activity: an embedding cache decorator with tests. | Matches by meaning instead of shared words, and real similarity scores to calibrate `MinScore`. |
| 8 | `Chapter08` | `RagOrchestrator`: authorization per engineer, prompt injection detection, masking, XML delimiters built with LINQ to XML, citation validation, answers cut off at the output limit treated as ungrounded, tickets that carry instructions for the model flagged for review, and an offline responder that cites its sources. Activity: phone number and obfuscated email masking, cached authorization decisions, conversation history in the prompt, and tests. | Real grounded answers, and how reliably a model cites its sources. |
| 9 | `Chapter09` | Evaluation with Microsoft.Extensions.AI.Evaluation: precision, recall, citation usefulness, and source overlap offline, the quality judges with a model through a judge client for reasoning deployments, judge failures that stop the run, stored results with an HTML report, a retrieval optimizer, prompt revision, a revision gate that also counts retrieval misses and invalid citations, and an offline quality gate test. Activity: a custom judge for answer focus. | LLM-judge scores for groundedness, relevance, retrieval, and completeness, and a comparison of prompt versions. |
| 10 | `Chapter10` | Microsoft Agent Framework: `ChatClientAgent` agents for triage and knowledge with typed tools and strict schemas, one agent per name with each run's tools in its run options, sessions per conversation, agent, and engineer with a version check, failure classification, run records, a drafting agent that takes its facts from the knowledge agent's cited answer, scoped keyed agents, and a schema update for older databases. Activity: an escalation agent with tests. | Real tool calls chosen by the model, and how it phrases a grounded draft. |
| 11 | `Chapter11` | A multi-agent workflow over the Chapter 10 agents with durable, versioned state, checkpointed retry that stops a failed step and answers 503 with `Retry-After`, handoffs to escalation, resume after a restart for the engineer who started the run, an API controller, and the same steps as an Agent Framework workflow with checkpoints. Activity: idempotency keys per engineer. | End-to-end latency of a full run and the token cost of each step. |
| 12 | `Chapter12` | OpenTelemetry for the GenAI spans of Microsoft.Extensions.AI and Agent Framework, domain events for run records and workflow steps, deterministic RAG integration tests, cost monitoring from every model call's usage, an embedding cache, readiness that reads a background AI probe instead of waiting for a model, workload identity credentials, the engineer from a validated token with the API's scope, a container image, CI, and Kubernetes manifests with a schema job and the restricted pod security settings. Activity: a file telemetry collector. | Real latency percentiles, token costs at your rates, and traces in the Aspire dashboard. |

## Notes

- Each `ChapterNN` folder replaces the previous one, so type names repeat on purpose. Open one checkpoint at a time.
- Projects pin exact package versions. Microsoft packages that ship with .NET 11, such as Entity Framework Core, System.Numerics.Tensors, and Microsoft.AspNetCore.Authentication.JwtBearer, use 11.0.0. Other libraries follow their own version lines; at the time of writing these are Microsoft.Extensions.AI 10.11.0, OpenAI 2.14.0, OllamaSharp 5.5.0, Microsoft.SemanticKernel.Core 1.81.0, Polly.Extensions 8.8.1, Azure.Identity 1.21.0, Azure.Search.Documents 12.0.0, Microsoft.Extensions.AI.Evaluation, .Quality, and .Reporting 10.11.0, Microsoft.Agents.AI and Microsoft.Agents.AI.Workflows 1.26.0, and the OpenTelemetry packages 1.20.1. Test projects use xunit 2.9.3, xunit.runner.visualstudio 4.0.1, Microsoft.NET.Test.Sdk 18.11.0, and Moq 4.21.0.
- No file in this folder contains a key or a connection secret. `.gitignore` excludes build output, local databases, evaluation results, and the Chapter 12 activity's telemetry files.

Found a problem in the code? Contact us at [unqbd.com/Contact](https://www.unqbd.com/Contact).
