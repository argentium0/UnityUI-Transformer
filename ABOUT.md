# About UnityUI Transformer

**UnityUI Transformer** is an automated, high-fidelity design-to-code pipeline that converts Figma designs directly into native **Unity UI Toolkit** structures (`.uxml` and `.uss`).

---

## 🎯 Overview & Mission

Building user interfaces in Unity Editor manually from Figma design files is time-consuming and error-prone. **UnityUI Transformer** bridges this gap by automatically extracting layout hierarchies, typography, color palettes, vector shapes, and raster assets from Figma, converting them into structured Intermediate Representation (IR) JSON documents, and generating native Unity UI Toolkit elements in C#.

---

## 🏗️ Architecture at a Glance

The project is architected as a clean, decoupled 3-tier monorepo:

1. **Tier 1: Figma Plugin (`@figma2unity/plugin`)**
   - Built with TypeScript and Preact.
   - Traverses Figma selection trees, maps flexbox layout properties (direction, gap, padding, sizing constraints), and extracts design tokens.
   - Packages extracted node trees and rasterized PNG/SVG assets into a `.f2u.zip` archive.

2. **Tier 2: Intermediate Representation (`@figma2unity/ir-schema`)**
   - Versioned Zod schema definitions serving as the contract between Figma and Unity.
   - Defines strongly-typed polymorphic node trees (Frames, Groups, Rectangles, Ellipses, Text, Vectors, Images, Component Instances, and Unsupported fallbacks).

3. **Tier 3: Unity Importer & Generator (`com.yourorg.figma2unity`)**
   - C# Editor extension installed via Unity Package Manager (UPM).
   - Polymorphic JSON deserialization (`IRNodeConverter`), asset postprocessing, and automated UXML tree and USS stylesheet generation.

---

## 🔑 Key Features

- **Flexbox to UI Toolkit Mapping**: Automatically maps Figma Auto-Layout parameters to UI Toolkit USS flex layout rules (`flex-direction`, `gap`, `padding`, `flex-grow`, `align-items`).
- **Design Tokens & Styling**: Generates clean `.uss` stylesheets with custom classes and variables for reusable visual styling.
- **Graceful Fallbacks**: Exotic or unsupported Figma layers automatically degrade gracefully into raster PNG assets without breaking the import pipeline.
- **Strict Schema Versioning**: Enforces major schema version compatibility between the plugin exporter and Unity editor importer.

---

## 🏷️ Repository Details

- **GitHub Repository**: [argentium0/UnityUI-Transformer](https://github.com/argentium0/UnityUI-Transformer)
- **Topics**: `unity`, `figma`, `ui-toolkit`, `uxml`, `uss`, `design-to-code`, `figma-plugin`, `unity3d`, `csharp`, `typescript`
