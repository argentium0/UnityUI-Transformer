import { z } from 'zod';

// ==========================================
// Basic Types & Values
// ==========================================

export const ColorValueSchema = z.object({
  r: z.number().min(0).max(1),
  g: z.number().min(0).max(1),
  b: z.number().min(0).max(1),
  a: z.number().min(0).max(1).default(1),
});
export type ColorValue = z.infer<typeof ColorValueSchema>;

export const BoundsSchema = z.object({
  x: z.number(),
  y: z.number(),
  width: z.number(),
  height: z.number(),
});
export type Bounds = z.infer<typeof BoundsSchema>;

// ==========================================
// Design Tokens
// ==========================================

export const ColorTokenSchema = z.object({
  id: z.string(),
  name: z.string(),
  value: ColorValueSchema,
  hex: z.string().optional(),
  description: z.string().optional(),
});
export type ColorToken = z.infer<typeof ColorTokenSchema>;

export const TypographyTokenSchema = z.object({
  id: z.string(),
  name: z.string(),
  fontFamily: z.string(),
  fontSize: z.number(),
  fontWeight: z.number().default(400),
  lineHeight: z.number().optional(),
  letterSpacing: z.number().optional(),
  textCase: z.enum(['ORIGINAL', 'UPPER', 'LOWER', 'TITLE']).optional(),
  textDecoration: z.enum(['NONE', 'UNDERLINE', 'STRIKETHROUGH']).optional(),
});
export type TypographyToken = z.infer<typeof TypographyTokenSchema>;

export const SpacingTokenSchema = z.object({
  id: z.string(),
  name: z.string(),
  value: z.number(),
});
export type SpacingToken = z.infer<typeof SpacingTokenSchema>;

export const EffectValueSchema = z.object({
  type: z.enum(['DROP_SHADOW', 'INNER_SHADOW', 'LAYER_BLUR', 'BACKGROUND_BLUR']),
  color: ColorValueSchema.optional(),
  offset: z.object({ x: z.number(), y: z.number() }).optional(),
  radius: z.number().optional(),
  spread: z.number().optional(),
});
export type EffectValue = z.infer<typeof EffectValueSchema>;

export const EffectTokenSchema = z.object({
  id: z.string(),
  name: z.string(),
  effects: z.array(EffectValueSchema),
});
export type EffectToken = z.infer<typeof EffectTokenSchema>;

export const TokensSchema = z.object({
  colors: z.array(ColorTokenSchema).default([]),
  typography: z.array(TypographyTokenSchema).default([]),
  spacing: z.array(SpacingTokenSchema).default([]),
  effects: z.array(EffectTokenSchema).default([]),
});
export type Tokens = z.infer<typeof TokensSchema>;

// ==========================================
// Layout & Styling Properties
// ==========================================

export const AutoLayoutSchema = z.object({
  layoutMode: z.enum(['NONE', 'HORIZONTAL', 'VERTICAL']).default('NONE'),
  gap: z.number().default(0),
  padding: z.object({
    top: z.number().default(0),
    right: z.number().default(0),
    bottom: z.number().default(0),
    left: z.number().default(0),
  }).default({ top: 0, right: 0, bottom: 0, left: 0 }),
  primaryAxisSizingMode: z.enum(['FIXED', 'AUTO']).default('FIXED'),
  counterAxisSizingMode: z.enum(['FIXED', 'AUTO']).default('FIXED'),
  primaryAxisAlign: z.enum(['MIN', 'CENTER', 'MAX', 'SPACE_BETWEEN']).default('MIN'),
  counterAxisAlign: z.enum(['MIN', 'CENTER', 'MAX', 'BASELINE']).default('MIN'),
  layoutAlign: z.enum(['STRETCH', 'INHERIT']).default('INHERIT'),
  layoutGrow: z.number().default(0),
});
export type AutoLayout = z.infer<typeof AutoLayoutSchema>;

export const ConstraintsSchema = z.object({
  horizontal: z.enum(['MIN', 'CENTER', 'MAX', 'STRETCH', 'SCALE']).default('MIN'),
  vertical: z.enum(['MIN', 'CENTER', 'MAX', 'STRETCH', 'SCALE']).default('MIN'),
});
export type Constraints = z.infer<typeof ConstraintsSchema>;

export const FillSchema = z.object({
  tokenId: z.string().optional(),
  type: z.enum(['SOLID', 'GRADIENT', 'IMAGE']).default('SOLID'),
  color: ColorValueSchema.optional(),
  opacity: z.number().min(0).max(1).optional(),
});
export type Fill = z.infer<typeof FillSchema>;

export const StrokeSchema = z.object({
  tokenId: z.string().optional(),
  color: ColorValueSchema.optional(),
  weight: z.number().default(1),
  align: z.enum(['INSIDE', 'OUTSIDE', 'CENTER']).default('INSIDE'),
  dashPattern: z.array(z.number()).optional(),
});
export type Stroke = z.infer<typeof StrokeSchema>;

export const CornerRadiusSchema = z.object({
  topLeft: z.number().default(0),
  topRight: z.number().default(0),
  bottomRight: z.number().default(0),
  bottomLeft: z.number().default(0),
});
export type CornerRadius = z.infer<typeof CornerRadiusSchema>;

// ==========================================
// Base Node & Discriminated Union
// ==========================================

const BaseNodeFields = {
  id: z.string(),
  name: z.string(),
  visible: z.boolean().default(true),
  opacity: z.number().min(0).max(1).default(1),
  rotation: z.number().default(0),
  bounds: BoundsSchema,
  autoLayout: AutoLayoutSchema.optional(),
  constraints: ConstraintsSchema.optional(),
  fills: z.array(FillSchema).default([]),
  strokes: z.array(StrokeSchema).default([]),
  cornerRadius: CornerRadiusSchema.default({ topLeft: 0, topRight: 0, bottomRight: 0, bottomLeft: 0 }),
  effects: z.array(EffectValueSchema).default([]),
};

export const RectangleNodeSchema = z.object({
  ...BaseNodeFields,
  type: z.literal('RECTANGLE'),
});

export const EllipseNodeSchema = z.object({
  ...BaseNodeFields,
  type: z.literal('ELLIPSE'),
});

export const VectorNodeSchema = z.object({
  ...BaseNodeFields,
  type: z.literal('VECTOR'),
  svgAssetRef: z.string().optional(),
  svgPathData: z.string().optional(),
});

export const TextNodeSchema = z.object({
  ...BaseNodeFields,
  type: z.literal('TEXT'),
  characters: z.string(),
  typographyTokenId: z.string().optional(),
  fontFamily: z.string().optional(),
  fontSize: z.number().optional(),
  fontWeight: z.number().optional(),
  textAlign: z.enum(['LEFT', 'CENTER', 'RIGHT', 'JUSTIFY']).default('LEFT'),
  textAutoResize: z.enum(['NONE', 'WIDTH_AND_HEIGHT', 'HEIGHT']).default('NONE'),
});

export const ImageNodeSchema = z.object({
  ...BaseNodeFields,
  type: z.literal('IMAGE'),
  imageAssetRef: z.string(),
  scaleMode: z.enum(['FILL', 'FIT', 'CROP', 'TILE']).default('FILL'),
});

export const FrameNodeSchema = z.object({
  ...BaseNodeFields,
  type: z.literal('FRAME'),
  children: z.array(z.lazy(() => IRNodeSchema)).default([]),
  clipsContent: z.boolean().default(false),
});

export const GroupNodeSchema = z.object({
  ...BaseNodeFields,
  type: z.literal('GROUP'),
  children: z.array(z.lazy(() => IRNodeSchema)).default([]),
});

export const ComponentInstanceNodeSchema = z.object({
  ...BaseNodeFields,
  type: z.literal('COMPONENT_INSTANCE'),
  componentId: z.string(),
  variantProperties: z.record(z.string()).optional(),
  children: z.array(z.lazy(() => IRNodeSchema)).default([]),
});

export const UnsupportedNodeSchema = z.object({
  ...BaseNodeFields,
  type: z.literal('UNSUPPORTED'),
  figmaNodeType: z.string(),
  children: z.array(z.lazy(() => IRNodeSchema)).optional(),
});

export const IRNodeSchema: z.ZodType<any> = z.discriminatedUnion('type', [
  FrameNodeSchema,
  GroupNodeSchema,
  RectangleNodeSchema,
  EllipseNodeSchema,
  VectorNodeSchema,
  TextNodeSchema,
  ImageNodeSchema,
  ComponentInstanceNodeSchema,
  UnsupportedNodeSchema,
]);

export type IRNode = z.infer<typeof IRNodeSchema>;

// ==========================================
// Root Document Schema
// ==========================================

export const MetadataSchema = z.object({
  exportedAt: z.string(),
  figmaFileKey: z.string().optional(),
  figmaFileName: z.string().optional(),
  generatorVersion: z.string().default('1.0.0'),
});
export type Metadata = z.infer<typeof MetadataSchema>;

export const IRDocumentSchema = z.object({
  schemaVersion: z.string().default('1.0.0'),
  metadata: MetadataSchema,
  tokens: TokensSchema,
  rootNodes: z.array(IRNodeSchema),
});
export type IRDocument = z.infer<typeof IRDocumentSchema>;
