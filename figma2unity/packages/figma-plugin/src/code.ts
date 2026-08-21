/// <reference types="@figma/plugin-typings" />
import { NodeTraverser } from './traversal.js';
import { AssetExporter } from './asset-exporter.js';

figma.showUI(__html__, { width: 340, height: 520 });

figma.ui.onmessage = async (msg: { type: string }) => {
  if (msg.type === 'START_SYNC') {
    try {
      const selection = figma.currentPage.selection;
      if (!selection || selection.length === 0) {
        figma.ui.postMessage({
          type: 'TRAVERSAL_ERROR',
          error: 'Select a frame to sync.',
        });
        return;
      }

      const selectedNode = selection[0];
      const targetNodes = [selectedNode];
      const fileName = (selectedNode.name || figma.root.name || 'FigmaExport').replace(/[^a-zA-Z0-9_-]/g, '_');

      // 1. Traverse node tree & tokens
      const traverser = new NodeTraverser();
      const traversalResult = await traverser.traverseNodes(targetNodes, fileName);

      // Log outgoing payload to Figma plugin console
      console.log('[Figma Plugin Payload Output]', JSON.stringify(traversalResult.document, null, 2));

      // 2. Export assets (PNGs & SVGs)
      const assetExporter = new AssetExporter();
      const { assets, metrics } = await assetExporter.exportAssets(traverser.getVisitedNodes());

      const totalNodes = traversalResult.summary.totalNodes;
      const unsupportedCount = traversalResult.summary.unsupportedCount;
      const rasterCount = metrics.rasterCount;
      const vectorCount = metrics.vectorCount;
      const supportedCount = Math.max(0, totalNodes - rasterCount - vectorCount - unsupportedCount);

      // 3. Convert assets to transferable array format for iframe postMessage clone
      const serializedAssets = assets.map((asset) => ({
        path: asset.path,
        data: Array.from(asset.data),
        mimeType: asset.mimeType,
      }));

      // 4. Send document, serialized assets, and metrics to UI iframe for zip creation and download
      figma.ui.postMessage({
        type: 'EXPORT_DATA',
        document: traversalResult.document,
        assets: serializedAssets,
        fileName: `${fileName}.f2u.zip`,
        summary: {
          totalNodes,
          supportedCount,
          rasterCount,
          vectorCount,
          unsupportedCount,
          nodeCounts: traversalResult.summary.nodeCounts,
        },
      });
    } catch (err: any) {
      figma.ui.postMessage({
        type: 'TRAVERSAL_ERROR',
        error: err?.message || String(err),
      });
    }
  } else if (msg.type === 'CANCEL') {
    figma.closePlugin();
  }
};

