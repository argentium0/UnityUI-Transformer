---
description: Enforces high-quality, modern UI design standards for both the Figma plugin and generated Unity UI.
globs: *.html, *.css, *.uxml, *.uss, *.tsx, *.jsx
---
# High-Fidelity UI Standards

To ensure the UI looks polished and professional across all tiers:

### Tier 1: Figma Plugin UI (HTML/Preact)
* **Native Feel:** Mimic Figma's native UI. Use the `Inter` font family, 11px to 12px font sizes for standard text.
* **Color Palette:** Use Figma's native tokens if possible, or a clean minimalist palette (e.g., `#18A0FB` for primary buttons, `#333333` for text, `#F5F5F5` for backgrounds).
* **Layout:** Utilize Flexbox for alignment. Keep padding consistent (8px or 16px grid). Use subtle borders (`1px solid #E5E5E5`) and a border-radius of `4px` or `6px` on interactive elements.
* **Feedback:** Always include visual loading states (e.g., a subtle spinner or progress bar) when the "Sync" process is running.

### Tier 3: Unity UI Toolkit (UXML/USS)
* **Flexbox Supremacy:** Rely entirely on UI Toolkit's Yoga layout. Avoid absolute positioning unless explicitly mapping a legacy constraint.
* **Token Application:** Ensure USS variables (`--theme-color-primary`, `--spacing-md`) are generated and utilized to keep the UI strictly consistent with the Figma source.
