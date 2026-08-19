import Fastify, { FastifyInstance } from 'fastify';
import cors from '@fastify/cors';
import { registerSyncRoute } from './routes/sync';

export function createServer(): FastifyInstance {
  const app = Fastify({
    logger: true,
    bodyLimit: 50 * 1024 * 1024, // 50 MB
  });

  // Register CORS plugin allowing requests from Figma plugin iframe origin
  app.register(cors, {
    origin: '*',
    methods: ['GET', 'POST', 'OPTIONS'],
    allowedHeaders: ['Content-Type', 'Authorization'],
  });

  // Health check route
  app.get('/health', async () => {
    return { status: 'ok', server: 'figma2unity-bridge-server' };
  });

  // Register /sync route
  app.register(registerSyncRoute);

  return app;
}
