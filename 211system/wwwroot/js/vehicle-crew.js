window.VehicleCrew = (function () {
    const MAX = 4;

    function token() { return localStorage.getItem('jwt'); }

    function ensureModal() {
        if (document.getElementById('vehicleCrewModal')) return;
        const wrap = document.createElement('div');
        wrap.innerHTML = `
        <div class="modal fade" id="vehicleCrewModal" tabindex="-1" role="dialog" aria-hidden="true">
            <div class="modal-dialog" role="document">
                <div class="modal-content dark-mode">
                    <div class="modal-header bg-dark text-white">
                        <h5 class="modal-title"><i class="fas fa-users"></i> Obsada pojazdu</h5>
                        <button type="button" class="close text-white" data-dismiss="modal"><span>&times;</span></button>
                    </div>
                    <div class="modal-body">
                        <p class="text-muted small mb-2" id="vc-commander"></p>
                        <p class="small mb-2">Zaznacz dodatkowych członków załogi (maks. ${MAX}; łącznie z dowódcą do 5 osób):</p>
                        <div id="vc-list" style="max-height:320px;overflow:auto;"></div>
                        <small id="vc-count" class="text-muted"></small>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-dismiss="modal">Anuluj</button>
                        <button type="button" class="btn btn-dark" id="vc-save"><i class="fas fa-save"></i> Zapisz obsadę</button>
                    </div>
                </div>
            </div>
        </div>`;
        document.body.appendChild(wrap.firstElementChild);
    }

    function updateCount() {
        const checked = document.querySelectorAll('#vc-list input[type=checkbox]:checked').length;
        const c = document.getElementById('vc-count');
        if (c) c.textContent = `Zaznaczono: ${checked} / ${MAX}`;
    }

    function enforceMax() {
        const boxes = document.querySelectorAll('#vc-list input[type=checkbox]');
        boxes.forEach(b => b.addEventListener('change', function () {
            const checked = document.querySelectorAll('#vc-list input[type=checkbox]:checked').length;
            if (checked > MAX) { this.checked = false; alert(`Maksymalnie ${MAX} dodatkowych członków załogi.`); }
            updateCount();
        }));
    }

    async function getCrew(type, vehicleId) {
        try {
            const r = await fetch(`/api/Crew/${type}/${vehicleId}`, { headers: { 'Authorization': 'Bearer ' + token() } });
            return r.ok ? await r.json() : [];
        } catch (e) { return []; }
    }

    async function setCrew(type, vehicleId, crew) {
        return fetch(`/api/Crew/${type}/${vehicleId}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + token() },
            body: JSON.stringify({ crew })
        });
    }

    // opts: { type, vehicleId, candidates:[{id,name}], commanderId, commanderName, onSaved }
    async function open(opts) {
        ensureModal();
        const candidates = (opts.candidates || []).filter(c => String(c.id) !== String(opts.commanderId || ''));
        const current = await getCrew(opts.type, opts.vehicleId);
        const currentIds = current.map(c => String(c.memberId || c.MemberId));

        document.getElementById('vc-commander').innerHTML = opts.commanderName
            ? `Dowódca/kierowca: <b>${opts.commanderName}</b>`
            : '<span class="text-warning">Brak przypisanego dowódcy/kierowcy.</span>';

        const listEl = document.getElementById('vc-list');
        if (!candidates.length) {
            listEl.innerHTML = '<p class="text-muted">Brak dostępnych pracowników do obsady.</p>';
        } else {
            listEl.innerHTML = candidates.map(c => {
                const checked = currentIds.includes(String(c.id)) ? 'checked' : '';
                return `<div class="form-check">
                    <input class="form-check-input" type="checkbox" value="${c.id}" data-name="${c.name}" id="vc-${c.id}" ${checked}>
                    <label class="form-check-label" for="vc-${c.id}">${c.name}</label>
                </div>`;
            }).join('');
        }
        enforceMax();
        updateCount();

        const saveBtn = document.getElementById('vc-save');
        saveBtn.onclick = async function () {
            const selected = Array.from(document.querySelectorAll('#vc-list input[type=checkbox]:checked'))
                .map(b => ({ memberId: b.value, memberName: b.getAttribute('data-name') }));
            const res = await setCrew(opts.type, opts.vehicleId, selected);
            if (res.ok) {
                $('#vehicleCrewModal').modal('hide');
                if (typeof opts.onSaved === 'function') opts.onSaved();
                else alert('Obsada zapisana.');
            } else {
                const e = await res.json().catch(() => ({}));
                alert(e.message || 'Nie udało się zapisać obsady (brak uprawnień lub błąd serwera).');
            }
        };

        $('#vehicleCrewModal').modal('show');
    }

    return { open, getCrew, setCrew };
})();
