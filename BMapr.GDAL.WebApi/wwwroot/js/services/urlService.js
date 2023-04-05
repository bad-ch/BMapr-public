export class UrlService {
    constructor(url) {
        if (!url || url === null || url === '') {
            console.error('wrong url');
            return;
        }
        this.Url = new URL(url);
    }
    setParameter(key, value, updateUrl = false) {
        let params = this.Url.searchParams;
        params.set(key, value);
        if (updateUrl) {
            this.updateUrl();
        }
    }
    getParameter(key) {
        let params = this.Url.searchParams;
        return params.get(key);
    }
    updateUrl() {
        window.history.replaceState('', '', this.Url.toString());
    }
}
//# sourceMappingURL=urlService.js.map