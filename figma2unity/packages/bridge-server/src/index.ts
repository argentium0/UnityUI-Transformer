import { createServer } from './server';
import { getConfig } from './config';

const config = getConfig();
const server = createServer();

server.listen({ port: config.port, host: '0.0.0.0' }, (err: Error | null, address: string) => {
  if (err) {
    server.log.error(err);
    process.exit(1);
  }
  server.log.info(`[Figma2Unity] Bridge server listening at ${address}`);
});
