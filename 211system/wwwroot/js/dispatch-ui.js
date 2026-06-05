let currentRouteLayer = null;
window.activeSimulations = {};
window.vehicleMarkers = {};
window.vehicleIncidentMap = window.vehicleIncidentMap || {};
window.dispatchData = window.dispatchData || { hospitals: [], polDepts: [], fireDepts: [], airbases: [], vehicles: {} };

window.VEH_STATUS = {
    InBase: 0,
    EnRoute: 1,
    OnScene: 2,
    Transporting: 3,
    Returning: 4,
    TransportingToHospital: 5
};

window.routeColorForService = function (serviceType) {
    if (serviceType === 'police') return '#007bff';
    if (serviceType === 'medic') return '#28a745';
    if (serviceType === 'fire') return '#dc3545';
    return '#343a40';
};

window.getIconByService = function (type) {
    let iconClass = 'fa-car';
    let bgColor = '#6c757d';

    if (type === 'police') { iconClass = 'fa-car-side'; bgColor = '#007bff'; }
    else if (type === 'medic') { iconClass = 'fa-ambulance'; bgColor = '#28a745'; }
    else if (type === 'fire') { iconClass = 'fa-fire-extinguisher'; bgColor = '#dc3545'; }
    else if (type === 'aviation') { iconClass = 'fa-helicopter'; bgColor = '#343a40'; }

    const htmlContent = `
        <div style="background-color: ${bgColor}; color: white; width: 32px; height: 32px; display: flex; align-items: center; justify-content: center; border-radius: 50%; border: 2px solid white; box-shadow: 0 2px 5px rgba(0,0,0,0.5);">
            <i class="fas ${iconClass}"></i>
        </div>`;

    return L.divIcon({
        html: htmlContent,
        className: 'custom-vehicle-marker',
        iconSize: [32, 32],
        iconAnchor: [16, 16],
        popupAnchor: [0, -16]
    });
};

window.getRandomLocationInRadius = function (centerLat, centerLng, radiusKm) {
    const radiusInDegrees = radiusKm / 111.3;
    const u = Math.random();
    const v = Math.random();
    const w = radiusInDegrees * Math.sqrt(u);
    const t = 2 * Math.PI * v;
    const deltaLat = w * Math.sin(t);
    const deltaLng = w * Math.cos(t) / Math.cos(centerLat * (Math.PI / 180));
    return { lat: centerLat + deltaLat, lng: centerLng + deltaLng };
};

async function pingVehicleLocation(id, type, lat, lng, statusId) {
    let url = '';
    if (type === 'police') url = `/api/Police/cars/${id}/location`;
    else if (type === 'fire') url = `/api/Fire/firetrucks/${id}/location`;
    else if (type === 'medic') url = `/api/Medical/ambulances/${id}/location`;
    else if (type === 'aviation') url = `/api/Aviation/units/${id}/location`;

    if (url !== '') {
        try {
            await fetch(url, {
                method: 'PUT',
                headers: { 'Authorization': 'Bearer ' + window.jwtToken, 'Content-Type': 'application/json' },
                body: JSON.stringify({ latitude: lat, longitude: lng, status: statusId })
            });
        } catch (e) { console.error("PING Error:", e); }
    }
}

async function drawRoute(startLat, startLon, endLat, endLon) {
    if (!map) return;
    if (currentRouteLayer) map.removeLayer(currentRouteLayer);
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
            console.log(`Dystans: ${distance} km, Czas: ${duration} min`);
        }
    } catch (e) { console.error("Błąd routingu:", e); }
}

window.startAirPatrolSimulation = async function (unitId, currentLat, currentLng, baseLat, baseLng, radiusKm) {
    if (window.activeSimulations[unitId]) return;

    const target = window.getRandomLocationInRadius(baseLat, baseLng, radiusKm || 20);
    let step = 0;
    const totalSteps = 40;
    const latStep = (target.lat - currentLat) / totalSteps;
    const lngStep = (target.lng - currentLng) / totalSteps;

    window.activeSimulations[unitId] = setInterval(async () => {
        if (step >= totalSteps) {
            clearInterval(window.activeSimulations[unitId]);
            delete window.activeSimulations[unitId];
            setTimeout(() => { window.refreshAll(); }, 20000);
            return;
        }

        const curLat = currentLat + (latStep * step);
        const curLng = currentLng + (lngStep * step);

        if (typeof map !== 'undefined') {
            if (window.vehicleMarkers[unitId]) {
                window.vehicleMarkers[unitId].setLatLng([curLat, curLng]);
            }
        }

        if (step % 5 === 0) {
            await pingVehicleLocation(unitId, 'aviation', curLat, curLng, 0);
        }
        step++;
    }, 1500);
};

window.startPatrolSimulation = async function (vehicleId, serviceType, currentLat, currentLng, baseLat, baseLng, radiusKm) {
    if (window.activeSimulations[vehicleId]) return;

    const target = window.getRandomLocationInRadius(baseLat, baseLng, radiusKm || 10);
    const url = `https://router.project-osrm.org/route/v1/driving/${currentLng},${currentLat};${target.lng},${target.lat}?overview=full&geometries=geojson`;

    try {
        const response = await fetch(url);
        const data = await response.json();

        if (data.routes && data.routes.length > 0) {
            const routeCoords = data.routes[0].geometry.coordinates.map(c => [c[1], c[0]]);
            let step = 0;
            const pingFrequency = 10;

            window.activeSimulations[vehicleId] = setInterval(async () => {
                if (step >= routeCoords.length) {
                    clearInterval(window.activeSimulations[vehicleId]);
                    delete window.activeSimulations[vehicleId];
                    setTimeout(() => { window.refreshAll(); }, 20000);
                    return;
                }

                const curLat = routeCoords[step][0];
                const curLng = routeCoords[step][1];

                if (typeof map !== 'undefined') {
                    if (!window.vehicleMarkers[vehicleId]) {
                        window.vehicleMarkers[vehicleId] = L.marker([curLat, curLng], { icon: getIconByService(serviceType) })
                            .addTo(map).bindPopup(`<b>Pojazd na patrolu</b><br>Typ: ${serviceType}`);
                    } else {
                        window.vehicleMarkers[vehicleId].setLatLng([curLat, curLng]);
                    }
                }

                if (step % pingFrequency === 0) {
                    await pingVehicleLocation(vehicleId, serviceType, curLat, curLng, 0);
                }
                step++;
            }, 1000);
        }
    } catch (e) { console.warn("OSRM Patrol Wait:", e); }
};

window.startAirSimulation = async function (unitId, serviceType, startLat, startLng, endLat, endLng, currentStatus = 1) {
    if (window.activeSimulations[unitId]) clearInterval(window.activeSimulations[unitId]);

    const routeCoords = [[startLat, startLng], [endLat, endLng]];
    const routeColor = (currentStatus === 3 || currentStatus === 4) ? '#f39c12' : '#ffffff';

    const flightLine = L.polyline(routeCoords, {
        color: routeColor, weight: 4, opacity: 0.8, dashArray: '5, 10'
    }).addTo(map);

    let step = 0;
    const totalSteps = 15;
    const latStep = (endLat - startLat) / totalSteps;
    const lngStep = (endLng - startLng) / totalSteps;

    window.activeSimulations[unitId] = setInterval(async () => {
        if (step >= totalSteps) {
            clearInterval(window.activeSimulations[unitId]);
            delete window.activeSimulations[unitId];
            if (typeof map !== 'undefined') map.removeLayer(flightLine);

            if (currentStatus === 1) {
                await pingVehicleLocation(unitId, 'aviation', endLat, endLng, 2);
            }
            else if (currentStatus === 3 || currentStatus === 4) {
                try {
                    await fetch(`/api/Aviation/units/${unitId}/free`, { method: 'POST', headers: { 'Authorization': 'Bearer ' + window.jwtToken } });
                } catch (e) { }

                const incidentId = document.getElementById('dispatchTargetIncidentId').value;
                if (incidentId) {
                    const fd = new FormData();
                    fd.append('NewStatus', 'Zakończone');
                    await fetch(`/api/CPR112/Incidents/${incidentId}/status`, {
                        method: 'PUT',
                        headers: { 'Authorization': 'Bearer ' + window.jwtToken },
                        body: fd
                    });
                }
                await pingVehicleLocation(unitId, 'aviation', endLat, endLng, 0);
            }

            window.refreshMapData();
            return;
        }

        const curLat = startLat + (latStep * step);
        const curLng = startLng + (lngStep * step);

        if (typeof map !== 'undefined') {
            if (!window.vehicleMarkers[unitId]) {
                window.vehicleMarkers[unitId] = L.marker([curLat, curLng], { icon: getIconByService('aviation') })
                    .addTo(map).bindPopup(`<b>HEMS / Lotnictwo</b><br>Status: W locie`);
            } else {
                window.vehicleMarkers[unitId].setLatLng([curLat, curLng]);
            }
        }

        if (step % 5 === 0) {
            await pingVehicleLocation(unitId, 'aviation', curLat, curLng, currentStatus);
        }
        step++;
    }, 1000);
};

window.startVehicleSimulation = async function (vehicleId, serviceType, startLat, startLng, endLat, endLng, currentStatus = 1) {
    const url = `https://router.project-osrm.org/route/v1/driving/${startLng},${startLat};${endLng},${endLat}?overview=full&geometries=geojson`;

    try {
        const response = await fetch(url);
        const data = await response.json();

        if (data.routes && data.routes.length > 0) {
            const routeCoords = data.routes[0].geometry.coordinates.map(c => [c[1], c[0]]);
            const routeColor = (currentStatus === 3 || currentStatus === 4) ? '#f39c12' : (serviceType === 'police' ? '#007bff' : (serviceType === 'medic' ? '#28a745' : '#dc3545'));

            const actionRouteLine = L.polyline(routeCoords, {
                color: routeColor, weight: 5, opacity: 0.8, dashArray: '10, 10'
            }).addTo(map);

            let step = 0;
            const pingFrequency = 5;

            if (window.activeSimulations[vehicleId]) clearInterval(window.activeSimulations[vehicleId]);

            window.activeSimulations[vehicleId] = setInterval(async () => {
                if (step >= routeCoords.length) {
                    clearInterval(window.activeSimulations[vehicleId]);
                    delete window.activeSimulations[vehicleId];
                    if (typeof map !== 'undefined') map.removeLayer(actionRouteLine);

                    if (currentStatus === 1) {
                        await pingVehicleLocation(vehicleId, serviceType, endLat, endLng, 2);
                    }
                    else if (currentStatus === 3 || currentStatus === 4) {
                        let freeUrl = '';
                        if (serviceType === 'medic') freeUrl = `/api/Medical/ambulances/${vehicleId}/free`;
                        else if (serviceType === 'police') freeUrl = `/api/Police/cars/${vehicleId}/free`;
                        else if (serviceType === 'fire') freeUrl = `/api/Fire/firetrucks/${vehicleId}/free`;

                        if (freeUrl !== '') {
                            await fetch(freeUrl, { method: 'POST', headers: { 'Authorization': 'Bearer ' + window.jwtToken } });
                        }

                        const incidentId = document.getElementById('dispatchTargetIncidentId').value;
                        if (incidentId) {
                            const fd = new FormData();
                            fd.append('NewStatus', 'Zakończone');
                            await fetch(`/api/CPR112/Incidents/${incidentId}/status`, {
                                method: 'PUT',
                                headers: { 'Authorization': 'Bearer ' + window.jwtToken },
                                body: fd
                            });
                        }
                        if (typeof window.refreshAll === 'function') {
                            await window.refreshAll();
                        }
                        await pingVehicleLocation(vehicleId, serviceType, endLat, endLng, 0);
                    }

                    if (typeof window.refreshAll === 'function') {
                        await window.refreshAll();
                    } else if (typeof window.refreshMapData === 'function') {
                        window.refreshMapData();
                    }
                    return;
                }

                const curLat = routeCoords[step][0];
                const curLng = routeCoords[step][1];

                if (typeof map !== 'undefined') {
                    if (!window.vehicleMarkers[vehicleId]) {
                        window.vehicleMarkers[vehicleId] = L.marker([curLat, curLng], { icon: getIconByService(serviceType) }).addTo(map);
                    } else {
                        window.vehicleMarkers[vehicleId].setLatLng([curLat, curLng]);
                    }
                }

                if (step % pingFrequency === 0) {
                    await pingVehicleLocation(vehicleId, serviceType, curLat, curLng, currentStatus);
                }
                step++;
            }, 1000);
        } else {
            console.warn(`[OSRM] Nie znaleziono drogi z ${startLat},${startLng} do ${endLat},${endLng}`);
        }
    } catch (e) { console.error("Błąd OSRM:", e); }
};

window.buildRoutePath = async function (serviceType, sLat, sLng, eLat, eLng) {
    const straightLine = (steps) => {
        const arr = [];
        for (let i = 0; i <= steps; i++) {
            arr.push([sLat + (eLat - sLat) * (i / steps), sLng + (eLng - sLng) * (i / steps)]);
        }
        return arr;
    };

    if (serviceType === 'aviation') {
        return straightLine(18);
    }

    try {
        const url = `https://router.project-osrm.org/route/v1/driving/${sLng},${sLat};${eLng},${eLat}?overview=full&geometries=geojson`;
        const response = await fetch(url);
        const data = await response.json();
        if (data.routes && data.routes.length > 0) {
            return data.routes[0].geometry.coordinates.map(c => [c[1], c[0]]);
        }
    } catch (e) {
        console.warn("OSRM niedostępny - przejazd po linii prostej:", e);
    }
    return straightLine(20);
};

window.animateUnitAlongPath = function (vehicleId, serviceType, coords, statusCode, color) {
    return new Promise((resolve) => {
        if (!coords || coords.length === 0) { resolve(null); return; }

        let line = null;
        if (typeof map !== 'undefined') {
            line = L.polyline(coords, { color: color || window.routeColorForService(serviceType), weight: 5, opacity: 0.8, dashArray: '10, 10' }).addTo(map);
        }

        let step = 0;
        const pingFrequency = 5;
        const intervalMs = serviceType === 'aviation' ? 800 : 600;

        const timer = setInterval(async () => {
            if (step >= coords.length) {
                clearInterval(timer);
                if (line && typeof map !== 'undefined') map.removeLayer(line);
                const last = coords[coords.length - 1];
                resolve({ lat: last[0], lng: last[1] });
                return;
            }

            const curLat = coords[step][0];
            const curLng = coords[step][1];

            if (typeof map !== 'undefined') {
                if (!window.vehicleMarkers[vehicleId]) {
                    window.vehicleMarkers[vehicleId] = L.marker([curLat, curLng], { icon: getIconByService(serviceType) }).addTo(map);
                } else {
                    window.vehicleMarkers[vehicleId].setLatLng([curLat, curLng]);
                }
            }

            if (step % pingFrequency === 0) {
                await pingVehicleLocation(vehicleId, serviceType, curLat, curLng, statusCode);
            }
            step++;
        }, intervalMs);
    });
};

window.simulateLeg = async function (vehicleId, serviceType, sLat, sLng, eLat, eLng, statusCode, color) {
    const coords = await window.buildRoutePath(serviceType, sLat, sLng, eLat, eLng);
    return await window.animateUnitAlongPath(vehicleId, serviceType, coords, statusCode, color);
};

window.freeUnit = async function (vehicleId, serviceType) {
    let url = '';
    if (serviceType === 'medic') url = `/api/Medical/ambulances/${vehicleId}/free`;
    else if (serviceType === 'police') url = `/api/Police/cars/${vehicleId}/free`;
    else if (serviceType === 'fire') url = `/api/Fire/firetrucks/${vehicleId}/free`;
    else if (serviceType === 'aviation') url = `/api/Aviation/units/${vehicleId}/free`;

    if (url !== '') {
        try {
            await fetch(url, { method: 'POST', headers: { 'Authorization': 'Bearer ' + window.jwtToken } });
        } catch (e) { console.error("Błąd zwalniania jednostki:", e); }
    }
};

window.getUnitBase = function (serviceType, vehicleId) {
    const data = window.dispatchData || {};
    const v = (data.vehicles || {})[vehicleId];
    if (!v) return null;

    let facility = null;
    if (serviceType === 'medic') {
        const hid = v.hospitalId || v.HospitalId;
        facility = (data.hospitals || []).find(h => (h.id || h.Id) === hid);
    } else if (serviceType === 'police') {
        const pid = v.pDepartmentId || v.PDepartmentId;
        facility = (data.polDepts || []).find(d => (d.id || d.Id) === pid);
    } else if (serviceType === 'fire') {
        const fid = v.fDepartmentId || v.FDepartmentId;
        facility = (data.fireDepts || []).find(d => (d.id || d.Id) === fid);
    } else if (serviceType === 'aviation') {
        const aid = v.airbaseId || v.AirbaseId;
        facility = (data.airbases || []).find(a => (a.id || a.Id) === aid);
    }

    if (!facility) return null;
    return {
        lat: parseFloat(facility.latitude || facility.Latitude),
        lng: parseFloat(facility.longitude || facility.Longitude)
    };
};

window.completeReturn = async function (vehicleId, serviceType, baseLat, baseLng) {
    await window.freeUnit(vehicleId, serviceType);
    if (baseLat && baseLng) {
        await pingVehicleLocation(vehicleId, serviceType, baseLat, baseLng, window.VEH_STATUS.InBase);
    }
    delete window.activeSimulations[vehicleId];
    delete window.vehicleIncidentMap[vehicleId];
    if (typeof window.refreshAll === 'function') await window.refreshAll();
};

window.ensureOnSceneModal = function () {
    if (document.getElementById('onSceneModal')) return;
    const wrapper = document.createElement('div');
    wrapper.innerHTML = `
        <div class="modal fade" id="onSceneModal" tabindex="-1" role="dialog" aria-hidden="true">
            <div class="modal-dialog" role="document">
                <div class="modal-content border-warning shadow-lg">
                    <div class="modal-header bg-warning text-dark">
                        <h5 class="modal-title font-weight-bold" id="onScene-title">Jednostka na miejscu</h5>
                        <button type="button" class="close text-dark" data-dismiss="modal"><span>&times;</span></button>
                    </div>
                    <div class="modal-body" id="onScene-body"></div>
                    <div class="modal-footer" id="onScene-footer"></div>
                </div>
            </div>
        </div>`;
    document.body.appendChild(wrapper.firstElementChild);
};

window.openOnSceneModal = async function (vehicleId, serviceType, incidentId, atLat, atLng) {
    window.ensureOnSceneModal();
    window._onSceneCtx = { vehicleId, serviceType, incidentId, atLat, atLng };
    window._onSceneActionTaken = false;

    $('#onSceneModal').off('hidden.bs.modal.onscene').on('hidden.bs.modal.onscene', function () {
        if (!window._onSceneActionTaken) {
            delete window.activeSimulations[vehicleId];
            if (typeof window.refreshAll === 'function') window.refreshAll();
        }
    });

    if (!window.dispatchData || (window.dispatchData.hospitals || []).length === 0) {
        if (typeof window.updateCounters === 'function') {
            try { await window.updateCounters(); } catch (e) { }
        }
    }

    const data = window.dispatchData || {};
    const titleEl = document.getElementById('onScene-title');
    const bodyEl = document.getElementById('onScene-body');
    const footerEl = document.getElementById('onScene-footer');
    const cancelBtn = `<button type="button" class="btn btn-link text-muted" data-dismiss="modal">Anuluj</button>`;

    let airSvc = null;
    if (serviceType === 'aviation') {
        const unit = (data.vehicles || {})[vehicleId] || {};
        airSvc = (unit.airServiceType !== undefined && unit.airServiceType !== null) ? Number(unit.airServiceType) : null;
    }

    const isMedicalDecision = (serviceType === 'medic') || (serviceType === 'aviation' && airSvc === 0);
    const isPoliceGroundDecision = (serviceType === 'police');

    if (isMedicalDecision) {
        const isAir = serviceType === 'aviation';
        titleEl.innerHTML = isAir
            ? '<i class="fas fa-helicopter mr-2"></i> Śmigłowiec HEMS na miejscu — decyzja'
            : '<i class="fas fa-ambulance mr-2"></i> Jednostka na miejscu — decyzja';
        const list = (data.hospitals || []);
        const opts = list.map(h => `<option value="${h.id || h.Id}">${h.name || h.Name}</option>`).join('');
        bodyEl.innerHTML = `
            <p>Jednostka dotarła na miejsce zdarzenia. Czy realizujesz <b>transport pacjenta</b> do szpitala?</p>
            <div class="form-group mb-0">
                <label>Szpital docelowy</label>
                <select id="onScene-target" class="form-control">${opts || '<option value="">Brak szpitali w systemie</option>'}</select>
            </div>`;
        footerEl.innerHTML = `
            <button type="button" class="btn btn-success font-weight-bold" onclick="window.confirmTransport()"><i class="fas fa-hospital"></i> Transport do szpitala</button>
            <button type="button" class="btn btn-secondary" onclick="window.finishOnScene()">Zakończ bez transportu</button>
            ${cancelBtn}`;
    } else if (isPoliceGroundDecision) {
        titleEl.innerHTML = '<i class="fas fa-car-side mr-2"></i> Radiowóz na miejscu — decyzja';
        const list = (data.polDepts || []);
        const opts = list.map(d => `<option value="${d.id || d.Id}">${d.name || d.Name}</option>`).join('');
        bodyEl.innerHTML = `
            <p>Radiowóz dotarł na miejsce. Czy realizujesz <b>przewóz osoby</b> na komisariat / komendę?</p>
            <div class="form-group mb-0">
                <label>Komisariat / Komenda docelowa</label>
                <select id="onScene-target" class="form-control">${opts || '<option value="">Brak komend w systemie</option>'}</select>
            </div>`;
        footerEl.innerHTML = `
            <button type="button" class="btn btn-primary font-weight-bold" onclick="window.confirmTransport()"><i class="fas fa-building"></i> Przewieź na komisariat</button>
            <button type="button" class="btn btn-secondary" onclick="window.finishOnScene()">Zakończ bez przewozu</button>
            ${cancelBtn}`;
    } else if (serviceType === 'aviation' && airSvc === 2) {
        titleEl.innerHTML = '<i class="fas fa-helicopter mr-2"></i> Lotnictwo gaśnicze na miejscu — działania';
        bodyEl.innerHTML = `<p>Maszyna nad miejscem zdarzenia. Wybierz realizowane <b>działanie z powietrza</b>; po jego zakończeniu maszyna wraca do bazy, a zgłoszenie zostaje domknięte (jeśli żadna inna służba nie działa).</p>`;
        footerEl.innerHTML = `
            <button type="button" class="btn btn-danger font-weight-bold" onclick="window.finishOnScene('Gaszenie pożaru z powietrza (zrzut wody)')"><i class="fas fa-fire"></i> Gaszenie z powietrza i powrót</button>
            <button type="button" class="btn btn-outline-danger" onclick="window.finishOnScene('Rozpoznanie pożarowe z powietrza')">Rozpoznanie i powrót</button>
            ${cancelBtn}`;
    } else if (serviceType === 'aviation' && airSvc === 1) {
        titleEl.innerHTML = '<i class="fas fa-helicopter mr-2"></i> Lotnictwo policyjne na miejscu — działania';
        bodyEl.innerHTML = `<p>Maszyna nad miejscem zdarzenia. Wybierz realizowane <b>działanie z powietrza</b>; po jego zakończeniu maszyna wraca do bazy, a zgłoszenie zostaje domknięte (jeśli żadna inna służba nie działa).</p>`;
        footerEl.innerHTML = `
            <button type="button" class="btn btn-primary font-weight-bold" onclick="window.finishOnScene('Poszukiwania / obserwacja z powietrza')"><i class="fas fa-binoculars"></i> Poszukiwania/obserwacja i powrót</button>
            <button type="button" class="btn btn-outline-primary" onclick="window.finishOnScene('Wsparcie z powietrza')">Wsparcie i powrót</button>
            ${cancelBtn}`;
    } else if (serviceType === 'aviation') {
        titleEl.innerHTML = '<i class="fas fa-helicopter mr-2"></i> Statek powietrzny na miejscu';
        bodyEl.innerHTML = `<p>Załoga zakończyła działania nad miejscem zdarzenia. Maszyna wraca do bazy.</p>`;
        footerEl.innerHTML = `
            <button type="button" class="btn btn-danger font-weight-bold" onclick="window.finishOnScene('Działania z powietrza')"><i class="fas fa-undo"></i> Zakończ i wróć do bazy</button>
            ${cancelBtn}`;
    } else {
        titleEl.innerHTML = '<i class="fas fa-fire-extinguisher mr-2"></i> Zastęp na miejscu';
        bodyEl.innerHTML = `<p>Zastęp PSP zakończył działania na miejscu zdarzenia. Straż nie realizuje transportu osób.</p>`;
        footerEl.innerHTML = `
            <button type="button" class="btn btn-danger font-weight-bold" onclick="window.finishOnScene()"><i class="fas fa-undo"></i> Zakończ i wróć do bazy</button>
            ${cancelBtn}`;
    }

    $('#onSceneModal').modal('show');
};

window.getVehicleLabel = function (vehicleId, serviceType) {
    const v = (window.dispatchData?.vehicles || {})[vehicleId];
    if (v) return v.licensePlate || v.LicensePlate || v.callsign || v.Callsign || vehicleId;
    return vehicleId;
};

window.confirmTransport = async function () {
    const ctx = window._onSceneCtx;
    if (!ctx) return;
    const { vehicleId, serviceType, incidentId } = ctx;

    const sel = document.getElementById('onScene-target');
    const targetId = sel ? sel.value : null;
    if (!targetId) { alert("Wybierz placówkę docelową transportu."); return; }

    const data = window.dispatchData || {};
    let target = null;
    if (serviceType === 'police') {
        target = (data.polDepts || []).find(d => (d.id || d.Id) === targetId);
    } else {
        target = (data.hospitals || []).find(h => (h.id || h.Id) === targetId);
    }
    if (!target) { alert("Nie znaleziono wybranej placówki."); return; }

    if (incidentId) {
        try {
            await fetch('/api/Transport/record', {
                method: 'POST',
                headers: {
                    'Authorization': 'Bearer ' + window.jwtToken,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    incidentId: incidentId,
                    vehicleId: vehicleId,
                    vehicleType: serviceType,
                    vehicleLabel: window.getVehicleLabel(vehicleId, serviceType),
                    destinationId: targetId,
                    destinationName: target.name || target.Name,
                    destinationType: serviceType === 'police' ? 'police_station' : 'hospital'
                })
            });
        } catch (e) { console.warn('[Transport] Nie udało się zapisać celu transportu:', e); }
    }

    window._onSceneActionTaken = true;
    $('#onSceneModal').modal('hide');

    const tLat = parseFloat(target.latitude || target.Latitude);
    const tLng = parseFloat(target.longitude || target.Longitude);
    const cur = window.vehicleMarkers[vehicleId] ? window.vehicleMarkers[vehicleId].getLatLng() : { lat: tLat, lng: tLng };
    const base = window.getUnitBase(serviceType, vehicleId);
    const bLat = base ? base.lat : tLat;
    const bLng = base ? base.lng : tLng;

    window.activeSimulations[vehicleId] = true;

    const transportStatus = serviceType === 'police' ? window.VEH_STATUS.Transporting : window.VEH_STATUS.TransportingToHospital;

    await window.simulateLeg(vehicleId, serviceType, cur.lat, cur.lng, tLat, tLng, transportStatus, '#f39c12');
    await pingVehicleLocation(vehicleId, serviceType, tLat, tLng, transportStatus);

    await window.simulateLeg(vehicleId, serviceType, tLat, tLng, bLat, bLng, window.VEH_STATUS.Returning, '#17a2b8');

    await window.completeReturn(vehicleId, serviceType, bLat, bLng);
};

window.finishOnScene = async function (actionLabel) {
    const ctx = window._onSceneCtx;
    if (!ctx) return;
    const { vehicleId, serviceType } = ctx;

    if (actionLabel) console.log(`[DZIAŁANIE] ${serviceType} (${vehicleId}): ${actionLabel}`);

    window._onSceneActionTaken = true;
    $('#onSceneModal').modal('hide');

    const base = window.getUnitBase(serviceType, vehicleId);
    const cur = window.vehicleMarkers[vehicleId] ? window.vehicleMarkers[vehicleId].getLatLng() : null;
    const bLat = base ? base.lat : (cur ? cur.lat : 0);
    const bLng = base ? base.lng : (cur ? cur.lng : 0);

    window.activeSimulations[vehicleId] = true;

    if (cur) {
        await window.simulateLeg(vehicleId, serviceType, cur.lat, cur.lng, bLat, bLng, window.VEH_STATUS.Returning, '#17a2b8');
    }
    await window.completeReturn(vehicleId, serviceType, bLat, bLng);
};

window.updateCounters = async function () {
    if (!window.jwtToken) return;
    const headers = { 'Authorization': 'Bearer ' + window.jwtToken };

    const fetchSafeArray = async (url) => {
        try {
            const res = await fetch(url, { headers });
            if (!res.ok) {
                console.error(`[Błąd API] ${url} zwrócił status: ${res.status}`);
                return [];
            }
            const data = await res.json();
            return Array.isArray(data) ? data : [];
        } catch (e) {
            console.error(`[Błąd Sieci] Nie udało się pobrać z ${url}:`, e);
            return [];
        }
    };

    try {
        const [p, f, m, air, polDepts, fireDepts, hospitals, airbases] = await Promise.all([
            fetchSafeArray('/api/Police/cars'),
            fetchSafeArray('/api/Fire/firetrucks'),
            fetchSafeArray('/api/Medical/ambulances'),
            fetchSafeArray('/api/Aviation/units'),
            fetchSafeArray('/api/Police/departments'),
            fetchSafeArray('/api/Fire/departments'),
            fetchSafeArray('/api/Medical/hospitals'),
            fetchSafeArray('/api/Aviation/airbases')
        ]);

        if (document.getElementById('status-police')) document.getElementById('status-police').textContent = `${p.filter(c => c.isAvailable !== false).length} / ${p.length}`;
        if (document.getElementById('status-fire')) document.getElementById('status-fire').textContent = `${f.filter(c => c.isAvailable !== false).length} / ${f.length}`;
        if (document.getElementById('status-medic')) document.getElementById('status-medic').textContent = `${m.filter(c => c.isAvailable !== false).length} / ${m.length}`;
        if (document.getElementById('status-aviation')) document.getElementById('status-aviation').textContent = `${air.filter(c => c.isAvailable !== false).length} / ${air.length}`;

        const allVehicles = [
            ...p.map(v => ({ ...v, serviceType: 'police' })),
            ...f.map(v => ({ ...v, serviceType: 'fire' })),
            ...m.map(v => ({ ...v, serviceType: 'medic' })),
            ...air.map(v => ({ ...v, airServiceType: (v.serviceType !== undefined ? v.serviceType : v.ServiceType), serviceType: 'aviation' }))
        ];

        window.dispatchData = { hospitals, polDepts, fireDepts, airbases, vehicles: {} };
        allVehicles.forEach(v => {
            const vid = v.id || v.Id;
            if (vid) window.dispatchData.vehicles[vid] = v;
        });

        if (typeof map !== 'undefined' && window.vehicleMarkers) {
            allVehicles.forEach(v => {
                const id = v.id || v.Id;
                const lat = parseFloat(v.latitude || v.Latitude);
                const lng = parseFloat(v.longitude || v.Longitude);

                const isAvail = (v.isAvailable !== undefined) ? v.isAvailable : ((v.IsAvailable !== undefined) ? v.IsAvailable : true);
                const currentStatus = (v.status !== undefined) ? v.status : ((v.Status !== undefined) ? v.Status : 0);
                const hId = v.hospitalId || v.HospitalId;

                if (!isNaN(lat) && !isNaN(lng) && lat !== 0 && lng !== 0) {
                    const statusText = isAvail ? 'W bazie / Patrol' : `Akcja (Status: ${currentStatus})`;
                    const plate = v.licensePlate || v.LicensePlate || v.callsign || v.Callsign;

                    let popupHtml = `<b>${plate}</b><br>Status: ${statusText}`;
                    if (!isAvail && currentStatus === window.VEH_STATUS.OnScene) {
                        const incId = window.vehicleIncidentMap[id] || '';
                        popupHtml += `<br><button class="btn btn-sm btn-warning mt-2" onclick="window.openOnSceneModal('${id}','${v.serviceType}','${incId}', ${lat}, ${lng})"><i class="fas fa-hand-paper"></i> Działania na miejscu</button>`;
                    }

                    if (!window.vehicleMarkers[id]) {
                        window.vehicleMarkers[id] = L.marker([lat, lng], { icon: getIconByService(v.serviceType) })
                            .addTo(map).bindPopup(popupHtml);
                    } else if (!window.activeSimulations[id]) {
                        window.vehicleMarkers[id].setLatLng([lat, lng]);
                        window.vehicleMarkers[id].setPopupContent(popupHtml);
                    }

                    if (!isAvail && (currentStatus === 3 || currentStatus === 4) && !window.activeSimulations[id]) {
                        let targetLat = 0; let targetLng = 0;

                        if (v.serviceType === 'aviation') {
                            const airb = airbases.find(a => (a.id || a.Id) === (v.airbaseId || v.AirbaseId));
                            if (airb) { targetLat = airb.latitude || airb.Latitude; targetLng = airb.longitude || airb.Longitude; }
                        }
                        else if (v.serviceType === 'medic') {
                            const hosp = hospitals.find(h => (h.id || h.Id) === hId);
                            if (hosp) { targetLat = parseFloat(hosp.latitude || hosp.Latitude); targetLng = parseFloat(hosp.longitude || hosp.Longitude); }
                        }
                        else if (v.serviceType === 'police') {
                            const police = polDepts.find(pd => (pd.id || pd.Id) === (v.pDepartmentId || v.PDepartmentId));
                            if (police) { targetLat = parseFloat(police.latitude || police.Latitude); targetLng = parseFloat(police.longitude || police.Longitude); }
                        }
                        else if (v.serviceType === 'fire') {
                            const fire = fireDepts.find(fd => (fd.id || fd.Id) === (v.fDepartmentId || v.FDepartmentId));
                            if (fire) { targetLat = parseFloat(fire.latitude || fire.Latitude); targetLng = parseFloat(fire.longitude || fire.Longitude); }
                        }

                        if (targetLat !== 0 && targetLng !== 0) {
                            if (v.serviceType === 'aviation') window.startAirSimulation(id, v.serviceType, lat, lng, targetLat, targetLng, currentStatus);
                            else window.startVehicleSimulation(id, v.serviceType, lat, lng, targetLat, targetLng, currentStatus);
                        }
                    }
                }
            });
        }

        p.filter(c => c.isAvailable !== false).forEach(car => {
            const dept = polDepts.find(d => (d.id || d.Id) === (car.pDepartmentId || car.PDepartmentId));
            if (dept && !window.activeSimulations[car.id || car.Id]) {
                const baseLat = dept.latitude || dept.Latitude;
                const baseLng = dept.longitude || dept.Longitude;
                window.startPatrolSimulation(car.id || car.Id, 'police', car.latitude || car.Latitude, car.longitude || car.Longitude, baseLat, baseLng, 10);
            }
        });

        air.filter(c => c.isAvailable !== false).forEach(heli => {
            const base = airbases.find(b => (b.id || b.Id) === (heli.airbaseId || heli.AirbaseId));
            if (base && !window.activeSimulations[heli.id || heli.Id]) {
                const baseLat = base.latitude || base.Latitude;
                const baseLng = base.longitude || base.Longitude;
                window.startAirPatrolSimulation(heli.id || heli.Id, heli.latitude || heli.Latitude, heli.longitude || heli.Longitude, baseLat, baseLng, 25);
            }
        });

    } catch (e) {
        console.error("Krytyczny błąd aktualizacji interfejsu dyspozytora:", e);
    }
};

window.toggleHelipadOptions = function () {
    const type = document.getElementById('centerType').value;
    if (type === 'Airbase') {
        document.getElementById('helipadOptionGroup').classList.add('d-none');
        document.getElementById('regionGroup').classList.add('d-none');
        document.getElementById('airbaseOptionGroup').classList.remove('d-none');
    } else {
        document.getElementById('helipadOptionGroup').classList.remove('d-none');
        document.getElementById('regionGroup').classList.remove('d-none');
        document.getElementById('airbaseOptionGroup').classList.add('d-none');
    }
};

window.registerNewCenter = async function () {
    const nameVal = document.getElementById('centerName').value;
    const latVal = document.getElementById('centerLat').value;
    const lngVal = document.getElementById('centerLng').value;
    const radiusVal = document.getElementById('centerRadius').value;
    const type = document.getElementById('centerType').value;

    if (!nameVal || !latVal || !lngVal) {
        alert("Wypełnij nazwę i wskaż lokalizację klikając na mapie!");
        return;
    }

    let url = '';
    let dto = {};
    const hasHelipad = document.getElementById('hasHelipad') ? document.getElementById('hasHelipad').checked : false;

    if (type === 'Airbase') {
        url = '/api/Aviation/airbases';
        dto = {
            name: nameVal,
            icaoCode: document.getElementById('centerIcao').value || "Brak",
            serviceType: parseInt(document.getElementById('centerAirService').value),
            latitude: parseFloat(latVal),
            longitude: parseFloat(lngVal)
        };
    } else {
        const regionVal = document.getElementById('centerRegion').value || "Brak Danych";
        dto = {
            name: nameVal, region: regionVal, address: regionVal, district: regionVal,
            latitude: parseFloat(latVal), longitude: parseFloat(lngVal),
            operatingRadiusKm: parseFloat(radiusVal), hasHelipad: hasHelipad
        };

        if (type === 'Hospital') url = '/api/Medical/hospitals';
        else if (type === 'Police') url = '/api/Police/departments';
        else if (type === 'Fire') url = '/api/Fire/departments';
        else url = '/api/Enc';
    }

    try {
        const response = await fetch(url, {
            method: 'POST',
            headers: { 'Authorization': 'Bearer ' + window.jwtToken, 'Content-Type': 'application/json' },
            body: JSON.stringify(dto)
        });

        if (response.ok) {
            if (type !== 'Airbase' && hasHelipad) {
                let airServiceType = 0;
                if (type === 'Police') airServiceType = 1;
                if (type === 'Fire') airServiceType = 2;

                const airbaseDto = {
                    name: `Lądowisko: ${nameVal}`,
                    icaoCode: "HLPD",
                    serviceType: airServiceType,
                    latitude: parseFloat(latVal), longitude: parseFloat(lngVal)
                };

                await fetch('/api/Aviation/airbases', {
                    method: 'POST',
                    headers: { 'Authorization': 'Bearer ' + window.jwtToken, 'Content-Type': 'application/json' },
                    body: JSON.stringify(airbaseDto)
                });
            }

            alert("Sukces! Placówka została dodana do systemu.");
            document.getElementById('createCenterForm').reset();
            window.toggleHelipadOptions();

            if (typeof window.loadCenters === 'function') await window.loadCenters();
            if (typeof window.loadCentersToSelect === 'function') await window.loadCentersToSelect();
            if (typeof window.refreshMapData === 'function') window.refreshMapData();
        } else {
            alert(`Błąd serwera: ${response.status}. Szczegóły: ${await response.text()}`);
        }
    } catch (e) { alert("Błąd krytyczny połączenia z serwerem."); }
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

window.openDispatchModal = async function (type, targetIncidentId = null, incLat = null, incLng = null) {
    document.getElementById('dispatchTargetIncidentId').value = targetIncidentId || '';
    const tbody = document.getElementById('available-units-list');
    tbody.innerHTML = '<tr><td colspan="4" class="text-center p-4">Ładowanie... <i class="fas fa-spinner fa-spin"></i></td></tr>';

    const titleEl = document.getElementById('dispatch-modal-title');
    const typeCol = document.getElementById('dispatch-col-type');
    const headerEl = document.getElementById('dispatch-modal-header');

    let apiUrl = '';
    let unitTypeMap = {};

    if (type === 'police') {
        titleEl.innerHTML = '<i class="fas fa-car-side mr-2"></i> Dysponowanie Radiowozu';
        headerEl.className = 'modal-header bg-primary text-white';
        typeCol.textContent = 'Typ';
        apiUrl = '/api/Police/cars';
    } else if (type === 'fire') {
        titleEl.innerHTML = '<i class="fas fa-fire-extinguisher mr-2"></i> Dysponowanie Wozu PSP';
        headerEl.className = 'modal-header bg-danger text-white';
        typeCol.textContent = 'Typ';
        apiUrl = '/api/Fire/firetrucks';
    } else if (type === 'medic') {
        titleEl.innerHTML = '<i class="fas fa-ambulance mr-2"></i> Dysponowanie Karetki';
        headerEl.className = 'modal-header bg-success text-white';
        typeCol.textContent = 'Typ Karetki';
        apiUrl = '/api/Medical/ambulances/available';
        unitTypeMap = { 0: "S", 1: "T", 2: "P", 3: "N" };
    } else if (type === 'aviation') {
        titleEl.innerHTML = '<i class="fas fa-helicopter mr-2"></i> Dysponowanie Lotnictwa';
        headerEl.className = 'modal-header bg-dark text-white';
        typeCol.textContent = 'Typ Maszyny';
        apiUrl = '/api/Aviation/units';
        unitTypeMap = { 0: "Śmigłowiec", 1: "Samolot" };
    }

    $('#universalDispatchModal').modal('show');

    const wContainer = document.getElementById('weather-widget-container');
    if (wContainer) wContainer.classList.add('d-none');

    if (incLat !== null && incLng !== null && wContainer) {
        fetch(`/api/weather/incident/${incLat}/${incLng}`, { headers: { 'Authorization': 'Bearer ' + window.jwtToken } })
            .then(res => res.json())
            .then(data => {
                document.getElementById('w-icon').src = data.ground.iconUrl;
                document.getElementById('w-temp').textContent = data.ground.temperature;
                document.getElementById('w-desc').textContent = data.ground.description;

                let groundAlerts = "";
                if (data.ground.isSlippery) groundAlerts += `<span class="badge badge-warning text-dark mr-1"><i class="fas fa-exclamation-triangle"></i> Ślisko</span>`;
                if (data.ground.isFoggy) groundAlerts += `<span class="badge badge-secondary mr-1"><i class="fas fa-smog"></i> Mgła</span>`;
                if (data.ground.isStormy) groundAlerts += `<span class="badge badge-danger mr-1"><i class="fas fa-bolt"></i> Burza</span>`;
                document.getElementById('w-ground-alerts').innerHTML = groundAlerts || '<span class="text-success"><i class="fas fa-check-circle"></i> Warunki dobre</span>';

                const fr = data.aviation.flightRules;
                const rulesEl = document.getElementById('w-flight-rules');
                rulesEl.textContent = fr;
                document.getElementById('w-station').textContent = `(Stacja: ${data.aviation.stationIcao || 'Brak'})`;

                const flightAlerts = document.getElementById('w-flight-alerts');
                if (fr === 'VFR') {
                    rulesEl.className = 'badge badge-success';
                    flightAlerts.innerHTML = '<span class="text-success"><i class="fas fa-check"></i> Można dysponować HEMS</span>';
                } else if (fr === 'MVFR') {
                    rulesEl.className = 'badge badge-warning text-dark';
                    flightAlerts.innerHTML = '<span class="text-warning"><i class="fas fa-exclamation"></i> Uwaga: Brzegowe warunki lotne</span>';
                } else if (fr === 'IFR' || fr === 'LIFR') {
                    rulesEl.className = 'badge badge-danger';
                    flightAlerts.innerHTML = '<span class="text-danger font-weight-bold"><i class="fas fa-ban"></i> Zakaz lotów VFR (IFR/LIFR)</span>';
                } else {
                    rulesEl.className = 'badge badge-secondary';
                    flightAlerts.innerHTML = '<span class="text-muted"><i class="fas fa-question-circle"></i> Brak danych ze stacji AVWX. Decyzja należy do pilota.</span>';
                }

                wContainer.classList.remove('d-none');
            })
            .catch(e => console.error("Błąd ładowania widgetu meteo:", e));
    }

    try {
        const res = await fetch(apiUrl, { headers: { 'Authorization': 'Bearer ' + window.jwtToken } });
        const units = await res.json();

        tbody.innerHTML = '';
        const availableUnits = units.filter(u => u.isAvailable !== false);

        if (availableUnits.length === 0) {
            tbody.innerHTML = '<tr><td colspan="4" class="text-center p-4">Brak wolnych jednostek w rejonie.</td></tr>';
            return;
        }

        availableUnits.forEach(u => {
            const id = u.id || u.Id;
            const plate = u.licensePlate || u.LicensePlate || u.callsign || u.Callsign;

            let uType = "Wóz";
            if (type === 'medic') uType = u.type !== undefined ? (unitTypeMap[u.type] || u.type) : "Karetka";
            else if (type === 'police') uType = "Radiowóz";
            else if (type === 'fire') uType = "Wóz Bojowy";
            else if (type === 'aviation') uType = u.type !== undefined ? (unitTypeMap[u.type] || u.type) : "Statek Powietrzny";

            let originCol = 'Baza macierzysta';
            if (type === 'aviation') {
                const airSvc = (u.serviceType !== undefined ? u.serviceType : u.ServiceType);
                const svcLabel = airSvc === 0 ? 'HEMS (med.)' : (airSvc === 1 ? 'Policja' : (airSvc === 2 ? 'Straż' : 'Lotnictwo'));
                const baseName = u.airbaseName || u.AirbaseName || 'Baza lotnicza';
                const pilot = u.pilotName || u.PilotName;
                originCol = `<div><i class="fas fa-warehouse text-muted mr-1"></i><b>${baseName}</b></div>`
                    + `<small class="badge badge-info">${svcLabel}</small>`
                    + (pilot ? ` <small class="text-muted">pilot: ${pilot}</small>` : ` <small class="text-danger">brak pilota</small>`);
            }

            let unitLat = u.latitude || u.Latitude || 0;
            let unitLng = u.longitude || u.Longitude || 0;
            if (window.vehicleMarkers[id]) {
                const ll = window.vehicleMarkers[id].getLatLng();
                unitLat = ll.lat;
                unitLng = ll.lng;
            }

            tbody.insertAdjacentHTML('beforeend', `
                <tr class="amb-row">
                    <td class="align-middle"><b>${plate}</b></td>
                    <td class="align-middle"><span class="badge badge-secondary">${uType}</span></td>
                    <td class="align-middle">${originCol}</td>
                    <td class="text-right align-middle">
                        ${(unitLat !== 0 && incLat !== null && type !== 'aviation') ?
                    `<button class="btn btn-sm btn-info shadow-sm mr-1" title="Podgląd dojazdu" onclick="window.drawRoute(${unitLat}, ${unitLng}, ${incLat}, ${incLng})">
                                <i class="fas fa-route"></i>
                            </button>` : ''
                }
                        <button class="btn btn-sm btn-dark font-weight-bold shadow-sm" onclick="window.dispatchUnit('${type}', '${id}', ${unitLat}, ${unitLng}, ${incLat}, ${incLng})">WYŚLIJ</button>
                    </td>
                </tr>`);
        });
    } catch (e) {
        tbody.innerHTML = '<tr><td colspan="4" class="text-center text-danger">Błąd ładowania danych z serwera.</td></tr>';
        console.error("Szczegóły błędu dysponowania:", e);
    }
};

window.dispatchUnit = async function (type, targetId, startLat, startLng, endLat, endLng) {
    let incidentId = document.getElementById('dispatchTargetIncidentId').value;
    if (!incidentId) incidentId = prompt("Podaj ID Zgłoszenia:");
    if (!incidentId) return;

    let url = '';
    if (type === 'police') url = `/api/Police/cars/${targetId}/assign/${incidentId}`;
    else if (type === 'fire') url = `/api/Fire/firetrucks/${targetId}/assign/${incidentId}`;
    else if (type === 'medic') url = `/api/Medical/ambulances/${targetId}/assign/${incidentId}`;
    else if (type === 'aviation') url = `/api/Aviation/units/${targetId}/assign/${incidentId}`;

    try {
        const res = await fetch(url, { method: 'PUT', headers: { 'Authorization': 'Bearer ' + window.jwtToken } });
        if (res.ok) {
            $('#universalDispatchModal').modal('hide');

            if (window.activeSimulations[targetId] && window.activeSimulations[targetId] !== true) {
                clearInterval(window.activeSimulations[targetId]);
            }
            delete window.activeSimulations[targetId];

            let realStartLat = startLat;
            let realStartLng = startLng;
            if (window.vehicleMarkers[targetId]) {
                const ll = window.vehicleMarkers[targetId].getLatLng();
                realStartLat = ll.lat;
                realStartLng = ll.lng;
            }

            window.vehicleIncidentMap[targetId] = incidentId;

            if (realStartLat && realStartLng && endLat && endLng) {
                console.log(`[ACTION] Jednostka ${targetId} wyjeżdża do zgłoszenia z pozycji ${realStartLat}, ${realStartLng}`);

                window.activeSimulations[targetId] = true;

                const enRouteColor = window.routeColorForService(type);
                await window.simulateLeg(targetId, type, realStartLat, realStartLng, endLat, endLng, window.VEH_STATUS.EnRoute, enRouteColor);

                await pingVehicleLocation(targetId, type, endLat, endLng, window.VEH_STATUS.OnScene);

                if (typeof window.refreshAll === 'function') await window.refreshAll();

                window.openOnSceneModal(targetId, type, incidentId, endLat, endLng);
            } else {
                window.refreshAll();
            }
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

        if (!latVal || !lngVal) { alert("Kliknij na mapie, aby wyznaczyć dokładną lokalizację zdarzenia!"); return; }
        if (!typeSelect.value) { alert("Wybierz typ zdarzenia!"); return; }

        btn.disabled = true;
        const formData = new FormData();
        formData.append('Description', document.getElementById('incDescription').value);
        formData.append('SeverityLevelId', parseInt(document.getElementById('incSeverity').value));
        formData.append('IncidentTypeId', parseInt(typeSelect.value));
        formData.append('Latitude', latVal);
        formData.append('Longitude', lngVal);

        //if (window.currentOperatorId) formData.append('OperatorId', window.currentOperatorId);

        const fileInput = document.getElementById('incPhoto');
        if (fileInput && fileInput.files[0]) formData.append('photo', fileInput.files[0]);

        try {
            const response = await fetch('/api/CPR112/Incidents', { method: 'POST', headers: { 'Authorization': 'Bearer ' + window.jwtToken }, body: formData });
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
        } catch (err) { console.error("Błąd sieci:", err); }
        finally { btn.disabled = false; }
    });

    document.getElementById('changeStatusForm')?.addEventListener('submit', async function (e) {
        e.preventDefault();
        const id = document.getElementById('editIncidentId').value;
        const fd = new FormData();
        fd.append('NewStatus', document.getElementById('editIncidentStatus').value);
        fd.append('NewSeverityLevelId', document.getElementById('editIncidentPriority').value);

        //if (window.currentOperatorId) {
        //    fd.append('OperatorId', window.currentOperatorId);
        //}

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
        const res = await fetch('/api/CPR112/Incidents/IncidentTypes', { headers: { 'Authorization': 'Bearer ' + window.jwtToken } });
        if (res.ok) {
            const types = await res.json();
            const select = document.getElementById('incType');
            if (select) {
                select.innerHTML = '<option value="">Wybierz typ...</option>' + types.map(t => `<option value="${t.id}">${t.name}</option>`).join('');
            }
        }
    } catch (e) { console.error(e); }
};

window.loadIncidentStats = async function () {
    try {
        const res = await fetch('/api/CPR112/Incidents/stats/summary', { headers: { 'Authorization': 'Bearer ' + window.jwtToken } });
        if (res.ok) {
            const data = await res.json();
            const container = document.getElementById('stats-summary');
            if (container) {
                container.innerHTML = data.map(s => `<span class="badge badge-secondary p-2 shadow-sm" style="font-size: 14px;">${s.name}: <span class="text-warning">${s.count}</span></span>`).join(' ');
            }
        }
    } catch (e) { console.error(e); }
};

window.showHistory = async function (id) {
    try {
        const res = await fetch(`/api/CPR112/Incidents/${id}/history`, { headers: { 'Authorization': 'Bearer ' + window.jwtToken } });
        if (res.ok) {
            const data = await res.json();
            const container = document.getElementById('history-timeline');
            if (!container) return;

            if (data.length === 0) {
                container.innerHTML = `<div><i class="fas fa-info bg-secondary"></i><div class="timeline-item bg-dark border-secondary"><div class="timeline-body text-white text-center p-3">Brak zarejestrowanych zmian dla tego zgłoszenia.</div></div></div>`;
            } else {
                container.innerHTML = data.map(h => {
                    const dateObj = new Date(h.changedAt);
                    const timeStr = dateObj.toLocaleTimeString('pl-PL', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
                    const dateStr = dateObj.toLocaleDateString('pl-PL');

                    let iconColor = 'bg-info'; let textColor = 'text-info'; let iconClass = 'fa-exchange-alt';
                    if (h.newStatus === 'W toku') { iconColor = 'bg-primary'; textColor = 'text-primary'; iconClass = 'fa-play'; }
                    if (h.newStatus === 'Zakończone') { iconColor = 'bg-success'; textColor = 'text-success'; iconClass = 'fa-check'; }
                    if (h.newStatus === 'Fałszywy alarm') { iconColor = 'bg-warning'; textColor = 'text-warning'; iconClass = 'fa-exclamation-triangle'; }
                    if (h.newStatus.includes('powrócił') || h.newStatus.includes('zakończył działania')) { iconColor = 'bg-secondary'; textColor = 'text-light'; iconClass = 'fa-undo'; }

                    const operatorInfo = h.operatorName ? `<span class="badge badge-secondary ml-2"><i class="fas fa-user-check"></i> ${h.operatorName}</span>` : '<span class="badge badge-secondary ml-2"><i class="fas fa-robot"></i> System</span>';

                    return `
                    <div>
                        <i class="fas ${iconClass} ${iconColor} text-white"></i>
                        <div class="timeline-item bg-dark border-secondary shadow-sm" style="border: 1px solid #6c757d;">
                            <span class="time text-light mt-2 mr-2"><i class="fas fa-clock"></i> ${timeStr} <small>(${dateStr})</small></span>
                            <h3 class="timeline-header border-secondary text-white border-bottom-0">
                                <b class="${textColor}">Aktualizacja Statusu</b> ${operatorInfo}
                            </h3>
                            <div class="timeline-body pt-0 text-white">
                                Stan zmieniony z <span class="text-secondary" style="text-decoration: line-through;">${h.oldStatus}</span> 
                                <i class="fas fa-arrow-right mx-2 text-secondary"></i> <b class="${textColor}">${h.newStatus}</b>
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
        const response = await fetch('/api/CPR112/Incidents', { headers: { 'Authorization': 'Bearer ' + window.jwtToken } });
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
                            <button class="btn btn-xs btn-outline-light ml-1 mr-1" onclick="window.showIncidentUnits('${inc.id}', '${inc.incidentNumber || ''}')" title="Przypisane jednostki i obsada"><i class="fas fa-users"></i></button>
                            <button class="btn btn-xs btn-outline-light mr-1" onclick="window.showHistory('${inc.id}')" title="Historia logów"><i class="fas fa-history"></i></button>
                            <button class="btn btn-xs btn-info" onclick="window.openEditModal('${inc.id}', '${inc.status}', '${inc.severity}')" title="Edytuj status"><i class="fas fa-edit"></i></button>
                            <button class="btn btn-xs btn-primary ml-1" onclick="window.openDispatchModal('police', '${inc.id}', ${inc.latitude}, ${inc.longitude})" title="Wyślij Policję"><i class="fas fa-shield-alt"></i></button>
                            <button class="btn btn-xs btn-danger ml-1" onclick="window.openDispatchModal('fire', '${inc.id}', ${inc.latitude}, ${inc.longitude})" title="Wyślij Straż"><i class="fas fa-fire"></i></button>
                            <button class="btn btn-xs btn-success ml-1" onclick="window.openDispatchModal('medic', '${inc.id}', ${inc.latitude}, ${inc.longitude})" title="Wyślij Medyków"><i class="fas fa-ambulance"></i></button>
                            <button class="btn btn-xs btn-dark ml-1" onclick="window.openDispatchModal('aviation', '${inc.id}', ${inc.latitude}, ${inc.longitude})" title="Wyślij Lotnictwo (HEMS/Policja)"><i class="fas fa-helicopter"></i></button>
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

window.showIncidentUnits = async function (incidentId, incidentNumber) {
    if (!document.getElementById('incidentUnitsModal')) {
        const wrap = document.createElement('div');
        wrap.innerHTML = `
        <div class="modal fade" id="incidentUnitsModal" tabindex="-1" role="dialog" aria-hidden="true">
            <div class="modal-dialog modal-lg" role="document">
                <div class="modal-content">
                    <div class="modal-header bg-dark text-white">
                        <h5 class="modal-title"><i class="fas fa-users"></i> Przypisane jednostki <span id="iu-num"></span></h5>
                        <button type="button" class="close text-white" data-dismiss="modal"><span>&times;</span></button>
                    </div>
                    <div class="modal-body" id="iu-body">Ładowanie...</div>
                </div>
            </div>
        </div>`;
        document.body.appendChild(wrap.firstElementChild);
    }
    document.getElementById('iu-num').textContent = incidentNumber ? '— ' + incidentNumber : '';
    const body = document.getElementById('iu-body');
    body.innerHTML = 'Ładowanie...';
    $('#incidentUnitsModal').modal('show');

    const statusMap = { 0: 'W bazie', 1: 'W drodze', 2: 'Na miejscu', 3: 'Transport', 4: 'Powrót', 5: 'Transport do szpitala' };
    try {
        const res = await fetch(`/api/CPR112/Incidents/${incidentId}/units`, { headers: { 'Authorization': 'Bearer ' + window.jwtToken } });
        if (!res.ok) { body.innerHTML = '<p class="text-danger">Brak dostępu lub błąd serwera.</p>'; return; }
        const data = await res.json();
        const units = Array.isArray(data) ? data : (data.units || []);
        const transports = Array.isArray(data) ? [] : (data.transports || []);
        if (!units.length) { body.innerHTML = '<p class="text-muted">Do tego zgłoszenia nie przypisano jeszcze żadnych jednostek.</p>'; return; }
        let html = `
            <table class="table table-sm table-striped">
                <thead><tr><th>Służba</th><th>Jednostka</th><th>Dowódca/Kierowca</th><th>Obsada</th><th>Status</th></tr></thead>
                <tbody>
                ${units.map(u => `
                    <tr class="${u.active ? '' : 'text-muted'}">
                        <td><b>${u.service}</b></td>
                        <td>${u.vehicle}</td>
                        <td>${u.commander}</td>
                        <td>${(u.crew && u.crew.length) ? u.crew.join(', ') : '<span class="text-muted">—</span>'}</td>
                        <td>${u.active ? `<span class="badge bg-success">Aktywna</span> ${statusMap[u.status] || ''}` : '<span class="badge bg-secondary">Zakończona</span>'}</td>
                    </tr>`).join('')}
                </tbody>
            </table>`;
        if (transports.length) {
            html += `<h6 class="mt-3"><i class="fas fa-hospital"></i> Transporty</h6><ul class="mb-0">` +
                transports.map(t => `<li><b>${t.destinationName}</b> — ${t.vehicleLabel} (${new Date(t.transportedAt).toLocaleString('pl-PL')})</li>`).join('') +
                `</ul>`;
        }
        body.innerHTML = html;
    } catch (e) { body.innerHTML = '<p class="text-danger">Błąd połączenia.</p>'; }
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