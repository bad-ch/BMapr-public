/**
 * MapServer UMN Mapfile Parser and Serializer in TypeScript
 * Mirrors the functionality of MapFileParser.cs
 */

import * as Defs from './mapfile';

// ============================================================================
// PARSER
// ============================================================================

enum CtxType {
    Map = 'Map',
    Web = 'Web',
    Metadata = 'Metadata',
    Projection = 'Projection',
    OutputFormat = 'OutputFormat',
    Layer = 'Layer',
    Class = 'Class',
    Style = 'Style',
    Label = 'Label',
    Leader = 'Leader',
    Validation = 'Validation',
    Join = 'Join',
    Identify = 'Identify',
    Composite = 'Composite',
    Symbol = 'Symbol'
}

interface Context {
    type: CtxType;
    node: any;
}

export class MapfileParser {
    public static parse(text: string, baseDir?: string): Defs.MapObj {
        // Expand INCLUDE directives if we know where to resolve them from
        text = MapfileParser.expandIncludes(text, baseDir);

        const map: Defs.MapObj = {
            config: [],
            projection: [],
            symbols: [],
            web: { metadata: {}, attributes: {} },
            outputFormats: [],
            layers: [],
            attributes: {}
        };

        const stack: Context[] = [];
        stack.push({ type: CtxType.Map, node: map });

        const lines = text.split(/\r?\n/);
        let lineNo = 0;

        for (const raw of lines) {
            lineNo++;
            const line = MapfileParser.stripComments(raw);
            if (!line.trim()) continue;

            const tokens = MapfileParser.tokenize(line);
            if (tokens.length === 0) continue;

            const head = tokens[0].toUpperCase();

            // Special handling: FEATURE blocks inside LAYER
            if (head === 'FEATURE') {
                const ctxFeat = stack[stack.length - 1];
                if (ctxFeat.type !== CtxType.Layer) {
                    throw new Error(`FEATURE outside LAYER at line ${lineNo}`);
                }

                const f: Defs.FeatureObj = { innerLines: [], attributes: {} };
                let depth = 0;
                let foundFeatureEnd = false;
                let lineIndex = lineNo;

                for (let i = lineIndex; i < lines.length; i++) {
                    lineNo++;
                    const innerRaw = lines[i];
                    const innerStripped = MapfileParser.stripComments(innerRaw);
                    const innerTokens = MapfileParser.tokenize(innerStripped);

                    if (innerTokens.length > 0) {
                        const innerHead = innerTokens[0].toUpperCase();
                        if (innerHead === 'POINTS' || innerHead === 'ITEMS') {
                            depth++;
                            f.innerLines.push(innerRaw);
                            continue;
                        }
                        if (innerHead === 'END') {
                            if (depth === 0) {
                                foundFeatureEnd = true;
                                break;
                            } else {
                                depth--;
                                f.innerLines.push(innerRaw);
                                continue;
                            }
                        }
                    }
                    f.innerLines.push(innerRaw);
                }

                if (!foundFeatureEnd) {
                    throw new Error(`FEATURE block started at line ${lineNo - f.innerLines.length - 1} is missing closing END`);
                }
                if (depth !== 0) {
                    throw new Error(`FEATURE block has unbalanced POINTS/ITEMS blocks (depth=${depth}) at line ${lineNo}`);
                }

                (ctxFeat.node as Defs.LayerObj).features.push(f);
                continue;
            }

            // Special handling: POINTS blocks inside SYMBOL
            if (head === 'POINTS') {
                const ctxSym = stack[stack.length - 1];
                if (ctxSym.type !== CtxType.Symbol) {
                    MapfileParser.applyKeyValuesToSymbol(ctxSym.node as Defs.SymbolObj, tokens);
                    continue;
                }

                const sym = ctxSym.node as Defs.SymbolObj;
                const pointsList: number[] = [];

                for (const v of tokens.slice(1)) {
                    if (v.toUpperCase() === 'END') break;
                    const num = parseFloat(v);
                    if (!isNaN(num)) {
                        pointsList.push(num);
                    }
                }

                if (!tokens.slice(1).some(t => t.toUpperCase() === 'END')) {
                    let foundPointsEnd = false;
                    for (let i = lineNo; i < lines.length; i++) {
                        lineNo++;
                        const innerStripped = MapfileParser.stripComments(lines[i]);
                        const innerTokens = MapfileParser.tokenize(innerStripped);
                        if (innerTokens.length > 0) {
                            const innerHead = innerTokens[0].toUpperCase();
                            if (innerHead === 'END') {
                                foundPointsEnd = true;
                                break;
                            }
                            for (const v of innerTokens) {
                                const num = parseFloat(v);
                                if (!isNaN(num)) {
                                    pointsList.push(num);
                                }
                            }
                        }
                    }
                    if (!foundPointsEnd) {
                        throw new Error(`POINTS block at line ${lineNo} is missing closing END`);
                    }
                }

                if (pointsList.length > 0) {
                    sym.points = pointsList;
                }
                continue;
            }

            if (head === 'END') {
                if (stack.length === 0) throw new Error(`Unexpected END at line ${lineNo}`);
                stack.pop();
                continue;
            }

            if (stack.length === 0) continue;
            const ctx = stack[stack.length - 1];

            // Open blocks
            if (head === 'MAP') {
                if (ctx.type !== CtxType.Map || !(ctx.node instanceof Object)) {
                    throw new Error(`Nested MAP not allowed at line ${lineNo}`);
                }
                continue;
            }

            if (head === 'WEB') {
                const web = (ctx.node as Defs.MapObj).web;
                stack.push({ type: CtxType.Web, node: web });
                continue;
            }

            if (head === 'METADATA') {
                switch (ctx.type) {
                    case CtxType.Web:
                        stack.push({ type: CtxType.Metadata, node: (ctx.node as Defs.WebObj).metadata });
                        break;
                    case CtxType.Layer:
                        stack.push({ type: CtxType.Metadata, node: (ctx.node as Defs.LayerObj).metadata });
                        break;
                    case CtxType.Class:
                        stack.push({ type: CtxType.Metadata, node: (ctx.node as Defs.ClassObj).metadata });
                        break;
                    default:
                        throw new Error(`METADATA not allowed here (line ${lineNo})`);
                }
                continue;
            }

            if (head === 'VALIDATION') {
                switch (ctx.type) {
                    case CtxType.Layer:
                        stack.push({ type: CtxType.Validation, node: (ctx.node as Defs.LayerObj).validation });
                        break;
                    case CtxType.Class:
                        stack.push({ type: CtxType.Validation, node: (ctx.node as Defs.ClassObj).validation });
                        break;
                    default:
                        throw new Error(`VALIDATION not allowed here (line ${lineNo})`);
                }
                continue;
            }

            if (head === 'PROJECTION') {
                if (ctx.type === CtxType.Map) {
                    stack.push({ type: CtxType.Projection, node: (ctx.node as Defs.MapObj).projection });
                    continue;
                }
                if (ctx.type === CtxType.Layer) {
                    stack.push({ type: CtxType.Projection, node: (ctx.node as Defs.LayerObj).projection });
                    continue;
                }
                throw new Error(`PROJECTION not allowed here (line ${lineNo})`);
            }

            if (head === 'OUTPUTFORMAT') {
                const of: Defs.OutputFormatObj = { attributes: {} };
                (ctx.node as Defs.MapObj).outputFormats.push(of);
                stack.push({ type: CtxType.OutputFormat, node: of });
                continue;
            }

            if (head === 'SYMBOL') {
                if (ctx.type === CtxType.Map) {
                    const sym: Defs.SymbolObj = { pattern: [], attributes: {} };
                    (ctx.node as Defs.MapObj).symbols.push(sym);
                    stack.push({ type: CtxType.Symbol, node: sym });
                    continue;
                }
                if (ctx.type === CtxType.Layer) {
                    const sym: Defs.SymbolObj = { pattern: [], attributes: {} };
                    (ctx.node as Defs.LayerObj).symbols.push(sym);
                    stack.push({ type: CtxType.Symbol, node: sym });
                    continue;
                }
                throw new Error(`SYMBOL not allowed here (line ${lineNo})`);
            }

            if (head === 'LAYER') {
                const layer: Defs.LayerObj = {
                    projection: [],
                    symbols: [],
                    metadata: {},
                    classes: [],
                    joins: [],
                    processing: [],
                    validation: {},
                    connectionOptions: {},
                    identify: {},
                    features: [],
                    attributes: {}
                };
                (ctx.node as Defs.MapObj).layers.push(layer);
                stack.push({ type: CtxType.Layer, node: layer });
                continue;
            }

            if (head === 'CLASS') {
                if (ctx.type !== CtxType.Layer) {
                    throw new Error(`CLASS outside LAYER at line ${lineNo}`);
                }
                const c: Defs.ClassObj = {
                    styles: [],
                    labels: [],
                    metadata: {},
                    validation: {},
                    attributes: {}
                };
                (ctx.node as Defs.LayerObj).classes.push(c);
                stack.push({ type: CtxType.Class, node: c });
                continue;
            }

            if (head === 'STYLE') {
                if (ctx.type === CtxType.Class) {
                    const s: Defs.StyleObj = { pattern: [], attributes: {} };
                    (ctx.node as Defs.ClassObj).styles.push(s);
                    stack.push({ type: CtxType.Style, node: s });
                    continue;
                }
                if (ctx.type === CtxType.Label) {
                    const s: Defs.StyleObj = { pattern: [], attributes: {} };
                    (ctx.node as Defs.LabelObj).styles.push(s);
                    stack.push({ type: CtxType.Style, node: s });
                    continue;
                }
                if (ctx.type === CtxType.Leader) {
                    const s: Defs.StyleObj = { pattern: [], attributes: {} };
                    (ctx.node as Defs.LeaderObj).styles.push(s);
                    stack.push({ type: CtxType.Style, node: s });
                    continue;
                }
                throw new Error(`STYLE not allowed here (line ${lineNo})`);
            }

            if (head === 'LABEL') {
                if (ctx.type !== CtxType.Class) {
                    throw new Error(`LABEL outside CLASS at line ${lineNo}`);
                }
                const l: Defs.LabelObj = { styles: [], attributes: {} };
                (ctx.node as Defs.ClassObj).labels.push(l);
                stack.push({ type: CtxType.Label, node: l });
                continue;
            }

            if (head === 'LEADER') {
                if (ctx.type !== CtxType.Class) {
                    throw new Error(`LEADER outside CLASS at line ${lineNo}`);
                }
                const ld: Defs.LeaderObj = { styles: [], attributes: {} };
                (ctx.node as Defs.ClassObj).leader = ld;
                stack.push({ type: CtxType.Leader, node: ld });
                continue;
            }

            if (head === 'JOIN') {
                if (ctx.type !== CtxType.Layer) {
                    throw new Error(`JOIN outside LAYER at line ${lineNo}`);
                }
                const j: Defs.JoinObj = { attributes: {} };
                (ctx.node as Defs.LayerObj).joins.push(j);
                stack.push({ type: CtxType.Join, node: j });
                continue;
            }

            if (head === 'IDENTIFY') {
                if (ctx.type !== CtxType.Layer) {
                    throw new Error(`IDENTIFY outside LAYER at line ${lineNo}`);
                }
                stack.push({ type: CtxType.Identify, node: (ctx.node as Defs.LayerObj).identify });
                continue;
            }

            if (head === 'COMPOSITE') {
                if (ctx.type !== CtxType.Layer) {
                    throw new Error(`COMPOSITE outside LAYER at line ${lineNo}`);
                }
                const comp: Defs.CompositeObj = { pattern: [], attributes: {} };
                (ctx.node as Defs.LayerObj).composite = comp;
                stack.push({ type: CtxType.Composite, node: comp });
                continue;
            }

            // Content by context
            switch (ctx.type) {
                case CtxType.Metadata:
                    MapfileParser.parseMetadataLine(ctx.node as Record<string, string>, tokens, lineNo);
                    break;
                case CtxType.Validation:
                    MapfileParser.parseValidationLine(ctx.node as Record<string, string>, tokens, lineNo);
                    break;
                case CtxType.Projection:
                    MapfileParser.parseProjectionLine(ctx.node as string[], tokens);
                    break;
                case CtxType.OutputFormat:
                    MapfileParser.applyKeyValuesToOutputFormat(ctx.node as Defs.OutputFormatObj, tokens);
                    break;
                case CtxType.Symbol:
                    MapfileParser.applyKeyValuesToSymbol(ctx.node as Defs.SymbolObj, tokens);
                    break;
                case CtxType.Web:
                    MapfileParser.addToAttributes((ctx.node as Defs.WebObj).attributes, tokens);
                    break;
                case CtxType.Layer:
                    MapfileParser.applyKeyValuesToLayer(ctx.node as Defs.LayerObj, tokens);
                    break;
                case CtxType.Class:
                    MapfileParser.applyKeyValuesToClass(ctx.node as Defs.ClassObj, tokens);
                    break;
                case CtxType.Style:
                    MapfileParser.applyKeyValuesToStyle(ctx.node as Defs.StyleObj, tokens);
                    break;
                case CtxType.Label:
                    MapfileParser.applyKeyValuesToLabel(ctx.node as Defs.LabelObj, tokens);
                    break;
                case CtxType.Leader:
                    MapfileParser.applyKeyValuesToLeader(ctx.node as Defs.LeaderObj, tokens);
                    break;
                case CtxType.Map:
                    MapfileParser.applyKeyValuesToMap(ctx.node as Defs.MapObj, tokens);
                    break;
                case CtxType.Join:
                    MapfileParser.applyKeyValuesToJoin(ctx.node as Defs.JoinObj, tokens);
                    break;
                case CtxType.Identify:
                    MapfileParser.addToAttributes(ctx.node as Defs.AttributesBag, tokens);
                    break;
                case CtxType.Composite:
                    MapfileParser.applyKeyValuesToComposite(ctx.node as Defs.CompositeObj, tokens);
                    break;
                default:
                    throw new Error(`Unhandled context at line ${lineNo}`);
            }
        }

        if (stack.length !== 0) {
            throw new Error('Unbalanced blocks: missing END(s)');
        }
        return map;
    }

    // ========== Parsing Methods ==========

    private static parseMetadataLine(dict: Record<string, string>, tokens: string[], lineNo: number): void {
        if (tokens.length >= 2) {
            const key = MapfileParser.unquote(tokens[0]);
            const val = tokens.slice(1).map(MapfileParser.unquote).join(' ');
            dict[key] = val;
        } else {
            throw new Error(`Invalid METADATA entry at line ${lineNo}`);
        }
    }

    private static parseValidationLine(dict: Record<string, string>, tokens: string[], lineNo: number): void {
        if (tokens.length >= 2) {
            const key = MapfileParser.unquote(tokens[0]);
            const val = tokens.slice(1).map(MapfileParser.unquote).join(' ');
            dict[key] = val;
        } else {
            throw new Error(`Invalid VALIDATION entry at line ${lineNo}`);
        }
    }

    private static parseProjectionLine(list: string[], tokens: string[]): void {
        const text = tokens.map(MapfileParser.unquote).join(' ').trim();
        if (text) {
            list.push(text);
        }
    }

    private static applyKeyValuesToMap(map: Defs.MapObj, tokens: string[]): void {
        const key = tokens[0].toUpperCase();
        const vals = tokens.slice(1);
        const joined = vals.join(' ');

        switch (key) {
            case 'NAME':
                map.name = MapfileParser.unquote(joined);
                break;
            case 'STATUS':
                map.status = joined.toUpperCase();
                break;
            case 'EXTENT':
                map.extent = MapfileParser.parseDoubles(vals, 4) as Defs.Double4;
                break;
            case 'SIZE':
                map.size = MapfileParser.parseInts(vals, 2) as Defs.Int2;
                break;
            case 'UNITS':
                map.units = joined;
                break;
            case 'IMAGETYPE':
                map.imageType = MapfileParser.unquote(joined);
                break;
            case 'IMAGECOLOR':
                if (vals.length >= 1 && vals[0].startsWith('#')) {
                    map.imageColorHex = MapfileParser.unquote(vals[0]);
                } else if (vals.length >= 3) {
                    map.imageColor = MapfileParser.parseInts(vals, 3) as Defs.Int3;
                } else {
                    MapfileParser.addToAttributes(map.attributes, tokens);
                }
                break;
            case 'SHAPEPATH':
                map.shapePath = MapfileParser.unquote(joined);
                break;
            case 'SYMBOLSET':
                map.symbolSet = MapfileParser.unquote(joined);
                break;
            case 'FONTSET':
                map.fontSet = MapfileParser.unquote(joined);
                break;
            case 'RESOLUTION':
                map.resolution = Math.floor(MapfileParser.parseDouble(vals));
                break;
            case 'DEFRESOLUTION':
                map.defResolution = Math.floor(MapfileParser.parseDouble(vals));
                break;
            case 'MAXSIZE':
                map.maxSize = Math.floor(MapfileParser.parseDouble(vals));
                break;
            case 'ANGLE':
                map.angle = MapfileParser.parseDouble(vals);
                break;
            case 'TEMPLATEPATTERN':
                map.templatePattern = MapfileParser.unquote(joined);
                break;
            case 'DATAPATTERN':
                map.dataPattern = MapfileParser.unquote(joined);
                break;
            case 'DEBUG':
                map.debug = joined;
                break;
            case 'TRANSPARENT':
                map.transparent = joined.toUpperCase();
                break;
            case 'CONFIG':
                if (vals.length >= 2) {
                    const ck = MapfileParser.unquote(vals[0]);
                    const cv = MapfileParser.unquote(vals.slice(1).join(' '));
                    map.config.push({ key: ck, value: cv });
                } else {
                    MapfileParser.addToAttributes(map.attributes, tokens);
                }
                break;
            case 'PROJECTION':
                break; // Handled in CtxType.Projection
            default:
                MapfileParser.addToAttributes(map.attributes, tokens);
                break;
        }
    }

    private static applyKeyValuesToLayer(layer: Defs.LayerObj, tokens: string[]): void {
        const key = tokens[0].toUpperCase();
        const vals = tokens.slice(1);
        const joined = vals.join(' ');

        switch (key) {
            case 'NAME':
                layer.name = MapfileParser.unquote(joined);
                break;
            case 'TYPE':
                layer.type = joined.toUpperCase();
                break;
            case 'STATUS':
                layer.status = joined.toUpperCase();
                break;
            case 'DATA':
                layer.data = MapfileParser.unquote(joined);
                break;
            case 'CONNECTIONTYPE':
                layer.connectionType = joined.toUpperCase();
                break;
            case 'CONNECTION':
                layer.connection = MapfileParser.unquote(joined);
                break;
            case 'GROUP':
                layer.group = MapfileParser.unquote(joined);
                break;
            case 'CLASSGROUP':
                layer.classGroup = MapfileParser.unquote(joined);
                break;
            case 'CLASSITEM':
                layer.classItem = MapfileParser.unquote(joined);
                break;
            case 'LABELITEM':
                layer.labelItem = MapfileParser.unquote(joined);
                break;
            case 'MINSCALEDENOM':
                layer.minScaleDenom = MapfileParser.parseDouble(vals);
                break;
            case 'MAXSCALEDENOM':
                layer.maxScaleDenom = MapfileParser.parseDouble(vals);
                break;
            case 'MINSCALE':
                layer.minScale = MapfileParser.parseDouble(vals);
                break;
            case 'MAXSCALE':
                layer.maxScale = MapfileParser.parseDouble(vals);
                break;
            case 'MINGEOWIDTH':
                layer.minGeoWidth = MapfileParser.parseDouble(vals);
                break;
            case 'MAXGEOWIDTH':
                layer.maxGeoWidth = MapfileParser.parseDouble(vals);
                break;
            case 'SYMBOLSCALEDENOM':
                layer.symbolScaleDenom = MapfileParser.parseDouble(vals);
                break;
            case 'EXTENT':
                layer.extent = MapfileParser.parseDoubles(vals, 4) as Defs.Double4;
                break;
            case 'UNITS':
                layer.units = joined;
                break;
            case 'LABELMINSCALEDENOM':
                layer.labelMinScaleDenom = MapfileParser.parseDouble(vals);
                break;
            case 'LABELMAXSCALEDENOM':
                layer.labelMaxScaleDenom = MapfileParser.parseDouble(vals);
                break;
            case 'LABELREQUIRES':
                layer.labelRequires = MapfileParser.unquote(joined);
                break;
            case 'LABELCACHE':
                layer.labelCache = joined.toUpperCase();
                break;
            case 'DEBUG':
                layer.debug = joined;
                break;
            case 'ENCODING':
                layer.encoding = MapfileParser.unquote(joined);
                break;
            case 'FILTER':
                layer.filter = MapfileParser.unquote(joined);
                break;
            case 'FILTERITEM':
                layer.filterItem = MapfileParser.unquote(joined);
                break;
            case 'MINFEATURESIZE':
                layer.minFeatureSize = Math.floor(MapfileParser.parseDouble(vals));
                break;
            case 'MAXFEATURES':
                layer.maxFeatures = Math.floor(MapfileParser.parseDouble(vals));
                break;
            case 'MASK':
                layer.mask = MapfileParser.unquote(joined);
                break;
            case 'STYLEITEM':
                layer.styleItem = joined;
                break;
            case 'GEOMTRANSFORM':
                layer.geomTransform = MapfileParser.unquote(joined);
                break;
            case 'POSTLABELCACHE':
                layer.postLabelCache = joined.toUpperCase();
                break;
            case 'REQUIRES':
                layer.requires = MapfileParser.unquote(joined);
                break;
            case 'TRANSFORM':
                layer.transform = joined.toUpperCase();
                break;
            case 'TOLERANCE':
                layer.tolerance = MapfileParser.parseDouble(vals);
                break;
            case 'TOLERANCEUNITS':
                layer.toleranceUnits = joined;
                break;
            case 'HEADER':
                layer.header = MapfileParser.unquote(joined);
                break;
            case 'FOOTER':
                layer.footer = MapfileParser.unquote(joined);
                break;
            case 'TEMPLATE':
                layer.template = MapfileParser.unquote(joined);
                break;
            case 'OFFSITE':
                if (vals.length >= 1 && vals[0].startsWith('#')) {
                    layer.offsiteHex = MapfileParser.unquote(vals[0]);
                } else if (vals.length >= 3) {
                    layer.offsiteColor = MapfileParser.parseInts(vals, 3) as Defs.Int3;
                } else {
                    MapfileParser.addToAttributes(layer.attributes, tokens);
                }
                break;
            case 'TILEINDEX':
                layer.tileIndex = MapfileParser.unquote(joined);
                break;
            case 'TILEITEM':
                layer.tileItem = MapfileParser.unquote(joined);
                break;
            case 'TILEFILTER':
                layer.tileFilter = MapfileParser.unquote(joined);
                break;
            case 'TILEFILTERITEM':
                layer.tileFilterItem = MapfileParser.unquote(joined);
                break;
            case 'PROCESSING':
                layer.processing.push(MapfileParser.unquote(joined));
                break;
            case 'CONNECTIONOPTIONS':
                if (vals.length >= 2) {
                    const ck = MapfileParser.unquote(vals[0]);
                    const cv = MapfileParser.unquote(vals.slice(1).join(' '));
                    layer.connectionOptions[ck] = cv;
                } else {
                    MapfileParser.addToAttributes(layer.attributes, tokens);
                }
                break;
            case 'PROJECTION':
                break; // Handled in CtxType.Projection
            default:
                MapfileParser.addToAttributes(layer.attributes, tokens);
                break;
        }
    }

    private static applyKeyValuesToClass(c: Defs.ClassObj, tokens: string[]): void {
        const key = tokens[0].toUpperCase();
        const vals = tokens.slice(1);
        const joined = vals.join(' ');

        switch (key) {
            case 'NAME':
                c.name = MapfileParser.unquote(joined);
                break;
            case 'TITLE':
                c.title = MapfileParser.unquote(joined);
                break;
            case 'STATUS':
                c.status = joined.toUpperCase();
                break;
            case 'GROUP':
                c.group = MapfileParser.unquote(joined);
                break;
            case 'EXPRESSION':
                c.expression = MapfileParser.unquote(joined);
                break;
            case 'TEXT':
                c.text = MapfileParser.unquote(joined);
                break;
            case 'TEMPLATE':
                c.template = MapfileParser.unquote(joined);
                break;
            case 'KEYIMAGE':
                c.keyImage = MapfileParser.unquote(joined);
                break;
            case 'MINSCALEDENOM':
                c.minScaleDenom = MapfileParser.parseDouble(vals);
                break;
            case 'MAXSCALEDENOM':
                c.maxScaleDenom = MapfileParser.parseDouble(vals);
                break;
            case 'MINFEATURESIZE':
                c.minFeatureSize = Math.floor(MapfileParser.parseDouble(vals));
                break;
            case 'DEBUG':
                c.debug = joined;
                break;
            case 'FALLBACK':
                c.fallback = joined.toUpperCase() === 'TRUE' || joined.toUpperCase() === 'ON';
                break;
            default:
                MapfileParser.addToAttributes(c.attributes, tokens);
                break;
        }
    }

    private static applyKeyValuesToLeader(l: Defs.LeaderObj, tokens: string[]): void {
        const key = tokens[0].toUpperCase();
        const vals = tokens.slice(1);

        switch (key) {
            case 'GRIDSTEP':
                l.gridStep = Math.floor(MapfileParser.parseDouble(vals));
                break;
            case 'MAXDISTANCE':
                l.maxDistance = Math.floor(MapfileParser.parseDouble(vals));
                break;
            default:
                MapfileParser.addToAttributes(l.attributes, tokens);
                break;
        }
    }

    private static applyKeyValuesToStyle(s: Defs.StyleObj, tokens: string[]): void {
        const key = tokens[0].toUpperCase();
        const vals = tokens.slice(1);
        const joined = vals.join(' ');

        switch (key) {
            case 'COLOR':
                if (vals.length >= 1 && vals[0].startsWith('#')) {
                    s.colorHex = MapfileParser.unquote(vals[0]);
                } else if (vals.length >= 1 && vals[0].startsWith('[')) {
                    s.colorAttr = joined;
                } else if (vals.length >= 3) {
                    s.color = MapfileParser.parseInts(vals, 3) as Defs.Int3;
                } else {
                    MapfileParser.addToAttributes(s.attributes, tokens);
                }
                break;
            case 'OUTLINECOLOR':
                if (vals.length >= 1 && vals[0].startsWith('#')) {
                    s.outlineColorHex = MapfileParser.unquote(vals[0]);
                } else if (vals.length >= 1 && vals[0].startsWith('[')) {
                    s.outlineColorAttr = joined;
                } else if (vals.length >= 3) {
                    s.outlineColor = MapfileParser.parseInts(vals, 3) as Defs.Int3;
                } else {
                    MapfileParser.addToAttributes(s.attributes, tokens);
                }
                break;
            case 'OUTLINEWIDTH':
                s.outlineWidth = MapfileParser.parseDouble(vals);
                break;
            case 'SYMBOL':
                s.symbol = MapfileParser.unquote(joined);
                break;
            case 'ANGLE':
                s.angle = joined;
                break;
            case 'WIDTH':
                s.width = MapfileParser.parseDouble(vals);
                break;
            case 'SIZE':
                if (vals.length === 1 && !isNaN(parseFloat(vals[0]))) {
                    s.size = parseFloat(vals[0]);
                } else {
                    s.sizeExpr = joined;
                }
                break;
            case 'MINSCALEDENOM':
                s.minScaleDenom = MapfileParser.parseDouble(vals);
                break;
            case 'MAXSCALEDENOM':
                s.maxScaleDenom = MapfileParser.parseDouble(vals);
                break;
            case 'MINWIDTH':
                s.minWidth = MapfileParser.parseDouble(vals);
                break;
            case 'MAXWIDTH':
                s.maxWidth = MapfileParser.parseDouble(vals);
                break;
            case 'MINSIZE':
                s.minSize = MapfileParser.parseDouble(vals);
                break;
            case 'MAXSIZE':
                s.maxSize = MapfileParser.parseDouble(vals);
                break;
            case 'GAP':
                s.gap = MapfileParser.parseDouble(vals);
                break;
            case 'INITIALGAP':
                s.initialGap = MapfileParser.parseDouble(vals);
                break;
            case 'OFFSET':
                s.offsetX = vals[0];
                s.offsetY = vals[1];
                break;
            case 'POLAROFFSET':
                s.polarOffsetR = vals[0];
                s.polarOffsetA = vals[1];
                break;
            case 'LINECAP':
                s.lineCap = joined.toUpperCase();
                break;
            case 'LINEJOIN':
                s.lineJoin = joined.toUpperCase();
                break;
            case 'LINEJOINMAXSIZE':
                s.lineJoinMaxSize = Math.floor(MapfileParser.parseDouble(vals));
                break;
            case 'PATTERN':
                for (const v of vals) {
                    const num = parseFloat(v);
                    if (!isNaN(num)) {
                        s.pattern.push(num);
                    }
                }
                break;
            case 'GEOMTRANSFORM':
                s.geomTransform = MapfileParser.unquote(joined);
                break;
            case 'OPACITY':
                s.opacity = joined;
                break;
            case 'RANGEITEM':
                s.rangeItem = MapfileParser.unquote(joined);
                break;
            case 'COLORRANGE':
                if (vals.length === 2 && vals[0].startsWith('#') && vals[1].startsWith('#')) {
                    s.colorRangeLow = { hex: MapfileParser.unquote(vals[0]) };
                    s.colorRangeHigh = { hex: MapfileParser.unquote(vals[1]) };
                } else if (vals.length === 6) {
                    s.colorRangeLow = { rgb: MapfileParser.parseInts(vals.slice(0, 3), 3) as Defs.Int3 };
                    s.colorRangeHigh = { rgb: MapfileParser.parseInts(vals.slice(3, 6), 3) as Defs.Int3 };
                } else {
                    MapfileParser.addToAttributes(s.attributes, tokens);
                }
                break;
            case 'DATARANGE':
                if (vals.length >= 2) {
                    s.dataRange = {
                        low: MapfileParser.parseDouble([vals[0]]),
                        high: MapfileParser.parseDouble([vals[1]])
                    };
                } else {
                    MapfileParser.addToAttributes(s.attributes, tokens);
                }
                break;
            default:
                MapfileParser.addToAttributes(s.attributes, tokens);
                break;
        }
    }

    private static applyKeyValuesToLabel(l: Defs.LabelObj, tokens: string[]): void {
        const key = tokens[0].toUpperCase();
        const vals = tokens.slice(1);
        const joined = vals.join(' ');

        switch (key) {
            case 'ALIGN':
                l.align = joined;
                break;
            case 'ANGLE':
                l.angle = joined;
                break;
            case 'BUFFER':
                l.buffer = Math.floor(MapfileParser.parseDouble(vals));
                break;
            case 'COLOR':
                if (vals.length >= 1 && vals[0].startsWith('#')) {
                    l.colorHex = MapfileParser.unquote(vals[0]);
                } else if (vals.length >= 1 && vals[0].startsWith('[')) {
                    l.colorAttr = joined;
                } else if (vals.length >= 3) {
                    l.color = MapfileParser.parseInts(vals, 3) as Defs.Int3;
                } else {
                    MapfileParser.addToAttributes(l.attributes, tokens);
                }
                break;
            case 'OUTLINECOLOR':
                if (vals.length >= 1 && vals[0].startsWith('#')) {
                    l.outlineColorHex = MapfileParser.unquote(vals[0]);
                } else if (vals.length >= 1 && vals[0].startsWith('[')) {
                    l.outlineColorAttr = joined;
                } else if (vals.length >= 3) {
                    l.outlineColor = MapfileParser.parseInts(vals, 3) as Defs.Int3;
                } else {
                    MapfileParser.addToAttributes(l.attributes, tokens);
                }
                break;
            case 'OUTLINEWIDTH':
                l.outlineWidth = Math.floor(MapfileParser.parseDouble(vals));
                break;
            case 'FONT':
                l.font = MapfileParser.unquote(joined);
                break;
            case 'TYPE':
                l.type = joined.toUpperCase();
                break;
            case 'SIZE':
                l.size = joined;
                break;
            case 'MAXOVERLAPANGLE':
                l.maxOverlapAngle = MapfileParser.parseDouble(vals);
                break;
            case 'MINSCALEDENOM':
                l.minScaleDenom = MapfileParser.parseDouble(vals);
                break;
            case 'MAXSCALEDENOM':
                l.maxScaleDenom = MapfileParser.parseDouble(vals);
                break;
            case 'MINDISTANCE':
                l.minDistance = Math.floor(MapfileParser.parseDouble(vals));
                break;
            case 'MINFEATURESIZE':
                l.minFeatureSize = joined.toUpperCase();
                break;
            case 'MINSIZE':
                l.minSize = Math.floor(MapfileParser.parseDouble(vals));
                break;
            case 'MAXSIZE':
                l.maxSize = Math.floor(MapfileParser.parseDouble(vals));
                break;
            case 'OFFSET':
                l.offsetX = vals[0];
                l.offsetY = vals[1];
                break;
            case 'POSITION':
                l.position = joined.toLowerCase();
                break;
            case 'PRIORITY':
                l.priority = joined;
                break;
            case 'PARTIALS':
                l.partials = joined.toUpperCase();
                break;
            case 'FORCE':
                l.force = joined.toUpperCase();
                break;
            case 'REPEATDISTANCE':
                l.repeatDistance = Math.floor(MapfileParser.parseDouble(vals));
                break;
            case 'SHADOWCOLOR':
                if (vals.length >= 1 && vals[0].startsWith('#')) {
                    l.shadowColorHex = MapfileParser.unquote(vals[0]);
                } else if (vals.length >= 3) {
                    l.shadowColor = MapfileParser.parseInts(vals, 3) as Defs.Int3;
                } else {
                    MapfileParser.addToAttributes(l.attributes, tokens);
                }
                break;
            case 'SHADOWSIZE':
                l.shadowSizeX = vals[0];
                l.shadowSizeY = vals[1];
                break;
            case 'TEXT':
                l.text = MapfileParser.unquote(joined);
                break;
            case 'WRAP':
                l.wrap = MapfileParser.unquote(joined);
                break;
            case 'MAXLENGTH':
                l.maxLength = Math.floor(MapfileParser.parseDouble(vals));
                break;
            default:
                MapfileParser.addToAttributes(l.attributes, tokens);
                break;
        }
    }

    private static applyKeyValuesToOutputFormat(of: Defs.OutputFormatObj, tokens: string[]): void {
        const key = tokens[0].toUpperCase();
        const vals = tokens.slice(1);
        const joined = vals.join(' ');

        switch (key) {
            case 'NAME':
                of.name = MapfileParser.unquote(joined);
                break;
            case 'DRIVER':
                of.driver = MapfileParser.unquote(joined);
                break;
            case 'MIMETYPE':
                of.mimeType = MapfileParser.unquote(joined);
                break;
            case 'EXTENSION':
                of.extension = MapfileParser.unquote(joined);
                break;
            case 'IMAGEMODE':
                of.imageMode = joined.toUpperCase();
                break;
            case 'TRANSPARENT':
                of.transparent = joined.toUpperCase();
                break;
            default:
                MapfileParser.addToAttributes(of.attributes, tokens);
                break;
        }
    }

    private static applyKeyValuesToSymbol(sym: Defs.SymbolObj, tokens: string[]): void {
        const key = tokens[0].toUpperCase();
        const vals = tokens.slice(1);
        const joined = vals.join(' ');

        switch (key) {
            case 'NAME':
                sym.name = MapfileParser.unquote(joined);
                break;
            case 'TYPE':
                sym.type = joined.toUpperCase();
                break;
            case 'IMAGE':
                sym.image = MapfileParser.unquote(joined);
                break;
            case 'FONT':
                sym.font = MapfileParser.unquote(joined);
                break;
            case 'CHARACTER':
                sym.character = MapfileParser.unquote(joined);
                break;
            case 'POINTS':
                break; // Handled specially in the main parser loop
            case 'GAP':
                sym.gap = Math.floor(MapfileParser.parseDouble(vals));
                break;
            case 'WIDTH':
                sym.width = MapfileParser.parseDouble(vals);
                break;
            case 'LINEWIDTH':
                sym.lineWidth = Math.floor(MapfileParser.parseDouble(vals));
                break;
            case 'SIZE':
                sym.size = MapfileParser.parseDouble(vals);
                break;
            case 'ANGLE':
                sym.angle = joined;
                break;
            case 'PATTERN':
                if (!sym.pattern) sym.pattern = [];
                for (const v of vals) {
                    const num = parseFloat(v);
                    if (!isNaN(num)) {
                        sym.pattern.push(num);
                    }
                }
                break;
            case 'OFFSET':
                sym.offsetX = vals[0];
                sym.offsetY = vals[1];
                break;
            case 'FILLED':
                sym.filled = joined.toUpperCase();
                break;
            case 'ANTIALIAS':
                sym.antialias = joined.toUpperCase();
                break;
            case 'ANCHORPOINT':
                sym.anchorPoint = joined;
                break;
            default:
                MapfileParser.addToAttributes(sym.attributes ?? {}, tokens);
                break;
        }
    }

    private static applyKeyValuesToJoin(j: Defs.JoinObj, tokens: string[]): void {
        const key = tokens[0].toUpperCase();
        const joined = tokens.slice(1).join(' ');

        switch (key) {
            case 'NAME':
                j.name = MapfileParser.unquote(joined);
                break;
            case 'TABLE':
                j.table = MapfileParser.unquote(joined);
                break;
            case 'FROM':
                j.from = MapfileParser.unquote(joined);
                break;
            case 'TO':
                j.to = MapfileParser.unquote(joined);
                break;
            case 'TYPE':
                j.type = joined.toUpperCase();
                break;
            case 'TEMPLATE':
                j.template = MapfileParser.unquote(joined);
                break;
            default:
                if (!j.attributes[key]) {
                    j.attributes[key] = [];
                }
                j.attributes[key].push(tokens.slice(1).map(MapfileParser.unquote));
                break;
        }
    }

    private static applyKeyValuesToComposite(comp: Defs.CompositeObj, tokens: string[]): void {
        const key = tokens[0].toUpperCase();
        const vals = tokens.slice(1);
        const joined = vals.join(' ');

        switch (key) {
            case 'OPACITY':
                comp.opacity = Math.floor(MapfileParser.parseDouble(vals));
                break;
            case 'COMPOP':
                comp.compOp = joined;
                break;
            case 'PATTERN':
                for (const v of vals) {
                    const num = parseFloat(v);
                    if (!isNaN(num)) {
                        comp.pattern!.push(num);
                    }
                }
                break;
            default:
                MapfileParser.addToAttributes(comp.attributes ?? {}, tokens);
                break;
        }
    }

    // ========== INCLUDE expansion (preprocessing) ==========

    private static expandIncludes(text: string, baseDir?: string): string {
        if (!baseDir) return text;

        const lines = text.split(/\r?\n/);
        const result: string[] = [];
        const includeStack = new Set<string>();

        for (const line of lines) {
            const stripped = MapfileParser.stripComments(line);
            const tokens = MapfileParser.tokenize(stripped);

            if (
                tokens.length > 0 &&
                tokens[0].toUpperCase() === 'INCLUDE' &&
                tokens.length >= 2
            ) {
                const incPathPart = tokens.slice(1).join(' ');
                const incPath = MapfileParser.unquote(incPathPart);

                // For now, we'll log a message that includes can't be resolved in browser/Node.js
                // In a real implementation, this would require backend support
                result.push(`# INCLUDE "${incPath}" (not resolved in client)`);
            } else {
                result.push(line);
            }
        }

        return result.join('\n');
    }

    // ========== Helpers ==========

    private static stripComments(line: string): string {
        let inQuotes = false;
        let result = '';

        for (let i = 0; i < line.length; i++) {
            const c = line[i];
            if (c === '"') {
                inQuotes = !inQuotes;
                result += c;
            } else if (!inQuotes && c === '#') {
                break;
            } else {
                result += c;
            }
        }

        return result.trim();
    }

    private static tokenize(line: string): string[] {
        const tokens: string[] = [];
        let current = '';
        let inQuotes = false;

        for (let i = 0; i < line.length; i++) {
            const c = line[i];
            if (c === '"') {
                current += c;
                inQuotes = !inQuotes;
            } else if (!inQuotes && /\s/.test(c)) {
                if (current) {
                    tokens.push(current);
                    current = '';
                }
            } else {
                current += c;
            }
        }

        if (current) {
            tokens.push(current);
        }

        return tokens;
    }

    private static unquote(s: string): string {
        s = s.trim();
        if (s.length >= 2 && s[0] === '"' && s[s.length - 1] === '"') {
            const inner = s.substring(1, s.length - 1);
            return inner.replace(/\\"/g, '"');
        }
        return s;
    }

    private static parseDoubles(vals: string[], expected: number): number[] {
        if (vals.length < expected) {
            throw new Error(`Expected ${expected} numbers, got ${vals.length}`);
        }
        const arr: number[] = [];
        for (let i = 0; i < expected; i++) {
            arr.push(parseFloat(vals[i]));
        }
        return arr;
    }

    private static parseInts(vals: string[], expected: number): number[] {
        if (vals.length < expected) {
            throw new Error(`Expected ${expected} integers, got ${vals.length}`);
        }
        const arr: number[] = [];
        for (let i = 0; i < expected; i++) {
            arr.push(parseInt(vals[i], 10));
        }
        return arr;
    }

    private static parseDouble(vals: string[]): number {
        if (vals.length < 1) {
            throw new Error('Expected a numeric value');
        }
        return parseFloat(vals[0]);
    }

    private static addToAttributes(bag: Defs.AttributesBag, tokens: string[]): void {
        const key = tokens[0];
        const vals = tokens.slice(1).map(MapfileParser.unquote);

        if (!bag[key]) {
            bag[key] = [];
        }
        bag[key].push(vals);
    }
}

// ============================================================================
// SERIALIZER
// ============================================================================

export class MapfileSerializer {
    public static mapToString(map: Defs.MapObj): string {
        const lines: string[] = [];
        const writer: TextWriter = { lines };
        this.writeMap(writer, map);
        return lines.join('\n');
    }

    private static writeIndent(writer: TextWriter, n: number): void {
        writer.lines.push('  '.repeat(n));
    }

    private static writeKeyValues(writer: TextWriter, indent: number, key: string, ...values: string[]): void {
        let line = '  '.repeat(indent);
        if (values.length === 0) {
            line += key;
        } else {
            line += key + ' ' + values.join(' ');
        }
        writer.lines.push(line);
    }

    private static quote(s: string): string {
        return '"' + s.replace(/"/g, '\\"') + '"';
    }

    private static maybeQuote(s: string): string {
        if (MapfileSerializer.needsQuoting(s)) {
            return MapfileSerializer.quote(s);
        }
        return s;
    }

    private static needsQuoting(s: string): boolean {
        if (!s || s.length === 0) return true;
        return /\s|#|"/.test(s);
    }

    private static writeAttributes(writer: TextWriter, indent: number, attrs: Defs.AttributesBag): void {
        for (const [key, valuesList] of Object.entries(attrs)) {
            for (const values of valuesList) {
                const rendered = values.map(v => MapfileSerializer.maybeQuote(v));
                MapfileSerializer.writeKeyValues(writer, indent, key.toUpperCase(), ...rendered);
            }
        }
    }

    private static writeMap(writer: TextWriter, map: Defs.MapObj): void {
        writer.lines.push('MAP');

        if (map.name) MapfileSerializer.writeKeyValues(writer, 1, 'NAME', MapfileSerializer.quote(map.name));
        if (map.status) MapfileSerializer.writeKeyValues(writer, 1, 'STATUS', map.status);
        if (map.extent && map.extent.length === 4) {
            MapfileSerializer.writeKeyValues(writer, 1, 'EXTENT', ...map.extent.map(d => d.toString()));
        }
        if (map.size && map.size.length === 2) {
            MapfileSerializer.writeKeyValues(writer, 1, 'SIZE', ...map.size.map(d => d.toString()));
        }
        if (map.units) MapfileSerializer.writeKeyValues(writer, 1, 'UNITS', map.units);

        if (map.imageType) MapfileSerializer.writeKeyValues(writer, 1, 'IMAGETYPE', MapfileSerializer.maybeQuote(map.imageType));
        if (map.imageColor && map.imageColor.length === 3) {
            MapfileSerializer.writeKeyValues(writer, 1, 'IMAGECOLOR', ...map.imageColor.map(i => i.toString()));
        } else if (map.imageColorHex) {
            MapfileSerializer.writeKeyValues(writer, 1, 'IMAGECOLOR', MapfileSerializer.quote(map.imageColorHex));
        }

        if (map.shapePath) MapfileSerializer.writeKeyValues(writer, 1, 'SHAPEPATH', MapfileSerializer.quote(map.shapePath));
        if (map.symbolSet) MapfileSerializer.writeKeyValues(writer, 1, 'SYMBOLSET', MapfileSerializer.quote(map.symbolSet));
        if (map.fontSet) MapfileSerializer.writeKeyValues(writer, 1, 'FONTSET', MapfileSerializer.quote(map.fontSet));

        if (map.resolution !== undefined) MapfileSerializer.writeKeyValues(writer, 1, 'RESOLUTION', map.resolution.toString());
        if (map.defResolution !== undefined) MapfileSerializer.writeKeyValues(writer, 1, 'DEFRESOLUTION', map.defResolution.toString());
        if (map.maxSize !== undefined) MapfileSerializer.writeKeyValues(writer, 1, 'MAXSIZE', map.maxSize.toString());
        if (map.angle !== undefined) MapfileSerializer.writeKeyValues(writer, 1, 'ANGLE', map.angle.toString());

        if (map.templatePattern) MapfileSerializer.writeKeyValues(writer, 1, 'TEMPLATEPATTERN', MapfileSerializer.quote(map.templatePattern));
        if (map.dataPattern) MapfileSerializer.writeKeyValues(writer, 1, 'DATAPATTERN', MapfileSerializer.quote(map.dataPattern));

        if (map.debug) MapfileSerializer.writeKeyValues(writer, 1, 'DEBUG', map.debug);
        if (map.transparent) MapfileSerializer.writeKeyValues(writer, 1, 'TRANSPARENT', map.transparent);

        for (const kv of map.config) {
            MapfileSerializer.writeKeyValues(writer, 1, 'CONFIG', MapfileSerializer.maybeQuote(kv.key), MapfileSerializer.maybeQuote(kv.value));
        }

        MapfileSerializer.writeAttributes(writer, 1, map.attributes);

        if (map.projection.length > 0) {
            writer.lines.push('  PROJECTION');
            for (const l of map.projection) {
                writer.lines.push('    ' + MapfileSerializer.quote(l));
            }
            writer.lines.push('  END');
        }

        for (const symbol of map.symbols) {
            MapfileSerializer.writeSymbol(writer, symbol, 1);
        }
        if (!MapfileSerializer.isWebEmpty(map.web)) {
            MapfileSerializer.writeWeb(writer, map.web, 1);
        }
        for (const of of map.outputFormats) {
            MapfileSerializer.writeOutputFormat(writer, of, 1);
        }
        for (const layer of map.layers) {
            MapfileSerializer.writeLayer(writer, layer, 1);
        }

        writer.lines.push('END');
    }

    private static writeWeb(writer: TextWriter, web: Defs.WebObj, indent: number): void {
        writer.lines.push('  '.repeat(indent) + 'WEB');
        MapfileSerializer.writeAttributes(writer, indent + 1, web.attributes);
        if (Object.keys(web.metadata).length > 0) {
            writer.lines.push('  '.repeat(indent + 1) + 'METADATA');
            for (const [key, val] of Object.entries(web.metadata)) {
                writer.lines.push('  '.repeat(indent + 2) + `${MapfileSerializer.quote(key)} ${MapfileSerializer.quote(val)}`);
            }
            writer.lines.push('  '.repeat(indent + 1) + 'END');
        }
        writer.lines.push('  '.repeat(indent) + 'END');
    }

    private static isWebEmpty(web: Defs.WebObj): boolean {
        return Object.keys(web.metadata).length === 0 && Object.keys(web.attributes).length === 0;
    }

    private static writeOutputFormat(writer: TextWriter, of: Defs.OutputFormatObj, indent: number): void {
        writer.lines.push('  '.repeat(indent) + 'OUTPUTFORMAT');
        if (of.name) MapfileSerializer.writeKeyValues(writer, indent + 1, 'NAME', MapfileSerializer.maybeQuote(of.name));
        if (of.driver) MapfileSerializer.writeKeyValues(writer, indent + 1, 'DRIVER', MapfileSerializer.maybeQuote(of.driver));
        if (of.mimeType) MapfileSerializer.writeKeyValues(writer, indent + 1, 'MIMETYPE', MapfileSerializer.quote(of.mimeType));
        if (of.extension) MapfileSerializer.writeKeyValues(writer, indent + 1, 'EXTENSION', MapfileSerializer.maybeQuote(of.extension));
        if (of.imageMode) MapfileSerializer.writeKeyValues(writer, indent + 1, 'IMAGEMODE', of.imageMode);
        if (of.transparent) MapfileSerializer.writeKeyValues(writer, indent + 1, 'TRANSPARENT', of.transparent);
        MapfileSerializer.writeAttributes(writer, indent + 1, of.attributes);
        writer.lines.push('  '.repeat(indent) + 'END');
    }

    private static writeSymbol(writer: TextWriter, sym: Defs.SymbolObj, indent: number): void {
        writer.lines.push('  '.repeat(indent) + 'SYMBOL');
        if (sym.name) MapfileSerializer.writeKeyValues(writer, indent + 1, 'NAME', MapfileSerializer.quote(sym.name));
        if (sym.type) MapfileSerializer.writeKeyValues(writer, indent + 1, 'TYPE', sym.type);
        if (sym.image) MapfileSerializer.writeKeyValues(writer, indent + 1, 'IMAGE', MapfileSerializer.quote(sym.image));
        if (sym.font) MapfileSerializer.writeKeyValues(writer, indent + 1, 'FONT', MapfileSerializer.quote(sym.font));
        if (sym.character) MapfileSerializer.writeKeyValues(writer, indent + 1, 'CHARACTER', MapfileSerializer.quote(sym.character));
        if (sym.points && sym.points.length > 0) {
            writer.lines.push('  '.repeat(indent + 1) + 'POINTS');
            let line = '  '.repeat(indent + 2);
            for (let i = 0; i < sym.points.length; i++) {
                if (i > 0) line += ' ';
                line += sym.points[i].toString();
            }
            writer.lines.push(line);
            writer.lines.push('  '.repeat(indent + 1) + 'END');
        }
        if (sym.gap !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'GAP', sym.gap.toString());
        if (sym.width !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'WIDTH', sym.width.toString());
        if (sym.lineWidth !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'LINEWIDTH', sym.lineWidth.toString());
        if (sym.size !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'SIZE', sym.size.toString());
        if (sym.angle) MapfileSerializer.writeKeyValues(writer, indent + 1, 'ANGLE', sym.angle);
        if (sym.pattern && sym.pattern.length > 0) {
            writer.lines.push('  '.repeat(indent + 1) + 'PATTERN');
            let line = '  '.repeat(indent + 2);
            for (let i = 0; i < sym.pattern.length; i++) {
                if (i > 0) line += ' ';
                line += sym.pattern[i].toString();
            }
            writer.lines.push(line);
            writer.lines.push('  '.repeat(indent + 1) + 'END');
        }
        if (sym.offsetX || sym.offsetY) {
            MapfileSerializer.writeKeyValues(writer, indent + 1, 'OFFSET', sym.offsetX || '0', sym.offsetY || '0');
        }
        if (sym.filled) MapfileSerializer.writeKeyValues(writer, indent + 1, 'FILLED', sym.filled);
        if (sym.antialias) MapfileSerializer.writeKeyValues(writer, indent + 1, 'ANTIALIAS', sym.antialias);
        if (sym.anchorPoint) MapfileSerializer.writeKeyValues(writer, indent + 1, 'ANCHORPOINT', sym.anchorPoint);
        MapfileSerializer.writeAttributes(writer, indent + 1, sym.attributes ?? {});
        writer.lines.push('  '.repeat(indent) + 'END');
    }

    private static writeLayer(writer: TextWriter, layer: Defs.LayerObj, indent: number): void {
        writer.lines.push('  '.repeat(indent) + 'LAYER');
        if (layer.name) MapfileSerializer.writeKeyValues(writer, indent + 1, 'NAME', MapfileSerializer.quote(layer.name));
        if (layer.type) MapfileSerializer.writeKeyValues(writer, indent + 1, 'TYPE', layer.type);
        if (layer.status) MapfileSerializer.writeKeyValues(writer, indent + 1, 'STATUS', layer.status);
        if (layer.data) MapfileSerializer.writeKeyValues(writer, indent + 1, 'DATA', MapfileSerializer.quote(layer.data));
        if (layer.connectionType) MapfileSerializer.writeKeyValues(writer, indent + 1, 'CONNECTIONTYPE', layer.connectionType);
        if (layer.connection) MapfileSerializer.writeKeyValues(writer, indent + 1, 'CONNECTION', MapfileSerializer.quote(layer.connection));

        if (layer.group) MapfileSerializer.writeKeyValues(writer, indent + 1, 'GROUP', MapfileSerializer.quote(layer.group));
        if (layer.classGroup) MapfileSerializer.writeKeyValues(writer, indent + 1, 'CLASSGROUP', MapfileSerializer.quote(layer.classGroup));
        if (layer.classItem) MapfileSerializer.writeKeyValues(writer, indent + 1, 'CLASSITEM', MapfileSerializer.quote(layer.classItem));
        if (layer.labelItem) MapfileSerializer.writeKeyValues(writer, indent + 1, 'LABELITEM', MapfileSerializer.quote(layer.labelItem));

        if (layer.minScaleDenom !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'MINSCALEDENOM', layer.minScaleDenom.toString());
        if (layer.maxScaleDenom !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'MAXSCALEDENOM', layer.maxScaleDenom.toString());
        if (layer.minScale !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'MINSCALE', layer.minScale.toString());
        if (layer.maxScale !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'MAXSCALE', layer.maxScale.toString());
        if (layer.minGeoWidth !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'MINGEOWIDTH', layer.minGeoWidth.toString());
        if (layer.maxGeoWidth !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'MAXGEOWIDTH', layer.maxGeoWidth.toString());
        if (layer.symbolScaleDenom !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'SYMBOLSCALEDENOM', layer.symbolScaleDenom.toString());

        if (layer.extent && layer.extent.length === 4) {
            MapfileSerializer.writeKeyValues(writer, indent + 1, 'EXTENT', ...layer.extent.map(d => d.toString()));
        }
        if (layer.units) MapfileSerializer.writeKeyValues(writer, indent + 1, 'UNITS', layer.units);

        if (layer.labelMinScaleDenom !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'LABELMINSCALEDENOM', layer.labelMinScaleDenom.toString());
        if (layer.labelMaxScaleDenom !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'LABELMAXSCALEDENOM', layer.labelMaxScaleDenom.toString());
        if (layer.labelRequires) MapfileSerializer.writeKeyValues(writer, indent + 1, 'LABELREQUIRES', MapfileSerializer.quote(layer.labelRequires));
        if (layer.labelCache) MapfileSerializer.writeKeyValues(writer, indent + 1, 'LABELCACHE', layer.labelCache);

        if (layer.debug) MapfileSerializer.writeKeyValues(writer, indent + 1, 'DEBUG', layer.debug);
        if (layer.encoding) MapfileSerializer.writeKeyValues(writer, indent + 1, 'ENCODING', MapfileSerializer.quote(layer.encoding));
        if (layer.filter) MapfileSerializer.writeKeyValues(writer, indent + 1, 'FILTER', MapfileSerializer.quote(layer.filter));
        if (layer.filterItem) MapfileSerializer.writeKeyValues(writer, indent + 1, 'FILTERITEM', MapfileSerializer.quote(layer.filterItem));
        if (layer.minFeatureSize !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'MINFEATURESIZE', layer.minFeatureSize.toString());
        if (layer.maxFeatures !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'MAXFEATURES', layer.maxFeatures.toString());
        if (layer.mask) MapfileSerializer.writeKeyValues(writer, indent + 1, 'MASK', MapfileSerializer.quote(layer.mask));
        if (layer.styleItem) MapfileSerializer.writeKeyValues(writer, indent + 1, 'STYLEITEM', layer.styleItem);
        if (layer.geomTransform) MapfileSerializer.writeKeyValues(writer, indent + 1, 'GEOMTRANSFORM', MapfileSerializer.quote(layer.geomTransform));
        if (layer.postLabelCache) MapfileSerializer.writeKeyValues(writer, indent + 1, 'POSTLABELCACHE', layer.postLabelCache);
        if (layer.requires) MapfileSerializer.writeKeyValues(writer, indent + 1, 'REQUIRES', MapfileSerializer.quote(layer.requires));
        if (layer.transform) MapfileSerializer.writeKeyValues(writer, indent + 1, 'TRANSFORM', layer.transform);
        if (layer.tolerance !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'TOLERANCE', layer.tolerance.toString());
        if (layer.toleranceUnits) MapfileSerializer.writeKeyValues(writer, indent + 1, 'TOLERANCEUNITS', layer.toleranceUnits);

        if (layer.header) MapfileSerializer.writeKeyValues(writer, indent + 1, 'HEADER', MapfileSerializer.quote(layer.header));
        if (layer.footer) MapfileSerializer.writeKeyValues(writer, indent + 1, 'FOOTER', MapfileSerializer.quote(layer.footer));
        if (layer.template) MapfileSerializer.writeKeyValues(writer, indent + 1, 'TEMPLATE', MapfileSerializer.quote(layer.template));

        if (layer.offsiteColor && layer.offsiteColor.length === 3) {
            MapfileSerializer.writeKeyValues(writer, indent + 1, 'OFFSITE', ...layer.offsiteColor.map(i => i.toString()));
        } else if (layer.offsiteHex) {
            MapfileSerializer.writeKeyValues(writer, indent + 1, 'OFFSITE', MapfileSerializer.quote(layer.offsiteHex));
        }

        if (layer.tileIndex) MapfileSerializer.writeKeyValues(writer, indent + 1, 'TILEINDEX', MapfileSerializer.quote(layer.tileIndex));
        if (layer.tileItem) MapfileSerializer.writeKeyValues(writer, indent + 1, 'TILEITEM', MapfileSerializer.quote(layer.tileItem));
        if (layer.tileFilter) MapfileSerializer.writeKeyValues(writer, indent + 1, 'TILEFILTER', MapfileSerializer.quote(layer.tileFilter));
        if (layer.tileFilterItem) MapfileSerializer.writeKeyValues(writer, indent + 1, 'TILEFILTERITEM', MapfileSerializer.quote(layer.tileFilterItem));

        for (const [key, val] of Object.entries(layer.connectionOptions)) {
            MapfileSerializer.writeKeyValues(writer, indent + 1, 'CONNECTIONOPTIONS', MapfileSerializer.quote(key), MapfileSerializer.quote(val));
        }
        for (const p of layer.processing) {
            MapfileSerializer.writeKeyValues(writer, indent + 1, 'PROCESSING', MapfileSerializer.quote(p));
        }

        if (Object.keys(layer.validation).length > 0) {
            writer.lines.push('  '.repeat(indent + 1) + 'VALIDATION');
            for (const [key, val] of Object.entries(layer.validation)) {
                writer.lines.push('  '.repeat(indent + 2) + `${MapfileSerializer.quote(key)} ${MapfileSerializer.quote(val)}`);
            }
            writer.lines.push('  '.repeat(indent + 1) + 'END');
        }

        if (Object.keys(layer.identify).length > 0) {
            writer.lines.push('  '.repeat(indent + 1) + 'IDENTIFY');
            MapfileSerializer.writeAttributes(writer, indent + 1, layer.identify);
            writer.lines.push('  '.repeat(indent + 1) + 'END');
        }

        for (const j of layer.joins) {
            MapfileSerializer.writeJoin(writer, j, indent + 1);
        }

        if (layer.composite) {
            MapfileSerializer.writeComposite(writer, layer.composite, indent + 1);
        }

        if (Object.keys(layer.metadata).length > 0) {
            writer.lines.push('  '.repeat(indent + 1) + 'METADATA');
            for (const [key, val] of Object.entries(layer.metadata)) {
                writer.lines.push('  '.repeat(indent + 2) + `${MapfileSerializer.quote(key)} ${MapfileSerializer.quote(val)}`);
            }
            writer.lines.push('  '.repeat(indent + 1) + 'END');
        }

        for (const f of layer.features) {
            MapfileSerializer.writeFeature(writer, f, indent + 1);
        }

        for (const symbol of layer.symbols) {
            MapfileSerializer.writeSymbol(writer, symbol, indent + 1);
        }

        for (const c of layer.classes) {
            MapfileSerializer.writeClass(writer, c, indent + 1);
        }

        if (layer.projection.length > 0) {
            writer.lines.push('  '.repeat(indent + 1) + 'PROJECTION');
            for (const l of layer.projection) {
                writer.lines.push('  '.repeat(indent + 2) + MapfileSerializer.quote(l));
            }
            writer.lines.push('  '.repeat(indent + 1) + 'END');
        }

        MapfileSerializer.writeAttributes(writer, indent + 1, layer.attributes);
        writer.lines.push('  '.repeat(indent) + 'END');
    }

    private static writeClass(writer: TextWriter, c: Defs.ClassObj, indent: number): void {
        writer.lines.push('  '.repeat(indent) + 'CLASS');
        if (c.name) MapfileSerializer.writeKeyValues(writer, indent + 1, 'NAME', MapfileSerializer.quote(c.name));
        if (c.title) MapfileSerializer.writeKeyValues(writer, indent + 1, 'TITLE', MapfileSerializer.quote(c.title));
        if (c.status) MapfileSerializer.writeKeyValues(writer, indent + 1, 'STATUS', c.status);
        if (c.group) MapfileSerializer.writeKeyValues(writer, indent + 1, 'GROUP', MapfileSerializer.quote(c.group));
        if (c.expression) MapfileSerializer.writeKeyValues(writer, indent + 1, 'EXPRESSION', MapfileSerializer.quote(c.expression));
        if (c.text) MapfileSerializer.writeKeyValues(writer, indent + 1, 'TEXT', MapfileSerializer.quote(c.text));
        if (c.template) MapfileSerializer.writeKeyValues(writer, indent + 1, 'TEMPLATE', MapfileSerializer.quote(c.template));
        if (c.keyImage) MapfileSerializer.writeKeyValues(writer, indent + 1, 'KEYIMAGE', MapfileSerializer.quote(c.keyImage));
        if (c.minScaleDenom !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'MINSCALEDENOM', c.minScaleDenom.toString());
        if (c.maxScaleDenom !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'MAXSCALEDENOM', c.maxScaleDenom.toString());
        if (c.minFeatureSize !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'MINFEATURESIZE', c.minFeatureSize.toString());
        if (c.debug) MapfileSerializer.writeKeyValues(writer, indent + 1, 'DEBUG', c.debug);
        if (c.fallback !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'FALLBACK', c.fallback ? 'TRUE' : 'FALSE');

        if (Object.keys(c.metadata).length > 0) {
            writer.lines.push('  '.repeat(indent + 1) + 'METADATA');
            for (const [key, val] of Object.entries(c.metadata)) {
                writer.lines.push('  '.repeat(indent + 2) + `${MapfileSerializer.quote(key)} ${MapfileSerializer.quote(val)}`);
            }
            writer.lines.push('  '.repeat(indent + 1) + 'END');
        }
        if (Object.keys(c.validation).length > 0) {
            writer.lines.push('  '.repeat(indent + 1) + 'VALIDATION');
            for (const [key, val] of Object.entries(c.validation)) {
                writer.lines.push('  '.repeat(indent + 2) + `${MapfileSerializer.quote(key)} ${MapfileSerializer.quote(val)}`);
            }
            writer.lines.push('  '.repeat(indent + 1) + 'END');
        }

        for (const s of c.styles) {
            MapfileSerializer.writeStyle(writer, s, indent + 1);
        }
        for (const l of c.labels) {
            MapfileSerializer.writeLabel(writer, l, indent + 1);
        }
        if (c.leader) {
            MapfileSerializer.writeLeader(writer, c.leader, indent + 1);
        }

        MapfileSerializer.writeAttributes(writer, indent + 1, c.attributes);
        writer.lines.push('  '.repeat(indent) + 'END');
    }

    private static writeStyle(writer: TextWriter, s: Defs.StyleObj, indent: number): void {
        writer.lines.push('  '.repeat(indent) + 'STYLE');

        if (s.color && s.color.length === 3) {
            MapfileSerializer.writeKeyValues(writer, indent + 1, 'COLOR', ...s.color.map(i => i.toString()));
        } else if (s.colorHex) {
            MapfileSerializer.writeKeyValues(writer, indent + 1, 'COLOR', MapfileSerializer.quote(s.colorHex));
        } else if (s.colorAttr) {
            MapfileSerializer.writeKeyValues(writer, indent + 1, 'COLOR', s.colorAttr);
        }

        if (s.outlineColor && s.outlineColor.length === 3) {
            MapfileSerializer.writeKeyValues(writer, indent + 1, 'OUTLINECOLOR', ...s.outlineColor.map(i => i.toString()));
        } else if (s.outlineColorHex) {
            MapfileSerializer.writeKeyValues(writer, indent + 1, 'OUTLINECOLOR', MapfileSerializer.quote(s.outlineColorHex));
        } else if (s.outlineColorAttr) {
            MapfileSerializer.writeKeyValues(writer, indent + 1, 'OUTLINECOLOR', s.outlineColorAttr);
        }
        if (s.outlineWidth !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'OUTLINEWIDTH', s.outlineWidth.toString());

        if (s.symbol) MapfileSerializer.writeKeyValues(writer, indent + 1, 'SYMBOL', MapfileSerializer.maybeQuote(s.symbol));
        if (s.angle) MapfileSerializer.writeKeyValues(writer, indent + 1, 'ANGLE', s.angle);
        if (s.width !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'WIDTH', s.width.toString());
        if (s.size !== undefined) {
            MapfileSerializer.writeKeyValues(writer, indent + 1, 'SIZE', s.size.toString());
        } else if (s.sizeExpr) {
            MapfileSerializer.writeKeyValues(writer, indent + 1, 'SIZE', s.sizeExpr);
        }

        if (s.minScaleDenom !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'MINSCALEDENOM', s.minScaleDenom.toString());
        if (s.maxScaleDenom !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'MAXSCALEDENOM', s.maxScaleDenom.toString());
        if (s.minWidth !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'MINWIDTH', s.minWidth.toString());
        if (s.maxWidth !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'MAXWIDTH', s.maxWidth.toString());
        if (s.minSize !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'MINSIZE', s.minSize.toString());
        if (s.maxSize !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'MAXSIZE', s.maxSize.toString());

        if (s.gap !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'GAP', s.gap.toString());
        if (s.initialGap !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'INITIALGAP', s.initialGap.toString());

        if (s.offsetX || s.offsetY) {
            MapfileSerializer.writeKeyValues(writer, indent + 1, 'OFFSET', s.offsetX || '0', s.offsetY || '0');
        }
        if (s.polarOffsetR || s.polarOffsetA) {
            MapfileSerializer.writeKeyValues(writer, indent + 1, 'POLAROFFSET', s.polarOffsetR || '0', s.polarOffsetA || '0');
        }

        if (s.lineCap) MapfileSerializer.writeKeyValues(writer, indent + 1, 'LINECAP', s.lineCap);
        if (s.lineJoin) MapfileSerializer.writeKeyValues(writer, indent + 1, 'LINEJOIN', s.lineJoin);
        if (s.lineJoinMaxSize !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'LINEJOINMAXSIZE', s.lineJoinMaxSize.toString());

        if (s.pattern && s.pattern.length > 0) {
            writer.lines.push('  '.repeat(indent + 1) + 'PATTERN');
            let line = '  '.repeat(indent + 2);
            for (let i = 0; i < s.pattern.length; i++) {
                if (i > 0) line += ' ';
                line += s.pattern[i].toString();
            }
            writer.lines.push(line);
            writer.lines.push('  '.repeat(indent + 1) + 'END');
        }

        if (s.geomTransform) MapfileSerializer.writeKeyValues(writer, indent + 1, 'GEOMTRANSFORM', MapfileSerializer.quote(s.geomTransform));
        if (s.opacity) MapfileSerializer.writeKeyValues(writer, indent + 1, 'OPACITY', s.opacity);

        if (s.rangeItem) MapfileSerializer.writeKeyValues(writer, indent + 1, 'RANGEITEM', MapfileSerializer.quote(s.rangeItem));
        if ((s.colorRangeLow?.rgb || s.colorRangeLow?.hex) && (s.colorRangeHigh?.rgb || s.colorRangeHigh?.hex)) {
            const low = s.colorRangeLow?.rgb
                ? s.colorRangeLow.rgb.join(' ')
                : MapfileSerializer.quote(s.colorRangeLow?.hex || '');
            const high = s.colorRangeHigh?.rgb
                ? s.colorRangeHigh.rgb.join(' ')
                : MapfileSerializer.quote(s.colorRangeHigh?.hex || '');
            MapfileSerializer.writeKeyValues(writer, indent + 1, 'COLORRANGE', low, high);
        }
        if (s.dataRange?.low !== undefined && s.dataRange?.high !== undefined) {
            MapfileSerializer.writeKeyValues(writer, indent + 1, 'DATARANGE', s.dataRange.low.toString(), s.dataRange.high.toString());
        }

        MapfileSerializer.writeAttributes(writer, indent + 1, s.attributes);
        writer.lines.push('  '.repeat(indent) + 'END');
    }

    private static writeLabel(writer: TextWriter, l: Defs.LabelObj, indent: number): void {
        writer.lines.push('  '.repeat(indent) + 'LABEL');
        if (l.align) MapfileSerializer.writeKeyValues(writer, indent + 1, 'ALIGN', l.align);
        if (l.angle) MapfileSerializer.writeKeyValues(writer, indent + 1, 'ANGLE', l.angle);
        if (l.buffer !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'BUFFER', l.buffer.toString());

        if (l.color && l.color.length === 3) {
            MapfileSerializer.writeKeyValues(writer, indent + 1, 'COLOR', ...l.color.map(i => i.toString()));
        } else if (l.colorHex) {
            MapfileSerializer.writeKeyValues(writer, indent + 1, 'COLOR', MapfileSerializer.quote(l.colorHex));
        } else if (l.colorAttr) {
            MapfileSerializer.writeKeyValues(writer, indent + 1, 'COLOR', l.colorAttr);
        }

        if (l.outlineColor && l.outlineColor.length === 3) {
            MapfileSerializer.writeKeyValues(writer, indent + 1, 'OUTLINECOLOR', ...l.outlineColor.map(i => i.toString()));
        } else if (l.outlineColorHex) {
            MapfileSerializer.writeKeyValues(writer, indent + 1, 'OUTLINECOLOR', MapfileSerializer.quote(l.outlineColorHex));
        } else if (l.outlineColorAttr) {
            MapfileSerializer.writeKeyValues(writer, indent + 1, 'OUTLINECOLOR', l.outlineColorAttr);
        }
        if (l.outlineWidth !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'OUTLINEWIDTH', l.outlineWidth.toString());

        if (l.font) MapfileSerializer.writeKeyValues(writer, indent + 1, 'FONT', MapfileSerializer.maybeQuote(l.font));
        if (l.type) MapfileSerializer.writeKeyValues(writer, indent + 1, 'TYPE', l.type);
        if (l.size) MapfileSerializer.writeKeyValues(writer, indent + 1, 'SIZE', l.size);

        if (l.maxOverlapAngle !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'MAXOVERLAPANGLE', l.maxOverlapAngle.toString());
        if (l.minScaleDenom !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'MINSCALEDENOM', l.minScaleDenom.toString());
        if (l.maxScaleDenom !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'MAXSCALEDENOM', l.maxScaleDenom.toString());
        if (l.minDistance !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'MINDISTANCE', l.minDistance.toString());
        if (l.minFeatureSize) MapfileSerializer.writeKeyValues(writer, indent + 1, 'MINFEATURESIZE', l.minFeatureSize);
        if (l.minSize !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'MINSIZE', l.minSize.toString());
        if (l.maxSize !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'MAXSIZE', l.maxSize.toString());

        if (l.offsetX || l.offsetY) {
            MapfileSerializer.writeKeyValues(writer, indent + 1, 'OFFSET', l.offsetX || '0', l.offsetY || '0');
        }
        if (l.position) MapfileSerializer.writeKeyValues(writer, indent + 1, 'POSITION', l.position);
        if (l.priority) MapfileSerializer.writeKeyValues(writer, indent + 1, 'PRIORITY', l.priority);
        if (l.partials) MapfileSerializer.writeKeyValues(writer, indent + 1, 'PARTIALS', l.partials);
        if (l.force) MapfileSerializer.writeKeyValues(writer, indent + 1, 'FORCE', l.force);

        if (l.repeatDistance !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'REPEATDISTANCE', l.repeatDistance.toString());
        if (l.shadowColor && l.shadowColor.length === 3) {
            MapfileSerializer.writeKeyValues(writer, indent + 1, 'SHADOWCOLOR', ...l.shadowColor.map(i => i.toString()));
        } else if (l.shadowColorHex) {
            MapfileSerializer.writeKeyValues(writer, indent + 1, 'SHADOWCOLOR', MapfileSerializer.quote(l.shadowColorHex));
        }
        if (l.shadowSizeX || l.shadowSizeY) {
            MapfileSerializer.writeKeyValues(writer, indent + 1, 'SHADOWSIZE', l.shadowSizeX || '0', l.shadowSizeY || '0');
        }

        if (l.text) MapfileSerializer.writeKeyValues(writer, indent + 1, 'TEXT', MapfileSerializer.quote(l.text));
        if (l.wrap) MapfileSerializer.writeKeyValues(writer, indent + 1, 'WRAP', MapfileSerializer.quote(l.wrap));
        if (l.maxLength !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'MAXLENGTH', l.maxLength.toString());

        for (const s of l.styles) {
            MapfileSerializer.writeStyle(writer, s, indent + 1);
        }
        MapfileSerializer.writeAttributes(writer, indent + 1, l.attributes);
        writer.lines.push('  '.repeat(indent) + 'END');
    }

    private static writeLeader(writer: TextWriter, l: Defs.LeaderObj, indent: number): void {
        writer.lines.push('  '.repeat(indent) + 'LEADER');
        if (l.gridStep !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'GRIDSTEP', l.gridStep.toString());
        if (l.maxDistance !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'MAXDISTANCE', l.maxDistance.toString());
        for (const s of l.styles) {
            MapfileSerializer.writeStyle(writer, s, indent + 1);
        }
        MapfileSerializer.writeAttributes(writer, indent + 1, l.attributes);
        writer.lines.push('  '.repeat(indent) + 'END');
    }

    private static writeJoin(writer: TextWriter, j: Defs.JoinObj, indent: number): void {
        writer.lines.push('  '.repeat(indent) + 'JOIN');
        if (j.name) MapfileSerializer.writeKeyValues(writer, indent + 1, 'NAME', MapfileSerializer.quote(j.name));
        if (j.table) MapfileSerializer.writeKeyValues(writer, indent + 1, 'TABLE', MapfileSerializer.quote(j.table));
        if (j.from) MapfileSerializer.writeKeyValues(writer, indent + 1, 'FROM', MapfileSerializer.quote(j.from));
        if (j.to) MapfileSerializer.writeKeyValues(writer, indent + 1, 'TO', MapfileSerializer.quote(j.to));
        if (j.type) MapfileSerializer.writeKeyValues(writer, indent + 1, 'TYPE', j.type);
        if (j.template) MapfileSerializer.writeKeyValues(writer, indent + 1, 'TEMPLATE', MapfileSerializer.quote(j.template));
        MapfileSerializer.writeAttributes(writer, indent + 1, j.attributes);
        writer.lines.push('  '.repeat(indent) + 'END');
    }

    private static writeComposite(writer: TextWriter, comp: Defs.CompositeObj, indent: number): void {
        writer.lines.push('  '.repeat(indent) + 'COMPOSITE');
        if (comp.opacity !== undefined) MapfileSerializer.writeKeyValues(writer, indent + 1, 'OPACITY', comp.opacity.toString());
        if (comp.compOp) MapfileSerializer.writeKeyValues(writer, indent + 1, 'COMPOP', comp.compOp);
        if (comp.pattern && comp.pattern.length > 0) {
            writer.lines.push('  '.repeat(indent + 1) + 'PATTERN');
            let line = '  '.repeat(indent + 2);
            for (let i = 0; i < comp.pattern.length; i++) {
                if (i > 0) line += ' ';
                line += comp.pattern[i].toString();
            }
            writer.lines.push(line);
            writer.lines.push('  '.repeat(indent + 1) + 'END');
        }
        MapfileSerializer.writeAttributes(writer, indent + 1, comp.attributes ?? {});
        writer.lines.push('  '.repeat(indent) + 'END');
    }

    private static writeFeature(writer: TextWriter, f: Defs.FeatureObj, indent: number): void {
        writer.lines.push('  '.repeat(indent) + 'FEATURE');
        for (const line of f.innerLines) {
            writer.lines.push(line);
        }
        writer.lines.push('  '.repeat(indent) + 'END');
    }
}

// ============================================================================
// INDIVIDUAL COMPONENT PARSER
// ============================================================================

export class MapfileComponentParser {
    /**
     * Parse a STYLE block from a string
     * @param text Mapfile text containing STYLE block
     */
    public static parseStyle(text: string): Defs.StyleObj {
        const style: Defs.StyleObj = { pattern: [], attributes: {} };
        const lines = text.split(/\r?\n/);
        let inStyle = false;

        for (const line of lines) {
            const trimmed = MapfileParser['stripComments'](line).trim();
            if (!trimmed) continue;

            const tokens = MapfileParser['tokenize'](trimmed);
            if (tokens.length === 0) continue;

            if (!inStyle && tokens[0].toUpperCase() === 'STYLE') {
                inStyle = true;
                continue;
            }

            if (inStyle) {
                if (tokens[0].toUpperCase() === 'END') break;
                MapfileParser['applyKeyValuesToStyle'](style, tokens);
            }
        }

        return style;
    }

    /**
     * Parse a SYMBOL block from a string
     * @param text Mapfile text containing SYMBOL block
     */
    public static parseSymbol(text: string): Defs.SymbolObj {
        const symbol: Defs.SymbolObj = { pattern: [], attributes: {} };
        const lines = text.split(/\r?\n/);
        let inSymbol = false;

        for (const line of lines) {
            const trimmed = MapfileParser['stripComments'](line).trim();
            if (!trimmed) continue;

            const tokens = MapfileParser['tokenize'](trimmed);
            if (tokens.length === 0) continue;

            if (!inSymbol && tokens[0].toUpperCase() === 'SYMBOL') {
                inSymbol = true;
                continue;
            }

            if (inSymbol) {
                if (tokens[0].toUpperCase() === 'END') break;
                MapfileParser['applyKeyValuesToSymbol'](symbol, tokens);
            }
        }

        return symbol;
    }

    /**
     * Parse a CLASS block from a string
     * @param text Mapfile text containing CLASS block
     */
    public static parseClass(text: string): Defs.ClassObj {
        const cls: Defs.ClassObj = { styles: [], labels: [], metadata: {}, validation: {}, attributes: {} };
        const lines = text.split(/\r?\n/);
        let inClass = false;
        const stack: string[] = [];

        for (const line of lines) {
            const trimmed = MapfileParser['stripComments'](line).trim();
            if (!trimmed) continue;

            const tokens = MapfileParser['tokenize'](trimmed);
            if (tokens.length === 0) continue;

            if (!inClass && tokens[0].toUpperCase() === 'CLASS') {
                inClass = true;
                continue;
            }

            if (inClass) {
                const head = tokens[0].toUpperCase();
                if (head === 'STYLE') {
                    const style: Defs.StyleObj = { pattern: [], attributes: {} };
                    cls.styles.push(style);
                    stack.push('STYLE');
                    continue;
                }
                if (head === 'LABEL') {
                    const label: Defs.LabelObj = { styles: [], attributes: {} };
                    cls.labels.push(label);
                    stack.push('LABEL');
                    continue;
                }
                if (head === 'LEADER') {
                    cls.leader = { styles: [], attributes: {} };
                    stack.push('LEADER');
                    continue;
                }
                if (head === 'END') {
                    if (stack.length > 0) {
                        stack.pop();
                    } else {
                        break;
                    }
                    continue;
                }
                if (stack.length === 0) {
                    MapfileParser['applyKeyValuesToClass'](cls, tokens);
                }
            }
        }

        return cls;
    }

    /**
     * Parse a LAYER block from a string
     * @param text Mapfile text containing LAYER block
     */
    public static parseLayer(text: string): Defs.LayerObj {
        const layer: Defs.LayerObj = {
            projection: [],
            symbols: [],
            metadata: {},
            classes: [],
            joins: [],
            processing: [],
            validation: {},
            connectionOptions: {},
            identify: {},
            features: [],
            attributes: {}
        };
        const lines = text.split(/\r?\n/);
        let inLayer = false;

        for (const line of lines) {
            const trimmed = MapfileParser['stripComments'](line).trim();
            if (!trimmed) continue;

            const tokens = MapfileParser['tokenize'](trimmed);
            if (tokens.length === 0) continue;

            if (!inLayer && tokens[0].toUpperCase() === 'LAYER') {
                inLayer = true;
                continue;
            }

            if (inLayer) {
                if (tokens[0].toUpperCase() === 'END') break;
                MapfileParser['applyKeyValuesToLayer'](layer, tokens);
            }
        }

        return layer;
    }

    /**
     * Parse a LABEL block from a string
     * @param text Mapfile text containing LABEL block
     */
    public static parseLabel(text: string): Defs.LabelObj {
        const label: Defs.LabelObj = { styles: [], attributes: {} };
        const lines = text.split(/\r?\n/);
        let inLabel = false;

        for (const line of lines) {
            const trimmed = MapfileParser['stripComments'](line).trim();
            if (!trimmed) continue;

            const tokens = MapfileParser['tokenize'](trimmed);
            if (tokens.length === 0) continue;

            if (!inLabel && tokens[0].toUpperCase() === 'LABEL') {
                inLabel = true;
                continue;
            }

            if (inLabel) {
                if (tokens[0].toUpperCase() === 'END') break;
                MapfileParser['applyKeyValuesToLabel'](label, tokens);
            }
        }

        return label;
    }

    /**
     * Parse a LEADER block from a string
     * @param text Mapfile text containing LEADER block
     */
    public static parseLeader(text: string): Defs.LeaderObj {
        const leader: Defs.LeaderObj = { styles: [], attributes: {} };
        const lines = text.split(/\r?\n/);
        let inLeader = false;

        for (const line of lines) {
            const trimmed = MapfileParser['stripComments'](line).trim();
            if (!trimmed) continue;

            const tokens = MapfileParser['tokenize'](trimmed);
            if (tokens.length === 0) continue;

            if (!inLeader && tokens[0].toUpperCase() === 'LEADER') {
                inLeader = true;
                continue;
            }

            if (inLeader) {
                if (tokens[0].toUpperCase() === 'END') break;
                MapfileParser['applyKeyValuesToLeader'](leader, tokens);
            }
        }

        return leader;
    }

    /**
     * Parse an OUTPUTFORMAT block from a string
     * @param text Mapfile text containing OUTPUTFORMAT block
     */
    public static parseOutputFormat(text: string): Defs.OutputFormatObj {
        const of: Defs.OutputFormatObj = { attributes: {} };
        const lines = text.split(/\r?\n/);
        let inOf = false;

        for (const line of lines) {
            const trimmed = MapfileParser['stripComments'](line).trim();
            if (!trimmed) continue;

            const tokens = MapfileParser['tokenize'](trimmed);
            if (tokens.length === 0) continue;

            if (!inOf && tokens[0].toUpperCase() === 'OUTPUTFORMAT') {
                inOf = true;
                continue;
            }

            if (inOf) {
                if (tokens[0].toUpperCase() === 'END') break;
                MapfileParser['applyKeyValuesToOutputFormat'](of, tokens);
            }
        }

        return of;
    }
}

// ============================================================================
// INDIVIDUAL COMPONENT SERIALIZER
// ============================================================================

export class MapfileComponentSerializer {
    /**
     * Serialize a STYLE object to mapfile string
     */
    public static styleToString(style: Defs.StyleObj): string {
        const lines: string[] = [];
        MapfileSerializer['writeStyle'](
            { lines } as TextWriter,
            style,
            0
        );
        return lines.join('\n');
    }

    /**
     * Serialize a SYMBOL object to mapfile string
     */
    public static symbolToString(symbol: Defs.SymbolObj): string {
        const lines: string[] = [];
        MapfileSerializer['writeSymbol'](
            { lines } as TextWriter,
            symbol,
            0
        );
        return lines.join('\n');
    }

    /**
     * Serialize a CLASS object to mapfile string
     */
    public static classToString(cls: Defs.ClassObj): string {
        const lines: string[] = [];
        MapfileSerializer['writeClass'](
            { lines } as TextWriter,
            cls,
            0
        );
        return lines.join('\n');
    }

    /**
     * Serialize a LAYER object to mapfile string
     */
    public static layerToString(layer: Defs.LayerObj): string {
        const lines: string[] = [];
        MapfileSerializer['writeLayer'](
            { lines } as TextWriter,
            layer,
            0
        );
        return lines.join('\n');
    }

    /**
     * Serialize a LABEL object to mapfile string
     */
    public static labelToString(label: Defs.LabelObj): string {
        const lines: string[] = [];
        MapfileSerializer['writeLabel'](
            { lines } as TextWriter,
            label,
            0
        );
        return lines.join('\n');
    }

    /**
     * Serialize a LEADER object to mapfile string
     */
    public static leaderToString(leader: Defs.LeaderObj): string {
        const lines: string[] = [];
        MapfileSerializer['writeLeader'](
            { lines } as TextWriter,
            leader,
            0
        );
        return lines.join('\n');
    }

    /**
     * Serialize an OUTPUTFORMAT object to mapfile string
     */
    public static outputFormatToString(of: Defs.OutputFormatObj): string {
        const lines: string[] = [];
        MapfileSerializer['writeOutputFormat'](
            { lines } as TextWriter,
            of,
            0
        );
        return lines.join('\n');
    }

    /**
     * Serialize a WEB object to mapfile string
     */
    public static webToString(web: Defs.WebObj): string {
        const lines: string[] = [];
        MapfileSerializer['writeWeb'](
            { lines } as TextWriter,
            web,
            0
        );
        return lines.join('\n');
    }

    /**
     * Serialize a JOIN object to mapfile string
     */
    public static joinToString(join: Defs.JoinObj): string {
        const lines: string[] = [];
        MapfileSerializer['writeJoin'](
            { lines } as TextWriter,
            join,
            0
        );
        return lines.join('\n');
    }

    /**
     * Serialize a COMPOSITE object to mapfile string
     */
    public static compositeToString(composite: Defs.CompositeObj): string {
        const lines: string[] = [];
        MapfileSerializer['writeComposite'](
            { lines } as TextWriter,
            composite,
            0
        );
        return lines.join('\n');
    }

    /**
     * Serialize a FEATURE object to mapfile string
     */
    public static featureToString(feature: Defs.FeatureObj): string {
        const lines: string[] = [];
        MapfileSerializer['writeFeature'](
            { lines } as TextWriter,
            feature,
            0
        );
        return lines.join('\n');
    }
}

// Helper type for text output
interface TextWriter {
    lines: string[];
}
