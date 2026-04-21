document.addEventListener("DOMContentLoaded", function () {
    document.getElementById('incPhoto')?.addEventListener('change', function (e) {
        document.getElementById('incPhotoLabel').innerText = e.target.files[0] ? e.target.files[0].name : "Wybierz plik...";
    });

    document.getElementById('editIncPhoto')?.addEventListener('change', function (e) {
        document.getElementById('editIncPhotoLabel').innerText = e.target.files[0] ? e.target.files[0].name : "Wybierz nowe zdjęcie...";
    });

    document.getElementById('createIncidentForm')?.addEventListener('submit', async function (e) {
        e.preventDefault();
        const btn = document.getElementById('btn-submit-incident');
        const locSelect = document.getElementById('incLocationId');

        if (!locSelect || !locSelect.value) {
            alert("Proszę wybrać lokalizację placówki!");
            return;
        }

        btn.disabled = true;
        const formData = new FormData();
        formData.append('Description', document.getElementById('incDescription').value);
        formData.append('Severity', document.getElementById('incSeverity').value);
        formData.append('LocationId', locSelect.value);

        const fileInput = document.getElementById('incPhoto');
        if (fileInput && fileInput.files[0]) formData.append('photo', fileInput.files[0]);

        try {
            const response = await fetch('/api/CPR112/Incidents', {
                method: 'POST',
                headers: { 'Authorization': 'Bearer ' + window.jwtToken },
                body: formData
            });

            if (response.ok) {
                document.getElementById('incDescription').value = '';
                if (fileInput) fileInput.value = '';
                document.getElementById('incPhotoLabel').innerText = "Wybierz plik...";
                await window.refreshAll();
            } else {
                const err = await response.json();
                alert("Błąd: " + (err.title || err.message || "Niepoprawne dane zgłoszenia."));
            }
        } catch (e) {
            console.error(e);
            alert("Błąd połączenia z serwerem.");
        } finally {
            btn.disabled = false;
        }
    });

    document.getElementById('changeStatusForm')?.addEventListener('submit', async function (e) {
        e.preventDefault();
        const btn = document.getElementById('btn-save-status');
        btn.disabled = true;
        const id = document.getElementById('editIncidentId').value;
        const fd = new FormData();
        fd.append('NewStatus', document.getElementById('editIncidentStatus').value);
        fd.append('NewSeverity', document.getElementById('editIncidentPriority').value);
        const fi = document.getElementById('editIncPhoto');
        if (fi && fi.files[0]) fd.append('newPhoto', fi.files[0]);

        try {
            const response = await fetch(`/api/CPR112/Incidents/${id}/status`, {
                method: 'PUT',
                headers: { 'Authorization': 'Bearer ' + window.jwtToken },
                body: fd
            });
            if (response.ok) {
                $('#statusModal').modal('hide');
                await window.refreshAll();
            }
        } catch (e) { console.error(e); } finally { btn.disabled = false; }
    });

    document.getElementById('createCenterForm')?.addEventListener('submit', async function (e) {
        e.preventDefault();
        const dto = {
            name: document.getElementById('centerName').value,
            region: document.getElementById('centerRegion').value
        };
        try {
            const response = await fetch('/api/Enc', {
                method: 'POST',
                headers: { 'Authorization': 'Bearer ' + window.jwtToken, 'Content-Type': 'application/json' },
                body: JSON.stringify(dto)
            });
            if (response.ok) {
                document.getElementById('centerName').value = '';
                document.getElementById('centerRegion').value = '';
                await window.loadCenters();
                await window.loadCentersToSelect();
            }
        } catch (e) { console.error(e); }
    });

    document.getElementById('changeRankForm')?.addEventListener('submit', async function (e) {
        e.preventDefault();
        const newRank = document.getElementById('editOperatorRank').value;
        const id = document.getElementById('editOperatorId').value;
        try {
            const res = await fetch(`/api/Operators/${id}/rank`, {
                method: 'PUT',
                headers: { 'Authorization': 'Bearer ' + window.jwtToken, 'Content-Type': 'application/json' },
                body: JSON.stringify({ newRank })
            });
            if (res.ok) {
                $('#rankModal').modal('hide');
                window.loadOperators();
            }
        } catch (error) { console.error(error); }
    });
});

window.updateCounters = async function () {
    if (!window.jwtToken) return;
    const headers = { 'Authorization': 'Bearer ' + window.jwtToken };

    try {
        const pRes = await fetch('/api/Police/cars', { headers });
        if (pRes.ok) {
            const cars = await pRes.json();
            const available = cars.filter(c => c.isAvailable !== false && (c.policemanId || c.PolicemanId)).length;
            document.getElementById('status-police').textContent = `${available} / ${cars.length}`;
        }

        const fRes = await fetch('/api/Fire/firetrucks', { headers });
        if (fRes.ok) {
            const trucks = await fRes.json();
            const available = trucks.filter(t => t.isAvailable !== false && (t.firemanId || t.FiremanId)).length;
            document.getElementById('status-fire').textContent = `${available} / ${trucks.length}`;
        }

        const mRes = await fetch('/api/Medical/ambulances', { headers });
        if (mRes.ok) {
            const ambs = await mRes.json();
            const available = ambs.filter(a => a.isAvailable !== false && (a.paramedicId || a.ParamedicId)).length;
            document.getElementById('status-medic').textContent = `${available} / ${ambs.length}`;
        }
    } catch (e) {
        console.error("Błąd aktualizacji liczników", e);
    }
};

window.openDispatchModal = async function (type, targetIncidentId = null) {
    document.getElementById('dispatchTargetIncidentId').value = targetIncidentId || '';

    const tbody = document.getElementById('available-units-list');
    tbody.innerHTML = '<tr><td colspan="4" class="text-center p-4">Ładowanie dostępnych jednostek... <i class="fas fa-spinner fa-spin"></i></td></tr>';

    const titleEl = document.getElementById('dispatch-modal-title');
    const typeCol = document.getElementById('dispatch-col-type');
    const headerEl = document.getElementById('dispatch-modal-header');

    let apiUrl = '';

    if (type === 'police') {
        titleEl.innerHTML = '<i class="fas fa-car-side mr-2"></i> Dysponowanie Radiowozu';
        headerEl.className = 'modal-header bg-primary text-white';
        typeCol.textContent = 'Kierowca / Ranga';
        apiUrl = '/api/Police/cars';
    } else if (type === 'fire') {
        titleEl.innerHTML = '<i class="fas fa-fire-extinguisher mr-2"></i> Dysponowanie Wozu PSP';
        headerEl.className = 'modal-header bg-danger text-white';
        typeCol.textContent = 'Dowódca Wozu';
        apiUrl = '/api/Fire/firetrucks';
    } else if (type === 'medic') {
        titleEl.innerHTML = '<i class="fas fa-ambulance mr-2"></i> Dysponowanie Karetki';
        headerEl.className = 'modal-header bg-success text-white';
        typeCol.textContent = 'Typ Karetki';
        apiUrl = '/api/Medical/ambulances';
    }

    $('#universalDispatchModal').modal('show');

    try {
        const res = await fetch(apiUrl, { headers: { 'Authorization': 'Bearer ' + window.jwtToken } });
        if (!res.ok) throw new Error("Błąd pobierania jednostek");
        let units = await res.json();

        let depts = [];
        let staff = [];

        if (type === 'police') {
            units = units.filter(u => u.isAvailable !== false && (u.policemanId || u.PolicemanId));
            const dRes = await fetch('/api/Police/departments', { headers: { 'Authorization': 'Bearer ' + window.jwtToken } });
            if (dRes.ok) depts = await dRes.json();
            const sRes = await fetch('/api/Police/policemen', { headers: { 'Authorization': 'Bearer ' + window.jwtToken } });
            if (sRes.ok) staff = await sRes.json();
        }
        else if (type === 'fire') {
            units = units.filter(u => u.isAvailable !== false && (u.firemanId || u.FiremanId));
            const dRes = await fetch('/api/Fire/departments', { headers: { 'Authorization': 'Bearer ' + window.jwtToken } });
            if (dRes.ok) depts = await dRes.json();
            const sRes = await fetch('/api/Fire/firemen', { headers: { 'Authorization': 'Bearer ' + window.jwtToken } });
            if (sRes.ok) staff = await sRes.json();
        }
        else if (type === 'medic') {
            units = units.filter(u => u.isAvailable !== false && (u.paramedicId || u.ParamedicId));
            const dRes = await fetch('/api/Medical/hospitals', { headers: { 'Authorization': 'Bearer ' + window.jwtToken } });
            if (dRes.ok) depts = await dRes.json();
        }

        tbody.innerHTML = '';
        if (units.length === 0) {
            tbody.innerHTML = `<tr><td colspan="4" class="text-center p-4 text-muted">Brak wolnych jednostek w bazie.</td></tr>`;
            return;
        }

        units.forEach(u => {
            const id = u.id || u.Id;
            const plate = u.licensePlate || u.LicensePlate;
            let typeDesc = "";
            let baseName = "";
            let actionPayload = "";

            if (type === 'police') {
                const dept = depts.find(d => (d.id || d.Id) === (u.pDepartmentId || u.PDepartmentId));
                const pers = staff.find(p => (p.id || p.Id) === (u.policemanId || u.PolicemanId));
                baseName = dept ? (dept.name || dept.Name) : "Brak bazy";
                typeDesc = pers ? `${pers.name || pers.Name} ${pers.lastname || pers.lastName || pers.LastName || ""}` : "Brak kierowcy";
                actionPayload = `'police', '${id}'`;
            }
            else if (type === 'fire') {
                const dept = depts.find(d => (d.id || d.Id) === (u.fDepartmentId || u.FDepartmentId));
                const pers = staff.find(p => (p.id || p.Id) === (u.firemanId || u.FiremanId));
                baseName = dept ? (dept.name || dept.Name) : "Brak bazy";
                typeDesc = pers ? `${pers.name || pers.Name} ${pers.lastname || pers.lastName || pers.LastName || ""}` : "Brak dowódcy";
                actionPayload = `'fire', '${id}'`;
            }
            else if (type === 'medic') {
                const dept = depts.find(d => (d.id || d.Id) === (u.hospitalId || u.HospitalId));
                baseName = dept ? (dept.name || dept.Name) : "Brak bazy";
                const tVal = u.type !== undefined ? u.type : u.Type;
                const tMap = { 0: "S", 1: "T", 2: "P", 3: "N" };
                typeDesc = `Typ: ${tMap[tVal] || tVal}`;
                actionPayload = `'medic', '${id}'`;
            }

            tbody.insertAdjacentHTML('beforeend', `
                <tr class="amb-row" onclick="dispatchUnit(${actionPayload})">
                    <td class="align-middle"><b>${plate}</b></td>
                    <td class="align-middle">${typeDesc}</td>
                    <td class="align-middle"><i class="fas fa-map-marker-alt text-muted mr-1"></i> ${baseName}</td>
                    <td class="text-right">
                        <button class="btn btn-sm btn-dark font-weight-bold shadow-sm">
                            <i class="fas fa-paper-plane"></i> Wyślij
                        </button>
                    </td>
                </tr>
            `);
        });

    } catch (e) {
        tbody.innerHTML = '<tr><td colspan="4" class="text-center p-4 text-danger">Wystąpił błąd podczas ładowania dostępnych sił.</td></tr>';
    }
};

window.dispatchUnit = async function (type, targetId) {
    let incidentId = document.getElementById('dispatchTargetIncidentId').value;

    if (!incidentId) {
        incidentId = prompt("Podaj ID Zgłoszenia (GUID) do którego chcesz wysłać jednostkę:");
    }
    if (!incidentId) return;

    let url = '';
    if (type === 'police') {
        url = `/api/Police/cars/${targetId}/assign/${incidentId}`;
    } else if (type === 'fire') {
        url = `/api/Fire/firetrucks/${targetId}/assign/${incidentId}`;
    } else if (type === 'medic') {
        url = `/api/Medical/ambulances/${targetId}/assign/${incidentId}`;
    }

    try {
        const res = await fetch(url, {
            method: 'PUT',
            headers: { 'Authorization': 'Bearer ' + window.jwtToken }
        });

        if (res.ok) {
            $('#universalDispatchModal').modal('hide');
            window.refreshAll();
        } else {
            const err = await res.text();
            alert("Błąd dysponowania: " + err);
        }
    } catch (e) {
        alert("Błąd sieci! Nie można połączyć się z centralą.");
    }
};

window.refreshAll = async function () {
    await Promise.all([
        window.loadIncidents(),
        window.updateCounters()
    ]);
};

window.checkAdminVisibility = function () {
    try {
        const payload = JSON.parse(atob(window.jwtToken.split('.')[1]));
        const role = payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || payload.role;
        const rolesArray = Array.isArray(role) ? role : [role];
        if (rolesArray.includes('Admin') || rolesArray.includes('Admin112')) {
            window.togglePanels();
        }
    } catch (e) { console.error("Błąd parsowania roli JWT:", e); }
};

window.togglePanels = function () {
    const h = window.location.hash;
    const ip = document.getElementById('incidents-panel');
    const op = document.getElementById('operators-panel');
    const cp = document.getElementById('centers-panel');

    if (!ip || !op || !cp) return;

    ip.classList.add('d-none');
    op.classList.add('d-none');
    cp.classList.add('d-none');

    if (h === '#admin-operator-section') {
        op.classList.remove('d-none');
        if (typeof window.loadOperators === 'function') window.loadOperators();
    }
    else if (h === '#admin-centers-section') {
        cp.classList.remove('d-none');
        if (typeof window.loadCenters === 'function') window.loadCenters();
    }
    else {
        ip.classList.remove('d-none');
        if (typeof window.refreshAll === 'function') window.refreshAll();
    }
};

window.openEditModal = function (id, status, priority) {
    document.getElementById('editIncidentId').value = id;
    document.getElementById('editIncidentStatus').value = status;
    document.getElementById('editIncidentPriority').value = priority;
    document.getElementById('editIncPhoto').value = '';
    document.getElementById('editIncPhotoLabel').innerText = "Wybierz nowe zdjęcie...";
    $('#statusModal').modal('show');
};

window.openEditRankModal = function (id, r) {
    document.getElementById('editOperatorId').value = id;
    document.getElementById('editOperatorRank').value = r;
    $('#rankModal').modal('show');
};

window.autoDispatchAI = function () {
    alert('System AI jest aktualnie konfigurowany...');
};

window.loadIncidents = async function () {
    const tableBody = document.getElementById('incidents-table-body');
    if (!tableBody) return;

    try {
        const response = await fetch('/api/CPR112/Incidents', {
            headers: { 'Authorization': 'Bearer ' + window.jwtToken }
        });
        if (response.ok) {
            const incidents = await response.json();
            if (incidents.length === 0) {
                tableBody.innerHTML = '<tr><td colspan="5" class="text-center text-muted p-4">Brak aktywnych zgłoszeń.</td></tr>';
                return;
            }

            const payload = JSON.parse(atob(window.jwtToken.split('.')[1]));
            const role = payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || payload.role;
            const rolesArray = Array.isArray(role) ? role : [role];
            const isAdmin = rolesArray.includes('Admin') || rolesArray.includes('Admin112');

            tableBody.innerHTML = incidents.map(inc => {
                let bc = inc.severity === 'Wysoki' ? 'danger' : (inc.severity === 'Średni' ? 'warning' : 'info');
                const pi = inc.photoUrl ? `<a href="${inc.photoUrl}" target="_blank" class="incident-photo-link ml-2"><i class="fas fa-image fa-lg"></i></a>` : "";
                return `
                <tr>
                    <td class="align-middle font-weight-bold text-primary">${inc.incidentNumber || inc.id.substring(0, 8)}</td>
                    <td class="align-middle">${inc.description} ${pi}</td>
                    <td class="align-middle"><span class="badge bg-${bc}">${inc.severity}</span></td>
                    <td class="align-middle font-weight-bold">${inc.status}</td>
                    <td class="align-middle text-right">
                        <div class="btn-group">
                            <button class="btn btn-xs btn-info" onclick="window.openEditModal('${inc.id}', '${inc.status}', '${inc.severity}')" title="Edytuj status"><i class="fas fa-edit"></i></button>
                            <button class="btn btn-xs btn-primary ml-1" onclick="window.openDispatchModal('police', '${inc.id}')" title="Wyślij Policję"><i class="fas fa-shield-alt"></i></button>
                            <button class="btn btn-xs btn-danger ml-1" onclick="window.openDispatchModal('fire', '${inc.id}')" title="Wyślij Straż"><i class="fas fa-fire"></i></button>
                            <button class="btn btn-xs btn-success ml-1" onclick="window.openDispatchModal('medic', '${inc.id}')" title="Wyślij Medyków"><i class="fas fa-ambulance"></i></button>
                            ${isAdmin ? `<button class="btn btn-xs btn-outline-danger ml-1" onclick="window.deleteIncident('${inc.id}')"><i class="fas fa-trash"></i></button>` : ''}
                        </div>
                    </td>
                </tr>`;
            }).join('');
        }
    } catch (e) { console.error("Błąd loadIncidents:", e); }
};

window.loadCentersToSelect = async function () {
    const selectReg = document.getElementById('regEncId');
    const selectLoc = document.getElementById('incLocationId');
    try {
        const response = await fetch('/api/Enc', { headers: { 'Authorization': 'Bearer ' + window.jwtToken } });
        if (response.ok) {
            const data = await response.json();
            if (data.length === 0) {
                const empty = '<option value="">Brak placówek w bazie</option>';
                if (selectReg) selectReg.innerHTML = empty;
                if (selectLoc) selectLoc.innerHTML = empty;
                return;
            }
            const options = data.map(c => `<option value="${c.id}">${c.name} (${c.region})</option>`).join('');
            if (selectReg) selectReg.innerHTML = options;
            if (selectLoc) selectLoc.innerHTML = options;
        }
    } catch (e) { console.error("Błąd pobierania placówek:", e); }
};

window.loadOperators = async function () {
    const tableBody = document.getElementById('operators-table-body');
    if (!tableBody) return;
    try {
        const response = await fetch('/api/Operators', { headers: { 'Authorization': 'Bearer ' + window.jwtToken } });
        if (response.ok) {
            const data = await response.json();
            tableBody.innerHTML = data.map(o => {
                let bs = (o.rank === 'Admin112' || o.rank === 'Admin') ? "background-color: #dc3545; color: white;" : (o.rank === 'Dyspozytor112' ? "background-color: #343a40; color: white;" : "background-color: #6c757d; color: white;");
                return `
                <tr>
                    <td class="align-middle"><i class="fas fa-user-circle text-secondary mr-2"></i> <b>${o.firstName} ${o.lastName}</b></td>
                    <td class="align-middle"><span class="badge p-2 shadow-sm" style="${bs} min-width: 100px; display: inline-block;">${o.rank}</span></td>
                    <td class="align-middle font-italic">${o.stationNumber}</td>
                    <td class="align-middle text-right">
                        <button class="btn btn-sm btn-outline-info mr-1" onclick="window.openEditRankModal('${o.id}', '${o.rank}')">Ranga</button>
                        <button class="btn btn-sm btn-outline-danger" onclick="window.deleteOperator('${o.id}')"><i class="fas fa-trash-alt"></i></button>
                    </td>
                </tr>`;
            }).join('');
        }
    } catch (e) { console.error("Błąd loadOperators:", e); }
};

window.registerNewOperator = async function () {
    const dto = {
        firstName: document.getElementById('regFirstName').value,
        lastName: document.getElementById('regLastName').value,
        stationNumber: document.getElementById('regStation').value,
        email: document.getElementById('regEmail').value,
        rank: document.getElementById('regRank').value,
        encId: document.getElementById('regEncId').value
    };
    try {
        const response = await fetch('/api/Operators', {
            method: 'POST',
            headers: { 'Authorization': 'Bearer ' + window.jwtToken, 'Content-Type': 'application/json' },
            body: JSON.stringify(dto)
        });
        if (response.ok) {
            const result = await response.json();
            alert(`Konto utworzone! Hasło tymczasowe: ${result.temporaryPassword}`);
            window.loadOperators();
        } else {
            const err = await response.json();
            alert("Błąd: " + (err.message || "Nie udało się zarejestrować operatora."));
        }
    } catch (e) { console.error("Błąd registerNewOperator:", e); }
};

window.loadCenters = async function () {
    const tableBody = document.getElementById('centers-table-body');
    if (!tableBody) return;
    try {
        const response = await fetch('/api/Enc', { headers: { 'Authorization': 'Bearer ' + window.jwtToken } });
        if (response.ok) {
            const data = await response.json();
            tableBody.innerHTML = data.map(c => `
                <tr>
                    <td class="align-middle"><b>${c.name}</b></td>
                    <td class="align-middle">${c.region || 'Brak'}</td>
                    <td class="align-middle"><small class="text-muted">${c.id}</small></td>
                    <td class="text-right align-middle">
                        <button class="btn btn-xs btn-outline-danger" onclick="window.deleteCenter('${c.id}')"><i class="fas fa-trash"></i></button>
                    </td>
                </tr>`).join('');
        }
    } catch (e) { console.error("Błąd loadCenters:", e); }
};

window.deleteCenter = async function (id) {
    if (confirm("Usunąć tę placówkę? Operacja jest nieodwracalna.")) {
        await fetch(`/api/Enc/${id}`, { method: 'DELETE', headers: { 'Authorization': 'Bearer ' + window.jwtToken } });
        await window.loadCenters();
        await window.loadCentersToSelect();
    }
};

window.deleteOperator = async function (id) {
    if (confirm("Usunąć operatora?")) {
        await fetch(`/api/Operators/${id}`, { method: 'DELETE', headers: { 'Authorization': 'Bearer ' + window.jwtToken } });
        await window.loadOperators();
    }
};

window.deleteIncident = async function (id) {
    if (confirm("Usunąć zgłoszenie?")) {
        const response = await fetch(`/api/CPR112/Incidents/${id}`, { method: 'DELETE', headers: { 'Authorization': 'Bearer ' + window.jwtToken } });
        if (response.ok) {
            await window.refreshAll();
        }
    }
};