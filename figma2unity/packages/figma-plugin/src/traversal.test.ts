import { describe, it, expect } from 'vitest';
import { NodeTraverser } from './traversal.js';
import { IRDocumentSchema } from '@figma2unity/ir-schema';

describe('NodeTraverser', () => {
  it('traverses mock Figma node tree and produces a valid IRDocument', async () => {
    const mockTree: any[] = [
      {
        id: '1:10',
        name: 'HeaderFrame',
        type: 'FRAME',
        visible: true,
        x: 0,
        y: 0,
        width: 400,
        height: 100,
        layoutMode: 'HORIZONTAL',
        itemSpacing: 12,
        paddingTop: 8,
        paddingRight: 16,
        paddingBottom: 8,
        paddingLeft: 16,
        primaryAxisSizingMode: 'FIXED',
        counterAxisSizingMode: 'AUTO',
        clipsContent: true,
        fills: [{ type: 'SOLID', color: { r: 0.2, g: 0.4, b: 0.8 }, opacity: 1 }],
        strokes: [],
        children: [
          {
            id: '1:11',
            name: 'TitleText',
            type: 'TEXT',
            visible: true,
            x: 16,
            y: 8,
            width: 200,
            height: 24,
            characters: 'Figma2Unity Importer',
            fontName: { family: 'Inter', style: 'Bold' },
            fontSize: 16,
            textAlignHorizontal: 'LEFT',
            fills: [],
            strokes: [],
          },
          {
            id: '1:12',
            name: 'UnsupportedWidget',
            type: 'WIDGET',
            visible: true,
            x: 220,
            y: 8,
            width: 50,
            height: 50,
            fills: [],
            strokes: [],
          },
        ],
      },
    ];

    const traverser = new NodeTraverser();
    const result = await traverser.traverseNodes(mockTree, 'MockFile');

    expect(result.summary.totalNodes).toBe(3);
    expect(result.summary.unsupportedCount).toBe(1);
    expect(result.summary.nodeCounts['FRAME']).toBe(1);
    expect(result.summary.nodeCounts['TEXT']).toBe(1);
    expect(result.summary.nodeCounts['UNSUPPORTED']).toBe(1);

    // Validate the generated document against Zod IRDocumentSchema
    const parseResult = IRDocumentSchema.safeParse(result.document);
    expect(parseResult.success).toBe(true);

    if (parseResult.success) {
      const rootFrame = parseResult.data.rootNodes[0];
      expect(rootFrame.type).toBe('FRAME');
      if (rootFrame.type === 'FRAME') {
        expect(rootFrame.autoLayout?.layoutMode).toBe('HORIZONTAL');
        expect(rootFrame.autoLayout?.gap).toBe(12);
        expect(rootFrame.autoLayout?.padding.right).toBe(16);
        expect(rootFrame.children.length).toBe(2);
        expect(rootFrame.children[1].type).toBe('UNSUPPORTED');
      }
    }
  });
});
