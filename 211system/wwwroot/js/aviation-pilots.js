window.AviationPilots = (function () {
    const personnelEndpoints = {
        0: { url: '/api/Medical/paramedics', label: 'ratownika' },
        1: { url: '/api/Police/policemen', label: 'policjanta' },
        2: { url: '/api/Fire/firemen', label: 'strażaka' }
    };

    function personLabel(p) {
        const name = p.name || p.Name || '';
        const last = p.lastName || p.LastName || p.lastname || p.Lastname || '';
        return (name + ' ' + last).trim() || (p.email || p.Email || 'Pracownik');
    }

    function personId(p) { return p.id || p.Id; }

    async function loadCandidates(serviceFilter, token) {
        const ep = personnelEndpoints[serviceFilter];
        if (!ep) return [];
        const res = await fetch(ep.url, { headers: { 'Authorization': 'Bearer ' + token } });
        if (!res.ok) return [];
        return await res.json();
    }

    async function assign(unitId, pilotId, pilotName, token) {
        return fetch('/api/Aviation/units/' + unitId + '/pilot', {
            method: 'PUT',
            headers: { 'Authorization': 'Bearer ' + token, 'Content-Type': 'application/json' },
            body: JSON.stringify({ pilotId: pilotId || null, pilotName: pilotName || null })
        });
    }

    async function endMission(unitId, token) {
        return fetch('/api/Aviation/units/' + unitId + '/free', {
            method: 'POST',
            headers: { 'Authorization': 'Bearer ' + token }
        });
    }

    async function myActiveMissions(myId, token) {
        if (!myId) return [];
        const res = await fetch('/api/Aviation/units', { headers: { 'Authorization': 'Bearer ' + token } });
        if (!res.ok) return [];
        const units = await res.json();
        return units.filter(function (u) {
            const pid = u.pilotId || u.PilotId;
            const avail = (u.isAvailable !== undefined ? u.isAvailable : u.IsAvailable);
            return pid && String(pid) === String(myId) && avail === false;
        });
    }

    return {
        personnelEndpoints: personnelEndpoints,
        personLabel: personLabel,
        personId: personId,
        loadCandidates: loadCandidates,
        assign: assign,
        endMission: endMission,
        myActiveMissions: myActiveMissions,
        label: function (sf) { return (personnelEndpoints[sf] || {}).label || 'pracownika'; }
    };
})();
