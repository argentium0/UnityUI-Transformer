import { FastifyInstance } from 'fastify';
import fs from 'fs/promises';
import { writeFileSync } from 'fs';
import path from 'path';

import { getConfig } from '../config';

export async function registerSyncRoute(fastify: FastifyInstance) {
  fastify.post('/sync', { bodyLimit: 50 * 1024 * 1024 }, async (request, reply) => {
    const config = getConfig();
    const STAGING_DIR = path.resolve(config.unityProjectPath, 'Temp/Figma2UnitySync');
    const { document, assets } = (request.body || {}) as any;

    await fs.rm(STAGING_DIR, { recursive: true, force: true }).catch(() => { });
    await fs.mkdir(path.join(STAGING_DIR, 'exports', 'images'), { recursive: true });
    await fs.mkdir(path.join(STAGING_DIR, 'exports', 'vectors'), { recursive: true });

    let filesWritten = 0;

    if (assets && Array.isArray(assets) && assets.length > 0) {
      // Execute file writing synchronously with pure binary Buffer to guarantee binary integrity
      assets.forEach((asset: any) => {
        const fileName = path.basename(asset.path || asset.name);
        let buffer: Buffer;

        if (Buffer.isBuffer(asset.data)) {
          buffer = asset.data;
        } else if (typeof asset.data === 'string') {
          buffer = Buffer.from(asset.data, 'base64');
        } else if (Array.isArray(asset.data) || asset.data instanceof Uint8Array) {
          buffer = Buffer.from(asset.data);
        } else {
          buffer = Buffer.from('');
        }

        const isSvg = fileName.endsWith('.svg');
        const folder = isSvg ? 'vectors' : 'images';
        
        // Strictly write binary buffer without string encoding parameters
        writeFileSync(path.join(STAGING_DIR, 'exports', folder, fileName), buffer);
        filesWritten++;
      });
    }

    // Write the full Figma document using strict UTF-8
    await fs.writeFile(path.join(STAGING_DIR, 'ir-document.json'), JSON.stringify(document, null, 2), 'utf8');
    filesWritten++;

    // Wait 50ms to ensure OS-level file handles are fully flushed before the C# watcher fires
    await new Promise(resolve => setTimeout(resolve, 50));

    // Create the completion trigger file
    await fs.writeFile(path.join(STAGING_DIR, 'sync.complete'), '', 'utf8');

    return {
      success: true,
      filesWritten,
      outputPath: STAGING_DIR,
      message: 'Live sync payload successfully written to Unity.'
    };
  });
}