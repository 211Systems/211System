window.parseJwt = window.parseJwt || function (token) {
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        return JSON.parse(window.atob(base64));
    } catch (e) { return null; }
};

window.setupDepartmentDetails = async function (config) {
    const token = window.jwtToken || localStorage.getItem('jwt');
    if (!token) return;

    const deptId = config.departmentId;
    let decodedToken = window.parseJwt(token);

    if (typeof config.onInit === 'function') {
        await config.onInit(decodedToken);
    }

    await loadDepartment();
    await loadStaff();

    async function loadDepartment() {
        try {
            const response = await fetch(config.endpoints.department, {
                headers: { 'Authorization': 'Bearer ' + token }
            });
            if (response.ok) {
                const depts = await response.json();
                const myDept = depts.find(d => d.id === deptId || d.Id === deptId);
                if (myDept) config.renderDepartment(myDept);
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

                const myStaff = allStaff.filter(p => {
                    const keys = Object.keys(p);
                    const matchingKey = keys.find(k => k.toLowerCase() === config.foreignKeyField.toLowerCase());
                    return matchingKey && p[matchingKey] === deptId;
                });

                tableBody.innerHTML = '';
                if (myStaff.length === 0) {
                    tableBody.innerHTML = `<tr><td colspan="${config.emptyStaffColspan || 5}" class="text-center font-weight-bold">Brak przypisanego personelu.</td></tr>`;
                    return;
                }

                myStaff.forEach(p => {
                    tableBody.insertAdjacentHTML('beforeend', config.renderStaffRow(p, decodedToken, allStaff));
                });
            }
        } catch (error) { console.error("Błąd pobierania personelu:", error); }
    }

    const addStaffForm = document.getElementById('addStaffForm');
    if (addStaffForm && config.endpoints.addStaff && config.buildStaffPayload) {
        addStaffForm.addEventListener('submit', async function (e) {
            e.preventDefault();
            const btnSubmit = document.getElementById('btn-submit-staff');
            const successAlert = document.getElementById('generated-password-alert');
            
            if (btnSubmit) btnSubmit.disabled = true;

            const payload = config.buildStaffPayload();

            try {
                const response = await fetch(config.endpoints.addStaff, {
                    method: 'POST',
                    headers: { 
                        'Authorization': 'Bearer ' + token,
                        'Content-Type': 'application/json' 
                    },
                    body: JSON.stringify(payload)
                });

                if (response.ok) {
                    const data = await response.json();
                    const generatedAuthToken = data.temporaryPassword || data.password || "Auto-Generowane!";
                    
                    const passDisplay = document.getElementById('temp-password-display');
                    if (passDisplay) passDisplay.textContent = generatedAuthToken;

                    if (successAlert) successAlert.classList.remove('d-none');
                    if (btnSubmit) btnSubmit.classList.add('d-none');
                    
                    await loadStaff();
                } else {
                    const errorData = await response.json().catch(() => ({}));
                    const errorMessage = errorData.message || "Błąd rejestracji - sprawdź poprawność danych.";
                    alert("Błąd: " + errorMessage);
                    
                    if (btnSubmit) btnSubmit.disabled = false;
                }
            } catch (error) {
                alert("Błąd sieci: " + error.message);
                if (btnSubmit) btnSubmit.disabled = false;
            }
        });
    }
};