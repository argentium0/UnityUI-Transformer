import path from 'path';
import dotenv from 'dotenv';

dotenv.config();

export interface ServerConfig {
  port: number;
  unityProjectPath: string;
}

export function getConfig(): ServerConfig {
  const port = parseInt(process.env.PORT || '3000', 10);
  const defaultUnityPath = path.resolve(
    __dirname,
    '../../../../unity/Temp/Figma2UnitySync'
  );
  const unityProjectPath = process.env.UNITY_PROJECT_PATH
    ? path.resolve(process.env.UNITY_PROJECT_PATH)
    : defaultUnityPath;

  return {
    port,
    unityProjectPath,
  };
}
