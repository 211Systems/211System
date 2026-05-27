window.parseJwt = function (token) {
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        return JSON.parse(window.atob(base64));
    } catch (e) { return null; }
};

window.setupCustomList = async function (config, tokenOverride) {
    const tableBody = document.getElementById(config.tableBodyId);
    if (!tableBody) return;

    const token = tokenOverride || localStorage.getItem('jwt');
    if (!token) {
        tableBody.innerHTML = `<tr><td colspan="${config.colspan || 5}" class="text-danger text-center font-weight-bold">Brak tokenu — zaloguj się ponownie.</td></tr>`;
        return;
    }

    try {
        const data = await config.loadData(token);

        if (data === null) return;

        tableBody.innerHTML = '';

        if (data.length === 0) {
            tableBody.innerHTML = `<tr><td colspan="${config.colspan || 5}" class="text-center font-weight-bold">${config.emptyMessage || 'Brak danych.'}</td></tr>`;
            return;
        }

        data.forEach((item, index) => {
            tableBody.insertAdjacentHTML('beforeend', config.renderRow(item, index));
        });

    } catch (error) {
        tableBody.innerHTML = `<tr><td colspan="${config.colspan || 5}" class="text-danger text-center font-weight-bold">Błąd pobierania danych.</td></tr>`;
        console.error("Błąd w setupCustomList:", error);
    }
};  