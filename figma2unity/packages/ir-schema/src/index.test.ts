import { describe, it, expect } from 'vitest';
import { readFileSync } from 'fs';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';
import { IRDocumentSchema } from './index.js';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const sampleCardDoc = JSON.parse(
  readFileSync(resolve(__dirname, './fixtures/sample-card-document.json'), 'utf-8')
);
const tokensOnlyDoc = JSON.parse(
  readFileSync(resolve(__dirname, './fixtures/tokens-only-document.json'), 'utf-8')
);

describe('IRDocumentSchema', () => {
  it('validates and matches snapshot for sample card document fixture', () => {
    const parseResult = IRDocumentSchema.safeParse(sampleCardDoc);
    expect(parseResult.success).toBe(true);
    if (parseResult.success) {
      expect(parseResult.data).toMatchSnapshot();
    }
  });

  it('validates and matches snapshot for tokens only document fixture', () => {
    const parseResult = IRDocumentSchema.safeParse(tokensOnlyDoc);
    expect(parseResult.success).toBe(true);
    if (parseResult.success) {
      expect(parseResult.data).toMatchSnapshot();
    }
  });

  it('handles UNSUPPORTED node fallback parsing gracefully', () => {
    const docWithUnsupported = {
      schemaVersion: '1.0.0',
      metadata: {
        exportedAt: '2026-08-17T12:00:00.000Z',
        generatorVersion: '1.0.0',
      },
      tokens: { colors: [], typography: [], spacing: [], effects: [] },
      rootNodes: [
        {
          id: '99:1',
          name: 'Widget3D',
          type: 'UNSUPPORTED',
          figmaNodeType: 'WIDGET',
          visible: true,
          opacity: 1,
          rotation: 0,
          bounds: { x: 0, y: 0, width: 100, height: 100 },
          fills: [],
          strokes: [],
          cornerRadius: { topLeft: 0, topRight: 0, bottomRight: 0, bottomLeft: 0 },
          effects: [],
        },
      ],
    };

    const parseResult = IRDocumentSchema.safeParse(docWithUnsupported);
    expect(parseResult.success).toBe(true);
    if (parseResult.success) {
      expect(parseResult.data.rootNodes[0].type).toBe('UNSUPPORTED');
    }
  });
});
