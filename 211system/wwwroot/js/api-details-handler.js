// Plik: wwwroot/js/api-details-handler.js
window.parseJwt = window.parseJwt || function (token) {
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        return JSON.parse(window.atob(base64));
    } catch (e) { return null; }
};

function setupDepartmentDetails(config) {
    const token = localStorage.getItem('jwt');
    const deptId = config.departmentId;
    let decodedToken = token ? window.parseJwt(token) : null;

    document.addEventListener("DOMContentLoaded", async function () {
        if (typeof config.onInit === 'function') {
            config.onInit(decodedToken);
        }

        await loadDepartment();
        await loadStaff();
    });

    async function loadDepartment() {
        try {
            const response = await fetch(config.endpoints.department, {
                headers: { 'Authorization': 'Bearer ' + token }
            });
            if (response.ok) {
                const depts = await response.json();
                const myDept = depts.find(d => d.id === deptId || d.Id === deptId);

                if (myDept) {
                    config.renderDepartment(myDept);
                } else {
                    document.getElementById('dept-name').textContent = "Nie znaleziono danych";
                }
            }
        } catch (error) { console.error("Błąd pobierania danych jednostki:", error); }
    }

    async function loadStaff() {
        const tableBody = document.getElementById('staff-table-body');
        if (!tableBody) return;

        try {
            const response = await fetch(config.endpoints.staff, {
                headers: { 'Authorization': 'Bearer ' + token }
            });
            if (response.ok) {
                const allStaff = await response.json();

                const fk = config.foreignKeyField;
                const fkPascal = fk.charAt(0).toUpperCase() + fk.slice(1);

                const myStaff = allStaff.filter(p => p[fk] === deptId || p[fkPascal] === deptId);

                tableBody.innerHTML = '';
                if (myStaff.length === 0) {
                    tableBody.innerHTML = `<tr><td colspan="${config.emptyStaffColspan || 5}" class="text-center">Brak przypisanego personelu.</td></tr>`;
                    return;
                }

                myStaff.forEach(p => {
                    tableBody.insertAdjacentHTML('beforeend', config.renderStaffRow(p, decodedToken, allStaff));
                });
            }
        } catch (error) { console.error("Błąd pobierania personelu:", error); }
    }

    const addStaffForm = document.getElementById(config.addStaffFormId || 'addStaffForm');
    if (addStaffForm) {
        addStaffForm.addEventListener('submit', async function (e) {
            e.preventDefault();

            const btnSubmit = document.getElementById('btn-submit-staff');
            const errorMsg = document.getElementById('staff-error');
            const successAlert = document.getElementById('generated-password-alert');

            btnSubmit.disabled = true;
            errorMsg.classList.add('d-none');

            const requestData = config.buildStaffPayload();

            try {
                const response = await fetch(config.endpoints.addStaff, {
                    method: 'POST',
                    headers: {
                        'Authorization': 'Bearer ' + token,
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(requestData)
                });

                if (response.ok) {
                    const result = await response.json();

                    const tempPass = result.temporaryPassword || result.password || requestData.password || "Brak (skontaktuj się z adminem)";
                    document.getElementById('temp-password-display').textContent = tempPass;

                    successAlert.classList.remove('d-none');
                    btnSubmit.classList.add('d-none');

                    await loadStaff();
                } else {
                    const errorData = await response.json();
                    if (errorData.errors) {
                        errorMsg.innerHTML = Object.values(errorData.errors).join("<br/>");
                    } else {
                        errorMsg.textContent = errorData.message || "Błąd walidacji lub serwera.";
                    }
                    errorMsg.classList.remove('d-none');
                    btnSubmit.disabled = false;
                }
            } catch (error) {
                errorMsg.textContent = "Błąd połączenia z serwerem.";
                errorMsg.classList.remove('d-none');
                btnSubmit.disabled = false;
            }
        });
    }
}