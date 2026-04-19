document.addEventListener("DOMContentLoaded", function () {
    document.getElementById('incPhoto')?.addEventListener('change', function (e) {
        document.getElementById('incPhotoLabel').innerText = e.target.files[0] ? e.target.files[0].name : "Wybierz plik...";
    });

    document.getElementById('editIncPhoto')?.addEventListener('change', function (e) {
        document.getElementById('editIncPhotoLabel').innerText = e.target.files[0] ? e.target.files[0].name : "Wybierz nowe zdjęcie...";
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
                actionPayload = `'police', '${u.policemanId || u.PolicemanId}'`;
            }
            else if (type === 'fire') {
                const dept = depts.find(d => (d.id || d.Id) === (u.fDepartmentId || u.FDepartmentId));
                const pers = staff.find(p => (p.id || p.Id) === (u.firemanId || u.FiremanId));
                baseName = dept ? (dept.name || dept.Name) : "Brak bazy";
                typeDesc = pers ? `${pers.name || pers.Name} ${pers.lastname || pers.lastName || pers.LastName || ""}` : "Brak dowódcy";
                actionPayload = `'fire', '${u.firemanId || u.FiremanId}'`;
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
    let method = '';

    if (type === 'police') {
        url = `/api/Police/operations/start?policemanId=${targetId}&reportId=${incidentId}`;
        method = 'POST';
    } else if (type === 'fire') {
        url = `/api/Fire/operations/start?firemanId=${targetId}&reportId=${incidentId}`;
        method = 'POST';
    } else if (type === 'medic') {
        url = `/api/Medical/ambulances/${targetId}/assign/${incidentId}`;
        method = 'PUT';
    }

    try {
        const res = await fetch(url, {
            method: method,
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
    if (typeof window.loadIncidents === 'function') {
        await window.loadIncidents();
    }
    await window.updateCounters();
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

//TO DO: RADEK!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
window.autoDispatchAI = function () {
    alert('System AI jest aktualnie konfigurowany...');
};