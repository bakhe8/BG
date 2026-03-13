# UX Audit Notes

## Stage 1 — System Understanding

### System UX Overview
The BG system is a **standalone, document-centric operational platform** designed for the lifecycle management of bank guarantees. It transitions from a "Document Intake" phase (where physical/PDF sources are digitized) to a "Request/Approval" phase (where business logic and workflows are applied). The UI is characterized by **high-density "Workspace" pages** that attempt to consolidate all relevant functionality for a specific role into a single view.

### User Roles Summary
1.  **Document Intake (Operator)**: High-frequency data entry. Responsibilities include scanning, OCR verification, and record creation. Needs high precision and speed.
2.  **Request Owner**: Occasional user. Responsibilities include creating requests (Extension, Release, etc.) and monitoring their personal pipeline. Needs clarity on status and next steps.
3.  **Approver (Managerial/Executive)**: Decision-maker. Responsibilities include reviewing "Dossiers", verifying history (ledger/signatures), and approving/rejecting. Needs "Glanceability" and clear risk flags.
4.  **Dispatcher**: Administrative. Focuses on the "Outbound" phase—printing, mailing, and recording bank delivery.

### Operational Goals
- **Digitization Accuracy**: Ensuring data extracted from bank documents is correct via human-in-the-loop review.
- **Workflow Integrity**: Enforcing a strict, configurable sequence of approvals and signatures.
- **Auditability**: Maintaining a complete "Ledger" and "Timeline" of every action taken on a guarantee.
- **Closure**: Successfully delivering signed letters to banks and recording the "Final" state of a guarantee.

---

## Stage 2 — UX Audit (Draft)

### Unnecessary Complexity
- **Workspace Overload**: The "Workspace" pages often contain informational panels (Pipeline steps, Quality gates, Future integration notes) that are static and likely irrelevant to daily operations after the initial onboarding.
- **Nested List Redundancy**: In the Approval Queue, showing the full "Prior Signatures", "Attachments", and "Timeline" list *inside each card* in a long scrollable list creates massive vertical bloat and repetition.

### Excessive Steps / Cognitive Friction
- **Context Switching (Actor Loading)**: The "Load Actor" feature, while flexible for testing or delegation, adds a layer of "Am I acting as myself or someone else?" friction.
- **Intake Flow**: The "Extract" -> "Manual Review" -> "Save" flow is clear, but the UI is split across many panes, requiring significant eye-tracking movement across a wide screen.

### Cognitive Friction Points (Initial List)
1. **Visual Hierarchy**: Data pairing (DLs) and Pill lists are used extensively without clear differentiation between "Primary Status" and "Secondary Metadata".
2. **Action Prominence**: Secondary actions (like "Load Actor") often occupy similar visual weight to primary workspace actions.
3. **Redundant Instructions**: Large "Instructional" panels (e.g., Stage 7 "Pipeline") consume valuable real estate on every page load.

---

## Stage 3 — User Workflow Mapping

### Workflow 1: Document Intake
- **Goal**: Transition a physical bank guarantee into a verified digital record.
- **Steps**:
    1. **Scenario Selection**: Choosing the core intent (New, Extension, etc.).
    2. **File Ingestion**: Uploading the source document.
    3. **Extraction**: System-driven OCR or native text extraction.
    4. **Human Verification**: Reviewing system-suggested values against the source PDF.
    5. **Categorization**: Assigning the guarantee to a specific business category (Contract vs. Purchase Order).
    6. **Commitment**: Saving the record to the ledger.
- **Decision Points**: Selection of extraction route (if auto-fail); Overriding OCR errors; Correctly identifying the guarantee category (critical for downstream workflow).
- **Completion State**: Active Guarantee is registered in the system.

### Workflow 2: Request Creation
- **Goal**: Initiate a business action (e.g., Release or Reduction) on an existing guarantee.
- **Steps**:
    1. **Contextual Search**: Finding the guarantee in the owner's dashboard.
    2. **Action Selection**: Choosing the request type.
    3. **Detail Entry**: Inputting requested amount, expiry, and notes.
    4. **Submission**: Handing off to the approval pipeline.
- **Decision Points**: Choosing between similar request types; Justification notes for approvers.
- **Completion State**: Request is "Pending Approval".

### Workflow 3: Approval & Governance
- **Goal**: Authoritative review and decision on a request.
- **Steps**:
    1. **Queue Browsing**: Identifying high-priority or blocked items.
    2. **Deep Inspection (Dossier)**: Reviewing history, attachments, and prior signatures.
    3. **Conflict Resolution**: Addressing governance blocks (e.g., same signature level conflicts).
    4. **Execution**: Approving, Returning for correction, or Rejecting.
- **Decision Points**: Validation of attachments; Checking ledger history for precedent; Resolving policy violations.
- **Completion State**: Request is "Approved" or "Closed".

---

## Stage 4 — UX Principles Evaluation

### Observed UX Principles
1. **Functional Isolation (Workspace Model)**: The system effectively silos complexity by role. A "Document Intake" user is not burdened with "Approval" logic.
2. **Explicit Provenance**: Every piece of data (especially from OCR) is labeled with its origin (Confidence score, Extraction Route).
3. **Data Integrity (Human-in-demand)**: The system enforces a manual review gate for critical fields, prioritizing accuracy over pure automation speed.

### Missing or Violated UX Principles
1. **Progressive Disclosure (Violated)**: The system exposes deep details (full ledger, signature history, technical metadata) immediately. This leads to vertical bloat in list views (e.g., Approval Queue).
2. **Signal-to-Noise Ratio (Low)**: Instructional panels and static "Future scanner" notes occupy persistent screen real estate, distracting from active tasks.
3. **Information Tiering (Weak)**: Primary indicators (Status: Active) and secondary metadata (Linked by: System) often share similar visual prominence (Pills).

---

## Stage 5 — Interaction Model Analysis

### Interaction Summary
- **Navigation**: Flat and Centralized. Most roles operate on a single "Workspace" landing page. Deep dives are handled by "Dossiers" which mirror the workspace's card structure.
- **Creation/Editing**: Inline and persistent. Forms are always visible or "one click away" on the workspace, favoring high-frequency power users.
- **Actor Context**: A unique "Load Actor" pattern allows for context switching/impersonation, which is powerful but increases cognitive load regarding current permissions/scope.

### Interaction Inconsistencies
- **Detail Access**: "My Requests" uses inline expansion/display, whereas "Approvals" provides a "Dossier" sub-page.
- **Context Locking**: Some workspaces "Lock" the actor context automatically, while others require manual loading, creating an inconsistent mental model of user identity.

---

## Stage 6 — Current Interface Evaluation

### Strengths
- **Thematic Consistency**: The multi-theme support (Emerald, Slate, Sand) is well-integrated into the CSS architecture.
- **Modern Aesthetic**: Use of backdrop filters, gradients, and rounded corners creates a premium "Enterprise Tool" feel.

### Cognitive Load & Scalability Risks
- **Vertical Bloat**: In the "Approval Queue", cards expand based on the amount of history. For complex requests with multiple signatures/attachments, the queue becomes difficult to scan efficiently.
- **Gaze Shift**: The 3-column "Intake" layout requires the user to track data across the entire width of the screen (Source -> Verified -> Review), which is exhausting over long sessions.

---

## Stage 7 — Pattern Consistency Review

### Consistency Report
- **Buttons**: Highly consistent use of `action-button` classes and interaction states.
- **Forms**: Standardized `stack-form` layout ensures predictable field placement.
- **Notifications**: `notice-banner` is a reliable pattern for feedback and governance warnings.

### Pattern Fragmentation
- **Pill Saturation**: The system uses "Pills" for almost all secondary metadata. This saturates the UI with similar-looking elements, making it hard to distinguish between a "Role", a "Tag", and a "Status".
- **List Variability**: Three different list types (`stack-list`, `workflow-stage-list`, `plain-list`) have subtle differences in styling that may feel "unpolished" as the system grows.

---

## Stage 8 — UX Risk Assessment

### High Impact Risks
1. **The "Vertical Bloat" Failure**: The Approval Queue currently shows full history and attachments for *every* item in a list. As request histories grow to 5+ stages, the queue will become un-scannable, requiring excessive physical scrolling and increasing the chance of missing critical items.
2. **Governance Gridlock**: Governance blocks are handled via banner alerts. As business rules become more complex, the current UI fails to provide a "Conflict Resolution" path, leaving users stranded with a "Blocked" message and no clear next step.

### Medium Impact Risks
1. **Intake Gaze Shift**: The wide 3-column layout for Document Intake requires substantial eye tracking. For professional operators processing 50+ documents a day, this is a significant source of physical and cognitive fatigue.

### Long-term Structural Risks
1. **Screen-Size Sensitivity**: The system's heavy reliance on wide-screen horizontal forms (3 columns) and side-by-side splits creates a structural barrier to mobile or tablet adoption, which may be required for executive approvals.
2. **Technical Metadata Dominance**: The system mixes technical provenance (confidence scores, extraction routes) with business data at the same visual level. Over time, "Engineering Noise" will eventually drown out "Business Signal".



