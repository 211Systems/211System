(function () {
    let detMap = null;
    let detLayers = [];

    function clearLayers() {
        if (!detMap) return;
        detLayers.forEach(function (l) {
            try { detMap.removeLayer(l); } catch (e) { }
        });
        detLayers = [];
    }

    window.renderIncidentDetailMap = async function (opts) {
        opts = opts || {};
        const token = localStorage.getItem('jwt');
        const mapDiv = document.getElementById('det-map');
        const navLink = document.getElementById('det-nav-link');
        const lat = parseFloat(opts.lat);
        const lng = parseFloat(opts.lng);
        const routeColor = opts.routeColor || '#dc3545';

        const hasCoords = !isNaN(lat) && !isNaN(lng) && !(lat === 0 && lng === 0);
        if (!hasCoords) {
            if (mapDiv) mapDiv.style.display = 'none';
            if (navLink) navLink.classList.add('d-none');
            return;
        }

        if (navLink) {
            navLink.href = 'https://www.google.com/maps/dir/?api=1&destination=' + lat + ',' + lng;
            navLink.classList.remove('d-none');
        }
        if (mapDiv) mapDiv.style.display = 'block';

        setTimeout(async function () {
            if (typeof L === 'undefined' || !mapDiv) return;

            if (!detMap) {
                detMap = L.map('det-map');
                L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                    maxZoom: 19,
                    attribution: '&copy; OpenStreetMap'
                }).addTo(detMap);
            }
            clearLayers();

            const incIcon = L.divIcon({
                html: '<i class="fas fa-map-marker-alt fa-2x" style="color:#dc3545;text-shadow:0 0 3px #000;"></i>',
                className: '',
                iconSize: [24, 24],
                iconAnchor: [12, 24]
            });
            const incMarker = L.marker([lat, lng], { icon: incIcon }).addTo(detMap).bindPopup('Miejsce zdarzenia');
            detLayers.push(incMarker);
            detMap.setView([lat, lng], 14);
            detMap.invalidateSize();

            if (!opts.vehiclesUrl || !opts.myId || !opts.idField) return;

            try {
                const res = await fetch(opts.vehiclesUrl, { headers: { 'Authorization': 'Bearer ' + token } });
                if (!res.ok) return;
                const vehicles = await res.json();
                const field = opts.idField;
                const capField = field.charAt(0).toUpperCase() + field.slice(1);
                const mine = vehicles.find(function (v) { return (v[field] || v[capField]) === opts.myId; });
                if (!mine) return;

                const vLat = mine.latitude != null ? mine.latitude : mine.Latitude;
                const vLng = mine.longitude != null ? mine.longitude : mine.Longitude;
                if (vLat == null || vLng == null || (vLat === 0 && vLng === 0)) return;

                const carIcon = L.divIcon({
                    html: '<i class="fas fa-location-arrow fa-lg" style="color:#0d6efd;text-shadow:0 0 3px #000;"></i>',
                    className: '',
                    iconSize: [20, 20],
                    iconAnchor: [10, 10]
                });
                const vMarker = L.marker([vLat, vLng], { icon: carIcon }).addTo(detMap).bindPopup('Twój pojazd');
                detLayers.push(vMarker);

                let routeCoords = null;
                try {
                    const osrm = 'https://router.project-osrm.org/route/v1/driving/' +
                        vLng + ',' + vLat + ';' + lng + ',' + lat + '?overview=full&geometries=geojson';
                    const r = await fetch(osrm);
                    const j = await r.json();
                    if (j.routes && j.routes[0]) {
                        routeCoords = j.routes[0].geometry.coordinates.map(function (c) { return [c[1], c[0]]; });
                    }
                } catch (e) {  }

                const line = routeCoords
                    ? L.polyline(routeCoords, { color: routeColor, weight: 4 })
                    : L.polyline([[vLat, vLng], [lat, lng]], { color: routeColor, weight: 3, dashArray: '6' });
                line.addTo(detMap);
                detLayers.push(line);
                detMap.fitBounds(line.getBounds(), { padding: [30, 30] });
            } catch (e) { }
        }, 350);
    };
})();
