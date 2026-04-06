document.addEventListener("DOMContentLoaded", function () {
    document.getElementById('incPhoto')?.addEventListener('change', function (e) {
        document.getElementById('incPhotoLabel').innerText = e.target.files[0] ? e.target.files[0].name : "Wybierz plik...";
    });

    document.getElementById('editIncPhoto')?.addEventListener('change', function (e) {
        document.getElementById('editIncPhotoLabel').innerText = e.target.files[0] ? e.target.files[0].name : "Wybierz nowe zdjęcie...";
    });
});

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

    ip.classList.add('d-none'); op.classList.add('d-none'); cp.classList.add('d-none');

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