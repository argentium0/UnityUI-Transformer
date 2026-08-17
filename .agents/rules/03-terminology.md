---
description: Enforces strict naming conventions and terminology across the entire codebase and documentation.
globs: *
---
# Terminology & Naming Standards

1. **Explicit Terminology:** You must always use the full word "**development**" in all documentation, comments, variable names, and output reports. Do not abbreviate it to "dev" under any circumstances (e.g., use `DevelopmentEnvironment` instead of `DevEnv`, `development_mode` instead of `dev_mode`).
2. **Casing:**
    * TypeScript/JavaScript: `camelCase` for variables, `PascalCase` for components/classes.
    * C# / Unity: `PascalCase` for public members and classes, `camelCase` for private fields (prefix with `_` if standard for the studio).
    * JSON / IR Schema: `camelCase` for all keys.
