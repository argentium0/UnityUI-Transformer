import { FastifyInstance } from 'fastify';
import fs from 'fs/promises';
import path from 'path';

import { getConfig } from '../config';

// Changed to a named export matching server.ts expectations
export async function registerSyncRoute(fastify: FastifyInstance) {
  fastify.post('/sync', async (request, reply) => {
    const config = getConfig();
    const STAGING_DIR = path.resolve(config.unityProjectPath, 'Temp/Figma2UnitySync');
    const { document, assets } = request.body as any;

    await fs.rm(STAGING_DIR, { recursive: true, force: true }).catch(() => { });
    await fs.mkdir(path.join(STAGING_DIR, 'exports', 'images'), { recursive: true });
    await fs.mkdir(path.join(STAGING_DIR, 'exports', 'vectors'), { recursive: true });

    if (assets && assets.length > 0) {
      await Promise.all(assets.map(async (asset: any) => {
        const fileName = path.basename(asset.path);
        const buffer = Buffer.from(asset.data, 'base64');
        const isSvg = fileName.endsWith('.svg');
        const folder = isSvg ? 'vectors' : 'images';
        await fs.writeFile(path.join(STAGING_DIR, 'exports', folder, fileName), buffer);
      }));
    }

    // Write the document
    await fs.writeFile(path.join(STAGING_DIR, 'ir-document.json'), JSON.stringify(document, null, 2), 'utf8');

    // Wait 50ms to ensure OS-level file handles are fully flushed before the C# watcher fires
    await new Promise(resolve => setTimeout(resolve, 50));

    // Create the completion trigger file
    await fs.writeFile(path.join(STAGING_DIR, 'sync.complete'), '', 'utf8');

    return { success: true, message: 'Live sync payload successfully written to Unity.' };
  });
}

async function exists(p: string) {
  try {
    await fs.access(p);
    return true;
  } catch {
    return false;
  }
}