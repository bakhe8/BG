# Institutional Design Alignment Report: KFSHRC

**Focus**: Aligning the BG System with the King Faisal Specialist Hospital & Research Centre (KFSHRC) Digital Identity.

---

## 🏛️ Institutional Identity Overview
Based on a diagnostic exploration of the KFSHRC digital ecosystem, internal systems are expected to mirror the institutional brand with high fidelity. The BG system, as an internal operational tool, should feel like a native extension of the KFSHRC software suite.

---

## 🎨 Design Token Alignment

### 1. Color Palette (Semantic Branding)
The current "generic green" in the BG system must be aligned with the official KFSHRC palette.

| Token | Institutional Value | Usage in BG System |
| :--- | :--- | :--- |
| **Primary Brand** | `#006847` (Deep Emerald) | Headers, Primary Actions, Brand Accents. |
| **Action Green** | `#1e7e34` | Hover states, Success indicators. |
| **Text Primary** | `#33354c` | High-contrast body text. |
| **Background Alt**| `#f8f9fa` | Surface separation in Workspaces. |

### 2. Typography (Professional Tone)
To achieve "Institutional Authority", the typography should be updated:
- **Arabic**: Adopt the **"KFSH"** font family (where available) or a modern institutional Naskh equivalent.
- **English**: **Noto Sans** or **Segoe UI** (as currently used) is acceptable but should be secondary to the Arabic brand identity.

### 3. Geometry & Radii
The KFSHRC design system favors **Modern Softness**:
- **Buttons**: Shift from standard rounded corners to **Pill-shaped (9999px)** for primary calls to action.
- **Cards/Panes**: Increase border-radius to **18px-22px** with extremely subtle shadows (`rgba(0,0,0,0.05)`).

---

## 🗺️ UX & RTL Alignment

### 1. Hierarchy of Information
KFSHRC systems exhibit a clear "Top-Down, Right-to-Left" priority. The **Operational Workspace** must ensure that:
- Critical status indicators (e.g., Confidence Chips) are always the first thing an Arabic reader sees (Right-aligned).
- Metadata labels follow the brand's minimalist labeling style.

### 2. "Management by Exception" in Institutional Context
The "Next Decision" philosophy aligns perfectly with medical/institutional efficiency. The design should:
- Use the **KFSHRC Red** only for critical governance blockers.
- Use **KFSHRC Emerald** for "Safe to Proceed" paths.

---

## 🚀 Recommended Style Updates (The "KFSH" Skin)

1. **Update CSS Variables**: Map `--accent` to `#006847`.
2. **Refine Buttons**:
   ```css
   .btn-primary { 
       border-radius: 999px; 
       background-color: #006847; 
   }
   ```
3. **Institutional Header**: Use the KFSHRC logo alongside the "BG Management System" title for co-branding.

**Conclusion**: Bridging the gap between a generic "Emerald" theme and the official KFSHRC brand will increase user trust and system adoption within the hospital's internal ecosystem.
