import * as esbuild from 'esbuild';
import { readFileSync, writeFileSync, mkdirSync, existsSync } from 'fs';

if (!existsSync('dist')) {
  mkdirSync('dist', { recursive: true });
}

// Build code.ts (Figma main thread)
await esbuild.build({
  entryPoints: ['src/code.ts'],
  bundle: true,
  outfile: 'dist/code.js',
  target: 'es2020',
  logLevel: 'info',
});

// Build ui.tsx (Preact UI iframe)
await esbuild.build({
  entryPoints: ['src/ui.tsx'],
  bundle: true,
  outfile: 'dist/ui.js',
  target: 'es2020',
  jsxFactory: 'h',
  jsxFragment: 'Fragment',
  jsxImportSource: 'preact',
  logLevel: 'info',
});

// Inline ui.js into dist/ui.html for Figma iframe compatibility
const htmlContent = readFileSync('src/ui.html', 'utf-8');
const jsContent = readFileSync('dist/ui.js', 'utf-8');
const bundledHtml = htmlContent.replace(
  '<script src="ui.js"></script>',
  `<script>\n${jsContent}\n</script>`
);

writeFileSync('dist/ui.html', bundledHtml, 'utf-8');
console.log('Successfully bundled inline HTML into dist/ui.html');

