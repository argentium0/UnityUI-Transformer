# Tech Stack — Figma → Unity UI Importer ("Figma2Unity")

## Architecture at a glance

Three tiers, deliberately decoupled so each can be built, tested, and versioned independently:

```
┌─────────────────────┐     IR JSON + assets     ┌──────────────────────┐     IR JSON + assets     ┌──────────────────────────┐
│  Tier 1: Figma       │ ───────────────────────► │  Tier 2: Bridge       │ ───────────────────────► │  Tier 3: Unity Package    │
│  Plugin (TypeScript) │      (.zip or POST)       │  (Node.js, optional)  │      (file write)         │  (C# Editor tooling)      │
└─────────────────────┘                           └──────────────────────┘                           └──────────────────────────┘
   Walks the node tree                              Local sync server so                                Parses IR, generates
   Exports images/SVG                                the plugin (sandboxed,                              UXML/USS or uGUI prefabs,
   Extracts design tokens                            no filesystem access)                                imports assets, builds
   Serializes to the IR                              can write straight into                              design-token ScriptableObjects
   schema                                            your Unity project                                   and a fidelity report
```

The IR (Intermediate Representation) JSON schema is the contract between tiers — design it first, version it, and treat it as the single source of truth for what "complete coverage" means.

---

## Tier 1 — Figma Plugin (extraction layer)

| Concern | Choice | Why |
|---|---|---|
| Language | TypeScript | Figma's typings (`@figma/plugin-typings`) are TS-first; catches node-shape mistakes at compile time |
| Runtime API | Figma Plugin API (`figma.*`) | Only API with access to the live, fully-resolved node tree — including boolean operations, resolved instance overrides, and variables |
| Fallback API | Figma REST API | Useful for a future headless/CI export path (no Figma desktop app needed), and for fetching file data outside an open editor session |
| Design tokens | Figma Variables API + Styles API | Pulls colors, type styles, effect styles, and (newer) bound variables as first-class token objects instead of inlined values — this is what makes "consistent color/style" possible at all |
| Bundler | esbuild | Plugin code must ship as a single bundled JS file; esbuild is fast enough for iterative agent-driven development |
| Plugin UI (iframe) | Plain HTML/CSS + Preact | The UI panel is just a sandboxed webpage; keep it minimal — it only needs a "Sync" button, scope picker (frame/page/file), and a progress/report view |
| Asset export | `node.exportAsync()` | Native Figma export for PNG (1x/2x/3x) and SVG per node |
| Schema validation | Zod | Validate the IR at the point of construction; compile the same schema to JSON Schema so the C# side can validate on import too |

---

## Tier 2 — Bridge (transport layer)

Figma plugins run in a **sandboxed iframe with no filesystem access** — this determines the whole transport design.

**MVP (recommended starting point): no server at all.**
The plugin UI packages the IR JSON + exported assets into a `.f2u.zip` via browser `Blob`/download, the user drags it into the Unity project (or a watched `Imports/` folder), and a Unity `AssetPostprocessor`/menu command unpacks it. Zero infra, zero always-running process, trivially debuggable.

**V2 (once the MVP pipeline is proven): local bridge server.**
| Concern | Choice | Why |
|---|---|---|
| Server | Node.js + Fastify | Lightweight, the plugin UI can `fetch()` to `localhost` (Figma plugin UIs do have network access even though they lack filesystem access) |
| Transport | `POST /sync` (batch) + optional WebSocket `/watch` | Batch endpoint writes IR + assets straight into the Unity project folder; WebSocket enables a "live preview" mode where saving in Figma triggers an auto re-import |
| Why still keep the zip path | Fallback / CI / no-server-running case | Bridge server should be an optional convenience layer on top of the same IR contract, not a hard dependency |

---

## Tier 3 — Unity Package (generation layer)

| Concern | Choice | Why |
|---|---|---|
| Packaging | Local/Git UPM package (`com.yourorg.figma2unity`) | Standard Unity distribution mechanism; installs via Package Manager, versionable, keeps generator code out of `Assets/` |
| Editor scripting | `UnityEditor` namespace, custom `EditorWindow` + menu items | All generation happens at edit time — matches "we don't need to use Unity for any UI work" (the importer *is* the UI work) |
| JSON parsing | Newtonsoft.Json (`com.unity.nuget.newtonsoft-json`) | Polymorphic node trees (rectangle vs text vs vector vs instance) are much easier to deserialize correctly than with `JsonUtility` |
| **UI target (primary)** | **UI Toolkit — generate UXML + USS** | Figma auto-layout is essentially flexbox. UI Toolkit's layout engine (Yoga) is flexbox-based, so gap/padding/hug/fill/align map almost 1:1. This is the single biggest fidelity win in the whole pipeline. |
| UI target (secondary, later phase) | uGUI — generate `Canvas`/`RectTransform` prefab hierarchies | Still the dominant runtime UI system in shipped games and most asset-store integrations exist for it. Requires manual translation of Figma auto-layout + constraints into anchors/pivots/`LayoutGroup`s — more code, lower fidelity ceiling, but worth adding once UI Toolkit output is solid. |
| Text | TextMeshPro | Non-negotiable in modern Unity UI; use the Editor Font Asset Creator API to generate TMP font assets from matched fonts |
| Vector shapes | `com.unity.vectorgraphics` importing the SVGs exported in Tier 1 | Keeps vectors crisp/scalable instead of rasterizing everything, matching Figma's own vector fidelity |
| Raster assets | Standard `TextureImporter`, scripted | Script sprite mode, pivot, and 9-slice border values (derived from Figma's corner-radius + stroke metadata) automatically per asset |
| Design tokens | Auto-generated `ScriptableObject`s: `ColorPalette`, `TypeRamp`, `SpacingScale`, `EffectStyle` | Nodes reference tokens by ID rather than inlining values, so re-syncing a rebrand updates every screen at once — this is what "consistent in terms of colour/style" actually requires architecturally, not just visually |
| Idempotent re-import | Sidecar `.figma2unity.meta.json` per generated asset storing the Figma node ID + content hash | Re-running sync updates existing assets in place instead of duplicating them, and lets you detect "hand-customized" regions that shouldn't be overwritten |

---

## Validation / QA tooling

| Concern | Choice | Why |
|---|---|---|
| Visual regression | Unity Test Framework PlayMode test capturing `ScreenCapture`, diffed against the Figma PNG export via a small Pillow/OpenCV script in CI | Turns "looks right" into a measurable, CI-enforced number (e.g., flag >2% pixel delta) |
| Schema tests | Vitest/Jest golden-file snapshots of the IR on sample Figma files | Catches accidental schema drift before it reaches Unity |
| Import report | Markdown/HTML generated per sync run | Lists nodes processed, fallback rasterizations, missing font matches — makes "did we miss anything" answerable at a glance instead of requiring manual comparison |

---

## Suggested repo layout

```
figma2unity/
├── docs/
│   ├── 01-tech-stack.md
│   ├── 02-prd.md
│   └── 03-antigravity-build-plan.md
├── packages/
│   ├── ir-schema/            # shared Zod schema + generated JSON Schema (source of truth)
│   ├── figma-plugin/         # Tier 1 — TypeScript, esbuild
│   └── bridge-server/        # Tier 2 — Node/Fastify (added in V2)
├── unity/
│   └── Packages/com.yourorg.figma2unity/   # Tier 3 — C# UPM package
│       ├── Editor/
│       └── Tests/
└── .github/workflows/        # CI: lint+test TS packages, headless Unity tests (GameCI)
```

## Dev environment

- **Antigravity IDE** as the primary build environment — use the Manager surface to run one agent per repo folder in parallel (e.g., a `figma-plugin/` agent and a `unity/` agent working simultaneously without stepping on each other), and the Editor view for hands-on debugging of generated output.
- Node.js LTS for the TS packages; Newtonsoft.Json + a recent Unity LTS (check whichever is current at build time — Unity 6000.x LTS as of writing) for the package.
- GitHub Actions CI: lint/test the TypeScript packages on every push; run Unity EditMode/PlayMode tests headlessly via a GameCI Docker image.

## Why not the obvious alternatives

- **REST API instead of a plugin:** the REST API doesn't expose the fully-resolved live node tree (boolean ops, instance overrides) as cleanly, and adds auth-token management for no real benefit at this stage. Worth adding later purely for headless/CI export.
- **HTML/CSS intermediate rendered in a Unity WebView:** technically simpler, but produces a web renderer embedded in Unity rather than native Unity UI — directly against the stated goal of a real, importable Unity UI.
- **uGUI-only, skip UI Toolkit:** uGUI has no native flexbox equivalent, so auto-layout-heavy design systems require significantly more anchor-math and produce lower-fidelity results. UI Toolkit as primary, uGUI as an added export target, gives the best fidelity now and broadest compatibility later.
