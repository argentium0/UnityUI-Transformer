# UnityUI Transformer

UnityUI Transformer is an automated design-to-code pipeline that converts Figma designs directly into native Unity UI Toolkit structures. By extracting design trees, layout metadata, assets, and design tokens from Figma, UnityUI Transformer eliminates manual UI reconstruction in Unity while maintaining style consistency and visual fidelity.

---

## System Architecture

The monorepo is structured into three decoupled tiers to ensure clear separation of concerns, independent testing, and versioning stability.

```
+------------------------+      IR JSON + Assets      +-------------------------+      IR JSON + Assets      +---------------------------+
|  Tier 1: Figma Plugin  | -------------------------> |  Tier 2: Bridge Server  | -------------------------> |   Tier 3: Unity Package   |
|      (TypeScript)      |       (.f2u.zip)           |    (Node.js, Optional)  |       (File Write)         |   (C# Editor Tooling)     |
+------------------------+                            +-------------------------+                            +---------------------------+
  Extracts Node Trees,                                  Local HTTP/WebSocket                                   Parses IR Schema,
  Design Tokens, Assets &                              Sync Server for Direct                                 ScriptableObjects, UXML/USS
  Serializes to IR Schema                               Unity File Writes                                      Structures & Reports
```

### Architecture Components

1. **Tier 1: Figma Plugin (`packages/figma-plugin`)**
   - Implemented in TypeScript using Preact for the UI iframe.
   - Recursively walks Figma selection trees and maps layout modes, padding, gap, sizing constraints, fills, strokes, and effects.
   - Extracts design tokens (colors, typography, spacing, effect styles).
   - Exports vector shapes as SVG and raster/unsupported nodes as PNG assets (1x, 2x, 3x).
   - Bundles the IR JSON document and assets into a `.f2u.zip` archive in the UI iframe environment.

2. **Tier 2: Shared Intermediate Representation (`packages/ir-schema`)**
   - Versioned Zod schema definition acting as the single source of truth contract between Figma and Unity.
   - Exports JSON Schema definitions for external tooling and cross-language compatibility.
   - Defines discriminated union node types: Frame, Group, Rectangle, Ellipse, Vector, Text, Image, ComponentInstance, and Unsupported.

3. **Tier 3: Unity Importer Package (`unity/Packages/com.yourorg.figma2unity`)**
   - Editor-time C# tool installed via Unity Package Manager (UPM).
   - Custom Newtonsoft.Json polymorphic deserializers (`IRNodeConverter`) converting IR JSON into C# model hierarchies.
   - Schema version validator enforcing major version compatibility (FR2).
   - Asset postprocessing configuring imported PNG assets as single Sprites with centered pivots.
   - EditMode unit testing suite validating package extraction and tree parsing.

---

## Monorepo Directory Structure

```
UnityUI Transformer/
├── docs/                                    # Technical architecture and product specifications
│   ├── 01-tech-stack.md                     # System architecture decisions and technical specifications
│   ├── 02-prd.md                            # Product requirements and coverage matrix
│   └── 03-antigravity-build-plan.md         # Phased implementation plan
├── figma2unity/                             # Workspace root for NPM packages and Unity package
│   ├── packages/
│   │   ├── ir-schema/                       # Shared Intermediate Representation Zod schema
│   │   └── figma-plugin/                    # Figma plugin source (Preact UI + main thread traversal)
│   ├── unity/
│   │   └── Packages/
│   │       └── com.yourorg.figma2unity/     # Unity package skeleton (C# Editor scripts & tests)
│   ├── package.json                         # NPM workspace configuration
│   ├── tsconfig.base.json                   # Base TypeScript compiler settings
│   ├── .eslintrc.json                       # ESLint static analysis configuration
│   └── vitest.config.ts                     # Vitest root test suite configuration
├── .gitignore                               # Comprehensive monorepo repository ignore rules
└── README.md                                # System documentation
```

---

## Key Features

- **Auto-Layout Mapping**: Figma auto-layout properties (direction, gap, padding, axis alignments, hug/fill sizing) map cleanly to flexbox-based UI Toolkit UXML structures and USS style rules.
- **Design Token Indirection**: Colors, typography, spacing, and effect styles are extracted into structured token collections. Nodes reference token IDs rather than hardcoding values, enabling global updates upon re-sync.
- **Graceful Degradation**: Unrecognized or exotic Figma node types fall back to rasterized PNG exports without dropping layers or failing the export pipeline.
- **Schema Version Enforcement**: The Unity importer validates incoming IR schema major versions, preventing incompatible package imports through modal error feedback.
- **Idempotent Import**: Re-importing updated sync packages updates target assets deterministically while respecting designated project structures.

---

## Installation & Setup

### Prerequisites

- **Node.js**: LTS version (v20.x or higher recommended)
- **NPM**: Version 9.x or higher with workspace support enabled
- **Unity**: Unity 6000.0 LTS or supported Editor versions

### Building the TypeScript Packages

To install dependencies and compile the Figma plugin and IR schema packages:

```bash
cd figma2unity
npm install
npm run build
```

To run unit tests across all workspace packages:

```bash
npm test
```

To run static analysis code linting:

```bash
npm run lint
```

### Installing the Unity Package

To install the importer into a Unity project:

1. Open your Unity project.
2. Open **Window > Package Manager**.
3. Click the **+** button and select **Add package from disk...**.
4. Select `unity/Packages/com.yourorg.figma2unity/package.json`.

---

## Workflow Guide

### Step 1: Exporting from Figma

1. Open your Figma design file.
2. Select the target Frame or Page to sync.
3. Launch the **Figma2Unity Importer** plugin.
4. Click **Sync Selection**.
5. The plugin traverses the node selection, exports assets, packages the IR document into `.f2u.zip`, and triggers a browser file download.

### Step 2: Importing into Unity

1. Open your Unity Editor project.
2. Navigate to the menu item: **Figma2Unity > Import Sync Package...**.
3. Select the downloaded `.f2u.zip` file.
4. The importer validates the schema version, extracts JSON data, deserializes the IR tree, copies PNG/SVG assets into `Assets/Figma2UnityImports/{PackageName}/`, and configures Sprite import settings.

---

## Testing & Quality Assurance

### TypeScript Workspace Tests

The TypeScript workspace uses Vitest to execute schema validation, node traversal, and zip packaging tests. Run tests via:

```bash
npm test
```

Test coverage includes:
- **`packages/ir-schema/src/index.test.ts`**: Validates fixture documents and unsupported node fallback handling against Zod schemas.
- **`packages/ir-schema/src/hello.test.ts`**: Base verification test.
- **`packages/figma-plugin/src/traversal.test.ts`**: Verifies node tree traversal and auto-layout property extractions.
- **`packages/figma-plugin/src/zip-packager.test.ts`**: Verifies in-memory `.f2u.zip` archive creation and extraction.

### Unity C# EditMode Tests

The Unity package includes EditMode unit tests located in `unity/Packages/com.yourorg.figma2unity/Tests/Editor/`:
- **`SyncPackageImporterTests.cs`**: Asserts major version matching rules, polymorphic `IRNode` JSON deserialization (`FrameNode`, `TextNode`, `ImageNode`, `UnsupportedNode`), and zip archive extraction.

---

## Development Guidelines

- Maintain strict separation between Tier 1 (Figma Plugin), Tier 2 (Bridge Server), and Tier 3 (Unity Package).
- Ensure all public C# methods and schema classes preserve full word documentation without abbreviated names.
- Do not introduce breaking schema changes without bumping the `schemaVersion` field in `@figma2unity/ir-schema`.
