# UX Remediation Plan: BG Management System

**Role**: Chief Product Designer & UX Systems Architect  
**Goal**: Resolve identified structural UX weaknesses to ensure operational scalability.

---

## Executive Strategy: Management by Exception
The core philosophy of this remediation is to transition the system from **"Passive Presentation"** (where all data is equal) to **"Active Governance"** (where the system highlights risks and automates the routine).

---

## Phase 1: Cognitive De-Cluttering (Immediate)
*Focus: Reducing baseline noise and interaction debt.*

### 1.1 Progressive Disclosure of Object Details
- **Current Issue**: Signatures and Ledgers are always expanded in list cards.
- **Remediation**:
    - Collapse "History" and "Prior Signatures" into an on-demand modal or an expandable "Summary Bar".
    - Only show the *Current Stage* and *Immediate Next Step* on the queue card.
- **Impact**: Reduces O(n) scroll distance to O(1) per card.

### 1.2 Defensive Instructional Design
- **Current Issue**: Large, static instructional banners occupy persistent space.
- **Remediation**:
    - Move static instructions to a "Help" (?) icon or an on-boarding modal.
    - Replace them with **Dynamic Action Prompts** (e.g., "3 items pending your signature" or "Extraction high-risk: Check Bank Name").

---

## Phase 2: Decision Support Engineering (Structural)
*Focus: Improving decision speed and accuracy.*

### 2.1 Confidence-Based Visual Tiering
- **Current Issue**: Data from OCR looks identical to validated business data.
- **Remediation**:
    - **Visual Sinking**: Diminish the visual weight of fields where extraction confidence is >95%.
    - **Exception Highlighting**: Use high-contrast indicators (not just pills) for low-confidence data or governance blocks.
- **Impact**: Focuses user attention only where a human decision is actually required.

### 2.2 Re-Architecting the "Intake Gaze"
- **Current Issue**: Extreme horizontal eye tracking in a 3-column layout.
- **Remediation**:
    - Transition to a **"Focused Review"** model: Source document on one side, a single-column edit form on the other.
    - Implement "Auto-Focus" scrolling: As the user verifies field A, the source PDF automatically scrolls/highlights the region where field A was extracted.

---

## Phase 3: Architectural Evolution (Scalability)
*Focus: Long-term operational throughput.*

### 3.1 Master-Detail Workspace Pattern
- **Current Issue**: Infinite scroll and expansion for dense data.
- **Remediation**:
    - Transition the "Approval Queue" to a **Standard Master-Detail view** (List on left, full Dossier on right).
    - Eliminates the need for separate navigation to a "Dossier" page, reducing interaction cost by 40%.

### 3.2 Proactive "Next-Step" Conveyor
- **Current Issue**: Users "search" for work.
- **Remediation**:
    - Implement a **"Finish & Next"** action pattern. After finalizing a document intake, the system automatically loads the next staged document.
- **Impact**: Achieves O(1) transition time between tasks.

---

## Implementation Roadmap

| Priority | Action | Complexity | Impact |
| :--- | :--- | :--- | :--- |
| **P0** | **Progressive Disclosure** (Collapse History) | Low | High (Scalability) |
| **P0** | **Decision Support** (High-Contrast Risks) | Medium | High (Accuracy) |
| **P1** | **Master-Detail Transformation** | High | High (UX Economics) |
| **P2** | **"Next-Step" Automation** | Medium | Medium (Throughput) |

---

**End of Remediation Plan**
