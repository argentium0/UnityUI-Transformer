import { zipSync, unzipSync } from 'fflate';
import type { IRDocument } from '@figma2unity/ir-schema';
import type { ExportedAsset } from './asset-exporter.js';

export class ZipPackager {
  public static createF2uZip(document: IRDocument, assets: ExportedAsset[]): Uint8Array {
    const textEncoder = new TextEncoder();
    const zipData: Record<string, Uint8Array> = {};

    // 1. Add IR Document JSON
    const docJson = JSON.stringify(document, null, 2);
    zipData['ir-document.json'] = textEncoder.encode(docJson);

    // 2. Add Exported Assets
    for (const asset of assets) {
      zipData[asset.path] = asset.data;
    }

    // 3. Compress synchronously using fflate
    return zipSync(zipData);
  }

  public static readF2uZip(zipBuffer: Uint8Array): { document: IRDocument; fileList: string[] } {
    const unzipped = unzipSync(zipBuffer);
    const textDecoder = new TextDecoder();
    const fileList = Object.keys(unzipped);

    const docBytes = unzipped['ir-document.json'];
    if (!docBytes) {
      throw new Error('Invalid .f2u.zip archive: missing ir-document.json');
    }

    const docJson = textDecoder.decode(docBytes);
    const document = JSON.parse(docJson) as IRDocument;

    return { document, fileList };
  }
}
