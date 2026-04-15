const currentPath = globalThis.location.pathname.toLowerCase();
const departmentId = globalThis.location.pathname.split('/').filter(Boolean).pop();
const token = localStorage.getItem('jwt');

let apiEndpoints = {};
let moduleConfig = {};

if (currentPath.includes('/medic/')) {
    apiEndpoints = { department: '/api/Medical/hospitals', personnel: '/api/Medical/paramedics' };
    moduleConfig = { roleKey: "rank", foreignKeyDept: "hospitalId", licenseLabel: "licenseNumber", lastNameKey: "lastName", accountIdKey: "paraAccountId", badgeColor: "badge-success", hierarchy: { "Admin": 100, "Kierownik Szpitala": 3, "Lekarz": 2, "Medyk": 1 }, hasRegion: false };
} else if (currentPath.includes('/police/')) {
    apiEndpoints = { department: '/api/Police/departments', personnel: '/api/Police/policemen' };
    moduleConfig = { roleKey: "rank", foreignKeyDept: "pDepartmentId", licenseLabel: "badgeNumber", lastNameKey: "lastname", accountIdKey: "policeAccountId", badgeColor: "badge-primary", hierarchy: { "Admin": 100, "Inspektor": 3, "Komendant": 2, "Policjant": 1 }, hasRegion: true };
} else if (currentPath.includes('/fire/')) {
    apiEndpoints = { department: '/api/Fire/departments', personnel: '/api/Fire/firemen' };
    moduleConfig = { roleKey: "rank", foreignKeyDept: "fDepartmentId", licenseLabel: "badgeNumber", lastNameKey: "lastname", accountIdKey: "fireAccountId", badgeColor: "badge-danger", hierarchy: { "Admin": 100, "Naczelnik": 3, "Kapitan": 2, "Strazak": 1 }, hasRegion: true };
}

function parseJwt(tokenString) {
    if (!tokenString || typeof tokenString !== 'string') {
        return null;
    }

    const parts = tokenString.split('.');
    if (parts.length !== 3) {
        return null;
    }

    try {
        const base64Url = parts[1];
        const base64 = base64Url.replaceAll('-', '+').replaceAll('_', '/');

        const jsonPayload = decodeURIComponent(
            globalThis.atob(base64).split('').map(function (c) {
                return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
            }).join('')
        );

        return JSON.parse(jsonPayload);
    } catch (e) {
        console.error("JWT Parsing Error:", e.message);
        return null;
    }
}

let currentUserRoles = [];
let currentUserEmail = "";
let myRankValue = 0;

if (token) {
    const decodedToken = parseJwt(token);
    if (decodedToken) {
        let roleClaim = decodedToken["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || decodedToken.role;
        currentUserRoles = Array.isArray(roleClaim) ? roleClaim : [roleClaim];
        currentUserEmail = decodedToken["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"] || decodedToken.email || "";
        currentUserRoles.forEach(r => { if (moduleConfig.hierarchy[r] && moduleConfig.hierarchy[r] > myRankValue) myRankValue = moduleConfig.hierarchy[r]; });
    }
}

document.addEventListener("DOMContentLoaded", async function () {
    if (!apiEndpoints.department) return;

    if (myRankValue >= 3) {
        const actionBox = document.getElementById('admin-department-actions');
        if (actionBox) actionBox.classList.remove('d-none');
        const editBtn = document.querySelector('[data-target="#editDepartmentModal"]');
        if (editBtn) editBtn.classList.remove('d-none');
    }

    const roleSelect = document.getElementById('staffRole');
    const editRoleSelect = document.getElementById('editStaffRole');
    const btnAddModal = document.querySelector('[data-target="#addPersonnelModal"]');

    const assignableRoles = Object.keys(moduleConfig.hierarchy).filter(role => role !== "Admin" && moduleConfig.hierarchy[role] < myRankValue);

    if (assignableRoles.length > 0) {
        if (btnAddModal) btnAddModal.classList.remove('d-none');
        if (roleSelect && editRoleSelect) {
            roleSelect.innerHTML = ''; editRoleSelect.innerHTML = '';
            assignableRoles.forEach(r => {
                const opt = `<option value="${r}">${r}</option>`;
                roleSelect.innerHTML += opt;
                editRoleSelect.innerHTML += opt;
            });
        }
    } else if (btnAddModal) {
        btnAddModal.classList.add('d-none');
    }

    await loadDepartmentDetails();
    await loadPersonnel();
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

                document.getElementById('editDeptName').value = dept.name || dept.Name;
                document.getElementById('editDeptAddress').value = dept.address || dept.Address;

                if (moduleConfig.hasRegion) {
                    document.getElementById('dept-region-container').classList.remove('d-none');
                    const region = dept.region || dept.Region || dept.district || dept.District || "";
                    document.getElementById('dept-region').textContent = region || "Brak danych";
                    document.getElementById('editDeptRegion').value = region;
                } else {
                    document.getElementById('editDeptRegionContainer').classList.add('d-none');
                }
            }
        }
    } catch (e) {
        console.error(e);
    }
}

async function loadPersonnel() {
    const tableBody = document.getElementById('personnel-table-body');
    if (!tableBody) return;

    try {
        const res = await fetch(apiEndpoints.personnel, { headers: { 'Authorization': 'Bearer ' + token } });
        if (res.ok) {
            const allStaff = await res.json();
            const deptStaff = allStaff.filter(s => {
                const sId = s[moduleConfig.foreignKeyDept] || s[moduleConfig.foreignKeyDept.charAt(0).toUpperCase() + moduleConfig.foreignKeyDept.slice(1)];
                return sId === departmentId;
            });

            tableBody.innerHTML = '';
            if (deptStaff.length === 0) {
                tableBody.innerHTML = '<tr><td colspan="5" class="text-center">Brak przypisanego personelu.</td></tr>';
                return;
            }

            deptStaff.forEach(p => {
                const pId = p.id || p.Id;
                const fName = p.name || p.Name || "";
                const lName = p.lastName || p.LastName || p.lastname || p.Lastname || "";
                const rank = p[moduleConfig.roleKey] || p[moduleConfig.roleKey.charAt(0).toUpperCase() + moduleConfig.roleKey.slice(1)] || "Pracownik";
                const license = p[moduleConfig.licenseLabel] || p[moduleConfig.licenseLabel.charAt(0).toUpperCase() + moduleConfig.licenseLabel.slice(1)] || "";

                const avatarUrl = p.avatarUrl || p.AvatarUrl || `https://ui-avatars.com/api/?name=${fName}+${lName}&background=random&color=fff`;
                const targetRankValue = moduleConfig.hierarchy[rank] || 0;

                let actionBtns = '';
                if (myRankValue > targetRankValue) {
                    actionBtns = `
                        <button onclick="globalThis.openEditPersonnelModal('${pId}', '${fName}', '${lName}', '${license}', '${rank}')" class="btn btn-sm btn-warning text-dark mr-1" title="Edytuj"><i class="fas fa-edit"></i></button>
                        <button onclick="globalThis.deletePersonnel('${pId}')" class="btn btn-sm btn-danger" title="Zwolnij"><i class="fas fa-trash"></i></button>
                    `;
                }

                tableBody.insertAdjacentHTML('beforeend', `
                    <tr>
                        <td class="text-center align-middle"><img src="${avatarUrl}" class="img-circle elevation-1" style="width: 40px; height: 40px; object-fit: cover;"></td>
                        <td class="align-middle"><b>${fName} ${lName}</b></td>
                        <td class="align-middle">${license}</td>
                        <td class="align-middle"><span class="badge ${moduleConfig.badgeColor} text-white" style="font-size: 0.9em; padding: 6px;">${rank}</span></td>
                        <td class="align-middle text-right">${actionBtns}</td>
                    </tr>
                `);
            });
        }
    } catch (e) {
        console.error(e);
    }
}

document.getElementById('editDepartmentForm')?.addEventListener('submit', async function (e) {
    e.preventDefault();
    const payload = {
        name: document.getElementById('editDeptName').value,
        address: document.getElementById('editDeptAddress').value,
        district: document.getElementById('editDeptRegion').value || ""
    };

    try {
        const res = await fetch(`${apiEndpoints.department}/${departmentId}`, {
            method: 'PUT',
            headers: { 'Authorization': 'Bearer ' + token, 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        if (res.ok) {
            $('#editDepartmentModal').modal('hide');
            await loadDepartmentDetails();
        } else {
            alert("Błąd podczas zapisywania zmian.");
        }
    } catch (e) {
        alert("Błąd sieci." + e.message);
    }
});

globalThis.openEditPersonnelModal = function (id, fname, lname, license, rank) {
    document.getElementById('editStaffId').value = id;
    document.getElementById('editStaffName').value = fname;
    document.getElementById('editStaffLastName').value = lname;
    document.getElementById('editStaffLicense').value = license;
    document.getElementById('editStaffRole').value = rank;
    $('#editPersonnelModal').modal('show');
};

document.getElementById('editPersonnelForm')?.addEventListener('submit', async function (e) {
    e.preventDefault();
    const id = document.getElementById('editStaffId').value;

    const payload = {
        name: document.getElementById('editStaffName').value,
        rank: document.getElementById('editStaffRole').value
    };
    payload[moduleConfig.lastNameKey] = document.getElementById('editStaffLastName').value;
    payload[moduleConfig.licenseLabel] = document.getElementById('editStaffLicense').value;

    try {
        const res = await fetch(`${apiEndpoints.personnel}/${id}`, {
            method: 'PUT',
            headers: { 'Authorization': 'Bearer ' + token, 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        if (res.ok) {
            $('#editPersonnelModal').modal('hide');
            await loadPersonnel();
        } else {
            alert("Błąd podczas edycji pracownika.");
        }
    } catch (e) {
        alert("Błąd sieci.");
    }
});

document.getElementById('addPersonnelForm')?.addEventListener('submit', async function (e) {
    e.preventDefault();
    const btnSubmit = document.getElementById('btn-submit-staff');
    const successAlert = document.getElementById('generated-password-alert');
    btnSubmit.disabled = true;

    const payload = {
        name: document.getElementById('staffName').value,
        email: document.getElementById('staffEmail').value,
        rank: document.getElementById('staffRole').value
    };
    payload[moduleConfig.lastNameKey] = document.getElementById('staffLastName').value;
    payload[moduleConfig.licenseLabel] = document.getElementById('staffLicense').value;
    payload[moduleConfig.foreignKeyDept] = departmentId;
    payload[moduleConfig.accountIdKey] = "Auto-Generated";

    try {
        const res = await fetch(apiEndpoints.personnel, {
            method: 'POST',
            headers: { 'Authorization': 'Bearer ' + token, 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        if (res.ok) {
            const data = await res.json();

            const fallbackUiText = "Auto-Generowane!";
            const generatedAuthToken = data.temporaryPassword || data.password;
            document.getElementById('temp-password-display').textContent = generatedAuthToken ? generatedAuthToken : fallbackUiText;

            successAlert.classList.remove('d-none');
            btnSubmit.classList.add('d-none');
            await loadPersonnel();
        } else {
            alert("Błąd rejestracji.");
            btnSubmit.disabled = false;
        }
    } catch (e) {
        alert("Błąd sieci.");
        btnSubmit.disabled = false;
    }
});

globalThis.deletePersonnel = async function (id) {
    if (!confirm("Czy na pewno chcesz zwolnić tego pracownika? Zostanie on usunięty z systemu.")) return;
    try {
        const res = await fetch(`${apiEndpoints.personnel}/${id}`, { method: 'DELETE', headers: { 'Authorization': 'Bearer ' + token } });
        if (res.ok) await loadPersonnel();
        else alert("Błąd usuwania.");
    } catch (e) {
        alert("Błąd sieci.");
    }
};