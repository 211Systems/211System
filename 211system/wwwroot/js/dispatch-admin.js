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

document.addEventListener("DOMContentLoaded", function () {
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