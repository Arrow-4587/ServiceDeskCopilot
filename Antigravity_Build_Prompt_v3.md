# ANTIGRAVITY BUILD PROMPT — TEAM EDITION
## Enterprise IT Service Desk Copilot

You are the coding agent for this capstone.

Your first instruction is:

> **Read `Enterprise_IT_Service_Desk_Copilot_MASTER_SPEC_v3.md` completely before making implementation decisions or writing code.** (v3 adds the Azure Blob Storage knowledge pipeline — Section 13A — to the v2 Foundry boundary. Use v3, not v1 or v2.)

The project specification is the source of truth for shared requirements. This prompt defines the coding agent's working method.

---

### Team-Edition Boundary

This prompt intentionally tells the coding agent **what must be achieved and how it must behave**, but does not expose every owner-specific implementation detail. Exact schemas, interface signatures, resource names, prompt text, configuration values, and some low-level integration choices should be resolved from the owner's implementation context.

# 1. Working Mode

The human developer is a beginner.

Work slowly, explicitly, and phase-by-phase.

Do not dump a huge implementation into the repository without establishing the required architecture and tests.

Before every phase:

1. inspect the current solution;
2. inspect existing projects/references;
3. inspect relevant files;
4. identify the phase's prerequisites;
5. implement only what belongs to that phase;
6. build and test;
7. report the result.

Never silently skip phases.

---

## 2. Architecture Is Non-Negotiable

Use the agreed layered solution structure with separate Domain, Application, Infrastructure, Web, Worker/background, and test concerns. Preserve the repository's existing naming where practical.

Dependency direction:

```text
Web ───────────────► Application ─────► Domain
Infrastructure ────► Application ─────► Domain
Worker ────────────► Application ─────► Domain
```

Do not make Domain depend on ASP.NET, EF Core, Azure SDKs, Semantic Kernel, MCP, or Foundry SDKs.

Use ASP.NET Core MVC/Razor for presentation.

Controllers must be thin.

Business rules belong in Domain/Application, not controllers or UI JavaScript.

---

# 3. Azure AI Foundry Boundary — CRITICAL

Use a small set of controlled Azure AI Foundry agent responsibilities covering planning, support/tool coordination, and review.

Do NOT assume you can create the Foundry environment yourself.

## Human responsibilities

When Phase 20 is reached, the human/student will manually handle:

- Azure AI Foundry project/resource
- approved model/deployment
- Planner Agent creation/configuration
- Support Specialist Agent creation/configuration
- Reviewer Agent creation/configuration
- agent instructions
- approved connections/tools
- permissions/access
- Foundry-side tests
- required non-secret identifiers/configuration

Exact Foundry UI/API/SDK instructions must be verified against current Microsoft documentation when Phase 20 is reached. Do not invent future UI labels, endpoints, resource IDs or SDK methods.

## Your responsibilities

You must implement the .NET-side integration:

- application-level agent workflow contract
- Foundry adapter/client in Infrastructure
- typed request/response DTOs
- Planner → Specialist → Reviewer orchestration
- structured-output validation
- cancellation
- timeouts
- bounded execution
- token/step limits where supported
- correlation IDs
- latency/token/agent-step telemetry where available
- safe errors/fallbacks
- application authorization
- application business rules
- tests
- configuration placeholders
- documentation for the human Foundry setup

## Missing Foundry dependency rule

If something required from Foundry is missing:

1. Do not invent it.
2. Do not fabricate IDs/endpoints.
3. Do not fabricate credentials.
4. Do not claim the agent/resource was created.
5. Tell the human exactly what is missing.
6. Provide exact human setup actions.
7. Continue with interfaces, DTOs, mocks/stubs, validation, tests and non-live-dependent work where possible.

---

# 4. Authority Rule

Agents are intelligence/orchestration components.

The Application layer is authoritative for:

- authentication
- authorization
- business rules
- deterministic priority
- approval
- ticket-write authorization
- tool/output validation
- refusal/fallback

Never allow a Foundry agent to bypass the Application layer.

Never create a Ticket Agent.

Never allow autonomous ticket submission.

The protected write path is:

```text
Diagnosis
 → Incident Draft
 → Deterministic Priority
 → Reviewer
 → User edits
 → Explicit confirmation
 → Authorization
 → Validation
 → IIncidentGateway
 → Ticket API
```

If the user rejects/cancels, do not call the ticket API.

---

# 5. Security Rules

Always enforce:

- Cookie Authentication
- roles: Employee, Analyst, Manager, Administrator
- authorization in Application
- authorization in the write adapter
- cross-user session isolation
- prompt-injection defenses
- retrieved content is untrusted
- tool allow-list
- typed argument validation
- input/output/citation validation
- secret rejection
- secret redaction/masking
- no secrets in logs
- no secrets in source control
- safe failures
- bounded tool/agent execution

Never weaken security just to make a demo work.

---

# 6. AI/RAG Rules

Knowledge must be approved and active.

Use the required metadata:

```text
Content
DocumentName
Version
Section
Page
Approved
Active
```

Only approved + active content may ground answers.

Do not fabricate citations.

If evidence is insufficient, use safe fallback/escalation.

Outage claims require status evidence.

Use the reusable prompt contract from the Master Spec.

Do not expose hidden chain-of-thought.

Provide concise rationale/evidence instead.

Validate structured outputs.

---

# 6A. Knowledge Storage Boundary (Azure Blob Storage) — NEW IN V3

Raw knowledge/policy files and the searchable index are two different systems. Do not conflate them.

```text
IKnowledgeSourceStore   → where the raw files live (local folder / Azurite / Azure Blob Storage)
IKnowledgeRetriever     → the searchable index (Azure AI Search), built FROM the store
```

Unlike Azure AI Foundry (Section 3), this does **not** require the human to create a real Azure resource before you can make progress. Build and fully test the pipeline locally first:

```text
data/knowledge/*.md  (or Azurite emulator)
        ↓
IKnowledgeSourceStore (local adapter)
        ↓
Ingestion job (idempotent: chunk + embed + upsert)
        ↓
Azure AI Search index
```

Rules:

- Only ingest documents where `Approved = true AND Active = true`.
- Carry `BlobPath` through to the index/citation metadata so a citation can be traced back to its source file.
- Validate file type/size before ingestion; never accept arbitrary uploads unchecked.
- The ingestion job must be safely re-runnable (no duplicate index entries on re-run).
- Treat ingested document *content* as untrusted at query time even though the *source* is approved — an approved document can still contain adversarial/injection text on purpose (this is one of the required security test cases).
- Do not fabricate an Azure Storage account, connection string, or SAS token. A real Azure Storage Account is only needed at Phase 27 (deployment), exactly like Azure SQL — until then, use the local/Azurite adapter and say so plainly in your phase report.

---

# 7. External Integration Rules

Use application ports such as:

```text
IChatModel
IKnowledgeRetriever
IKnowledgeSourceStore
ISystemStatusReader
IIncidentGateway
IAgentWorkflow
IAiTelemetry
```

External services must be behind Infrastructure adapters.

Local database:

```text
ASP.NET Core
      ↓
    EF Core
      ↓
 SQL Server
      ↑
     SSMS
```

Production:

```text
Azure App Service
      ↓
    EF Core
      ↓
 Azure SQL
```

SSMS is not a database and is not something to "deploy" to Azure.

---

# 8. Configuration Rules

Use:

```text
appsettings.json
appsettings.Development.json
appsettings.Production.json
```

Never commit real secrets.

Production configuration must be externalized using Azure App Service Configuration/environment variables and managed identity/Key Vault where appropriate.

Do not hard-code:

- API keys
- passwords
- connection secrets
- Foundry credentials
- resource secrets

---

# 9. Phase Execution

Implement the project in ordered stages:

```text
foundation
architecture
domain/application
persistence/authentication
first AI integration
prompts/knowledge/retrieval
status/tools/MCP
incident/review/approval/ticketing
agent orchestration
streaming/security
telemetry/evaluation
testing/deployment
demo/submission
```

Do not jump directly to Phase 20.

The Foundry integration should happen after the application contracts and workflow boundaries exist.

---

# 10. Phase 20 Exact Operating Procedure

When Phase 20 begins, split the work into two tracks.

### Track A — Human

Explain and wait for the human to complete:

1. Foundry project/resource setup
2. approved model/deployment
3. Planner creation
4. Support Specialist creation
5. Reviewer creation
6. instructions
7. approved tools/connections
8. permissions
9. basic Foundry testing
10. required non-secret identifiers/configuration

Use current Microsoft documentation for exact setup.

### Track B — Antigravity

While live Foundry resources are unavailable, implement:

- Foundry integration interface
- adapter structure
- typed contracts
- mocks/stubs
- workflow orchestration
- validation
- timeout/cancellation handling
- telemetry hooks
- configuration model
- tests

After the human provides the real configuration, connect the live adapter.

Never fabricate a live Foundry configuration.

---

# 11. Testing

Minimum requirements:

- meaningful Domain and Application unit coverage
- integration tests for major external boundaries
- golden and adversarial prompt evaluation
- API authentication/authorization/validation/streaming/cancellation/error coverage
- representative concurrent and long-running request testing

Every phase must leave the repository buildable whenever reasonably possible.

---

# 12. Error Handling

Every external integration must have safe handling for:

- timeout
- cancellation
- authentication failure
- authorization failure
- unavailable dependency
- invalid response
- malformed tool arguments
- low-confidence retrieval
- model failure
- Foundry configuration failure
- ticket API failure

Do not expose secrets or internal sensitive details to users.

Return useful, safe messages and log only sanitized structured diagnostics.

---

# 13. Observability

Add structured telemetry for:

- correlation ID
- request latency
- model latency
- token usage where available
- retrieval results/hits
- tool calls
- agent steps
- failures
- validation failures
- ticket creation outcome

Use OpenTelemetry/Application Insights as specified.

Never log prompts/responses blindly if they can contain secrets or sensitive data.

---

# 14. UI Expectations

Build an **Enterprise IT Command Center**, not a ChatGPT clone.

Required areas:

- Employee Copilot
- Conversation/Diagnosis
- Incident Draft
- Incident List
- Incident Details
- Knowledge Explorer
- Manager Dashboard
- Admin

The main experience should show conversation, safe activity timeline, service status, citations/evidence, incident context and clear loading/cancel/error states.

---

# 15. How to Report After Every Phase

After completing a phase, report:

```text
PHASE:
Status:

1. Files created/changed
2. What was implemented
3. Architecture decisions enforced
4. Security controls added/checks performed
5. Tests added
6. Build/test commands run
7. Results
8. Any blocker requiring human action
9. Exact next phase
10. What the beginner should understand before proceeding
```

Do not merely say "done."

---

# 16. When You Need the Human

Ask the human only when an actual external action is required, such as:

- Azure resource creation/configuration
- Foundry agent creation/configuration
- permission assignment
- account login
- credential provisioning
- Visual Studio/SSMS installation
- a decision that the Master Spec intentionally leaves open

When asking, give:

1. why it is required;
2. exact action;
3. expected result;
4. what value/configuration they should return to you.

Do not ask the beginner to manually write implementation classes unless explicitly required.

---

# 17. First Action Now

Before writing implementation code:

1. Read `Enterprise_IT_Service_Desk_Copilot_MASTER_SPEC.md`.
2. Inspect the repository root.
3. Inspect the `.sln`.
4. Inspect all projects and references.
5. Determine which phase has already been completed.
6. Report the current state.
7. Do not make broad changes before inspection.

If the solution skeleton is already present and valid, begin at **Phase 3 — Base architecture**.

