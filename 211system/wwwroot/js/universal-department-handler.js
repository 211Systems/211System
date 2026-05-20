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
    if (!tokenString || typeof tokenString !== 'string') return null;
    const parts = tokenString.split('.');
    if (parts.length !== 3) return null;

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
        
        const hierarchyKeys = Object.keys(moduleConfig.hierarchy);
        
        currentUserRoles.forEach(r => { 
            if (!r) return;
            const match = hierarchyKeys.find(k => k.toLowerCase() === r.toLowerCase());
            if (match && moduleConfig.hierarchy[match] > myRankValue) {
                myRankValue = moduleConfig.hierarchy[match]; 
            }
        });
        if (currentUserRoles.some(r => r && r.toLowerCase() === "admin")) {
            myRankValue = 100;
        }
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
    const btnAddModal = document.getElementById('btn-open-personnel-modal');

    let assignableRoles = [];
    if (myRankValue >= 2) {
        assignableRoles = Object.keys(moduleConfig.hierarchy).filter(role => role !== "Admin" && moduleConfig.hierarchy[role] <= myRankValue);
    }

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
                    const regionContainer = document.getElementById('editDeptRegionContainer');
                    if (regionContainer) regionContainer.classList.add('d-none');
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
                tableBody.innerHTML = '<tr><td colspan="6" class="text-center">Brak przypisanego personelu.</td></tr>';
                return;
            }

            deptStaff.forEach(p => {
                const pId = p.id || p.Id;
                const fName = p.name || p.Name || "";
                const lName = p.lastName || p.LastName || p.lastname || p.Lastname || "";
                const rank = p[moduleConfig.roleKey] || p[moduleConfig.roleKey.charAt(0).toUpperCase() + moduleConfig.roleKey.slice(1)] || "Pracownik";
                const license = p[moduleConfig.licenseLabel] || p[moduleConfig.licenseLabel.charAt(0).toUpperCase() + moduleConfig.licenseLabel.slice(1)] || "";
                const email = p.email || p.Email || "";

                const avatarUrl = p.avatarUrl || p.AvatarUrl || `https://ui-avatars.com/api/?name=${fName}+${lName}&background=random&color=fff`;
                const targetRankValue = moduleConfig.hierarchy[rank] || 0;

                let actionBtns = '';
                if (myRankValue > targetRankValue || myRankValue === 100) {
                    actionBtns = `
                        <button onclick="globalThis.manageAccountLock('${email}')" class="btn btn-sm btn-dark mr-1" title="Zarządzaj dostępem (Zablokuj/Odblokuj)"><i class="fas fa-user-lock"></i></button>
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
                        <td class="align-middle">
                            ${email}<br/>
                            <span id="lock-status-${pId}"><span class="badge bg-secondary"><i class="fas fa-spinner fa-spin"></i> Sprawdzanie...</span></span>
                        </td>
                        <td class="align-middle text-right" style="white-space: nowrap;">${actionBtns}</td>
                    </tr>
                `);

                if(email && email !== "Brak Emaila" && email !== "Brak") {
                    setTimeout(async () => {
                        try {
                            const statusRes = await fetch(`/api/Auth/status/${email}`, { headers: { 'Authorization': 'Bearer ' + token } });
                            if (statusRes.ok) {
                                const isLocked = await statusRes.json();
                                const badgeEl = document.getElementById(`lock-status-${pId}`);
                                if(badgeEl) {
                                    badgeEl.innerHTML = isLocked 
                                        ? '<span class="badge bg-danger"><i class="fas fa-lock"></i> Zablokowane</span>'
                                        : '<span class="badge bg-success"><i class="fas fa-lock-open"></i> Aktywne</span>';
                                }
                            }
                        } catch (e) {}
                    }, 50);
                } else {
                    const badgeEl = document.getElementById(`lock-status-${pId}`);
                    if(badgeEl) badgeEl.innerHTML = '<span class="badge bg-dark">Brak konta</span>';
                }
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
        district: document.getElementById('editDeptRegion') ? document.getElementById('editDeptRegion').value : ""
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
    if (btnSubmit) btnSubmit.disabled = true;

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
            
            const passDisplay = document.getElementById('temp-password-display');
            if (passDisplay) passDisplay.textContent = generatedAuthToken ? generatedAuthToken : fallbackUiText;

            if (successAlert) successAlert.classList.remove('d-none');
            if (btnSubmit) btnSubmit.classList.add('d-none');
            await loadPersonnel();
        } else {
            const errorData = await res.json().catch(() => ({}));
            const errorMessage = errorData.message || "Błąd rejestracji – sprawdź poprawność danych (email, imię, nazwisko).";
            alert("Błąd: " + errorMessage);
            
            if (btnSubmit) btnSubmit.disabled = false;
        }
    } catch (e) {
        alert("Błąd sieci.");
        if (btnSubmit) btnSubmit.disabled = false;
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

globalThis.manageAccountLock = async function(email) {
    if (!email || email === "Brak Emaila" || email === "Brak") {
        alert("Brak przypisanego adresu email. Skontaktuj się z administratorem.");
        return;
    }

    try {
        const statusRes = await fetch(`/api/Auth/status/${email}`, { headers: { 'Authorization': 'Bearer ' + token } });
        if (!statusRes.ok) throw new Error("Błąd pobierania statusu.");
        const isLocked = await statusRes.json();

        if (isLocked) {
            if(confirm(`UWAGA: Konto funkcjonariusza ${email} jest obecnie ZABLOKOWANE.\n\nCzy chcesz je ODBLOKOWAĆ i wygenerować nowe hasło dostępowe?`)) {
                const unlockRes = await fetch(`/api/Auth/unlock/${email}`, { method: 'POST', headers: { 'Authorization': 'Bearer ' + token } });
                
                if(unlockRes.ok) {
                    const data = await unlockRes.json();
                    document.getElementById('new-password-display').innerText = data.newPassword;
                    $('#unlockedAccountModal').modal('show');
                    $('#unlockedAccountModal').on('hidden.bs.modal', function () {
                        window.location.reload();
                    });
                } else {
                    alert("Wystąpił błąd podczas odblokowywania konta.");
                }
            }
        } else {
            if(confirm(`Konto funkcjonariusza ${email} jest obecnie AKTYWNE.\n\nCzy na pewno chcesz zablokować dostęp do systemu 211?`)) {
                const lockRes = await fetch(`/api/Auth/lock/${email}`, { method: 'POST', headers: { 'Authorization': 'Bearer ' + token } });
                
                if(lockRes.ok) {
                    alert("Konto zostało pomyślnie zablokowane!");
                    window.location.reload();
                } else {
                    alert("Wystąpił błąd podczas blokowania konta.");
                }
            }
        }
    } catch (e) {
        alert("Błąd połączenia z serwerem autoryzacji.");
    }
};


window.deleteDepartment = async function() {
    console.log("Rozpoczynam procedurę usuwania placówki...");
    console.log("URL endpointu: ", apiEndpoints.department);
    console.log("ID placówki: ", departmentId);

    if (!confirm("UWAGA! Czy na pewno chcesz usunąć całą placówkę? Zostaną usunięci również wszyscy pracownicy!")) {
        return;
    }

    try {
        const url = `${apiEndpoints.department}/${departmentId}`;
        console.log("Wysyłam żądanie DELETE na adres: ", url);

        const response = await fetch(url, {
            method: 'DELETE',
            headers: { 'Authorization': 'Bearer ' + token }
        });

        console.log("Status odpowiedzi z serwera: ", response.status);

        if (response.ok) {
            alert("Placówka usunięta pomyślnie.");
            if (currentPath.includes('/police/')) {
                window.location.href = '/Police/Home';
            } else if (currentPath.includes('/fire/')) {
                window.location.href = '/Fire/Home';
            } else {
                window.location.href = '/';
            }
        } else {
            const errorText = await response.text();
            console.error("Błąd z serwera: ", errorText);
            alert(`Błąd usuwania! Serwer zwrócił status: ${response.status}.\nSzczegóły w konsoli (F12). \n\nNajpierw upewnij się, że zwolniłeś wszystkich pracowników!`);
        }
    } catch (error) {
        console.error("Krytyczny błąd sieci: ", error);
        alert("Błąd połączenia z serwerem podczas usuwania.");
    }
};