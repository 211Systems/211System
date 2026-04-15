const currentPath = window.location.pathname.toLowerCase();
const departmentId = window.location.pathname.split('/').filter(Boolean).pop();
const token = localStorage.getItem('jwt');

let apiEndpoints = {};
let moduleConfig = {};

if (currentPath.includes('/medic/')) {
    apiEndpoints = {
        department: '/api/Medical/hospitals',
        personnel: '/api/Medical/paramedics',
        vehicles: '/api/Medical/ambulances'
    };
    moduleConfig = {
        roleKey: "rank",
        foreignKeyDept: "hospitalId",
        licenseLabel: "licenseNumber",
        vehicleTypeLabel: "type",
        allowedRolesAdmin: ["Admin", "Kierownik Szpitala"],
        allowedRolesWorker: ["Lekarz", "Medyk"],
        rolesToAssign: ["Kierownik Szpitala", "Lekarz", "Medyk"],
        workerRoleToAdd: "Medyk",
        hasRegion: false
    };
}
else if (currentPath.includes('/police/')) {
    apiEndpoints = {
        department: '/api/Police/departments',
        personnel: '/api/Police/policemen',
        vehicles: '/api/Police/cars'
    };
    moduleConfig = {
        roleKey: "rank",
        foreignKeyDept: "pDepartmentId",
        licenseLabel: "badgeNumber",
        vehicleTypeLabel: "licensePlate",
        allowedRolesAdmin: ["Admin", "Inspektor", "Komendant"],
        allowedRolesWorker: ["Policjant"],
        rolesToAssign: ["Komendant", "Policjant"],
        workerRoleToAdd: "Policjant",
        hasRegion: true 
    };
}
else if (currentPath.includes('/fire/')) {
    apiEndpoints = {
        department: '/api/Fire/departments',
        personnel: '/api/Fire/firemen',
        vehicles: '/api/Fire/firetrucks'
    };
    moduleConfig = {
        roleKey: "rank",
        foreignKeyDept: "fDepartmentId",
        licenseLabel: "badgeNumber",
        vehicleTypeLabel: "licensePlate",
        allowedRolesAdmin: ["Admin", "Naczelnik", "Kapitan"],
        allowedRolesWorker: ["Strazak"],
        rolesToAssign: ["Kapitan", "Strazak"],
        workerRoleToAdd: "Strazak",
        hasRegion: true 
    };
}

function parseJwt(token) {
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        return JSON.parse(window.atob(base64));
    } catch (e) { return null; }
}

let currentUserRoles = [];
let currentUserEmail = "";

if (token) {
    const decodedToken = parseJwt(token);
    if (decodedToken) {
        let roleClaim = decodedToken["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || decodedToken.role;
        currentUserRoles = Array.isArray(roleClaim) ? roleClaim : [roleClaim];
        currentUserEmail = decodedToken["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"] || decodedToken.email || "";
    }
}

const isAdmin = currentUserRoles.includes("Admin") || moduleConfig.allowedRolesAdmin.some(r => currentUserRoles.includes(r));
const isWorker = moduleConfig.allowedRolesWorker.some(r => currentUserRoles.includes(r));
let isMyDepartment = false;

document.addEventListener("DOMContentLoaded", async function () {
    if (!apiEndpoints.department) {
        console.error("Unrecognized module path.");
        return;
    }

    if (currentUserRoles.includes("Admin") || currentUserRoles.includes("Inspektor") || currentUserRoles.includes("Naczelnik")) {
        const actionBox = document.getElementById('admin-department-actions');
        if (actionBox) actionBox.classList.remove('d-none');
    }

    const roleSelect = document.getElementById('staffRole');
    if (roleSelect) {
        roleSelect.innerHTML = '';
        if (isAdmin) {
            moduleConfig.rolesToAssign.forEach(r => {
                roleSelect.innerHTML += `<option value="${r}">${r}</option>`;
            });
        } else {
            roleSelect.innerHTML = `<option value="${moduleConfig.workerRoleToAdd}">${moduleConfig.workerRoleToAdd}</option>`;
        }
    }

    await loadDepartmentDetails();
    await loadPersonnel();
    await loadVehicles();
});

async function loadDepartmentDetails() {
    try {
        const res = await fetch(apiEndpoints.department, { headers: { 'Authorization': 'Bearer ' + token } });
        if (res.ok) {
            const depts = await res.json();
            const dept = depts.find(d => (d.id || d.Id) === departmentId || (d.pDepartmentId || d.PDepartmentId) === departmentId || (d.fDepartmentId || d.FDepartmentId) === departmentId);

            if (dept) {
                document.getElementById('dept-name').textContent = dept.name || dept.Name;
                document.getElementById('dept-address').textContent = dept.address || dept.Address;

                if (moduleConfig.hasRegion) {
                    document.getElementById('dept-region-container').classList.remove('d-none');
                    document.getElementById('dept-region').textContent = dept.region || dept.Region || "Brak danych";
                }
            }
        }
    } catch (e) { console.error("Error loading department", e); }
}

async function loadPersonnel() {
    const tableBody = document.getElementById('personnel-table-body');
    if (!tableBody) return;

    try {
        const res = await fetch(apiEndpoints.personnel, { headers: { 'Authorization': 'Bearer ' + token } });
        if (res.ok) {
            const allStaff = await res.json();

            const myRecord = allStaff.find(s => s.email === currentUserEmail || s.Email === currentUserEmail);
            if (myRecord) {
                const myDeptId = myRecord[moduleConfig.foreignKeyDept] || myRecord[moduleConfig.foreignKeyDept.charAt(0).toUpperCase() + moduleConfig.foreignKeyDept.slice(1)];
                if (myDeptId === departmentId) isMyDepartment = true;
            }

            const btnAdd = document.getElementById('btn-open-personnel-modal');
            if (btnAdd && (isAdmin || (isWorker && isMyDepartment))) {
                btnAdd.classList.remove('d-none');
            }
            const deptStaff = allStaff.filter(s => {
                const sId = s[moduleConfig.foreignKeyDept] || s[moduleConfig.foreignKeyDept.charAt(0).toUpperCase() + moduleConfig.foreignKeyDept.slice(1)];
                return sId === departmentId;
            });

            tableBody.innerHTML = '';
            if (deptStaff.length === 0) {
                tableBody.innerHTML = '<tr><td colspan="6" class="text-center">Brak przypisanego personelu.</td></tr>';
                return;
            }

            deptStaff.forEach(p => {
                const pId = p.id || p.Id;
                const fName = p.name || p.Name;
                const lName = p.lastName || p.LastName;
                const rank = p[moduleConfig.roleKey] || p[moduleConfig.roleKey.charAt(0).toUpperCase() + moduleConfig.roleKey.slice(1)] || "Pracownik";
                const license = p[moduleConfig.licenseLabel] || p[moduleConfig.licenseLabel.charAt(0).toUpperCase() + moduleConfig.licenseLabel.slice(1)];

                const avatarUrl = p.avatarUrl || p.AvatarUrl || `https://ui-avatars.com/api/?name=${fName}+${lName}&background=random&color=fff`;

                let actionBtns = '';
                if (isAdmin || (isWorker && isMyDepartment && rank === moduleConfig.workerRoleToAdd)) {
                    actionBtns = `<button onclick="deletePersonnel('${pId}')" class="btn btn-sm btn-danger"><i class="fas fa-trash"></i></button>`;
                }

                tableBody.insertAdjacentHTML('beforeend', `
                    <tr>
                        <td class="text-center align-middle"><img src="${avatarUrl}" class="img-circle elevation-1" style="width: 40px; height: 40px; object-fit: cover;"></td>
                        <td class="align-middle"><b>${fName} ${lName}</b></td>
                        <td class="align-middle">${license}</td>
                        <td class="align-middle"><span class="badge badge-info">${rank}</span></td>
                        <td class="align-middle">${p.email || p.Email}</td>
                        <td class="align-middle text-right">${actionBtns}</td>
                    </tr>
                `);
            });
        }
    } catch (e) { console.error("Error loading personnel", e); }
}

async function loadVehicles() {
    const tableBody = document.getElementById('vehicles-table-body') || document.getElementById('ambulances-table-body') || document.getElementById('police-cars-table-body');
    if (!tableBody) return;

    try {
        const res = await fetch(apiEndpoints.vehicles, { headers: { 'Authorization': 'Bearer ' + token } });
        if (res.ok) {
            const allVehicles = await res.json();

            const deptVehicles = allVehicles.filter(v => {
                const vDeptId = v[moduleConfig.foreignKeyDept] || v[moduleConfig.foreignKeyDept.charAt(0).toUpperCase() + moduleConfig.foreignKeyDept.slice(1)];
                return vDeptId === departmentId;
            });

            tableBody.innerHTML = '';
            if (deptVehicles.length === 0) {
                tableBody.innerHTML = '<tr><td colspan="6" class="text-center text-muted">Brak przypisanych pojazdów do tej placówki.</td></tr>';
                return;
            }

            deptVehicles.forEach((v, index) => {
                const vId = v.id || v.Id;
                const plate = v.licensePlate || v.LicensePlate || "Brak Tablicy";

                const typeVal = v[moduleConfig.vehicleTypeLabel] || v[moduleConfig.vehicleTypeLabel.charAt(0).toUpperCase() + moduleConfig.vehicleTypeLabel.slice(1)] || "Standard";

                const isAvailable = v.isAvailable !== undefined ? v.isAvailable : (v.IsAvailable !== undefined ? v.IsAvailable : true);
                const statusBadge = isAvailable
                    ? '<span class="badge badge-success">Dostępny</span>'
                    : '<span class="badge badge-danger">W Akcji / Zajęty</span>';

                let actionBtns = '';
                if (isAdmin || (isWorker && isMyDepartment)) {
                    actionBtns = `<button onclick="deleteVehicle('${vId}')" class="btn btn-sm btn-danger" title="Usuń Pojazd"><i class="fas fa-trash"></i></button>`;
                }

                const row = `<tr>
                    <td>${index + 1}</td>
                    <td><b>${plate}</b></td>
                    <td><span class="badge badge-secondary">${typeVal}</span></td>
                    <td><i class="fas fa-building text-muted"></i> Przypisany</td>
                    <td>${statusBadge}</td>
                    <td class="text-right" style="white-space: nowrap;">${actionBtns}</td>
                </tr>`;
                tableBody.insertAdjacentHTML('beforeend', row);
            });
        } else {
            tableBody.innerHTML = '<tr><td colspan="6" class="text-center text-warning">Brak endpointu API dla pojazdów. Dodaj go w C#!</td></tr>';
        }
    } catch (e) {
        console.error("Error loading vehicles", e);
        tableBody.innerHTML = '<tr><td colspan="6" class="text-center text-danger">Błąd połączenia.</td></tr>';
    }
}

window.deleteVehicle = async function (id) {
    if (!confirm("Czy na pewno chcesz wyrejestrować ten pojazd?")) return;
    try {
        const res = await fetch(`${apiEndpoints.vehicles}/${id}`, {
            method: 'DELETE',
            headers: { 'Authorization': 'Bearer ' + token }
        });
        if (res.ok) await loadVehicles();
        else alert("Błąd usuwania pojazdu. Może być przypisany do akcji.");
    } catch (e) { alert("Błąd sieci."); }
};

document.getElementById('addPersonnelForm')?.addEventListener('submit', async function (e) {
    e.preventDefault();
    const btnSubmit = document.getElementById('btn-submit-staff');
    const errorMsg = document.getElementById('staff-error');
    const successAlert = document.getElementById('generated-password-alert');

    btnSubmit.disabled = true;
    errorMsg.classList.add('d-none');

    const payload = {
        name: document.getElementById('staffName').value,
        lastName: document.getElementById('staffLastName').value,
        email: document.getElementById('staffEmail').value,
        rank: document.getElementById('staffRole').value,
        paraAccountId: "Auto-Generated"
    };

    payload[moduleConfig.licenseLabel] = document.getElementById('staffLicense').value;
    payload[moduleConfig.foreignKeyDept] = departmentId;

    try {
        const res = await fetch(apiEndpoints.personnel, {
            method: 'POST',
            headers: { 'Authorization': 'Bearer ' + token, 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (res.ok) {
            const data = await res.json();
            document.getElementById('temp-password-display').textContent = data.temporaryPassword || data.password || "Temp!234";
            successAlert.classList.remove('d-none');
            btnSubmit.classList.add('d-none');
            await loadPersonnel();
        } else {
            const err = await res.json();
            errorMsg.textContent = err.message || "Błąd rejestracji.";
            errorMsg.classList.remove('d-none');
            btnSubmit.disabled = false;
        }
    } catch (e) {
        errorMsg.textContent = "Błąd sieci.";
        errorMsg.classList.remove('d-none');
        btnSubmit.disabled = false;
    }
});

window.deletePersonnel = async function (id) {
    if (!confirm("Czy na pewno chcesz zwolnić tego pracownika?")) return;
    try {
        const res = await fetch(`${apiEndpoints.personnel}/${id}`, { method: 'DELETE', headers: { 'Authorization': 'Bearer ' + token } });
        if (res.ok) await loadPersonnel();
        else alert("Błąd usuwania.");
    } catch (e) { alert("Błąd sieci."); }
};