/**
 * TypeScript Interface Definitions for MapServer Inline Symbol Support
 * Auto-generated from C# MapFileParser.cs
 * 
 * Exported from: BMapr.GDAL.WebApi.Services.MapFileParser
 */

// ===== TYPE ALIASES =====
export type Int2 = [number, number];
export type Int3 = [number, number, number];
export type Double4 = [number, number, number, number];

export type AttributesBag = Record<string, string[][]>;

export type KeyValuePair<K = string, V = string> = { key: K; value: V };

// ===== SYMBOL DEFINITIONS =====

/**
 * Represents a SYMBOL block in a MapFile
 * Symbols can be defined at MAP level or LAYER level
 * Supports multiple symbol types: ELLIPSE, VECTOR, PIXMAP, TRUETYPE, HATCH, SIMPLE
 */
export interface SymbolObj {
  /** Symbol identifier name */
  name?: string;

  /** Symbol type: ELLIPSE, VECTOR, PIXMAP, TRUETYPE, HATCH, SIMPLE */
  type?: string;

  /** Image file path (for PIXMAP symbols) */
  image?: string;

  /** Font name (for TRUETYPE symbols) */
  font?: string;

  /** Character to display (for TRUETYPE symbols) */
  character?: string;

  /** Array of coordinate points defining vector shapes (for VECTOR symbols) */
  points?: number[];

  /** Gap between repeated symbols */
  gap?: number;

  /** Width of symbol outline/line */
  lineWidth?: number;

  /** Whether symbol is filled: TRUE|FALSE|ON|OFF */
  filled?: string;

  /** Anchor point definition */
  anchorPoint?: string;

  /** Dictionary of custom/unknown attributes */
  attributes?: AttributesBag;
}

// ===== MAP OBJECT WITH SYMBOLS =====

/**
 * Extended MAP object with inline symbol support
 */
export interface MapObj {
  /** Map name */
  name?: string;

  /** Map status: ON|OFF */
  status?: string;

  /** Map extent [minx, miny, maxx, maxy] */
  extent?: Double4;

  /** Map size [width, height] in pixels */
  size?: Int2;

  /** Map units: DEGREES|FEET|METERS|etc */
  units?: string;

  /** Shape path */
  shapePath?: string;

  /** Symbol set file path */
  symbolSet?: string;

  /** Font set file path */
  fontSet?: string;

  /** Image type: PNG|PNG24|JPEG|etc */
  imageType?: string;

  /** Image background color [R, G, B] */
  imageColor?: Int3;

  /** Image background color (hex) */
  imageColorHex?: string;

  /** Template pattern */
  templatePattern?: string;

  /** Data pattern */
  dataPattern?: string;

  /** Resolution in DPI */
  resolution?: number;

  /** Default resolution */
  defResolution?: number;

  /** Maximum image size */
  maxSize?: number;

  /** Rotation angle */
  angle?: number;

  /** Debug level */
  debug?: string;

  /** Transparency setting */
  transparent?: string;

  /** Configuration parameters */
  config: KeyValuePair[];

  /** Projection definitions */
  projection: string[];

  /** **NEW: Inline symbols defined at MAP level** */
  symbols: SymbolObj[];

  /** Web configuration */
  web: WebObj;

  /** Output format definitions */
  outputFormats: OutputFormatObj[];

  /** Layer definitions */
  layers: LayerObj[];

  /** Custom attributes */
  attributes: AttributesBag;
}

// ===== LAYER OBJECT WITH SYMBOLS =====

/**
 * Extended LAYER object with inline symbol support
 */
export interface LayerObj {
  /** Layer name */
  name?: string;

  /** Layer type: POINT|LINE|POLYGON|RASTER|CIRCLE|ELLIPSE|etc */
  type?: string;

  /** Layer status: ON|OFF|DEFAULT */
  status?: string;

  /** Data source path or query */
  data?: string;

  /** Connection type: LOCAL|OGR|POSTGRES|MYSQL|etc */
  connectionType?: string;

  /** Connection string */
  connection?: string;

  /** Class item field name */
  classItem?: string;

  /** Label item field name */
  labelItem?: string;

  /** Class group name */
  classGroup?: string;

  /** Layer group name */
  group?: string;

  /** Minimum scale denominator */
  minScaleDenom?: number;

  /** Maximum scale denominator */
  maxScaleDenom?: number;

  /** Minimum scale */
  minScale?: number;

  /** Maximum scale */
  maxScale?: number;

  /** Minimum geographic width */
  minGeoWidth?: number;

  /** Maximum geographic width */
  maxGeoWidth?: number;

  /** Symbol scale denominator */
  symbolScaleDenom?: number;

  /** Layer extent [minx, miny, maxx, maxy] */
  extent?: Double4;

  /** Layer units */
  units?: string;

  /** Label minimum scale denominator */
  labelMinScaleDenom?: number;

  /** Label maximum scale denominator */
  labelMaxScaleDenom?: number;

  /** Label requires expression */
  labelRequires?: string;

  /** Label cache setting */
  labelCache?: string;

  /** Debug level */
  debug?: string;

  /** Character encoding */
  encoding?: string;

  /** Filter expression */
  filter?: string;

  /** Filter item field name */
  filterItem?: string;

  /** Minimum feature size in pixels */
  minFeatureSize?: number;

  /** Maximum number of features to return */
  maxFeatures?: number;

  /** Mask expression */
  mask?: string;

  /** Style item field name */
  styleItem?: string;

  /** Geometry transform */
  geomTransform?: string;

  /** Post-label cache setting */
  postLabelCache?: string;

  /** Requires expression */
  requires?: string;

  /** Transform setting: ON|OFF */
  transform?: string;

  /** Tolerance for geometry operations */
  tolerance?: number;

  /** Tolerance units */
  toleranceUnits?: string;

  /** Header template path */
  header?: string;

  /** Footer template path */
  footer?: string;

  /** Feature template path */
  template?: string;

  /** Offsite color [R, G, B] */
  offsiteColor?: Int3;

  /** Offsite color (hex) */
  offsiteHex?: string;

  /** Tile index file path */
  tileIndex?: string;

  /** Tile item field name */
  tileItem?: string;

  /** Tile filter expression */
  tileFilter?: string;

  /** Tile filter item field name */
  tileFilterItem?: string;

  /** Projection definitions */
  projection: string[];

  /** **NEW: Inline symbols defined at LAYER level** */
  symbols: SymbolObj[];

  /** Metadata key-value pairs */
  metadata: Record<string, string>;

  /** Class definitions */
  classes: ClassObj[];

  /** Join definitions */
  joins: JoinObj[];

  /** Processing directives */
  processing: string[];

  /** Validation rules */
  validation: Record<string, string>;

  /** Connection options */
  connectionOptions: Record<string, string>;

  /** Identify configurations */
  identify: AttributesBag;

  /** Feature definitions */
  features: FeatureObj[];

  /** Composite settings */
  composite?: CompositeObj;

  /** Custom attributes */
  attributes: AttributesBag;
}

/**
 * WEB object configuration
 */
export interface IWebObj {
  /** Metadata key-value pairs */
  metadata?: Record<string, string>;

  /** Custom attributes */
  attributes?: Record<string, string[][]>;
}

/**
 * Output format definition
 */
export interface IOutputFormatObj {
  /** Output format name */
  name?: string;

  /** Output driver */
  driver?: string;

  /** MIME type */
  mimeType?: string;

  /** File extension */
  extension?: string;

  /** Image mode */
  imageMode?: string;

  /** Transparency setting */
  transparent?: string;

  /** Custom attributes */
  attributes?: Record<string, string[][]>;
}

/**
 * CLASS object definition
 */
export interface IClassObj {
  /** Class name */
  name?: string;

  /** Class title/legend label */
  title?: string;

  /** Class status: ON|OFF */
  status?: string;

  /** Class grouping */
  group?: string;

  /** Class expression for matching */
  expression?: string;

  /** Text to display */
  text?: string;

  /** Template path */
  template?: string;

  /** Key image path */
  keyImage?: string;

  /** Minimum scale denominator */
  minScaleDenom?: number;

  /** Maximum scale denominator */
  maxScaleDenom?: number;

  /** Minimum feature size */
  minFeatureSize?: number;

  /** Debug level */
  debug?: string;

  /** Fallback class */
  fallback?: boolean;

  /** Metadata key-value pairs */
  metadata?: Record<string, string>;

  /** Validation rules */
  validation?: Record<string, string>;

  /** Style definitions */
  styles?: IStyleObj[];

  /** Label definitions */
  labels?: ILabelObj[];

  /** Leader definition */
  leader?: ILeaderObj;

  /** Custom attributes */
  attributes?: Record<string, string[][]>;
}

/**
 * STYLE object definition
 */
export interface IStyleObj {
  /** Color [R, G, B] */
  color?: number[];

  /** Color (hex) */
  colorHex?: string;

  /** Color (attribute binding) */
  colorAttr?: string;

  /** Outline color [R, G, B] */
  outlineColor?: number[];

  /** Outline color (hex) */
  outlineColorHex?: string;

  /** Outline color (attribute binding) */
  outlineColorAttr?: string;

  /** Outline width */
  outlineWidth?: number;

  /** Symbol name or definition */
  symbol?: string;

  /** Symbol angle */
  angle?: string;

  /** Symbol width */
  width?: number;

  /** Symbol size */
  size?: number;

  /** Size expression */
  sizeExpr?: string;

  /** Minimum scale denominator */
  minScaleDenom?: number;

  /** Maximum scale denominator */
  maxScaleDenom?: number;

  /** Minimum width */
  minWidth?: number;

  /** Maximum width */
  maxWidth?: number;

  /** Minimum size */
  minSize?: number;

  /** Maximum size */
  maxSize?: number;

  /** Gap between symbols */
  gap?: number;

  /** Initial gap */
  initialGap?: number;

  /** X offset */
  offsetX?: string;

  /** Y offset */
  offsetY?: string;

  /** Polar offset R */
  polarOffsetR?: string;

  /** Polar offset angle */
  polarOffsetA?: string;

  /** Line cap style: BUTT|ROUND|SQUARE */
  lineCap?: string;

  /** Line join style: MITER|ROUND|BEVEL */
  lineJoin?: string;

  /** Line join maximum size */
  lineJoinMaxSize?: number;

  /** Line pattern */
  pattern?: number[];

  /** Geometry transform */
  geomTransform?: string;

  /** Opacity (0-100 or attribute binding) */
  opacity?: string;

  /** Range item field name */
  rangeItem?: string;

  /** Color range low value */
  colorRangeLow?: {
    rgb?: number[];
    hex?: string;
  };

  /** Color range high value */
  colorRangeHigh?: {
    rgb?: number[];
    hex?: string;
  };

  /** Data range */
  dataRange?: {
    low?: number;
    high?: number;
  };

  /** Custom attributes */
  attributes?: Record<string, string[][]>;
}

/**
 * LABEL object definition
 */
export interface ILabelObj {
  /** Label alignment: left|center|right */
  align?: string;

  /** Label angle: auto|auto2|follow|numeric */
  angle?: string;

  /** Label buffer in pixels */
  buffer?: number;

  /** Label color [R, G, B] */
  color?: number[];

  /** Label color (hex) */
  colorHex?: string;

  /** Label color (attribute binding) */
  colorAttr?: string;

  /** Label outline color [R, G, B] */
  outlineColor?: number[];

  /** Label outline color (hex) */
  outlineColorHex?: string;

  /** Label outline color (attribute binding) */
  outlineColorAttr?: string;

  /** Label outline width */
  outlineWidth?: number;

  /** Font name */
  font?: string;

  /** Font type: bitmap|truetype */
  type?: string;

  /** Font size */
  size?: string;

  /** Maximum overlap angle */
  maxOverlapAngle?: number;

  /** Minimum scale denominator */
  minScaleDenom?: number;

  /** Maximum scale denominator */
  maxScaleDenom?: number;

  /** Minimum distance between labels */
  minDistance?: number;

  /** Minimum feature size: auto|numeric */
  minFeatureSize?: string;

  /** Minimum label size */
  minSize?: number;

  /** Maximum label size */
  maxSize?: number;

  /** X offset */
  offsetX?: string;

  /** Y offset */
  offsetY?: string;

  /** Label position: ul|ur|ll|lr|cc|etc */
  position?: string;

  /** Label priority (0-10) */
  priority?: string;

  /** Allow partial labels: true|false */
  partials?: string;

  /** Force label placement: true|false|group */
  force?: string;

  /** Repeat distance for line labels */
  repeatDistance?: number;

  /** Shadow color [R, G, B] */
  shadowColor?: number[];

  /** Shadow color (hex) */
  shadowColorHex?: string;

  /** Shadow X offset */
  shadowSizeX?: string;

  /** Shadow Y offset */
  shadowSizeY?: string;

  /** Label text template */
  text?: string;

  /** Word wrap character */
  wrap?: string;

  /** Maximum label length */
  maxLength?: number;

  /** Label styles */
  styles?: IStyleObj[];

  /** Custom attributes */
  attributes?: Record<string, string[][]>;
}

/**
 * LEADER object definition
 */
export interface ILeaderObj {
  /** Grid step for leader lines */
  gridStep?: number;

  /** Maximum distance for leader lines */
  maxDistance?: number;

  /** Leader line styles */
  styles?: IStyleObj[];

  /** Custom attributes */
  attributes?: Record<string, string[][]>;
}

/**
 * JOIN object definition
 */
export interface IJoinObj {
  /** Join name */
  name?: string;

  /** Table to join */
  table?: string;

  /** From field */
  from?: string;

  /** To field */
  to?: string;

  /** Join type: ONE-TO-ONE|ONE-TO-MANY */
  type?: string;

  /** Template path */
  template?: string;

  /** Custom attributes */
  attributes?: Record<string, string[][]>;
}

/**
 * COMPOSITE object definition
 */
export interface ICompositeObj {
  /** Composite opacity (0-100) */
  opacity?: number;

  /** Composite operation */
  compOp?: string;

  /** Composite pattern */
  pattern?: number[];

  /** Custom attributes */
  attributes?: Record<string, string[][]>;
}

/**
 * FEATURE object definition
 */
export interface IFeatureObj {
  /** Feature inner lines (raw WKT, POINTS, ITEMS, etc.) */
  innerLines?: string[];

  /** Custom attributes */
  attributes?: Record<string, string[][]>;
}

/**
 * Complete MapFile structure with inline symbol support
 */
export interface IMapFile extends IMapObjWithSymbols {
  // Inherits all properties from IMapObjWithSymbols
}

/**
 * Symbol Type Enum
 */
export enum SymbolType {
  ELLIPSE = 'ELLIPSE',
  VECTOR = 'VECTOR',
  PIXMAP = 'PIXMAP',
  TRUETYPE = 'TRUETYPE',
  HATCH = 'HATCH',
  SIMPLE = 'SIMPLE',
}

/**
 * Layer Type Enum
 */
export enum LayerType {
  POINT = 'POINT',
  LINE = 'LINE',
  POLYGON = 'POLYGON',
  RASTER = 'RASTER',
  CIRCLE = 'CIRCLE',
  ELLIPSE = 'ELLIPSE',
}

/**
 * Connection Type Enum
 */
export enum ConnectionType {
  LOCAL = 'LOCAL',
  OGR = 'OGR',
  POSTGRES = 'POSTGRES',
  MYSQL = 'MYSQL',
  ORACLESPATIAL = 'ORACLESPATIAL',
  WMS = 'WMS',
  CONTOUR = 'CONTOUR',
  RASTER = 'RASTER',
}

/**
 * Unit Type Enum
 */
export enum UnitType {
  DEGREES = 'DEGREES',
  FEET = 'FEET',
  METERS = 'METERS',
  MILES = 'MILES',
  INCHES = 'INCHES',
  CENTIMETERS = 'CENTIMETERS',
  NAUTICALMILES = 'NAUTICALMILES',
}

/**
 * Status Enum
 */
export enum Status {
  ON = 'ON',
  OFF = 'OFF',
  DEFAULT = 'DEFAULT',
}

/**
 * Helper type for creating new symbols
 */
export type ISymbolObjInput = Omit<ISymbolObj, 'attributes'> & {
  attributes?: Record<string, string[][]>;
};

/**
 * Helper type for creating new map objects
 */
export type IMapObjInput = Omit<IMapObjWithSymbols, 'symbols' | 'layers' | 'outputFormats' | 'config' | 'projection' | 'web' | 'attributes'> & {
  symbols?: ISymbolObjInput[];
  layers?: ILayerObjInput[];
  outputFormats?: IOutputFormatObj[];
  config?: Array<{ key: string; value: string }>;
  projection?: string[];
  web?: IWebObj;
  attributes?: Record<string, string[][]>;
};

/**
 * Helper type for creating new layer objects
 */
export type ILayerObjInput = Omit<ILayerObjWithSymbols, 'symbols' | 'classes' | 'joins' | 'features' | 'projection' | 'processing' | 'metadata' | 'validation' | 'connectionOptions' | 'identify' | 'composite' | 'attributes'> & {
  symbols?: ISymbolObjInput[];
  classes?: IClassObj[];
  joins?: IJoinObj[];
  features?: IFeatureObj[];
  projection?: string[];
  processing?: string[];
  metadata?: Record<string, string>;
  validation?: Record<string, string>;
  connectionOptions?: Record<string, string>;
  identify?: Record<string, string[][]>;
  composite?: ICompositeObj;
  attributes?: Record<string, string[][]>;
};
