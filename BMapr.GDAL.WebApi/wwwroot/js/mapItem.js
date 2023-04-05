import Map from 'ol/Map';
import OSM from 'ol/source/OSM';
import TileLayer from 'ol/layer/Tile';
import TileWMS from 'ol/source/TileWMS';
import View from 'ol/View';
import Projection from 'ol/proj/Projection';
import proj4 from 'proj4';
import { register } from 'ol/proj/proj4';
import { UrlService } from './services/urlService';
export class MapItem {
    constructor(htmlId, config, project) {
        this.config = config;
        this.mapMd = config.maps[project]; // todo support for sdeveral maps
        proj4.defs('EPSG:21781', '+proj=somerc +lat_0=46.95240555555556 +lon_0=7.439583333333333 +k_0=1 ' +
            '+x_0=600000 +y_0=200000 +ellps=bessel ' +
            '+towgs84=660.077,13.551,369.344,2.484,1.783,2.939,5.66 +units=m +no_defs');
        register(proj4);
        proj4.defs('EPSG:3857', '+title=GoogleMercator +proj=merc +a=6378137 +b=6378137 +lat_ts=0.0 +lon_0=0.0 +x_0=0.0 +y_0=0 +k=1.0 +units=m +nadgrids=@null +no_defs');
        register(proj4);
        const projection = new Projection({
            code: 'EPSG:21781',
            extent: [485869.5728, 76443.1884, 837076.5648, 299941.7864],
            units: 'm'
        });
        const projectionGM = new Projection({
            code: 'EPSG:3857',
            extent: [947482, 6001487, 947763, 6001647],
            units: 'm'
        });
        const extent = [420000, 30000, 900000, 350000];
        this.map = new Map({
            layers: [
                new TileLayer()
            ],
            target: htmlId,
            view: new View({
                center: [0, 0],
                zoom: 2,
            }),
        });
        const osmLayer = new TileLayer({
            source: new OSM(),
        });
        this.map.addLayer(osmLayer);
        const onlineRessource = this.mapMd.web.metadata['wms_onlineresource'];
        this.mapMd.layers.forEach(layerItem => {
            const layer = new TileLayer({
                source: new TileWMS({
                    url: onlineRessource,
                    params: { 'LAYERS': layerItem.name, 'TILED': true },
                    serverType: 'mapserver',
                    transition: 0,
                    projection: 'EPSG:21781',
                }),
            });
            this.map.addLayer(layer);
        });
        this.map.on('moveend', this.onMoveEnd);
        this.takeParameterFromUrl();
    }
    takeParameterFromUrl() {
        const urlService = new UrlService(document.location.href);
        const cX = parseFloat(urlService.getParameter('cX'));
        const cY = parseFloat(urlService.getParameter('cY'));
        const resolution = parseFloat(urlService.getParameter('zoom'));
        if (!cX || !cY || !resolution) {
            console.info('no parameters taken from url');
            return;
        }
        console.info('Parameters set from url');
        this.map.getView().setCenter([cX, cY]);
        this.map.getView().setResolution(resolution);
    }
    onMoveEnd(evt) {
        const map = evt.map;
        const view = map.getView();
        const extent = view.calculateExtent(map.getSize());
        const resolution = view.getResolution();
        const center = view.getCenter();
        const url = new UrlService(document.location.href);
        url.setParameter('cX', center[0].toString(), true);
        url.setParameter('cY', center[1].toString(), true);
        url.setParameter('zoom', resolution.toString(), true);
    }
    registerProjection(code, definition) {
        proj4.defs(code, definition);
        register(proj4);
    }
}
//# sourceMappingURL=mapItem.js.map