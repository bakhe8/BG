# BG Enterprise Re-Evaluation Brief

Use this brief to keep every external model focused on the same real goal.

## Final Goal

The goal is not to say:

- the UI is good or bad
- the code is clean or messy

The goal is to produce a full institutional operating picture that answers:

- what truly works today
- what looks right but is not enterprise-grade
- what is still missing for a real enterprise system
- where failure exists:
  - documentation
  - implementation
  - or the link between them
- what the gaps are by role, by screen, and by state
- what the target end-state should be
- how to reach it without breaking the current architecture

## The 7 Evaluation Axes

### Axis A
- architecture and boundaries
- platform decisions
- independence now, integration later

### Axis B
- operational model
- role/state behavior
- exception and recovery coverage

### Axis C
- UI / UX by surface type:
  - Workspace
  - Queue
  - Drawer
  - Dossier
  - Admin Grid
  - Form Editor

### Axis D
- domain and data model fitness
- auditability
- traceability
- model completeness

### Axis E
- enterprise closure topics:
  - RTL
  - concurrency
  - notifications
  - search
  - analytics
  - error recovery
  - legal print preview

### Axis F
- OCR usefulness
- review contract
- provenance and evidence visibility
- path to enterprise-safe OCR

### Axis G
- production readiness
- milestone closure
- what still blocks enterprise maturity

## Required Evaluation Method

Every serious model session should follow these layers:

1. truth reading
   - docs map
   - source documents
   - execution plans
2. real code reading
   - pages
   - partials
   - services
   - contracts
   - models
   - authorization
   - tests
3. live behavior reading
   - happy paths
   - empty states
   - blocked states
   - exception flows
   - recovery paths
4. alignment
   - docs vs code vs UI
5. institutional synthesis
   - gap
   - target state
   - migration path

## Required Deliverables

Every full program evaluation should aim to produce:

- Executive Assessment
- Documentation vs Code vs UI Alignment Report
- Role-by-Role Operational Audit
- Surface-by-Surface UX Audit
- Domain & Model Audit
- Enterprise Gap Catalogue
- Target Blueprint Pack
- Program Roadmap
- Enterprise Scorecard

## Mandatory Per-Point Format

For every important point:

1. current reality
2. evidence
3. why it is not enterprise-grade
4. target state
5. blueprint
6. access path
   - quick fix
   - structural fix
   - enterprise fix
7. priority
8. cost
9. risk

## Non-Negotiable Constraints

- do not propose a rewrite by default
- preserve:
  - ASP.NET Core 8
  - Razor Pages
  - PostgreSQL
  - IIS
  - server-driven baseline
- focus on:
  - decision clarity
  - operational throughput
  - governance safety
  - official output trust
  - recovery maturity
  - enterprise scalability

## Practical Sequencing

1. current truth
2. gaps and failures
3. target enterprise picture
4. staged transition path

The runtime prompts for `gpt4`, `gemini`, and `claude` should all stay aligned
to this brief while keeping their own specialization.
