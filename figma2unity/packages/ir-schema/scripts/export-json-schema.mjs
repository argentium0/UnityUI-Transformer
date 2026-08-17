import { zodToJsonSchema } from 'zod-to-json-schema';
import { IRDocumentSchema } from '../dist/index.js';
import { writeFileSync, mkdirSync, existsSync } from 'fs';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const jsonSchema = zodToJsonSchema(IRDocumentSchema, 'IRDocument');

const outDir = resolve(__dirname, '../dist');
if (!existsSync(outDir)) {
  mkdirSync(outDir, { recursive: true });
}

const outFile = resolve(outDir, 'ir-schema.json');
writeFileSync(outFile, JSON.stringify(jsonSchema, null, 2), 'utf-8');
console.log(`JSON Schema exported successfully to ${outFile}`);
