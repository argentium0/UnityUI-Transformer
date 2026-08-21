using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UnityUITransformer.App.Services
{
    public class ManualPdfGenerator
    {
        public static void GeneratePdfManual(string destinationFilePath)
        {
            byte[] pdfData = BuildPdfData();
            File.WriteAllBytes(destinationFilePath, pdfData);
        }

        private static byte[] BuildPdfData()
        {
            var pdf = new StringBuilder();
            var xrefs = new List<long>();

            void AppendLine(string line) => pdf.Append(line + "\n");
            long GetPos() => Encoding.UTF8.GetByteCount(pdf.ToString());

            // Header
            AppendLine("%PDF-1.4");
            AppendLine("%\xFF\xFF\xFF\xFF");

            // Obj 1: Catalog
            xrefs.Add(GetPos());
            AppendLine("1 0 obj");
            AppendLine("<< /Type /Catalog /Pages 2 0 R >>");
            AppendLine("endobj");

            // Obj 2: Pages
            xrefs.Add(GetPos());
            AppendLine("2 0 obj");
            AppendLine("<< /Type /Pages /Kids [ 5 0 R 7 0 R 9 0 R ] /Count 3 >>");
            AppendLine("endobj");

            // Obj 3: Font Regular
            xrefs.Add(GetPos());
            AppendLine("3 0 obj");
            AppendLine("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
            AppendLine("endobj");

            // Obj 4: Font Bold
            xrefs.Add(GetPos());
            AppendLine("4 0 obj");
            AppendLine("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>");
            AppendLine("endobj");

            // Page 1 Stream
            string page1Stream = CreatePage1Stream();
            xrefs.Add(GetPos()); // Obj 5: Page 1
            AppendLine("5 0 obj");
            AppendLine("<< /Type /Page /Parent 2 0 R /MediaBox [ 0 0 612 792 ] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents 6 0 R >>");
            AppendLine("endobj");

            xrefs.Add(GetPos()); // Obj 6: Page 1 Contents
            AppendLine("6 0 obj");
            AppendLine($"<< /Length {Encoding.UTF8.GetByteCount(page1Stream)} >>");
            AppendLine("stream");
            pdf.Append(page1Stream);
            AppendLine("endstream");
            AppendLine("endobj");

            // Page 2 Stream
            string page2Stream = CreatePage2Stream();
            xrefs.Add(GetPos()); // Obj 7: Page 2
            AppendLine("7 0 obj");
            AppendLine("<< /Type /Page /Parent 2 0 R /MediaBox [ 0 0 612 792 ] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents 8 0 R >>");
            AppendLine("endobj");

            xrefs.Add(GetPos()); // Obj 8: Page 2 Contents
            AppendLine("8 0 obj");
            AppendLine($"<< /Length {Encoding.UTF8.GetByteCount(page2Stream)} >>");
            AppendLine("stream");
            pdf.Append(page2Stream);
            AppendLine("endstream");
            AppendLine("endobj");

            // Page 3 Stream
            string page3Stream = CreatePage3Stream();
            xrefs.Add(GetPos()); // Obj 9: Page 3
            AppendLine("9 0 obj");
            AppendLine("<< /Type /Page /Parent 2 0 R /MediaBox [ 0 0 612 792 ] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents 10 0 R >>");
            AppendLine("endobj");

            xrefs.Add(GetPos()); // Obj 10: Page 3 Contents
            AppendLine("10 0 obj");
            AppendLine($"<< /Length {Encoding.UTF8.GetByteCount(page3Stream)} >>");
            AppendLine("stream");
            pdf.Append(page3Stream);
            AppendLine("endstream");
            AppendLine("endobj");

            // Xref Table
            long xrefPos = GetPos();
            AppendLine("xref");
            AppendLine($"0 {xrefs.Count + 1}");
            AppendLine("0000000000 65535 f ");
            foreach (var pos in xrefs)
            {
                AppendLine($"{pos:D10} 00000 n ");
            }

            // Trailer
            AppendLine("trailer");
            AppendLine($"<< /Size {xrefs.Count + 1} /Root 1 0 R >>");
            AppendLine("startxref");
            AppendLine($"{xrefPos}");
            AppendLine("%%EOF");

            return Encoding.UTF8.GetBytes(pdf.ToString());
        }

        private static string Escape(string text) =>
            text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

        private static string CreatePage1Stream()
        {
            var sb = new StringBuilder();

            // Dark Top Header Box
            sb.AppendLine("0.09 0.09 0.10 rg 36 710 540 50 re f");
            // Accent Neon Line
            sb.AppendLine("0.83 1.00 0.20 rg 36 706 540 4 re f");

            // Title
            sb.AppendLine("BT /F2 16 Tf 1.0 1.0 1.0 rg 48 732 Td (" + Escape("UnityUI Transformer — User Manual & Setup Guide") + ") Tj ET");
            sb.AppendLine("BT /F1 9 Tf 0.83 1.00 0.20 rg 48 717 Td (" + Escape("Pro Max v1.0.0 | High-Performance Figma to Unity UI Toolkit Bridge") + ") Tj ET");

            // Section 1
            sb.AppendLine("BT /F2 13 Tf 0.15 0.15 0.18 rg 36 670 Td (" + Escape("1. Introduction") + ") Tj ET");
            sb.AppendLine("0.83 1.00 0.20 rg 36 663 540 1.5 re f");

            sb.AppendLine("BT /F1 10 Tf 0.2 0.2 0.2 rg 36 645 Td (" + Escape("UnityUI Transformer is a high-speed desktop pipeline designed to convert Figma design frames") + ") Tj ET");
            sb.AppendLine("BT /F1 10 Tf 0.2 0.2 0.2 rg 36 631 Td (" + Escape("directly into native Unity UI Toolkit layout files (.uxml) and USS style sheets (.uss).") + ") Tj ET");
            sb.AppendLine("BT /F1 10 Tf 0.2 0.2 0.2 rg 36 615 Td (" + Escape("By eliminating manual XML/CSS rewriting, designers and developers can achieve instant 1:1 parity") + ") Tj ET");
            sb.AppendLine("BT /F1 10 Tf 0.2 0.2 0.2 rg 36 601 Td (" + Escape("between Figma components and production Unity runtime UI.") + ") Tj ET");

            // Key Architectural Features
            sb.AppendLine("BT /F2 10.5 Tf 0.1 0.1 0.1 rg 36 575 Td (" + Escape("Key Architectural Highlights:") + ") Tj ET");
            sb.AppendLine("BT /F1 9.5 Tf 0.25 0.25 0.25 rg 50 558 Td (" + Escape("- Direct REST API tree parsing with recursive node traversal.") + ") Tj ET");
            sb.AppendLine("BT /F1 9.5 Tf 0.25 0.25 0.25 rg 50 544 Td (" + Escape("- Automatic flexbox rule mapping (padding, gap, item spacing, alignment).") + ") Tj ET");
            sb.AppendLine("BT /F1 9.5 Tf 0.25 0.25 0.25 rg 50 530 Td (" + Escape("- Windows DPAPI session security using ProtectedData CurrentUser encryption.") + ") Tj ET");
            sb.AppendLine("BT /F1 9.5 Tf 0.25 0.25 0.25 rg 50 516 Td (" + Escape("- Asynchronous thread dispatching preventing main thread UI locks during sync.") + ") Tj ET");

            // Section 2
            sb.AppendLine("BT /F2 13 Tf 0.15 0.15 0.18 rg 36 480 Td (" + Escape("2. Getting Started & Authentication") + ") Tj ET");
            sb.AppendLine("0.83 1.00 0.20 rg 36 473 540 1.5 re f");

            sb.AppendLine("BT /F1 10 Tf 0.2 0.2 0.2 rg 36 455 Td (" + Escape("Follow these steps to connect your Figma account securely:") + ") Tj ET");
            sb.AppendLine("BT /F2 10 Tf 0.1 0.1 0.1 rg 50 437 Td (" + Escape("Step 1: Initiate OAuth Sign-In") + ") Tj ET");
            sb.AppendLine("BT /F1 9.5 Tf 0.25 0.25 0.25 rg 65 423 Td (" + Escape("Click 'Connect with Figma' on Screen 1 (AuthView). The app initiates a secure OAuth 2.0 PKCE flow.") + ") Tj ET");

            sb.AppendLine("BT /F2 10 Tf 0.1 0.1 0.1 rg 50 403 Td (" + Escape("Step 2: Complete Browser Authorization") + ") Tj ET");
            sb.AppendLine("BT /F1 9.5 Tf 0.25 0.25 0.25 rg 65 389 Td (" + Escape("Grant read access permissions on Figma's authorization page. OAuth tokens return to Supabase SDK.") + ") Tj ET");

            sb.AppendLine("BT /F2 10 Tf 0.1 0.1 0.1 rg 50 369 Td (" + Escape("Step 3: Encrypted Session Storage") + ") Tj ET");
            sb.AppendLine("BT /F1 9.5 Tf 0.25 0.25 0.25 rg 65 355 Td (" + Escape("Tokens are encrypted with DPAPI and written to %LOCALAPPDATA%\\UnityUITransformer\\session.dat.") + ") Tj ET");
            sb.AppendLine("BT /F1 9.5 Tf 0.25 0.25 0.25 rg 65 341 Td (" + Escape("Subsequent app launches restore your session automatically without requiring re-authentication.") + ") Tj ET");

            // Page Footer
            sb.AppendLine("0.85 0.85 0.85 rg 36 40 540 0.8 re f");
            sb.AppendLine("BT /F1 8.5 Tf 0.5 0.5 0.5 rg 36 26 Td (" + Escape("UnityUI Transformer Manual — Page 1 of 3") + ") Tj ET");
            sb.AppendLine("BT /F1 8.5 Tf 0.5 0.5 0.5 rg 450 26 Td (" + Escape("Confidential & Proprietary") + ") Tj ET");

            return sb.ToString();
        }

        private static string CreatePage2Stream()
        {
            var sb = new StringBuilder();

            // Section 3
            sb.AppendLine("BT /F2 13 Tf 0.15 0.15 0.18 rg 36 740 Td (" + Escape("3. Configuring Your Source & Target") + ") Tj ET");
            sb.AppendLine("0.83 1.00 0.20 rg 36 733 540 1.5 re f");

            sb.AppendLine("BT /F1 10 Tf 0.2 0.2 0.2 rg 36 715 Td (" + Escape("Screen 2 (ConfigView) requires specifying your source Figma frame URL and target Unity directory.") + ") Tj ET");

            sb.AppendLine("BT /F2 10 Tf 0.1 0.1 0.1 rg 50 695 Td (" + Escape("Copying Figma Frame URLs (Strict Node ID Validation):") + ") Tj ET");
            sb.AppendLine("BT /F1 9.5 Tf 0.25 0.25 0.25 rg 65 681 Td (" + Escape("1. Open your Figma design file in your browser or desktop app.") + ") Tj ET");
            sb.AppendLine("BT /F1 9.5 Tf 0.25 0.25 0.25 rg 65 667 Td (" + Escape("2. Select the specific Frame, Component, or Canvas element you want to export.") + ") Tj ET");
            sb.AppendLine("BT /F1 9.5 Tf 0.25 0.25 0.25 rg 65 653 Td (" + Escape("3. Copy the URL. Ensure it contains node-id=... (e.g. figma.com/design/KEY/Title?node-id=102-45).") + ") Tj ET");
            sb.AppendLine("BT /F1 9.5 Tf 0.25 0.25 0.25 rg 65 639 Td (" + Escape("   *Note: URLs lacking node-id will trigger a validation guardrail error in ConfigView.") + ") Tj ET");

            sb.AppendLine("BT /F2 10 Tf 0.1 0.1 0.1 rg 50 619 Td (" + Escape("Linking Unity Destination Directory:") + ") Tj ET");
            sb.AppendLine("BT /F1 9.5 Tf 0.25 0.25 0.25 rg 65 605 Td (" + Escape("1. Click 'Browse' to choose your target Unity project directory (e.g. C:\\MyProject\\Assets\\UI).") + ") Tj ET");
            sb.AppendLine("BT /F1 9.5 Tf 0.25 0.25 0.25 rg 65 591 Td (" + Escape("2. The validator verifies that the selected path is valid before enabling 'Continue to Sync'.") + ") Tj ET");

            // Section 4
            sb.AppendLine("BT /F2 13 Tf 0.15 0.15 0.18 rg 36 550 Td (" + Escape("4. The Transformation Engine") + ") Tj ET");
            sb.AppendLine("0.83 1.00 0.20 rg 36 543 540 1.5 re f");

            sb.AppendLine("BT /F1 10 Tf 0.2 0.2 0.2 rg 36 525 Td (" + Escape("The core transformation engine processes Figma nodes into clean Unity UI Toolkit markup:") + ") Tj ET");

            sb.AppendLine("BT /F2 10.5 Tf 0.1 0.1 0.1 rg 48 505 Td (" + Escape("A. UXML Generation (UxmlGenerator.cs)") + ") Tj ET");
            sb.AppendLine("BT /F1 9.5 Tf 0.25 0.25 0.25 rg 60 491 Td (" + Escape("- FRAME, RECTANGLE, GROUP -> <ui:VisualElement>") + ") Tj ET");
            sb.AppendLine("BT /F1 9.5 Tf 0.25 0.25 0.25 rg 60 477 Td (" + Escape("- TEXT -> <ui:Label text=\"...\" />") + ") Tj ET");
            sb.AppendLine("BT /F1 9.5 Tf 0.25 0.25 0.25 rg 60 463 Td (" + Escape("- Generates unique CSS class attributes for each element.") + ") Tj ET");

            sb.AppendLine("BT /F2 10.5 Tf 0.1 0.1 0.1 rg 48 441 Td (" + Escape("B. USS Style Rule Generation (UssGenerator.cs)") + ") Tj ET");
            sb.AppendLine("BT /F1 9.5 Tf 0.25 0.25 0.25 rg 60 427 Td (" + Escape("- Layout: flex-direction, align-items, justify-content, padding, margin.") + ") Tj ET");
            sb.AppendLine("BT /F1 9.5 Tf 0.25 0.25 0.25 rg 60 413 Td (" + Escape("- Fills: background-color / color mapped from 0-1 float RGB to 0-255 RGBA strings.") + ") Tj ET");
            sb.AppendLine("BT /F1 9.5 Tf 0.25 0.25 0.25 rg 60 399 Td (" + Escape("- Typography: font-size, font-weight, text-align.") + ") Tj ET");
            sb.AppendLine("BT /F1 9.5 Tf 0.25 0.25 0.25 rg 60 385 Td (" + Escape("- Borders: border-color, border-width, border-radius.") + ") Tj ET");

            // Page Footer
            sb.AppendLine("0.85 0.85 0.85 rg 36 40 540 0.8 re f");
            sb.AppendLine("BT /F1 8.5 Tf 0.5 0.5 0.5 rg 36 26 Td (" + Escape("UnityUI Transformer Manual — Page 2 of 3") + ") Tj ET");
            sb.AppendLine("BT /F1 8.5 Tf 0.5 0.5 0.5 rg 450 26 Td (" + Escape("Confidential & Proprietary") + ") Tj ET");

            return sb.ToString();
        }

        private static string CreatePage3Stream()
        {
            var sb = new StringBuilder();

            // Section 5
            sb.AppendLine("BT /F2 13 Tf 0.15 0.15 0.18 rg 36 740 Td (" + Escape("5. Troubleshooting, Security & Session Management") + ") Tj ET");
            sb.AppendLine("0.83 1.00 0.20 rg 36 733 540 1.5 re f");

            sb.AppendLine("BT /F2 10.5 Tf 0.1 0.1 0.1 rg 36 712 Td (" + Escape("Security Guardrails:") + ") Tj ET");
            sb.AppendLine("BT /F1 9.5 Tf 0.25 0.25 0.25 rg 50 696 Td (" + Escape("- Windows DPAPI Encryption: Local session files use System.Security.Cryptography.ProtectedData.") + ") Tj ET");
            sb.AppendLine("BT /F1 9.5 Tf 0.25 0.25 0.25 rg 50 682 Td (" + Escape("- Zero Hardcoded Secrets: No OAuth client secrets are embedded in application source binaries.") + ") Tj ET");
            sb.AppendLine("BT /F1 9.5 Tf 0.25 0.25 0.25 rg 50 668 Td (" + Escape("- Exception Traps: DispatcherUnhandledException handlers prevent unexpected runtime crashes.") + ") Tj ET");

            sb.AppendLine("BT /F2 10.5 Tf 0.1 0.1 0.1 rg 36 646 Td (" + Escape("Common Troubleshooting Scenarios:") + ") Tj ET");
            sb.AppendLine("BT /F2 10 Tf 0.1 0.1 0.1 rg 50 630 Td (" + Escape("Scenario A: 'Invalid Figma URL. A specific node-id is required.'") + ") Tj ET");
            sb.AppendLine("BT /F1 9.5 Tf 0.25 0.25 0.25 rg 65 616 Td (" + Escape("Resolution: Select a frame in Figma before copying the address bar URL.") + ") Tj ET");

            sb.AppendLine("BT /F2 10 Tf 0.1 0.1 0.1 rg 50 596 Td (" + Escape("Scenario B: Session Token Expiry / Re-authentication Needed") + ") Tj ET");
            sb.AppendLine("BT /F1 9.5 Tf 0.25 0.25 0.25 rg 65 582 Td (" + Escape("Resolution: Open Settings (gear icon) and click 'Disconnect Account'.") + ") Tj ET");
            sb.AppendLine("BT /F1 9.5 Tf 0.25 0.25 0.25 rg 65 568 Td (" + Escape("This purges session.dat and returns you to Step 1 for clean re-login.") + ") Tj ET");

            sb.AppendLine("BT /F2 10 Tf 0.1 0.1 0.1 rg 50 548 Td (" + Escape("Scenario C: Empty USS Style Blocks") + ") Tj ET");
            sb.AppendLine("BT /F1 9.5 Tf 0.25 0.25 0.25 rg 65 534 Td (" + Escape("Resolution: Ensure your Figma file uses explicit fills or text styles.") + ") Tj ET");

            // Decorative Box
            sb.AppendLine("0.96 0.98 0.92 rg 36 440 540 70 re f");
            sb.AppendLine("0.83 1.00 0.20 rg 36 440 540 70 re S");
            sb.AppendLine("BT /F2 11 Tf 0.15 0.25 0.05 rg 50 490 Td (" + Escape("Ready to Transform Your UI?") + ") Tj ET");
            sb.AppendLine("BT /F1 9.5 Tf 0.2 0.2 0.2 rg 50 472 Td (" + Escape("For further updates, documentation, or issue reports, visit the official repository.") + ") Tj ET");
            sb.AppendLine("BT /F1 9.5 Tf 0.2 0.2 0.2 rg 50 456 Td (" + Escape("UnityUI Transformer — Enterprise Figma Conversion Solution") + ") Tj ET");

            // Page Footer
            sb.AppendLine("0.85 0.85 0.85 rg 36 40 540 0.8 re f");
            sb.AppendLine("BT /F1 8.5 Tf 0.5 0.5 0.5 rg 36 26 Td (" + Escape("UnityUI Transformer Manual — Page 3 of 3") + ") Tj ET");
            sb.AppendLine("BT /F1 8.5 Tf 0.5 0.5 0.5 rg 450 26 Td (" + Escape("Confidential & Proprietary") + ") Tj ET");

            return sb.ToString();
        }
    }
}
