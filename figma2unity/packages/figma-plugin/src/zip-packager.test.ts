import { describe, it, expect } from 'vitest';
import { ZipPackager } from './zip-packager.js';
import { AssetExporter } from './asset-exporter.js';
import { NodeTraverser } from './traversal.js';
import { IRDocumentSchema } from '@figma2unity/ir-schema';

describe('ZipPackager & AssetExporter Integration', () => {
  it('packages IR document and exported assets into a valid .f2u.zip archive', async () => {
    const mockTree: any[] = [
      {
        id: '10:1',
        name: 'CardFrame',
        type: 'FRAME',
        visible: true,
        x: 0,
        y: 0,
        width: 300,
        height: 200,
        fills: [],
        strokes: [],
        children: [
          {
            id: '10:2',
            name: 'StarIcon',
            type: 'STAR',
            visible: true,
            x: 10,
            y: 10,
            width: 24,
            height: 24,
            fills: [],
            strokes: [],
          },
          {
            id: '10:3',
            name: 'CoverImage',
            type: 'RECTANGLE',
            visible: true,
            x: 0,
            y: 40,
            width: 300,
            height: 160,
            fills: [{ type: 'IMAGE' }],
            strokes: [],
          },
        ],
      },
    ];

    // 1. Traverse node tree
    const traverser = new NodeTraverser();
    const traversalResult = await traverser.traverseNodes(mockTree, 'TestCardFile');

    // 2. Export assets
    const assetExporter = new AssetExporter();
    const { assets, metrics } = await assetExporter.exportAssets(traverser.getVisitedNodes());

    expect(metrics.vectorCount).toBe(1); // StarIcon rasterized as PNG
    expect(metrics.rasterCount).toBe(1); // CoverImage exported as PNG 1x/2x/3x

    // 3. Package into .f2u.zip
    const zipBytes = ZipPackager.createF2uZip(traversalResult.document, assets);
    expect(zipBytes.length).toBeGreaterThan(0);

    // 4. Unpack zip and verify contents
    const { document, fileList } = ZipPackager.readF2uZip(zipBytes);

    expect(fileList).toContain('ir-document.json');
    expect(fileList).toContain('exports/images/10_2@1x.png');
    expect(fileList).toContain('exports/images/10_2@2x.png');
    expect(fileList).toContain('exports/images/10_2@3x.png');
    expect(fileList).toContain('exports/images/10_3@1x.png');
    expect(fileList).toContain('exports/images/10_3@2x.png');
    expect(fileList).toContain('exports/images/10_3@3x.png');

    // Validate unzipped document against Zod IRDocumentSchema
    const parseResult = IRDocumentSchema.safeParse(document);
    expect(parseResult.success).toBe(true);
  });
});
