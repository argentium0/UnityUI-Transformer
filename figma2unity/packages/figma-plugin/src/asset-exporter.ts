export interface ExportedAsset {
  path: string;
  data: Uint8Array;
  mimeType: 'image/png';
}

export interface ExportMetrics {
  rasterCount: number;
  vectorCount: number;
  failedCount: number;
}

export function sanitizeAssetFileName(fileName: string): string {
  if (!fileName) return fileName;
  let sanitized = fileName.replace(/[@\s]/g, '_');
  return sanitized.replace(/[^a-zA-Z0-9_.-]/g, '');
}

export class AssetExporter {
  public async exportAssets(nodes: readonly SceneNode[]): Promise<{ assets: ExportedAsset[]; metrics: ExportMetrics }> {
    const assets: ExportedAsset[] = [];
    const metrics: ExportMetrics = { rasterCount: 0, vectorCount: 0, failedCount: 0 };

    for (const node of nodes) {
      const rawId = node.id.replace(/[:/]/g, '_');
      const isVector = ['VECTOR', 'STAR', 'POLYGON', 'BOOLEAN_OPERATION', 'LINE'].includes(node.type);
      const fills = Array.isArray((node as any).fills) ? (node as any).fills : [];
      const hasImageFill = fills.some((f: any) => f.type === 'IMAGE');
      const hasGradientFill = fills.some((f: any) => f && typeof f.type === 'string' && f.type.startsWith('GRADIENT_'));
      const hasMultipleFills = fills.length > 1;
      const requiresFlattenedImage = hasGradientFill || hasMultipleFills;
      const isExplicitExport = Array.isArray((node as any).exportSettings) && (node as any).exportSettings.length > 0;
      const isFrame = ['FRAME', 'SECTION'].includes(node.type);
      const hasChildren = 'children' in node && Array.isArray((node as any).children) && (node as any).children.length > 0;
      const isUnsupported = !['FRAME', 'SECTION', 'GROUP', 'RECTANGLE', 'ELLIPSE', 'TEXT', 'INSTANCE', 'COMPONENT', 'COMPONENT_SET'].includes(node.type) && !isVector;

      // STOP FLATTENING FRAMES (UNLESS gradient/multi-fill, image fill, or explicit export):
      if (isFrame && hasChildren && !hasImageFill && !requiresFlattenedImage && !isExplicitExport) {
        continue;
      }

      if (isVector) {
        try {
          const [png1x, png2x, png3x] = await Promise.all([
            this.exportImageFillOrNode(node, 1),
            this.exportImageFillOrNode(node, 2),
            this.exportImageFillOrNode(node, 3),
          ]);

          assets.push({ path: `exports/images/${sanitizeAssetFileName(`${rawId}_1x.png`)}`, data: png1x, mimeType: 'image/png' });
          assets.push({ path: `exports/images/${sanitizeAssetFileName(`${rawId}_2x.png`)}`, data: png2x, mimeType: 'image/png' });
          assets.push({ path: `exports/images/${sanitizeAssetFileName(`${rawId}_3x.png`)}`, data: png3x, mimeType: 'image/png' });
          metrics.vectorCount++;
        } catch {
          metrics.failedCount++;
        }
      } else if (hasImageFill || requiresFlattenedImage || isExplicitExport || isUnsupported || (!isFrame && node.type === 'RECTANGLE')) {
        try {
          const [png1x, png2x, png3x] = await Promise.all([
            this.exportImageFillOrNode(node, 1),
            this.exportImageFillOrNode(node, 2),
            this.exportImageFillOrNode(node, 3),
          ]);

          assets.push({ path: `exports/images/${sanitizeAssetFileName(`${rawId}_1x.png`)}`, data: png1x, mimeType: 'image/png' });
          assets.push({ path: `exports/images/${sanitizeAssetFileName(`${rawId}_2x.png`)}`, data: png2x, mimeType: 'image/png' });
          assets.push({ path: `exports/images/${sanitizeAssetFileName(`${rawId}_3x.png`)}`, data: png3x, mimeType: 'image/png' });
          metrics.rasterCount++;
        } catch {
          metrics.failedCount++;
        }
      }
    }

    return { assets, metrics };
  }

  private async exportImageFillOrNode(node: SceneNode, scale: number): Promise<Uint8Array> {
    // 1. Extract raw background image fill bytes directly from Figma image store if present,
    // avoiding calling exportAsync on frame nodes which would render child elements into the PNG.
    if (Array.isArray((node as any).fills)) {
      const imageFill = (node as any).fills.find((f: any) => f.type === 'IMAGE' && f.imageHash);
      if (imageFill && typeof figma !== 'undefined' && typeof (figma as any).getImageByHash === 'function') {
        try {
          const imageObj = (figma as any).getImageByHash(imageFill.imageHash);
          if (imageObj && typeof imageObj.getBytesAsync === 'function') {
            return await imageObj.getBytesAsync();
          }
        } catch {
          // Fall through to exportAsync if getBytesAsync fails
        }
      }
    }

    // 2. Fallback to exportAsync on standalone nodes
    if (typeof node.exportAsync === 'function') {
      return await node.exportAsync({
        format: 'PNG',
        constraint: { type: 'SCALE', value: scale },
      });
    }

    // 3. Mock fallback for test environment
    return this.encodeString(`MOCK_PNG_DATA_SCALE_${scale}`);
  }

  private encodeString(str: string): Uint8Array {
    if (typeof TextEncoder !== 'undefined') {
      return new TextEncoder().encode(str);
    }
    const bytes = new Uint8Array(str.length);
    for (let i = 0; i < str.length; i++) {
      bytes[i] = str.charCodeAt(i) & 0xff;
    }
    return bytes;
  }
}


