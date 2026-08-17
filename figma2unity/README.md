# UnityUI Transformer — Workspace Package Directory

This directory contains the workspace configuration, shared IR schema, Figma plugin source code, and Unity package skeleton for **UnityUI Transformer**.

---

## Workspace Structure

- **`packages/ir-schema/`**: Shared Intermediate Representation Zod schema and exported JSON Schema (`@figma2unity/ir-schema`).
- **`packages/figma-plugin/`**: Figma plugin source code built with Preact and esbuild (`@figma2unity/figma-plugin`).
- **`unity/Packages/com.yourorg.figma2unity/`**: Unity Package Manager (UPM) C# Editor importer package.

---

## Development Commands

Run all commands from the `figma2unity/` directory root:

### Build
Compiles all workspace TypeScript packages, bundles the Figma plugin, and exports JSON Schema definitions:

```bash
npm run build
```

### Test
Executes Vitest unit test suites across all packages:

```bash
npm test
```

### Lint
Executes static code analysis via ESLint across `packages/`:

```bash
npm run lint
```

For complete system architecture, installation steps, and workflow documentation, see the root [README.md](../README.md).
