import type {
  IRDocument,
  IRNode,
  Tokens,
  ColorToken,
  TypographyToken,
  EffectToken,
  AutoLayout,
  Bounds,
  Fill,
  Stroke,
  CornerRadius,
  EffectValue,
} from '@figma2unity/ir-schema';
import { sanitizeAssetFileName } from './asset-exporter.js';

export interface TraversalSummary {
  totalNodes: number;
  unsupportedCount: number;
  nodeCounts: Record<string, number>;
}

export interface TraversalResult {
  document: IRDocument;
  summary: TraversalSummary;
}

export class NodeTraverser {
  private tokens: Tokens = {
    colors: [],
    typography: [],
    spacing: [],
    effects: [],
  };
  private tokenMap = new Map<string, string>(); // figma style/variable ID -> token ID
  private visitedNodes: SceneNode[] = [];
  private summary: TraversalSummary = {
    totalNodes: 0,
    unsupportedCount: 0,
    nodeCounts: {},
  };

  public getVisitedNodes(): SceneNode[] {
    return this.visitedNodes;
  }

  /**
   * Main entry point for walking Figma nodes and extracting an IRDocument.
   */
  public async traverseNodes(
    nodes: readonly SceneNode[],
    fileName = 'FigmaFile'
  ): Promise<TraversalResult> {
    this.resetState();

    // Extract design tokens from local styles / variables if available
    await this.extractTokens();

    const rootNodes: IRNode[] = [];
    for (const node of nodes) {
      if (node.visible !== false) {
        const irNode = this.convertNode(node);
        rootNodes.push(irNode);
      }
    }

    const document: IRDocument = {
      schemaVersion: '1.0.0',
      metadata: {
        exportedAt: new Date().toISOString(),
        figmaFileName: fileName,
        generatorVersion: '1.0.0',
      },
      tokens: this.tokens,
      rootNodes,
    };

    return {
      document,
      summary: this.summary,
    };
  }

  private resetState(): void {
    this.tokens = { colors: [], typography: [], spacing: [], effects: [] };
    this.tokenMap.clear();
    this.visitedNodes = [];
    this.summary = { totalNodes: 0, unsupportedCount: 0, nodeCounts: {} };
  }

  private incrementCount(type: string): void {
    this.summary.totalNodes++;
    this.summary.nodeCounts[type] = (this.summary.nodeCounts[type] || 0) + 1;
  }

  private async extractTokens(): Promise<void> {
    if (typeof figma === 'undefined') return;

    try {
      // Paint Styles (Color Tokens)
      if (figma.getLocalPaintStyles) {
        const paintStyles = figma.getLocalPaintStyles();
        for (const style of paintStyles) {
          const paint = style.paints[0];
          if (paint && paint.type === 'SOLID') {
            const tokenId = `color-${style.id}`;
            const token: ColorToken = {
              id: tokenId,
              name: style.name,
              value: {
                r: paint.color.r,
                g: paint.color.g,
                b: paint.color.b,
                a: paint.opacity ?? 1,
              },
            };
            this.tokens.colors.push(token);
            this.tokenMap.set(style.id, tokenId);
          }
        }
      }

      // Text Styles (Typography Tokens)
      if (figma.getLocalTextStyles) {
        const textStyles = figma.getLocalTextStyles();
        for (const style of textStyles) {
          const tokenId = `type-${style.id}`;
          const token: TypographyToken = {
            id: tokenId,
            name: style.name,
            fontFamily: style.fontName.family,
            fontSize: style.fontSize,
            fontWeight: 400,
          };
          this.tokens.typography.push(token);
          this.tokenMap.set(style.id, tokenId);
        }
      }

      // Effect Styles (Effect Tokens)
      if (figma.getLocalEffectStyles) {
        const effectStyles = figma.getLocalEffectStyles();
        for (const style of effectStyles) {
          const tokenId = `effect-${style.id}`;
          const effects: EffectValue[] = style.effects.map((e) => {
            if (e.type === 'DROP_SHADOW' || e.type === 'INNER_SHADOW') {
              return {
                type: e.type,
                color: { r: e.color.r, g: e.color.g, b: e.color.b, a: e.color.a },
                offset: { x: e.offset.x, y: e.offset.y },
                radius: e.radius,
                spread: e.spread ?? 0,
              };
            }
            return {
              type: e.type === 'BACKGROUND_BLUR' ? 'BACKGROUND_BLUR' : 'LAYER_BLUR',
              radius: 'radius' in e ? (e as any).radius : 0,
            };
          });

          const token: EffectToken = {
            id: tokenId,
            name: style.name,
            effects,
          };
          this.tokens.effects.push(token);
          this.tokenMap.set(style.id, tokenId);
        }
      }
    } catch {
      // In non-Figma or mock test environments
    }
  }

  public convertNode(node: SceneNode): IRNode {
    this.visitedNodes.push(node);
    const baseFields = this.extractBaseFields(node);

    switch (node.type) {
      case 'FRAME':
      case 'SECTION': {
        this.incrementCount('FRAME');
        const frameNode = node as FrameNode;
        const children = this.convertChildren(frameNode.children);
        const autoLayout = this.extractAutoLayout(frameNode);

        // P0 Fix 3: Detect FRAME nodes with IMAGE fills and emit imageAssetRef
        const frameFills = Array.isArray(frameNode.fills) ? frameNode.fills : [];
        const frameHasImageFill = frameFills.some((f: Paint) => f.type === 'IMAGE');
        const frameResult: any = {
          ...baseFields,
          type: 'FRAME',
          autoLayout,
          clipsContent: frameNode.clipsContent ?? false,
          children,
        };
        if (frameHasImageFill) {
          frameResult.imageAssetRef = `images/${sanitizeAssetFileName(`${node.id.replace(/[:/]/g, '_')}_1x.png`)}`;
        }
        return frameResult;
      }

      case 'GROUP': {
        this.incrementCount('GROUP');
        const groupNode = node as GroupNode;
        // P1 Fix 4: Relativize group children coordinates to the group's own origin
        const children = this.convertChildrenRelativeToParent(
          groupNode.children,
          groupNode.x,
          groupNode.y
        );
        return {
          ...baseFields,
          type: 'GROUP',
          children,
        };
      }

      case 'RECTANGLE': {
        // Check if rectangle has an image fill
        const fills = (node as GeometryMixin).fills;
        const hasImageFill = Array.isArray(fills) && fills.some((f) => f.type === 'IMAGE');
        if (hasImageFill) {
          this.incrementCount('IMAGE');
          return {
            ...baseFields,
            type: 'IMAGE',
            imageAssetRef: `images/${sanitizeAssetFileName(`${node.id.replace(/[:/]/g, '_')}_1x.png`)}`,
            scaleMode: 'FILL',
          };
        }
        this.incrementCount('RECTANGLE');
        return {
          ...baseFields,
          type: 'RECTANGLE',
        };
      }

      case 'ELLIPSE': {
        this.incrementCount('ELLIPSE');
        return {
          ...baseFields,
          type: 'ELLIPSE',
        };
      }

      case 'VECTOR':
      case 'STAR':
      case 'POLYGON':
      case 'BOOLEAN_OPERATION':
      case 'LINE': {
        this.incrementCount('VECTOR');
        return {
          ...baseFields,
          type: 'VECTOR',
          svgAssetRef: `images/${sanitizeAssetFileName(`${node.id.replace(/[:/]/g, '_')}_1x.png`)}`,
        };
      }

      case 'TEXT': {
        this.incrementCount('TEXT');
        const textNode = node as TextNode;
        return {
          ...baseFields,
          type: 'TEXT',
          characters: textNode.characters || '',
          fontFamily: typeof textNode.fontName !== 'symbol' ? textNode.fontName?.family : undefined,
          fontSize: typeof textNode.fontSize === 'number' ? textNode.fontSize : undefined,
          textAlign: typeof textNode.textAlignHorizontal === 'string' ? (textNode.textAlignHorizontal as any) : 'LEFT',
          textAlignVertical: typeof textNode.textAlignVertical === 'string' ? (textNode.textAlignVertical as any) : 'TOP',
          textDecoration: typeof textNode.textDecoration === 'string' ? (textNode.textDecoration as any) : 'NONE',
        };
      }

      case 'INSTANCE':
      case 'COMPONENT':
      case 'COMPONENT_SET': {
        this.incrementCount('COMPONENT_INSTANCE');
        const instanceNode = node as InstanceNode;
        const children = 'children' in instanceNode ? this.convertChildren(instanceNode.children) : [];
        return {
          ...baseFields,
          type: 'COMPONENT_INSTANCE',
          componentId: instanceNode.mainComponent?.id || node.id,
          variantProperties: instanceNode.variantProperties || undefined,
          children,
        };
      }

      default: {
        this.incrementCount('UNSUPPORTED');
        this.summary.unsupportedCount++;
        const children = 'children' in node ? this.convertChildren((node as any).children) : undefined;
        return {
          ...baseFields,
          type: 'UNSUPPORTED',
          figmaNodeType: node.type,
          children,
        };
      }
    }
  }

  private convertChildren(children?: readonly SceneNode[]): IRNode[] {
    if (!children) return [];
    const result: IRNode[] = [];
    for (const child of children) {
      if (child.visible !== false) {
        result.push(this.convertNode(child));
      }
    }
    return result;
  }

  /**
   * P1 Fix 4: For GROUP children, Figma reports x/y in the parent's coordinate space
   * (absolute relative to the root frame), not relative to the group itself.
   * We must subtract the group's own origin to get local offsets.
   */
  private convertChildrenRelativeToParent(
    children: readonly SceneNode[] | undefined,
    parentX: number,
    parentY: number
  ): IRNode[] {
    if (!children) return [];
    const result: IRNode[] = [];
    for (const child of children) {
      if (child.visible !== false) {
        const irNode = this.convertNode(child);
        if (irNode.bounds) {
          irNode.bounds.x -= parentX;
          irNode.bounds.y -= parentY;
        }
        result.push(irNode);
      }
    }
    return result;
  }

  private extractBaseFields(node: SceneNode) {
    const bounds: Bounds = {
      x: node.x ?? 0,
      y: node.y ?? 0,
      width: node.width ?? 0,
      height: node.height ?? 0,
    };

    const fills = this.extractFills(node);
    const strokes = this.extractStrokes(node);
    const cornerRadius = this.extractCornerRadius(node);
    const effects = this.extractEffects(node);

    // P1 Fix 5: Extract layoutAlign as a per-child property (not from autoLayout)
    const layoutAlign = 'layoutAlign' in node ? ((node as any).layoutAlign as string) || 'INHERIT' : 'INHERIT';

    return {
      id: node.id,
      name: node.name,
      visible: node.visible ?? true,
      opacity: 'opacity' in node ? (node as any).opacity : 1,
      rotation: 'rotation' in node ? (node as any).rotation : 0,
      bounds,
      layoutPositioning: 'layoutPositioning' in node ? ((node as any).layoutPositioning as 'AUTO' | 'ABSOLUTE') || 'AUTO' : 'AUTO',
      layoutAlign,
      fills,
      strokes,
      cornerRadius,
      effects,
    };
  }

  private extractAutoLayout(node: FrameNode): AutoLayout {
    const layoutMode = (node.layoutMode as 'NONE' | 'HORIZONTAL' | 'VERTICAL') || 'NONE';
    const padding = {
      top: node.paddingTop ?? 0,
      right: node.paddingRight ?? 0,
      bottom: node.paddingBottom ?? 0,
      left: node.paddingLeft ?? 0,
    };

    return {
      layoutMode,
      gap: node.itemSpacing ?? 0,
      padding,
      primaryAxisSizingMode: (node.primaryAxisSizingMode as 'FIXED' | 'AUTO') || 'FIXED',
      counterAxisSizingMode: (node.counterAxisSizingMode as 'FIXED' | 'AUTO') || 'FIXED',
      primaryAxisAlign: ((node as any).primaryAxisAlignItems || (node as any).primaryAxisAlign || 'MIN') as any,
      counterAxisAlign: ((node as any).counterAxisAlignItems || (node as any).counterAxisAlign || 'MIN') as any,
      layoutAlign: (node.layoutAlign as any) || 'INHERIT',
      layoutGrow: node.layoutGrow ?? 0,
    };
  }

  private extractFills(node: SceneNode): Fill[] {
    if (!('fills' in node) || !Array.isArray(node.fills)) return [];
    const fills: Fill[] = [];
    for (const fill of node.fills as Paint[]) {
      if (fill.visible === false) continue;

      let tokenId: string | undefined;
      if ('fillStyleId' in node && typeof (node as any).fillStyleId === 'string') {
        tokenId = this.tokenMap.get((node as any).fillStyleId);
      }

      if (fill.type === 'SOLID') {
        fills.push({
          tokenId,
          type: 'SOLID',
          color: { r: fill.color.r, g: fill.color.g, b: fill.color.b, a: fill.opacity ?? 1 },
          opacity: fill.opacity,
        });
      } else if (fill.type === 'IMAGE') {
        fills.push({
          tokenId,
          type: 'IMAGE',
        });
      } else if (fill.type.startsWith('GRADIENT')) {
        fills.push({
          tokenId,
          type: 'GRADIENT',
        });
      }
    }
    return fills;
  }

  private extractStrokes(node: SceneNode): Stroke[] {
    if (!('strokes' in node) || !Array.isArray(node.strokes)) return [];
    const strokes: Stroke[] = [];
    const weight = 'strokeWeight' in node && typeof (node as any).strokeWeight === 'number' ? (node as any).strokeWeight : 1;
    const align = 'strokeAlign' in node ? ((node as any).strokeAlign as any) : 'INSIDE';

    for (const stroke of node.strokes as Paint[]) {
      if (stroke.visible === false) continue;

      let tokenId: string | undefined;
      if ('strokeStyleId' in node && typeof (node as any).strokeStyleId === 'string') {
        tokenId = this.tokenMap.get((node as any).strokeStyleId);
      }

      if (stroke.type === 'SOLID') {
        strokes.push({
          tokenId,
          weight,
          align,
          color: { r: stroke.color.r, g: stroke.color.g, b: stroke.color.b, a: stroke.opacity ?? 1 },
        });
      }
    }
    return strokes;
  }

  private extractCornerRadius(node: SceneNode): CornerRadius {
    if ('topLeftRadius' in node) {
      const n = node as RectangleNode;
      return {
        topLeft: n.topLeftRadius ?? 0,
        topRight: n.topRightRadius ?? 0,
        bottomRight: n.bottomRightRadius ?? 0,
        bottomLeft: n.bottomLeftRadius ?? 0,
      };
    }
    if ('cornerRadius' in node && typeof (node as any).cornerRadius === 'number') {
      const r = (node as any).cornerRadius;
      return { topLeft: r, topRight: r, bottomRight: r, bottomLeft: r };
    }
    return { topLeft: 0, topRight: 0, bottomRight: 0, bottomLeft: 0 };
  }

  private extractEffects(node: SceneNode): EffectValue[] {
    if (!('effects' in node) || !Array.isArray(node.effects)) return [];
    const effects: EffectValue[] = [];
    for (const effect of node.effects as Effect[]) {
      if (effect.visible === false) continue;
      if (effect.type === 'DROP_SHADOW' || effect.type === 'INNER_SHADOW') {
        effects.push({
          type: effect.type,
          color: { r: effect.color.r, g: effect.color.g, b: effect.color.b, a: effect.color.a },
          offset: { x: effect.offset.x, y: effect.offset.y },
          radius: effect.radius,
          spread: effect.spread,
        });
      } else if (effect.type === 'LAYER_BLUR' || effect.type === 'BACKGROUND_BLUR') {
        effects.push({
          type: effect.type,
          radius: effect.radius,
        });
      }
    }
    return effects;
  }
}
