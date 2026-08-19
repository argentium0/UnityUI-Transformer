import { describe, it, expect, beforeAll, afterAll } from 'vitest';
import { createServer } from './server';
import fs from 'fs';
import path from 'path';

describe('Bridge Server API', () => {
  const app = createServer();

  beforeAll(async () => {
    await app.ready();
  });

  afterAll(async () => {
    await app.close();
  });

  it('GET /health returns status ok', async () => {
    const response = await app.inject({
      method: 'GET',
      url: '/health',
    });

    expect(response.statusCode).toBe(200);
    const body = JSON.parse(response.body);
    expect(body.status).toBe('ok');
    expect(body.server).toBe('figma2unity-bridge-server');
  });

  it('POST /sync receives document and assets and writes to disk', async () => {
    const testDoc = {
      schemaVersion: '1.0.0',
      metadata: { exportedAt: new Date().toISOString() },
      tokens: { colors: [], typography: [], spacing: [], effects: [] },
      rootNodes: [],
    };

    const response = await app.inject({
      method: 'POST',
      url: '/sync',
      payload: {
        packageName: 'TestBridgeSync',
        document: testDoc,
        assets: [
          {
            path: 'exports/images/test.png',
            data: Buffer.from('fake-png-binary-data').toString('base64'),
          },
        ],
      },
    });

    expect(response.statusCode).toBe(200);
    const body = JSON.parse(response.body);
    expect(body.success).toBe(true);
    expect(body.filesWritten).toBe(2);

    // Verify ir-document.json and assets exist in staging output path
    const docPath = path.join(body.outputPath, 'ir-document.json');
    expect(fs.existsSync(docPath)).toBe(true);
    const diskDoc = JSON.parse(fs.readFileSync(docPath, 'utf8'));
    expect(diskDoc.schemaVersion).toBe('1.0.0');

    const assetPath = path.join(body.outputPath, 'exports', 'images', 'test.png');
    expect(fs.existsSync(assetPath)).toBe(true);

    const triggerPath = path.join(body.outputPath, 'sync.complete');
    expect(fs.existsSync(triggerPath)).toBe(true);

    // Cleanup generated temp test files if present
    if (body.outputPath && fs.existsSync(body.outputPath)) {
      try {
        fs.rmSync(body.outputPath, { recursive: true, force: true });
      } catch {
        // Ignore
      }
    }
  });
});
