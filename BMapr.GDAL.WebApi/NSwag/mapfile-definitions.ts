/**
 * Complete TypeScript definitions for MapServer UMN mapfile parser
 * Includes all model classes and the new inline symbol support
 */

// ============================================================================
// TYPE ALIASES
// ============================================================================

export type Int2 = [number, number];
export type Int3 = [number, number, number];
export type Double4 = [number, number, number, number];

export type AttributesBag = Record<string, string[][]>;

export type KeyValuePair<K = string, V = string> = { key: K; value: V };

// ============================================================================
// ENUMERATIONS (optional, commented out for reference)
// ============================================================================

// export enum SymbolType {
//   Ellipse = "ELLIPSE",
//   Vector = "VECTOR",
//   Pixmap = "PIXMAP",
//   Truetype = "TRUETYPE",
//   Hatch = "HATCH",
//   Simple = "SIMPLE"
// }

// export enum LayerType {
//   Point = "POINT",
//   Line = "LINE",
//   Polygon = "POLYGON",
//   Raster = "RASTER",
//   Circle = "CIRCLE"
// }

// export enum ConnectionType {
//   Shapefile = "SHAPEFILE",
//   PostGis = "POSTGIS",
//   Ogr = "OGR",
//   WMS = "WMS",
//   WFS = "WFS",
//   Mysql = "MYSQL"
// }

// export enum UnitType {
//   Inches = "INCHES",
//   Feet = "FEET",
//   Miles = "MILES",
//   Meters = "METERS",
//   Kilometers = "KILOMETERS",
//   Degrees = "DEGREES",
//   Dd = "DD"
// }

// export enum Status {
//   On = "ON",
//   Off = "OFF"
// }

// ============================================================================
// SYMBOL OBJECT (NEW - inline symbol support)
// ============================================================================

export interface SymbolObj {
  /** Symbol name for reference */
  name?: string;

  /** Symbol type: ELLIPSE, VECTOR, PIXMAP, TRUETYPE, HATCH, SIMPLE */
  type?: string;

  /** Path to image file for PIXMAP symbols */
  image?: string;

  /** Font name for TRUETYPE symbols */
  font?: string;

  /** Character code for TRUETYPE symbols */
  character?: string;

  /** Floating-point coordinates for VECTOR/ELLIPSE symbols */
  points?: number[];

  /** Gap between repeated symbols */
  gap?: number;

  /** Width of symbol */
  width?: number;

  /** Line width for symbol outline */
  lineWidth?: number;

  /** Size of symbol */
  size?: number;

  /** Angular rotation of symbols (numeric or AUTO) */
  angle?: string;

  /** Pattern for symbol rendering */
  pattern?: number[];

  /** X-axis offset */
  offsetX?: string;

  /** Y-axis offset */
  offsetY?: string;

  /** Whether symbol is filled: TRUE or FALSE */
  filled?: string;

  /** Antialiasing control: ON or OFF */
  antialias?: string;

  /** Anchor point for symbol positioning */
  anchorPoint?: string;

  /** Custom attributes */
  attributes?: AttributesBag;
}

// ============================================================================
// MAP OBJECT
// ============================================================================

export interface MapObj {
  name?: string;
  status?: string;
  extent?: Double4;
  size?: Int2;
  units?: string;

  shapePath?: string;
  symbolSet?: string;
  fontSet?: string;
  imageType?: string;
  imageColor?: Int3;
  imageColorHex?: string;
  templatePattern?: string;
  dataPattern?: string;
  resolution?: number;
  defResolution?: number;
  maxSize?: number;
  angle?: number;
  debug?: string;
  transparent?: string;

  config: KeyValuePair[];

  projection: string[];
  /** Inline symbol definitions at MAP level */
  symbols: SymbolObj[];
  web: WebObj;
  outputFormats: OutputFormatObj[];
  layers: LayerObj[];

  attributes: AttributesBag;
}

// ============================================================================
// WEB OBJECT
// ============================================================================

export interface WebObj {
  metadata: Record<string, string>;
  attributes: AttributesBag;
  isEmpty?: boolean; // derived in C#
}

// ============================================================================
// OUTPUT FORMAT OBJECT
// ============================================================================

export interface OutputFormatObj {
  name?: string;
  driver?: string;
  mimeType?: string;
  extension?: string;
  imageMode?: string;
  transparent?: string;
  attributes: AttributesBag;
}

// ============================================================================
// COMPOSITE OBJECT
// ============================================================================

export interface CompositeObj {
  opacity?: number;
  compOp?: string;
  pattern?: number[];
  attributes?: AttributesBag;
}

// ============================================================================
// LAYER OBJECT
// ============================================================================

export interface LayerObj {
  name?: string;
  type?: string;
  status?: string;
  data?: string;
  connectionType?: string;
  connection?: string;

  classItem?: string;
  labelItem?: string;
  classGroup?: string;
  group?: string;

  minScaleDenom?: number;
  maxScaleDenom?: number;
  minScale?: number;
  maxScale?: number;
  minGeoWidth?: number;
  maxGeoWidth?: number;
  symbolScaleDenom?: number;

  extent?: Double4;
  units?: string;

  labelMinScaleDenom?: number;
  labelMaxScaleDenom?: number;
  labelRequires?: string;
  labelCache?: string;

  debug?: string;
  encoding?: string;
  filter?: string;
  filterItem?: string;
  minFeatureSize?: number;
  maxFeatures?: number;
  mask?: string;
  styleItem?: string;
  geomTransform?: string;
  postLabelCache?: string;
  requires?: string;
  transform?: string;
  tolerance?: number;
  toleranceUnits?: string;
  header?: string;
  footer?: string;
  template?: string;

  offsiteColor?: Int3;
  offsiteHex?: string;

  tileIndex?: string;
  tileItem?: string;
  tileFilter?: string;
  tileFilterItem?: string;

  projection: string[];
  /** Inline symbol definitions at LAYER level */
  symbols: SymbolObj[];
  metadata: Record<string, string>;
  classes: ClassObj[];
  joins: JoinObj[];

  processing: string[];
  validation: Record<string, string>;
  connectionOptions: Record<string, string>;
  identify: AttributesBag;

  composite?: CompositeObj;
  features: FeatureObj[];

  attributes: AttributesBag;
}

// ============================================================================
// JOIN OBJECT
// ============================================================================

export interface JoinObj {
  name?: string;
  table?: string;
  from?: string;
  to?: string;
  type?: string;
  template?: string;
  attributes: AttributesBag;
}

// ============================================================================
// FEATURE OBJECT
// ============================================================================

export interface FeatureObj {
  /** Raw inner lines of FEATURE block */
  innerLines: string[];
  attributes: AttributesBag;
}

// ============================================================================
// CLASS OBJECT
// ============================================================================

export interface ClassObj {
  name?: string;
  title?: string;
  status?: string;
  group?: string;
  expression?: string;
  text?: string;
  template?: string;
  keyImage?: string;
  minScaleDenom?: number;
  maxScaleDenom?: number;
  minFeatureSize?: number;
  debug?: string;
  fallback?: boolean;

  styles: StyleObj[];
  labels: LabelObj[];
  leader?: LeaderObj;

  metadata: Record<string, string>;
  validation: Record<string, string>;
  attributes: AttributesBag;
}

// ============================================================================
// LEADER OBJECT
// ============================================================================

export interface LeaderObj {
  gridStep?: number;
  maxDistance?: number;
  styles: StyleObj[];
  attributes: AttributesBag;
}

// ============================================================================
// STYLE OBJECT
// ============================================================================

export interface StyleObj {
  // COLOR (RGB/hex/attr)
  color?: Int3;
  colorHex?: string;
  colorAttr?: string;

  outlineColor?: Int3;
  outlineColorHex?: string;
  outlineColorAttr?: string;
  outlineWidth?: number;

  symbol?: string;

  /** numeric|AUTO|[attr] as string */
  angle?: string;
  width?: number;
  size?: number;
  /** [attr] or expression */
  sizeExpr?: string;

  minScaleDenom?: number;
  maxScaleDenom?: number;
  minWidth?: number;
  maxWidth?: number;
  minSize?: number;
  maxSize?: number;

  gap?: number;
  initialGap?: number;

  /** allow attribute binding */
  offsetX?: string;
  offsetY?: string;
  polarOffsetR?: string;
  polarOffsetA?: string;

  lineCap?: string;
  lineJoin?: string;
  lineJoinMaxSize?: number;

  pattern: number[];

  geomTransform?: string;
  /** numeric or [attr] */
  opacity?: string;

  rangeItem?: string;
  colorRangeLow?: { rgb?: Int3; hex?: string };
  colorRangeHigh?: { rgb?: Int3; hex?: string };
  dataRange?: { low?: number; high?: number };

  attributes: AttributesBag;
}

// ============================================================================
// LABEL OBJECT
// ============================================================================

export interface LabelObj {
  /** ALIGN left|center|right|[attr] */
  align?: string;
  /** ANGLE auto|auto2|follow|deg|[attr] */
  angle?: string;
  buffer?: number;

  color?: Int3;
  colorHex?: string;
  colorAttr?: string;

  outlineColor?: Int3;
  outlineColorHex?: string;
  outlineColorAttr?: string;
  outlineWidth?: number;

  /** name or [attr] */
  font?: string;
  /** bitmap|truetype */
  type?: string;
  /** int|keyword|[attr]|(expr) */
  size?: string;

  maxOverlapAngle?: number;
  minScaleDenom?: number;
  maxScaleDenom?: number;
  minDistance?: number;
  /** int|auto */
  minFeatureSize?: string;
  minSize?: number;
  maxSize?: number;

  /** supports [attr] */
  offsetX?: string;
  offsetY?: string;

  /** ul|...|auto|[attr] */
  position?: string;
  /** int|[attr]|(expr) */
  priority?: string;
  /** true|false */
  partials?: string;
  /** true|false|group */
  force?: string;

  repeatDistance?: number;
  shadowColor?: Int3;
  shadowColorHex?: string;
  shadowSizeX?: string;
  shadowSizeY?: string;

  /** string|expression */
  text?: string;
  /** char */
  wrap?: string;
  maxLength?: number;

  /** label styling via STYLE GEOMTRANSFORM */
  styles: StyleObj[];
  attributes: AttributesBag;
}
