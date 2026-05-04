let currentRouteLayer = null;

async function drawRoute(startLat, startLon, endLat, endLon) {
    if (!map) return;

    if (currentRouteLayer) {
        map.removeLayer(currentRouteLayer);
    }

    const url = `https://router.project-osrm.org/route/v1/driving/${startLon},${startLat};${endLon},${endLat}?overview=full&geometries=geojson`;

    try {
        const response = await fetch(url);
        const data = await response.json();

        if (data.routes && data.routes.length > 0) {
            const coordinates = data.routes[0].geometry.coordinates.map(c => [c[1], c[0]]);
            const distance = (data.routes[0].distance / 1000).toFixed(2);
            const duration = Math.round(data.routes[0].duration / 60);

            currentRouteLayer = L.polyline(coordinates, { color: 'blue', weight: 5, opacity: 0.7 }).addTo(map);

            map.fitBounds(currentRouteLayer.getBounds());

            console.log(`Dystans: ${distance} km, Przewidywany czas: ${duration} min`);
        }
    } catch (e) {
        console.error("Błąd routingu:", e);
    }
}

window.registerNewCenter = async function () {
    const nameVal = document.getElementById('centerName').value;
    const regionVal = document.getElementById('centerRegion').value;

    if (!nameVal || !regionVal) {
        alert("Proszę uzupełnić nazwę placówki i region!");
        return;
    }

    const dto = { name: nameVal, region: regionVal };

    try {
        const response = await fetch('/api/Enc', {
            method: 'POST',
            headers: {
                'Authorization': 'Bearer ' + window.jwtToken,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(dto)
        });

        if (response.ok) {
            alert("Sukces! Placówka została dodana do systemu.");
            document.getElementById('centerName').value = '';
            document.getElementById('centerRegion').value = '';
            if (typeof window.loadCenters === 'function') await window.loadCenters();
            if (typeof window.loadCentersToSelect === 'function') await window.loadCentersToSelect();
        } else {
            const errorText = await response.text();
            alert(`Błąd serwera: ${response.status}. Szczegóły: ${errorText}`);
        }
    } catch (e) {
        console.error("Błąd registerNewCenter:", e);
        alert("Błąd krytyczny połączenia z serwerem.");
    }
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

    if (!dto.email || !dto.lastName) {
        alert("Uzupełnij wymagane dane operatora!");
        return;
    }

    try {
        const response = await fetch('/api/Operators', {
            method: 'POST',
            headers: {
                'Authorization': 'Bearer ' + window.jwtToken,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(dto)
        });

        if (response.ok) {
            const result = await response.json();
            alert(`Konto utworzone! Hasło tymczasowe: ${result.temporaryPassword}`);
            if (typeof window.loadOperators === 'function') window.loadOperators();
        } else {
            const err = await response.json();
            alert("Błąd: " + (err.message || "Nie udało się zarejestrować operatora."));
        }
    } catch (e) {
        console.error("Błąd registerNewOperator:", e);
    }
};

window.openDispatchModal = async function (type, targetIncidentId = null) {
    document.getElementById('dispatchTargetIncidentId').value = targetIncidentId || '';
    const tbody = document.getElementById('available-units-list');
    tbody.innerHTML = '<tr><td colspan="4" class="text-center p-4">Ładowanie... <i class="fas fa-spinner fa-spin"></i></td></tr>';

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
        const units = await res.json();

        tbody.innerHTML = '';
        if (units.length === 0) {
            tbody.innerHTML = '<tr><td colspan="4" class="text-center p-4">Brak wolnych jednostek.</td></tr>';
            return;
        }

        units.filter(u => u.isAvailable !== false).forEach(u => {
            const id = u.id || u.Id;
            const plate = u.licensePlate || u.LicensePlate;
            tbody.insertAdjacentHTML('beforeend', `
                <tr class="amb-row" onclick="window.dispatchUnit('${type}', '${id}')">
                    <td><b>${plate}</b></td>
                    <td>Aktywny</td>
                    <td>Baza systemowa</td>
                    <td class="text-right"><button class="btn btn-sm btn-dark">Wyślij</button></td>
                </tr>`);
        });
    } catch (e) {
        tbody.innerHTML = '<tr><td colspan="4" class="text-center text-danger">Błąd ładowania danych.</td></tr>';
    }
};

window.dispatchUnit = async function (type, targetId) {
    let incidentId = document.getElementById('dispatchTargetIncidentId').value;
    if (!incidentId) incidentId = prompt("Podaj ID Zgłoszenia:");
    if (!incidentId) return;

    let url = '';
    if (type === 'police') url = `/api/Police/cars/${targetId}/assign/${incidentId}`;
    else if (type === 'fire') url = `/api/Fire/firetrucks/${targetId}/assign/${incidentId}`;
    else if (type === 'medic') url = `/api/Medical/ambulances/${targetId}/assign/${incidentId}`;

    try {
        const res = await fetch(url, { method: 'PUT', headers: { 'Authorization': 'Bearer ' + window.jwtToken } });
        if (res.ok) {
            $('#universalDispatchModal').modal('hide');
            window.refreshAll();
        } else {
            const err = await res.json();
            alert("Błąd dysponowania: " + (err.message || "Wystąpił błąd bazy danych."));
        }
    } catch (e) { alert("Błąd sieci!"); }
};

window.deleteCenter = async function (id) {
    if (confirm("Usunąć tę placówkę?")) {
        await fetch(`/api/Enc/${id}`, { method: 'DELETE', headers: { 'Authorization': 'Bearer ' + window.jwtToken } });
        window.loadCenters();
        window.loadCentersToSelect();
    }
};

window.deleteOperator = async function (id) {
    if (confirm("Usunąć operatora?")) {
        await fetch(`/api/Operators/${id}`, { method: 'DELETE', headers: { 'Authorization': 'Bearer ' + window.jwtToken } });
        window.loadOperators();
    }
};

window.deleteIncident = async function (id) {
    if (confirm("Usunąć zgłoszenie?")) {
        await fetch(`/api/CPR112/Incidents/${id}`, { method: 'DELETE', headers: { 'Authorization': 'Bearer ' + window.jwtToken } });
        window.refreshAll();
    }
};

window.openEditModal = function (id, status, priority) {
    document.getElementById('editIncidentId').value = id;
    document.getElementById('editIncidentStatus').value = status;
    document.getElementById('editIncidentPriority').value = priority;
    $('#statusModal').modal('show');
};

window.openEditRankModal = function (id, r) {
    document.getElementById('editOperatorId').value = id;
    document.getElementById('editOperatorRank').value = r;
    $('#rankModal').modal('show');
};

document.addEventListener("DOMContentLoaded", function () {
    const setupFileLabel = (inputId, labelId) => {
        document.getElementById(inputId)?.addEventListener('change', function (e) {
            document.getElementById(labelId).innerText = e.target.files[0] ? e.target.files[0].name : "Wybierz plik...";
        });
    };
    setupFileLabel('incPhoto', 'incPhotoLabel');
    setupFileLabel('editIncPhoto', 'editIncPhotoLabel');

    document.getElementById('createIncidentForm')?.addEventListener('submit', async function (e) {
        e.preventDefault();
        const btn = document.getElementById('btn-submit-incident');
        const typeSelect = document.getElementById('incType');

        const latVal = document.getElementById('incLat').value;
        const lngVal = document.getElementById('incLng').value;

        if (!latVal || !lngVal) {
            alert("Kliknij na mapie, aby wyznaczyć dokładną lokalizację zdarzenia!");
            return;
        }
        if (!typeSelect.value) {
            alert("Wybierz typ zdarzenia!");
            return;
        }

        btn.disabled = true;
        const formData = new FormData();

        formData.append('Description', document.getElementById('incDescription').value);
        formData.append('SeverityLevelId', parseInt(document.getElementById('incSeverity').value));
        formData.append('IncidentTypeId', parseInt(typeSelect.value));

        formData.append('Latitude', latVal);
        formData.append('Longitude', lngVal);

        if (window.currentOperatorId) {
            formData.append('OperatorId', window.currentOperatorId);
        }

        const fileInput = document.getElementById('incPhoto');
        if (fileInput && fileInput.files[0]) {
            formData.append('photo', fileInput.files[0]);
        }

        try {
            const response = await fetch('/api/CPR112/Incidents', {
                method: 'POST',
                headers: { 'Authorization': 'Bearer ' + window.jwtToken },
                body: formData
            });

            if (response.ok) {
                alert("Zgłoszenie zarejestrowane pomyślnie!");
                document.getElementById('incDescription').value = '';
                document.getElementById('incType').value = '';
                document.getElementById('incLat').value = '';
                document.getElementById('incLng').value = '';
                if (fileInput) fileInput.value = '';
                document.getElementById('incPhotoLabel').innerText = "Wybierz plik...";

                window.refreshAll();
            } else {
                const errData = await response.json();
                console.error("Szczegóły błędu 400:", errData);
                alert("Błąd serwera. Sprawdź konsolę.");
            }
        } catch (err) {
            console.error("Błąd sieci:", err);
        } finally {
            btn.disabled = false;
        }
    });

    document.getElementById('changeStatusForm')?.addEventListener('submit', async function (e) {
        e.preventDefault();
        const id = document.getElementById('editIncidentId').value;
        const fd = new FormData();
        fd.append('NewStatus', document.getElementById('editIncidentStatus').value);
        fd.append('NewSeverity', document.getElementById('editIncidentPriority').value);

        const fi = document.getElementById('editIncPhoto');
        if (fi && fi.files[0]) fd.append('newPhoto', fi.files[0]);

        try {
            const res = await fetch(`/api/CPR112/Incidents/${id}/status`, {
                method: 'PUT',
                headers: { 'Authorization': 'Bearer ' + window.jwtToken },
                body: fd
            });
            if (res.ok) {
                $('#statusModal').modal('hide');
                window.refreshAll();
            }
        } catch (e) { console.error(e); }
    });
});

window.loadIncidentTypes = async function () {
    try {
        const res = await fetch('/api/CPR112/Incidents/IncidentTypes', {
            headers: { 'Authorization': 'Bearer ' + window.jwtToken }
        });
        if (res.ok) {
            const types = await res.json();
            const select = document.getElementById('incType');
            if (select) {
                select.innerHTML = '<option value="">Wybierz typ...</option>' +
                    types.map(t => `<option value="${t.id}">${t.name}</option>`).join('');
            }
        }
    } catch (e) { console.error(e); }
};

window.loadIncidentStats = async function () {
    try {
        const res = await fetch('/api/CPR112/Incidents/stats/summary', {
            headers: { 'Authorization': 'Bearer ' + window.jwtToken }
        });
        if (res.ok) {
            const data = await res.json();
            const container = document.getElementById('stats-summary');
            if (container) {
                container.innerHTML = data.map(s => `
                    <span class="badge badge-secondary p-2 shadow-sm" style="font-size: 14px;">
                        ${s.name}: <span class="text-warning">${s.count}</span>
                    </span>
                `).join(' ');
            }
        }
    } catch (e) { console.error(e); }
};

window.showHistory = async function (id) {
    try {
        const res = await fetch(`/api/CPR112/Incidents/${id}/history`, {
            headers: { 'Authorization': 'Bearer ' + window.jwtToken }
        });
        if (res.ok) {
            const data = await res.json();
            const container = document.getElementById('history-timeline');

            if (data.length === 0) {
                container.innerHTML = `
                    <div>
                        <i class="fas fa-info bg-secondary"></i>
                        <div class="timeline-item bg-dark border-secondary">
                            <div class="timeline-body text-white text-center p-3">Brak zarejestrowanych zmian dla tego zgłoszenia.</div>
                        </div>
                    </div>`;
            } else {
                container.innerHTML = data.map(h => {
                    const dateObj = new Date(h.changedAt);
                    const timeStr = dateObj.toLocaleTimeString('pl-PL', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
                    const dateStr = dateObj.toLocaleDateString('pl-PL');

                    let iconColor = 'bg-info';
                    let textColor = 'text-info';
                    let iconClass = 'fa-exchange-alt';

                    if (h.newStatus === 'W toku') { iconColor = 'bg-primary'; textColor = 'text-primary'; iconClass = 'fa-play'; }
                    if (h.newStatus === 'Zakończone') { iconColor = 'bg-success'; textColor = 'text-success'; iconClass = 'fa-check'; }
                    if (h.newStatus === 'Fałszywy alarm') { iconColor = 'bg-warning'; textColor = 'text-warning'; iconClass = 'fa-exclamation-triangle'; }

                    if (h.newStatus.includes('powrócił') || h.newStatus.includes('zakończył działania')) {
                        iconColor = 'bg-secondary';
                        textColor = 'text-light';
                        iconClass = 'fa-undo';
                    }

                    return `
                    <!-- element osi czasu -->
                    <div>
                        <i class="fas ${iconClass} ${iconColor} text-white"></i>
                        <div class="timeline-item bg-dark border-secondary shadow-sm" style="border: 1px solid #6c757d;">
                            <span class="time text-light mt-2 mr-2"><i class="fas fa-clock"></i> ${timeStr} <small>(${dateStr})</small></span>
                            <h3 class="timeline-header border-secondary text-white border-bottom-0"><b class="${textColor}">Aktualizacja Statusu</b></h3>
                            
                            <!-- POPRAWKA: text-white dodane do kontenera z tekstem -->
                            <div class="timeline-body pt-0 text-white">
                                Stan zmieniony z <span class="text-secondary" style="text-decoration: line-through;">${h.oldStatus}</span> 
                                <i class="fas fa-arrow-right mx-2 text-secondary"></i> 
                                <b class="${textColor}">${h.newStatus}</b>
                            </div>
                        </div>
                    </div>`;
                }).join('') + `<div><i class="fas fa-stop bg-secondary text-white"></i></div>`;
            }

            $('#historyModal').modal('show');
        }
    } catch (e) { console.error("Błąd historii:", e); }
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

            const payload = JSON.parse(atob(window.jwtToken.split('.')[1]));
            const role = payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || payload.role;
            const rolesArray = Array.isArray(role) ? role : [role];
            const isAdmin = rolesArray.includes('Admin') || rolesArray.includes('Admin112');

            tableBody.innerHTML = incidents.map(inc => {
                let bc = inc.severity === 'Wysoki' ? 'danger' : (inc.severity === 'Średni' ? 'warning' : 'info');
                if (inc.severity === 'Krytyczny') bc = 'dark';

                return `
                <tr>
                    <td class="align-middle font-weight-bold text-primary">${inc.incidentNumber || inc.id.substring(0, 8)}</td>
                    <td class="align-middle">${inc.description}</td>
                    <td class="align-middle text-muted font-weight-bold">${inc.incidentType || 'Brak'}</td>
                    <td class="align-middle"><span class="badge bg-${bc}">${inc.severity}</span></td>
                    <td class="align-middle font-weight-bold">${inc.status}</td>
                    <td class="align-middle text-right">
                        <div class="btn-group">
                            <button class="btn btn-xs btn-outline-light ml-1 mr-1" onclick="window.showHistory('${inc.id}')" title="Historia logów"><i class="fas fa-history"></i></button>
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
    } catch (e) {
        console.error("Błąd ładowania incydentów:", e);
        tableBody.innerHTML = '<tr><td colspan="6" class="text-center text-danger">Błąd połączenia z bazą danych.</td></tr>';
    }
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
                    <td><b>${c.name}</b></td>
                    <td>${c.region}</td>
                    <td><small>${c.id}</small></td>
                    <td class="text-right">
                        <button class="btn btn-xs btn-danger" onclick="window.deleteCenter('${c.id}')"><i class="fas fa-trash"></i></button>
                    </td>
                </tr>`).join('');
        }
    } catch (e) { console.error(e); }
};

window.loadCentersToSelect = async function () {
    const selectLoc = document.getElementById('incLocationId');
    const selectReg = document.getElementById('regEncId');
    try {
        const response = await fetch('/api/Enc', { headers: { 'Authorization': 'Bearer ' + window.jwtToken } });
        if (response.ok) {
            const data = await response.json();
            const options = data.map(c => `<option value="${c.id}">${c.name} (${c.region})</option>`).join('');
            if (selectLoc) selectLoc.innerHTML = options;
            if (selectReg) selectReg.innerHTML = options;
        }
    } catch (e) { console.error(e); }
};

window.loadOperators = async function () {
    const tableBody = document.getElementById('operators-table-body');
    if (!tableBody) return;
    try {
        const response = await fetch('/api/Operators', { headers: { 'Authorization': 'Bearer ' + window.jwtToken } });
        if (response.ok) {
            const data = await response.json();
            tableBody.innerHTML = data.map(o => `
                <tr>
                    <td><b>${o.firstName} ${o.lastName}</b></td>
                    <td>${o.rank}</td>
                    <td>${o.stationNumber}</td>
                    <td class="text-right">
                        <button class="btn btn-sm btn-danger" onclick="window.deleteOperator('${o.id}')"><i class="fas fa-trash"></i></button>
                    </td>
                </tr>`).join('');
        }
    } catch (e) { console.error(e); }
};

window.updateCounters = async function () {
    if (!window.jwtToken) return;
    const headers = { 'Authorization': 'Bearer ' + window.jwtToken };
    try {
        const [p, f, m] = await Promise.all([
            fetch('/api/Police/cars', { headers }).then(r => r.json()),
            fetch('/api/Fire/firetrucks', { headers }).then(r => r.json()),
            fetch('/api/Medical/ambulances', { headers }).then(r => r.json())
        ]);
        document.getElementById('status-police').textContent = `${p.filter(c => c.isAvailable).length} / ${p.length}`;
        document.getElementById('status-fire').textContent = `${f.filter(c => c.isAvailable).length} / ${f.length}`;
        document.getElementById('status-medic').textContent = `${m.filter(c => c.isAvailable).length} / ${m.length}`;
    } catch (e) { console.error(e); }
};

window.refreshAll = async function () {
    await Promise.all([
        window.loadIncidents(),
        window.updateCounters(),
        window.loadIncidentStats()
    ]);
    if (typeof window.refreshMapData === 'function') window.refreshMapData();
};

window.togglePanels = function () {
    const h = window.location.hash;
    const ip = document.getElementById('incidents-panel');
    const op = document.getElementById('operators-panel');
    const cp = document.getElementById('centers-panel');

    if (!ip || !op || !cp) return;

    [ip, op, cp].forEach(p => p.classList.add('d-none'));

    if (h === '#admin-operator-section') {
        op.classList.remove('d-none');
        window.loadOperators();
    } else if (h === '#admin-centers-section') {
        cp.classList.remove('d-none');
        window.loadCenters();
    } else {
        ip.classList.remove('d-none');
        window.refreshAll();
    }
};

window.checkAdminVisibility = function () {
    try {
        const payload = JSON.parse(atob(window.jwtToken.split('.')[1]));
        const role = payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || payload.role;
        const rolesArray = Array.isArray(role) ? role : [role];
        if (rolesArray.includes('Admin') || rolesArray.includes('Admin112')) {
            document.getElementById('nav-admin-cpr-container')?.classList.remove('d-none');
            document.getElementById('nav-admin-centers-container')?.classList.remove('d-none');
        }
    } catch (e) { console.error(e); }
};