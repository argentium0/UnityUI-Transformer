import { describe, it, expect, beforeAll, afterAll } from 'vitest';
import { createServer } from './server';
import fs from 'fs';

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
            data: Buffer.from('fake-png-data').toString('base64'),
          },
        ],
      },
    });

    expect(response.statusCode).toBe(200);
    const body = JSON.parse(response.body);
    expect(body.success).toBe(true);
    expect(body.filesWritten).toBe(2);

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
