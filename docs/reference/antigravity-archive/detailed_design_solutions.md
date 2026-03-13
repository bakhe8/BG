# Detailed Design Solutions: BG Management System

**Author**: Chief Product Designer & UX Systems Architect  
**Goal**: Provide concrete, high-fidelity design specifications for system remediation.

---

## 1. Focused Intake Review (Solution for Gaze Shift)

The 3-column layout is replaced with a **Dual-Pane Focused Workspace**.

### UI Specification

- **Left Pane (60%)**: A persistent PDF viewer with "Deep-Link Highlighting".
- **Right Pane (40%)**: A single-column, scrollable form for data verification.
- **Visual Hierarchy**: The large scenario cards are moved to a "Pre-Intake" modal or a condensed sidebar to maximize vertical space for the active task.

### Focused Intake High-Fidelity Mockup

![Focused Intake Mockup](focused_intake_mockup_1773343212426.png)

*Figure 1: Focused review model featuring side-by-side verification and confidence-based tiering.*

### Key Component Behaviors

- **Confidence Sync**: Fields with low OCR confidence (e.g., <80%) are highlighted with a `field-status--warning` border.
- **Scroll Synchronization**: Clicking a form field on the right automatically pans the PDF viewer on the left to the corresponding coordinate of the extracted text.

---

## 2. Master-Detail Approval Workspace (Solution for Vertical Bloat)

The infinite-scroll card list is replaced with a **Structural Master-Detail Grid**.

### Detail View UI Specification

- **Master List (Left)**: Condensed "Summary Cards" showing only: Request Type, Urgency, and Primary ID.
- **Detail View (Center/Right)**: The full "Dossier" content, including the timeline and attachments, is displayed here only for the selected item.

### Master-Detail High-Fidelity Mockup

![Master-Detail Approvals](master_detail_approvals_mockup_1773343229196.png)

*Figure 2: Master-Detail pattern for approvals, enabling rapid context switching without vertical scaling issues.*

### Interaction Logic

- **Keyboard Navigation**: Users can use `Up/Down` keys to traverse the master list and `Enter` to open the full dossier.
- **Batch Selection**: The master list supports multi-select checkboxes for routine, high-confidence approvals.

---

## 3. Interaction Economics Improvements

### The "Conveyor Belt" Pattern

- **Logic**: After the user clicks "Save/Finalize", the system does not return to a blank state.
- **Behavior**: It automatically loads the **"Next Most Urgent"** document or request from the user's queue.
- **Visual implementation**: A "Success & Next" transition toast appears briefly while the new context loads.

---

## 4. Visual Component Standardization

### Status Indicators (Replacing Pill Monotony)

- **Primary Actions**: Emerald Green, high contrast.
- **Governance Blocks**: Ruby Red, sharp corners, bold text.
- **Provenance/Technical metadata**: Muted Grey, smaller font size, non-pill shape (simple labels).

---

### Final Implementation Note

These designs are optimized for the existing "Sand" theme but introduce mandatory high-contrast elements to ensure accessibility and operational speed.
