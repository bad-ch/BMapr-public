import * as log from 'loglevel';
export class ConfigService {
    constructor(host, projects, token) {
        this.projects = projects;
        this.host = host;
        this.token = token;
        this.config = {
            maps: {},
            crsDefinitions: {},
            legends: {}
        };
    }
    async loadConfig() {
        const requests = new Array();
        this.projects.forEach((project) => {
            requests.push(this.getDataByType(`${this.host}/api/project/getmapdata/${project}?token=${this.token}`, 'map', project));
            requests.push(this.getDataByType(`${this.host}/api/project/getlegend/${project}?token=${this.token}`, 'legend', project));
        });
        await Promise.all(requests).then(responsesWithType => {
            responsesWithType.forEach(item => {
                if (!item) {
                    return;
                }
                this.assembleConfig(item);
            });
        });
        const requestsPost = new Array();
        for (const key in this.config.maps) {
            const map = this.config.maps[key];
            if (map.web.metadata['wms_srs']) {
                const parts = map.web.metadata['wms_srs'].split(' '); // todo wfs_srs
                for (const index in parts) {
                    if (parts[index].startsWith('EPSG:')) {
                        const epsg = parseInt(parts[index].replace('EPSG:', ''));
                        requestsPost.push(this.getDataByType(`${this.host}/api/spatial/getproj4js/${epsg}`, 'crs', 'SYS'));
                    }
                }
            }
        }
        await Promise.all(requestsPost).then(responsesWithType => {
            responsesWithType.forEach(item => {
                if (!item) {
                    return;
                }
                this.assembleConfig(item);
            });
        });
    }
    assembleConfig(response) {
        if (response.type === 'map') {
            const mapObj = response.content;
            this.config.maps[response.project] = mapObj;
        }
        else if (response.type === 'crs') {
            const crsDefinition = response.content;
            if (!this.config.crsDefinitions[crsDefinition.epsg]) {
                this.config.crsDefinitions[crsDefinition.epsg] = crsDefinition;
            }
        }
        else if (response.type === 'legend') {
            const legend = response.content;
            this.config.legends[response.project] = legend;
        }
        else {
            log.error(`Unkown type ${response.type}, content: ${JSON.stringify(response.content)}`);
        }
    }
    getDataByType(url, type, project) {
        return fetch(url)
            .then(response => response.json())
            .then(data => {
            return { type, project: project, content: data };
        });
    }
}
//# sourceMappingURL=configService.js.map