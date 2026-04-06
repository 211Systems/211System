function setupApiList(config) {
    document.addEventListener("DOMContentLoaded", async function () {
        const tableBody = document.getElementById(config.tableBodyId);
        if (!tableBody) return;

        const token = localStorage.getItem('jwt');

        try {
            const response = await fetch(config.apiUrl, {
                method: 'GET',
                headers: {
                    'Authorization': 'Bearer ' + token,
                    'Content-Type': 'application/json'
                }
            });

            if (response.ok) {
                const data = await response.json();
                tableBody.innerHTML = '';

                if (data.length === 0) {
                    tableBody.innerHTML = `<tr><td colspan="${config.colspan || 5}" class="text-center">${config.emptyMessage || 'Brak danych.'}</td></tr>`;
                    return;
                }

                data.forEach((item, index) => {
                    tableBody.insertAdjacentHTML('beforeend', config.renderRow(item, index));
                });
            } else {
                tableBody.innerHTML = `<tr><td colspan="${config.colspan || 5}" class="text-danger text-center">Błąd autoryzacji lub serwera.</td></tr>`;
            }
        } catch (error) {
            tableBody.innerHTML = `<tr><td colspan="${config.colspan || 5}" class="text-danger text-center">Błąd połączenia z API.</td></tr>`;
            console.error(error);
        }
    });
}