window.refreshAll = async function () {
    await Promise.all([window.loadIncidents(), window.loadServiceStatuses()]);
};

window.loadServiceStatuses = async function () {
    try {
        const response = await fetch('/api/Medical/ambulances', {
            headers: { 'Authorization': 'Bearer ' + window.jwtToken }
        });
        if (response.ok) {
            const data = await response.json();
            const available = data.filter(a => a.isAvailable === true).length;
            const el = document.getElementById('status-medic');
            if (el) el.textContent = `${available} / ${data.length}`;
        }
    } catch (e) { console.error("Błąd loadServiceStatuses:", e); }
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
                            <button class="btn btn-xs btn-primary ml-1" onclick="window.startOperation('${inc.id}', 'police')" title="Wyślij Policję"><i class="fas fa-shield-alt"></i></button>
                            <button class="btn btn-xs btn-danger ml-1" onclick="window.startOperation('${inc.id}', 'fire')" title="Wyślij Straż"><i class="fas fa-fire"></i></button>
                            <button class="btn btn-xs btn-success ml-1" onclick="window.openDispatchModal('${inc.id}')" title="Wyślij Medyków"><i class="fas fa-ambulance"></i></button>
                            ${isAdmin ? `<button class="btn btn-xs btn-outline-danger ml-1" onclick="window.deleteIncident('${inc.id}')"><i class="fas fa-trash"></i></button>` : ''}
                        </div>
                    </td>
                </tr>`;
            }).join('');
        }
    } catch (e) { console.error("Błąd loadIncidents:", e); }
};

window.startOperation = async function (incidentId, serviceType) {
    let url = "";
    let bodyData = { incidentId: incidentId };

    if (serviceType === 'police') url = "/api/Dispatch/police/start";
    else if (serviceType === 'fire') url = "/api/Dispatch/fire/start";
    else return;

    try {
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Authorization': 'Bearer ' + window.jwtToken,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(bodyData)
        });

        if (response.ok) {
            await window.refreshAll();
        } else {
            const err = await response.json();
            alert("Błąd startu operacji: " + (err.message || "Błąd serwera"));
        }
    } catch (e) { console.error("Błąd startOperation:", e); }
};

window.openDispatchModal = async function (incidentId) {
    document.getElementById('dispatchTargetIncidentId').value = incidentId;
    const listBody = document.getElementById('available-ambulances-list');
    listBody.innerHTML = '<tr><td colspan="4" class="text-center p-4"><i class="fas fa-sync fa-spin"></i> Skanowanie floty...</td></tr>';
    $('#dispatchAmbulanceModal').modal('show');
    try {
        const response = await fetch('/api/Medical/ambulances/available', {
            headers: { 'Authorization': 'Bearer ' + window.jwtToken }
        });
        if (response.ok) {
            const available = await response.json();
            if (available.length === 0) {
                listBody.innerHTML = '<tr><td colspan="4" class="text-center text-danger p-4">Brak wolnych jednostek!</td></tr>';
                return;
            }
            const typeMap = { 0: "S", 1: "T", 2: "P", 3: "N" };
            listBody.innerHTML = available.map(amb => `
                <tr class="amb-row">
                    <td class="align-middle"><b>${amb.licensePlate}</b></td>
                    <td class="align-middle"><span class="badge badge-warning">${typeMap[amb.type] || amb.type}</span></td>
                    <td class="align-middle"><small>${amb.hospitalId.substring(0, 8)}...</small></td>
                    <td class="text-right align-middle">
                        <button class="btn btn-sm btn-success font-weight-bold shadow-sm" onclick="window.confirmDispatch('${amb.id}')">WYŚLIJ</button>
                    </td>
                </tr>`).join('');
        }
    } catch (e) { console.error("Błąd openDispatchModal:", e); }
};

window.confirmDispatch = async function (ambulanceId) {
    const incidentId = document.getElementById('dispatchTargetIncidentId').value;
    try {
        const response = await fetch(`/api/Medical/ambulances/${ambulanceId}/assign/${incidentId}`, {
            method: 'PUT',
            headers: { 'Authorization': 'Bearer ' + window.jwtToken }
        });
        if (response.ok) {
            $('#dispatchAmbulanceModal').modal('hide');
            await window.refreshAll();
        } else {
            const err = await response.json();
            alert("Błąd: " + (err.message || "Nie udało się zadysponować karetki."));
        }
    } catch (e) { console.error("Błąd confirmDispatch:", e); }
};

window.deleteIncident = async function (id) {
    if (confirm("Usunąć zgłoszenie?")) {
        const response = await fetch(`/api/CPR112/Incidents/${id}`, { method: 'DELETE', headers: { 'Authorization': 'Bearer ' + window.jwtToken } });
        if (response.ok) {
            await window.refreshAll();
        }
    }
};

document.addEventListener("DOMContentLoaded", function () {
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
});