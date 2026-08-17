# PRD — Figma → Unity UI Importer ("Figma2Unity")

## 1. Summary

A tool that takes any Figma design (frame, page, or whole file) and converts it into a native, importable Unity UI — matching every visible component and staying visually and stylistically consistent with the source — without anyone hand-building UI inside the Unity Editor.

## 2. Problem statement

Designers work in Figma; Unity engineers currently rebuild every screen from screenshots or specs by hand. This is slow, error-prone, and drifts from the design source of truth every time Figma is updated. There is no reliable, complete, automated path from a Figma file to a ready-to-use Unity UI hierarchy.

## 3. Goals

- **Structural completeness:** every visible Figma node type is represented by *something* in Unity output — nothing is silently dropped.
- **Visual fidelity:** generated Unity screens match the Figma export within a defined pixel-difference tolerance.
- **Style consistency:** colors, type styles, spacing, and effects are driven from a single set of reusable design tokens, not duplicated per-instance — so a rebrand or token change propagates everywhere.
- **Fast iteration loop:** minutes, not days, from a Figma publish to a usable, up-to-date Unity prefab/UI document.
- **No manual UI construction in Unity** for any supported node type — the tool *is* the UI work.

## 4. Non-goals (v1)

- Faithful reproduction of Figma prototype interactions, transitions, or Smart Animate.
- Full two-way sync (pushing Unity edits back into Figma).
- Runtime, in-game live-reloading from Figma (this is an editor-time tool).
- Support for FigJam boards, embedded widgets, or third-party Figma plugin output.

## 5. Users / personas

- **Designer (Figma owner):** wants confidence that what they publish ships unmodified.
- **Unity/gameplay engineer:** wants a ready prefab or UI Document they can wire up logic to, without touching visuals.
- **Tech artist / pipeline owner:** owns and extends the tool, needs it pluggable for new node types, custom shaders, and studio-specific conventions.

## 6. Scope — Figma feature coverage matrix

| Figma feature | Priority | Fidelity note |
|---|---|---|
| Frame / Group hierarchy | P0 | 1:1 structural mirror, layer names preserved |
| Auto-layout (direction, gap, padding, hug/fill/fixed sizing, alignment) | P0 | Primary driver for choosing UI Toolkit (flexbox-based) as the generation target |
| Legacy constraints (left/right/top/bottom/scale/center) | P0 | Needed for the uGUI export path and any non-auto-layout frames |
| Rectangle / Ellipse / Polygon / Star | P0 | Native shapes where possible; SVG fallback otherwise |
| Vector nodes & boolean operations | P1 | Exported as SVG, imported via Vector Graphics package |
| Text — multiple styles per node, rich text spans, auto-resize modes | P0 | Mapped to TextMeshPro; mixed-style runs may need TMP rich-text tags |
| Image fills | P0 | Exported raster at 1x/2x/3x |
| Solid fill / gradients (linear, radial, angular) | P0 | Native Unity gradient support where the target supports it; otherwise baked texture |
| Stroke (incl. dashed) | P1 | Native where supported; dashed strokes may need a baked fallback |
| Corner radius, incl. independent per-corner | P0 | Drives 9-slice metadata for raster fallbacks |
| Effects: drop shadow / inner shadow / layer blur / background blur | P1 | No native Unity UI equivalent — needs a shader-based approximation or baked-sprite fallback (see Risks) |
| Opacity / blend modes | P1 | Direct opacity supported; exotic blend modes may require baking |
| Components, instances, and variant properties | P0 | Instances resolve to prefab variants (uGUI) or state-toggled USS classes (UI Toolkit) |
| Component overrides on instances | P1 | Must be preserved, not silently reset to the master component |
| Interactive states via variants (hover/pressed/disabled) | P1 | Mapped to Unity's `Selectable`/pseudo-class states where a clear mapping exists |
| Clipping / masks | P1 | Native clipping where the target supports it; baked fallback otherwise |
| Z-order | P0 | Preserved via generation order |

Each item ships behind a clear "supported / fallback / unsupported (logged)" status — never a silent drop.

## 7. Functional requirements

- **FR1 — Export scope:** the Figma plugin can export a selected frame, an entire page, or the whole file.
- **FR2 — Versioned schema:** the IR schema is explicitly versioned and documented; the importer refuses (with a clear error) to read a mismatched major version.
- **FR3 — Token indirection:** design tokens (color/type/spacing/effect) are extracted once and referenced by ID from nodes, never inlined, so a token edit propagates to every screen on re-sync.
- **FR4 — Structural mirroring:** the Unity importer produces one generated hierarchy per Figma frame whose structure and layer order mirror the source 1:1, with deterministic name-sanitization and collision handling.
- **FR5 — Idempotent re-import:** re-running import on an already-generated screen updates it in place rather than duplicating it, and preserves any user-added components attached within a designated "safe zone" that the importer never overwrites.
- **FR6 — Graceful degradation:** unsupported or unrecognized node types are rasterized as an image fallback and logged as a warning — never silently dropped.
- **FR7 — Variant mapping is configurable:** component variants can map to either UI Toolkit state classes or uGUI prefab variants, selectable per project.
- **FR8 — Font handling is explicit:** fonts are auto-matched where possible; anything unmatched is flagged in the report for manual assignment — never silently substituted without disclosure.
- **FR9 — Post-import report:** every sync run produces a Markdown/HTML report summarizing nodes processed, fidelity warnings, missing fonts, and fallback rasterizations.

## 8. Non-functional requirements

- **Performance:** a ~200-frame file completes sync within a defined time budget (to be benchmarked; target order of minutes, not tens of minutes).
- **Fidelity target:** ≤2% average pixel-difference versus the Figma export at reference resolution, measured by the CI visual-regression harness.
- **Extensibility:** new node-type handlers are pluggable without modifying core tree-traversal code.
- **Determinism:** generated files have stable, diff-readable ordering so they're usable in source control and code review.
- **Cross-platform:** both the plugin build and the Unity package work on Windows and macOS.

## 9. Success metrics

- % of encountered node types round-tripped without falling back to a rasterized image.
- Average pixel-diff score across the visual-regression suite.
- Wall-clock time from "designer publishes in Figma" to "usable Unity prefab/UI Document."
- Engineer-reported hours saved per screen versus manual rebuilding.

## 10. Risks & mitigations

| Risk | Mitigation |
|---|---|
| Font licensing prevents automatic embedding | Auto-match where license permits; otherwise flag in the report for manual asset assignment rather than silently substituting |
| Some Figma effects (blur, layered shadows) have no native Unity UI equivalent | Ship a small shader/material library for common approximations; fall back to a baked sprite when no shader approximation is acceptable |
| Large files hit Figma API rate limits or export slowly | Chunk exports by page/frame; cache by content hash so unchanged nodes aren't re-exported |
| Generated output and hand-customized Unity work drift apart or get clobbered | Enforce an explicit "generated vs. hand-authored" boundary (safe-zone markers) that re-import never touches |
| Fidelity regressions creep in silently over time | CI-enforced visual-regression threshold blocks merges that exceed the pixel-diff budget |

## 11. Milestones

- **M0 — Spike:** single static frame, solid colors + text only → basic uGUI Image/Text. Proves the end-to-end path works at all.
- **M1 — MVP:** auto-layout, images, vector fallback, UI Toolkit (UXML/USS) generation, zip-based single-project sync.
- **M2 — Consistency layer:** design-token ScriptableObjects, font matching, post-import report.
- **M3 — Broader coverage:** uGUI secondary exporter, component variants mapped to interactive states.
- **M4 — Live workflow:** bridge server with auto-sync on save, visual-regression CI gate.
- **M5 — Hardening:** public package polish, documentation, sample project.
