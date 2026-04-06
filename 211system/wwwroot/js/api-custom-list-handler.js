window.parseJwt = function (token) {
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        return JSON.parse(window.atob(base64));
    } catch (e) { return null; }
};

async function setupCustomList(config) {
    document.addEventListener("DOMContentLoaded", async function () {
        const tableBody = document.getElementById(config.tableBodyId);
        if (!tableBody) return;

        const token = localStorage.getItem('jwt');
        if (!token) return;

        try {
            const data = await config.loadData(token);

            if (data === null) return;

            tableBody.innerHTML = '';

            if (data.length === 0) {
                tableBody.innerHTML = `<tr><td colspan="${config.colspan || 5}" class="text-center">${config.emptyMessage || 'Brak danych.'}</td></tr>`;
                return;
            }

            data.forEach((item, index) => {
                tableBody.insertAdjacentHTML('beforeend', config.renderRow(item, index));
            });

        } catch (error) {
            tableBody.innerHTML = `<tr><td colspan="${config.colspan || 5}" class="text-danger text-center">Błąd pobierania danych.</td></tr>`;
            console.error(error);
        }
    });
}