import { FastifyInstance, FastifyRequest, FastifyReply } from 'fastify';
import fs from 'fs';
import path from 'path';
import { getConfig } from '../config';

export interface SyncAssetPayload {
  path: string;
  data: string; // base64 encoded
}

export interface SyncRequestPayload {
  packageName?: string;
  document: any;
  assets?: SyncAssetPayload[];
}

export async function registerSyncRoute(fastify: FastifyInstance): Promise<void> {
  fastify.post('/sync', async (request: FastifyRequest, reply: FastifyReply) => {
    const config = getConfig();
    const body = request.body as SyncRequestPayload;

    if (!body || !body.document) {
      return reply.status(400).send({
        success: false,
        message: 'Invalid payload: missing "document" property.',
      });
    }

    const packageName = body.packageName || 'FigmaSyncPackage';
    const targetFolder = path.join(config.unityProjectPath, packageName);

    if (!fs.existsSync(targetFolder)) {
      fs.mkdirSync(targetFolder, { recursive: true });
    }

    // 1. Write IR document JSON
    const irDocPath = path.join(targetFolder, 'ir-document.json');
    fs.writeFileSync(irDocPath, JSON.stringify(body.document, null, 2), 'utf-8');
    let filesWritten = 1;

    // 2. Write base64 asset files
    if (body.assets && Array.isArray(body.assets)) {
      for (const asset of body.assets) {
        if (!asset.path || !asset.data) continue;

        const assetFullPath = path.join(targetFolder, asset.path);
        const parentDir = path.dirname(assetFullPath);

        if (!fs.existsSync(parentDir)) {
          fs.mkdirSync(parentDir, { recursive: true });
        }

        const buffer = Buffer.from(asset.data, 'base64');
        fs.writeFileSync(assetFullPath, buffer);
        filesWritten++;
      }
    }

    return reply.status(200).send({
      success: true,
      message: `Successfully synced package '${packageName}' to Unity project.`,
      outputPath: targetFolder,
      filesWritten,
    });
  });
}
