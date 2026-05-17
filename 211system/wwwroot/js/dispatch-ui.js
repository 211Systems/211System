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
            if (type === 'medic') {
                uType = u.type !== undefined ? (unitTypeMap[u.type] || u.type) : "Karetka";
            } else if (type === 'police') {
                uType = "Radiowóz";
            } else if (type === 'fire') {
                uType = "Wóz Bojowy";
            } else if (type === 'aviation') {
                uType = u.type !== undefined ? (unitTypeMap[u.type] || u.type) : "Statek Powietrzny";
            }

            const unitLat = u.latitude || u.Latitude || 0;
            const unitLng = u.longitude || u.Longitude || 0;

            tbody.insertAdjacentHTML('beforeend', `
                <tr class="amb-row">
                    <td class="align-middle"><b>${plate}</b></td>
                    <td class="align-middle"><span class="badge badge-secondary">${uType}</span></td>
                    <td class="align-middle">Baza macierzysta</td>
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

            if (window.activeSimulations[targetId]) {
                clearInterval(window.activeSimulations[targetId]);
                delete window.activeSimulations[targetId];
            }

            if (startLat && startLng && endLat && endLng) {
                console.log(`[ACTION] Jednostka ${targetId} wyjeżdża do zgłoszenia!`);
                if (type === 'aviation') {
                    window.startAirSimulation(targetId, type, startLat, startLng, endLat, endLng);
                } else {
                    window.startVehicleSimulation(targetId, type, startLat, startLng, endLat, endLng);
                }
            }

            window.refreshAll();
        } else {
            const err = await res.json();
            alert("Błąd dysponowania: " + (err.message || "Wystąpił błąd bazy danych."));
        }
    } catch (e) { alert("Błąd sieci!"); }
};

window.activeSimulations = {};
window.vehicleMarkers = {};

const iconPoliceCar = new L.Icon({ iconUrl: 'https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-2x-violet.png', shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/0.7.7/images/marker-shadow.png', iconSize: [16, 26], iconAnchor: [8, 26] });
const iconAmbulance = new L.Icon({ iconUrl: 'https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-2x-green.png', shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/0.7.7/images/marker-shadow.png', iconSize: [16, 26], iconAnchor: [8, 26] });
const iconFireTruck = new L.Icon({ iconUrl: 'https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-2x-orange.png', shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/0.7.7/images/marker-shadow.png', iconSize: [16, 26], iconAnchor: [8, 26] });
const iconAviation = new L.Icon({ iconUrl: 'https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-2x-grey.png', shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/0.7.7/images/marker-shadow.png', iconSize: [16, 26], iconAnchor: [8, 26] });

function getIconByService(type) {
    if (type === 'police') return iconPoliceCar;
    if (type === 'medic') return iconAmbulance;
    if (type === 'aviation') return iconAviation;
    return iconFireTruck;
}

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

                        await pingVehicleLocation(vehicleId, serviceType, endLat, endLng, 0);
                    }

                    window.refreshMapData();
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
        step++;
    }, 1000);
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
    // else if (type === 'aviation') url = `/api/Aviation/units/${id}/location`;

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
        const [p, f, m, air, polDepts, fireDepts, hospitals] = await Promise.all([
            fetch('/api/Police/cars', { headers }).then(r => r.json()),
            fetch('/api/Fire/firetrucks', { headers }).then(r => r.json()),
            fetch('/api/Medical/ambulances', { headers }).then(r => r.json()),
            fetch('/api/Aviation/units', { headers }).then(r => r.json().catch(() => [])),
            fetch('/api/Police/departments', { headers }).then(r => r.json()),
            fetch('/api/Fire/departments', { headers }).then(r => r.json()),
            fetch('/api/Medical/hospitals', { headers }).then(r => r.json())
        ]);

        document.getElementById('status-police').textContent = `${p.filter(c => c.isAvailable !== false && c.IsAvailable !== false).length} / ${p.length}`;
        document.getElementById('status-fire').textContent = `${f.filter(c => c.isAvailable !== false && c.IsAvailable !== false).length} / ${f.length}`;
        document.getElementById('status-medic').textContent = `${m.filter(c => c.isAvailable !== false && c.IsAvailable !== false).length} / ${m.length}`;

        if (typeof map !== 'undefined' && window.vehicleMarkers) {
            const allVehicles = [
                ...p.map(v => ({ ...v, serviceType: 'police' })),
                ...f.map(v => ({ ...v, serviceType: 'fire' })),
                ...m.map(v => ({ ...v, serviceType: 'medic' })),
                ...air.map(v => ({ ...v, serviceType: 'aviation' }))
            ];

            allVehicles.forEach(v => {
                const id = v.id || v.Id;
                const lat = parseFloat(v.latitude || v.Latitude);
                const lng = parseFloat(v.longitude || v.Longitude);

                const isAvail = (v.isAvailable !== undefined) ? v.isAvailable : ((v.IsAvailable !== undefined) ? v.IsAvailable : true);
                const currentStatus = (v.status !== undefined) ? v.status : ((v.Status !== undefined) ? v.Status : 0);
                const hId = v.hospitalId || v.HospitalId;

                if (!isNaN(lat) && !isNaN(lng) && lat !== 0 && lng !== 0) {
                    const statusText = isAvail ? 'Wolny (W bazie/Patrol)' : `Zajęty (Status: ${currentStatus})`;

                    if (!window.vehicleMarkers[id]) {
                        window.vehicleMarkers[id] = L.marker([lat, lng], { icon: getIconByService(v.serviceType) })
                            .addTo(map).bindPopup(`<b>${v.licensePlate || v.LicensePlate || v.Callsign || v.callsign}</b><br>Status: ${statusText}`);
                    } else if (!window.activeSimulations[id]) {
                        window.vehicleMarkers[id].setLatLng([lat, lng]);
                        window.vehicleMarkers[id].setPopupContent(`<b>${v.licensePlate || v.LicensePlate || v.Callsign || v.callsign}</b><br>Status: ${statusText}`);
                    }

                    if (!isAvail && (currentStatus === 3 || currentStatus === 4) && !window.activeSimulations[id]) {

                        console.log(`[CADD] Wykryto pojazd zgłaszający Powrót/Transport! ID: ${id} | Status: ${currentStatus}`);

                        let targetLat = 0; let targetLng = 0;

                        if (v.serviceType === 'aviation') {
                            const airbase = v.airbase || v.Airbase;
                            if (airbase) { targetLat = airbase.latitude || airbase.Latitude; targetLng = airbase.longitude || airbase.Longitude; }
                        }
                        else if (v.serviceType === 'medic') {
                            const hosp = hospitals.find(h => (h.id || h.Id) === hId);
                            if (hosp) { targetLat = parseFloat(hosp.latitude || hosp.Latitude); targetLng = parseFloat(hosp.longitude || hosp.Longitude); }
                        }

                        if (targetLat !== 0 && targetLng !== 0) {
                            if (v.serviceType === 'aviation') {
                                window.startAirSimulation(id, v.serviceType, lat, lng, targetLat, targetLng, currentStatus);
                            } else {
                                window.startVehicleSimulation(id, v.serviceType, lat, lng, targetLat, targetLng, currentStatus);
                            }
                        }
                    }
                }
            });
        }

        p.filter(c => c.isAvailable !== false && c.IsAvailable !== false).forEach(car => {
            const dept = polDepts.find(d => (d.id || d.Id) === (car.pDepartmentId || car.PDepartmentId));
            if (dept && !window.activeSimulations[car.id || car.Id]) {
                const baseLat = dept.latitude || dept.Latitude;
                const baseLng = dept.longitude || dept.Longitude;
                const rad = dept.operatingRadiusKm || 15;
                window.startPatrolSimulation(car.id || car.Id, 'police', car.latitude || car.Latitude, car.longitude || car.Longitude, baseLat, baseLng, rad);
            }
        });

        m.filter(c => c.isAvailable !== false && c.IsAvailable !== false).forEach(amb => {
            const hosp = hospitals.find(h => (h.id || h.Id) === (amb.hospitalId || amb.HospitalId));
            if (hosp && !window.activeSimulations[amb.id || amb.Id]) {
                const baseLat = hosp.latitude || hosp.Latitude;
                const baseLng = hosp.longitude || hosp.Longitude;
                const rad = hosp.operatingRadiusKm || 15;
                window.startPatrolSimulation(amb.id || amb.Id, 'medic', amb.latitude || amb.Latitude, amb.longitude || amb.Longitude, baseLat, baseLng, rad);
            }
        });

    } catch (e) {
        console.error("Błąd aktualizacji liczników i pojazdów:", e);
    }
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