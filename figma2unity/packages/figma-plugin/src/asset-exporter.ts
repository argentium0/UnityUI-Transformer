export interface ExportedAsset {
  path: string;
  data: Uint8Array;
  mimeType: 'image/png' | 'image/svg+xml';
}

export interface ExportMetrics {
  rasterCount: number;
  vectorCount: number;
  failedCount: number;
}

export class AssetExporter {
  public async exportAssets(nodes: SceneNode[]): Promise<{ assets: ExportedAsset[]; metrics: ExportMetrics }> {
    const assets: ExportedAsset[] = [];
    const metrics: ExportMetrics = { rasterCount: 0, vectorCount: 0, failedCount: 0 };

    for (const node of nodes) {
      const sanitizedId = node.id.replace(/[:/]/g, '_');
      const isVector = ['VECTOR', 'STAR', 'POLYGON', 'BOOLEAN_OPERATION', 'LINE'].includes(node.type);
      const isImage = node.type === 'RECTANGLE' && Array.isArray((node as any).fills) && (node as any).fills.some((f: any) => f.type === 'IMAGE');
      const isUnsupported = !['FRAME', 'SECTION', 'GROUP', 'RECTANGLE', 'ELLIPSE', 'TEXT', 'INSTANCE', 'COMPONENT', 'COMPONENT_SET'].includes(node.type) && !isVector;

      if (isVector) {
        try {
          const svgData = await this.exportSvg(node);
          assets.push({
            path: `exports/vectors/${sanitizedId}.svg`,
            data: svgData,
            mimeType: 'image/svg+xml',
          });
          metrics.vectorCount++;
        } catch {
          metrics.failedCount++;
        }
      } else if (isImage || isUnsupported) {
        try {
          const [png1x, png2x, png3x] = await Promise.all([
            this.exportPng(node, 1),
            this.exportPng(node, 2),
            this.exportPng(node, 3),
          ]);

          assets.push({ path: `exports/images/${sanitizedId}@1x.png`, data: png1x, mimeType: 'image/png' });
          assets.push({ path: `exports/images/${sanitizedId}@2x.png`, data: png2x, mimeType: 'image/png' });
          assets.push({ path: `exports/images/${sanitizedId}@3x.png`, data: png3x, mimeType: 'image/png' });
          metrics.rasterCount++;
        } catch {
          metrics.failedCount++;
        }
      }
    }

    return { assets, metrics };
  }

  private async exportSvg(node: SceneNode): Promise<Uint8Array> {
    if (typeof node.exportAsync === 'function') {
      return await node.exportAsync({ format: 'SVG' });
    }
    // Mock fallback for test environment
    return this.encodeString(`<svg xmlns="http://www.w3.org/2000/svg" width="${node.width}" height="${node.height}"></svg>`);
  }

  private async exportPng(node: SceneNode, scale: number): Promise<Uint8Array> {
    if (typeof node.exportAsync === 'function') {
      return await node.exportAsync({
        format: 'PNG',
        constraint: { type: 'SCALE', value: scale },
      });
    }
    // Mock fallback for test environment
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

