# Build Plan — Prompt-by-Prompt Guide for Antigravity IDE

This plan assumes `01-tech-stack.md` and `02-prd.md` live in `docs/` at the repo root so every agent can be pointed at them for grounding, instead of re-explaining the whole design in every prompt.

## How to work this plan in Antigravity

Antigravity gives you two surfaces:

- **Editor view** — a familiar AI-powered IDE (tab completions, inline commands, an agent side panel) for hands-on work and debugging generated output.
- **Manager surface** — where you spawn, orchestrate, and observe agents working asynchronously, review their **Artifacts** (plans, screenshots, recordings, verification output), and run agents in parallel across folders.

Recommended pattern for this project:

1. Run phases 0–1 as a single agent in the Editor view (foundational, sequential, easy to review line-by-line).
2. From phase 2 onward, use the Manager surface to run **one agent on `packages/figma-plugin/` and one agent on `unity/`** in parallel — they touch disjoint folders, so there's no merge conflict, and you can review each Artifact independently before merging.
3. Before letting an agent execute multi-file changes, ask it to propose a plan first and wait for approval — this is worth doing explicitly even though Antigravity produces plan Artifacts by default, since it gives you a clean checkpoint per phase.
4. After each phase, read the Artifact, actually open the generated files in Editor view, and only then move to the next phase's prompt.

---

## Phase 0 — Repo scaffolding

```
Create a monorepo called figma2unity with this structure:

figma2unity/
  docs/                (I will add 01-tech-stack.md and 02-prd.md here myself)
  packages/
    ir-schema/          TypeScript package, Zod-based schema definitions
    figma-plugin/        Figma plugin scaffold (manifest.json, TypeScript, esbuild config)
  unity/
    Packages/com.yourorg.figma2unity/   local UPM package skeleton with Editor/ and Tests/ folders
  .github/workflows/     placeholder CI workflow files

Set up TypeScript, ESLint, and Vitest for the packages/ workspace using npm workspaces.
Set up the Unity package skeleton with a valid package.json (UPM manifest) and an empty
assembly definition in Editor/. Don't implement any logic yet — just working, buildable
scaffolding with a passing "hello world" test in each TS package.
```

After this runs, drop the two doc files into `docs/` yourself so later prompts can reference them.

## Phase 1 — Define the shared IR schema

```
Read docs/01-tech-stack.md and docs/02-prd.md, specifically the "Figma feature coverage
matrix" in the PRD. In packages/ir-schema, define a versioned Zod schema for the
Intermediate Representation described in the tech stack doc:

- A root document with a schema version field, a list of design tokens
  (colors, type styles, spacing, effect styles — each with a stable ID), and a tree
  of nodes.
- A discriminated union node type covering: Frame, Group, Rectangle, Ellipse, Vector,
  Text, Image, ComponentInstance — matching the P0/P1 rows of the coverage matrix.
- Each node carries: id, name, type, visible, position/size, auto-layout properties
  (direction, gap, padding, sizing mode per axis, alignment), fill(s) referencing a
  token ID or an inline value, stroke, corner radius (per-corner), effects
  (referencing effect-style tokens), and children.
- Generate a JSON Schema export from the same Zod definitions so it can be shared
  with the Unity/C# side later.
- Write Vitest golden-file tests that construct a couple of sample documents and
  snapshot-validate them against the schema.

Propose the schema shape as a plan first before writing code.
```

## Phase 2 — Figma plugin: node traversal and IR export

```
In packages/figma-plugin, using the schema from packages/ir-schema, implement the
core traversal:

- On invocation, read the user's current selection (or the whole page if nothing is
  selected) using the Figma Plugin API.
- Recursively walk the node tree, mapping each Figma node type to the corresponding
  IR node type. For frames/groups with auto-layout, extract layoutMode, itemSpacing,
  padding, and primary/counter axis sizing modes.
- Extract Figma Variables and Styles (color, text, effect) into the IR's design-token
  list, and make node fills/strokes/effects reference token IDs wherever a node is
  bound to a variable or style, falling back to inline values otherwise.
- For any node type not yet covered by the schema, still emit an IR node with
  type "Unsupported" plus its original Figma type, rather than skipping it — this
  feeds the fallback rasterization path later.
- Build a minimal plugin UI (HTML+Preact) with a "Sync Selection" button that runs
  the traversal and shows a summary count of nodes processed and any "Unsupported"
  nodes found.

Bundle with esbuild. Show me the plan before implementing the traversal function.
```

## Phase 3 — Figma plugin: asset export and packaging

```
Extend the figma-plugin package:

- For any node whose fill is an image, or whose type is Vector/Boolean-operation,
  or whose IR type is "Unsupported", export it via node.exportAsync: PNG at 1x/2x/3x
  for images and unsupported nodes, SVG for vector nodes.
- Package the IR JSON document plus all exported assets into a single .f2u.zip
  (use a lightweight in-browser zip library compatible with the plugin's sandboxed
  iframe — no Node filesystem APIs are available here).
- Trigger a browser download of the zip from the plugin UI when "Sync Selection"
  completes, and show the per-node-type breakdown (supported / fallback-rasterized /
  unsupported) in the UI so the designer/engineer can see gaps immediately.
- Write a couple of integration-style tests using Figma's plugin API mocks if
  available, otherwise unit test the packaging logic with a fixture IR document.
```

## Phase 4 — Unity package: import and parsing

```
In unity/Packages/com.yourorg.figma2unity, add the Newtonsoft.Json dependency and:

- Implement a menu command "Figma2Unity > Import Sync Package..." that lets the
  user pick a .f2u.zip file, unzips it into a temp folder, and deserializes the IR
  JSON into C# model classes that mirror the schema from packages/ir-schema
  (read docs/01-tech-stack.md for the intended type mapping).
- Validate the schema version and fail with a clear error dialog on a major
  version mismatch, per FR2 in docs/02-prd.md.
- Copy exported image/SVG assets into a predictable Assets/ subfolder, scripting
  TextureImporter settings (sprite mode, pivot) for raster assets.
- Write EditMode tests that import a small fixture .f2u.zip and assert the parsed
  node tree matches expectations.

Propose the C# type layout before writing the importer.
```

## Phase 5 — Unity: UI Toolkit generator (primary target)

```
Add a generator in unity/Packages/com.yourorg.figma2unity/Editor that turns the
parsed IR tree into UXML + USS:

- One UXML file per top-level Figma frame, with a VisualElement hierarchy that
  mirrors the IR tree 1:1 in structure and naming (sanitize names, handle
  collisions deterministically per FR4 in the PRD).
- Map auto-layout properties onto USS flex properties (flex-direction, gap via
  margin since UI Toolkit's Yoga version needs checking, padding, align-items,
  justify-content) and hug/fill/fixed sizing onto flex-grow/flex-shrink/width-height.
- Generate one shared USS stylesheet per synced file (not per screen) so repeated
  styles are deduplicated, and reference design tokens (once Phase 6 exists) rather
  than hardcoding values.
- For "Unsupported" IR nodes, render them as an Image VisualElement using the
  rasterized fallback asset, and log a warning line to the import report.
- Write an EditMode test that generates UXML from a fixture IR document and asserts
  on the resulting VisualTreeAsset's element count and hierarchy shape.
```

## Phase 6 — Design tokens and font matching

```
Per docs/02-prd.md FR3 and FR8:

- Generate ScriptableObject assets for the token categories in the IR
  (ColorPalette, TypeRamp, SpacingScale, EffectStyle), one asset per token
  collection, keyed by the same token IDs used in the IR.
- Update the Phase 5 UXML/USS generator to reference these tokens (as USS custom
  properties / variables) instead of inlining values, so re-syncing after a token
  change updates every generated screen.
- For text nodes, attempt to match the Figma font family/weight to an existing
  TMP Font Asset in the project by name; if none exists, use the TMP Editor Font
  Asset Creator API to generate one from an available font file, and if no font
  file is available at all, leave a clearly flagged placeholder and record it as
  a "missing font" entry rather than silently substituting a default.
```

## Phase 7 — Fallback handling and the import report

```
Implement the post-import report described in FR6 and FR9 of docs/02-prd.md:

- During Phase 4-6's import/generation pass, accumulate a structured log of:
  nodes processed by type, nodes that fell back to rasterization and why,
  missing font matches, and any schema-version or validation warnings.
- Render this as a Markdown (and simple HTML) report written next to the
  imported assets, and show a summary dialog at the end of the import menu
  command with a link to open the full report.
- Add EditMode tests asserting that a fixture import with a deliberately
  "unsupported" node type produces the expected report entries.
```

## Phase 8 — Visual regression harness

```
Build a QA harness per docs/01-tech-stack.md's "Validation / QA tooling" section:

- A PlayMode test that loads a generated UXML screen into a UIDocument, waits a
  frame, and captures a screenshot via ScreenCapture.
- A small standalone script (Python + Pillow, or a .NET equivalent if you'd
  rather keep it in-repo as C#) that pixel-diffs that screenshot against the
  corresponding Figma PNG export from the same sync package, and fails if the
  difference exceeds 2%, matching the fidelity target in docs/02-prd.md.
- Wire this into .github/workflows so CI runs it headlessly (GameCI-style Unity
  Docker image) on every PR that touches the generator code.

Report back on what's actually feasible to run headlessly versus what needs a
local machine with Unity installed, before implementing the CI wiring.
```

## Phase 9 — uGUI secondary exporter

```
Add an alternate generation path (selectable per FR7) that turns the same
parsed IR tree into a uGUI Canvas/RectTransform prefab hierarchy instead of
UXML/USS:

- Translate auto-layout properties to Unity LayoutGroup components
  (HorizontalLayoutGroup/VerticalLayoutGroup + ContentSizeFitter) where a clean
  mapping exists, and fall back to computed anchor min/max + pivot + sizeDelta
  from Figma's legacy constraints where it doesn't.
- Map Text nodes to TextMeshProUGUI, Image fills to Image/RawImage with the
  9-slice border data from Phase 4, and component variants with
  hover/pressed/disabled states to Button/Selectable transition states.
- Reuse the same design-token ScriptableObjects from Phase 6 rather than
  duplicating a second token system.
- Share as much of the fallback/report logic from Phase 7 as possible between
  the two generators — flag in your plan if that needs refactoring the
  generator interface first.
```

## Phase 10 — Live bridge server (V2)

```
Per docs/01-tech-stack.md's Tier 2 "V2" section, add packages/bridge-server:
a Node.js + Fastify server exposing POST /sync (writes an incoming IR+assets
payload directly into a configured Unity project's import folder) and an
optional WebSocket /watch endpoint. Update the figma-plugin's UI to offer
"Sync via zip download" (existing) or "Sync live to localhost" (new) as two
explicit modes, defaulting to zip. Write integration tests that POST a
fixture payload and assert the files land correctly on disk.
```

## Phase 11 — Docs, sample project, packaging

```
Prepare figma2unity for real use:

- Write a top-level README covering install (Figma plugin + Unity UPM
  package), the sync workflow, and how to read the import report.
- Build a small sample Figma-derived Unity project under a samples~ folder in
  the UPM package demonstrating a synced screen with tokens, auto-layout, and
  at least one fallback-rasterized node, so new users can see expected output
  immediately.
- Tag a v1.0.0 release matching Milestone M5 in docs/02-prd.md, and record a
  screen capture Artifact walking through a full sync from Figma to Unity for
  the README.
```

---

## Tips for prompting Antigravity effectively on this project

- **Ground every prompt in the docs, don't restate them.** Reference `docs/01-tech-stack.md` and `docs/02-prd.md` by path (as the prompts above do) rather than pasting their content in — the agent can read the files, and this keeps the docs as the actual single source of truth if you revise them later.
- **Ask for a plan before multi-file execution**, especially for Phases 1, 2, 5, and 9 — the ones with real design decisions embedded in them (schema shape, layout-mapping edge cases, generator interface). Cheap to review, expensive to unwind.
- **Only parallelize across non-overlapping folders.** `packages/figma-plugin/` and `unity/` are safe to run simultaneously on the Manager surface from Phase 2 onward; don't split work within the same folder across two agents.
- **Verify, don't just trust the Artifact.** For Unity-side phases, explicitly ask the agent to run tests via Unity's batch-mode test runner if Unity is installed locally, and to say plainly in its report if it *can't* verify (no local Unity install) rather than assuming success.
- **Feed real Figma fixtures early.** From Phase 2 onward, export a couple of real (not synthetic) Figma frames covering auto-layout, images, and at least one deliberately exotic effect, and ask agents to test against those fixtures — synthetic test data tends to under-represent the messy cases the coverage matrix exists to catch.
