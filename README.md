<div align="center">

# UnityUI Transformer
### High-Performance Figma to Unity UI Toolkit Compilation Engine

![.NET 9.0](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![WPF](https://img.shields.io/badge/UI-WPF-0078D4?style=for-the-badge&logo=windows&logoColor=white)
![Platform](https://img.shields.io/badge/Platform-Windows%20x64-555555?style=for-the-badge&logo=windows&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-2ea44f?style=for-the-badge)
![Figma API](https://img.shields.io/badge/Figma-OAuth%202.0-F24E1E?style=for-the-badge&logo=figma&logoColor=white)

<br/>

[![UnityUI Transformer Banner](https://raw.githubusercontent.com/argentium0/UnityUI-Transformer/main/docs/neon_cubic_badge_logo.jpg)](https://argentium0.github.io/UnityUI-Transformer/)

[**Live Landing Page**](https://argentium0.github.io/UnityUI-Transformer/) • [**Download Executable (v1.0.0)**](https://github.com/argentium0/UnityUI-Transformer/releases/latest/download/UnityUITransformer.App.exe) • [**How It Works**](https://argentium0.github.io/UnityUI-Transformer/how-it-works.html)

</div>

---

> [!WARNING]
> ## ⚠️ Development Notice: Desktop App Paused
> Due to aggressive rate-limiting (429 Too Many Requests) and architectural constraints within the Figma REST API, the standalone WPF Desktop Application is currently on hold. 
> 
> For stable, rate-limit-free synchronization, please use the **Plugin-to-Server Pipeline** outlined below.

---

## 🚀 Alternative Pipeline: Figma Plugin -> Fastify -> Unity

This approach bypasses the REST API limits by extracting the layout data directly from within the Figma canvas using a dedicated plugin, routing it through a local Fastify server, and outputting a ready-to-use Unity Package.

### Prerequisites
* Node.js installed on your machine.
* Unity Editor (UI Toolkit enabled).
* The custom Figma Plugin installed in your Figma workspace.

### Step 1: Start the Fastify Server
The local server listens for the JSON payload sent by the Figma plugin and converts it into Unity assets.
1. Open your terminal and navigate to the server directory.
2. Install the dependencies:
   ```bash
   npm install
   ```
3. Boot the server on port 3000:
   ```bash
   npm run start
   ```
   *(Ensure the terminal displays `Server listening on http://localhost:3000`)*

### Step 2: Export from Figma
1. Open your target design file in the Figma desktop or web app.
2. Launch the **UnityUI Transformer Figma Plugin**.
3. Click **Export to Localhost**. The plugin will traverse the Auto Layout tree, batch the images, and POST the payload to your Fastify server.

### Step 3: Import into Unity
Once the Fastify server processes the payload, it will generate a `.unitypackage` file in your output directory.
1. Open your Unity project.
2. Go to **Assets > Import Package > Custom Package...**
3. Select the newly generated package to import your `.uxml`, `.uss`, and `.png` files directly into your project.

*Maintained by Muhammad Abdullah*

---

## ⚡ Overview & Elevator Pitch

**UnityUI Transformer** is a standalone, enterprise-grade desktop utility that bridges the gap between Figma design tokens and Unity UI Toolkit. By parsing Figma REST API node trees, the engine translates Auto-Layout flex direction, padding, gap, and bounds directly into production-ready `.uxml` layout templates and `.uss` style sheets.

Built natively in **C# 13**, **.NET 9.0**, and **WPF**, the application provides real-time streaming compilation logs, strict node-id URL parameter validation, and zero cloud footprint through local Windows DPAPI encryption.

---

## ✨ Core Features

| Feature | Description |
| :--- | :--- |
| **Pixel-Perfect USS** | Translates Figma Auto-Layout parameters (`flex-direction`, `row-gap`, `padding`, `align-items`, `bounds`) into native Unity UI Toolkit USS flexbox rules. |
| **Zero Cloud Footprint** | Enterprise-grade local security using Windows Data Protection API (`ProtectedData`). No user design tokens or OAuth keys are stored on remote servers. |
| **Figma Native OAuth 2.0** | Direct PKCE authorization flow supporting real-time Figma profile handle and avatar badge synchronization. |
| **Node ID Validation** | Strict URL guardrails enforcing `node-id=...` parameters to prevent premature execution on raw file links. |
| **PDF User Manual Generator** | Embedded native PDF engine generating structured 3-page setup guides (`UnityUI_Transformer_User_Manual.pdf`) directly to local target directories. |
| **Single-File Standalone** | Self-contained x64 Windows executable requiring zero external runtime or .NET SDK installation. |

---

## 🔄 How It Works

```text
+-----------------------+      +---------------------------------+      +-----------------------------------+
|   1. USER INPUT       |      |    2. SECURE HANDSHAKE          |      |    3. THE ENGINE                  |
| (Figma Node URL)      | ---> |  (Figma OAuth 2.0 PKCE ->       | ---> | (JSON AST -> UXML/USS Generator   |
| [node-id=102:405]     |      |   DPAPI Encryption Vault)       |      |  -> Unity Assets UI Folder)       |
+-----------------------+      +---------------------------------+      +-----------------------------------+
```

1. **User Input:** Paste a Figma node URL containing a valid `node-id=...` parameter.
2. **Secure Handshake:** Authenticate via Figma OAuth 2.0 PKCE. Session tokens are protected using local Windows DPAPI (`session.dat`).
3. **The Engine:** `UxmlGenerator` and `UssGenerator` parse the Figma AST JSON and compile structured UXML layout trees and USS style sheets directly into your target Unity project directory.

---

## 🚀 Installation & Usage

### Option 1: Direct Executable Download (Recommended)
1. Download the latest standalone release: [**UnityUITransformer.App.exe**](https://github.com/argentium0/UnityUI-Transformer/releases/latest/download/UnityUITransformer.App.exe).
2. Double-click to launch (no installer required).

### Option 2: Step-by-Step Workflow
1. Launch **UnityUI Transformer**.
2. Click **Connect with Figma** to authenticate using official OAuth 2.0 PKCE.
3. Open your Figma design file, select a Frame or Component, and copy its browser URL.
4. Paste the URL into the **Figma Design File / Node URL** text box.
5. Browse and select your target Unity project directory (e.g. `C:\MyUnityProject\Assets\UI`).
6. Click **Start Transformation** to generate `.uxml` and `.uss` assets!

---

## 🛠️ Development & Building

To clone, build, and run the project locally from source:

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Windows 10/11 x64 OS

### Build & Run Commands

```bash
# Clone the repository
git clone https://github.com/argentium0/UnityUI-Transformer.git
cd UnityUI-Transformer

# Restore and build the solution
dotnet build desktop/src/UnityUITransformer.App/UnityUITransformer.App.csproj

# Run automated unit tests (22 test suite)
dotnet test desktop/src/UnityUITransformer.App.Tests/UnityUITransformer.App.Tests.csproj

# Publish standalone single-file Release executable
dotnet publish desktop/src/UnityUITransformer.App/UnityUITransformer.App.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true -o ./publish
```

---

## 💻 Technology Stack

| Layer | Technology | Usage |
| :--- | :--- | :--- |
| **Language** | C# 13 | Core application & transformation logic |
| **Framework** | .NET 9.0 | Runtime & SDK foundation |
| **UI Framework** | WPF (Windows Presentation Foundation) | Dark/neon desktop user interface |
| **Authentication** | Figma OAuth 2.0 PKCE & Supabase Auth | OAuth token handling & profile sync |
| **Security** | Windows DPAPI (`ProtectedData`) | Local DPAPI encryption for `session.dat` |
| **PDF Engine** | Native C# PDF 1.4 Generator | User manual export generation |
| **Web Landing Page** | HTML5, CSS3, GSAP 3.12, ScrollTrigger | GitHub Pages interactive website |

---

## 📄 License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.
