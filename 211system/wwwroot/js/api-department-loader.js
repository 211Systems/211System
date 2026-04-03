async function loadDepartmentsForDropdown(config) {
    const token = localStorage.getItem('jwt');
    if (!token) return;

    let roles = [];
    let email = "";
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const decodedToken = JSON.parse(window.atob(base64));

        let roleClaim = decodedToken["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || decodedToken.role;
        roles = Array.isArray(roleClaim) ? roleClaim : [roleClaim];
        email = decodedToken["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"] || decodedToken.email || "";
    } catch (e) { console.error("Błąd dekodowania JWT", e); return; }

    const hasAccess = config.allowedRoles.some(role => roles.includes(role));
    if (!hasAccess) {
        window.location.href = config.fallbackUrl;
        return;
    }

    const selectElement = document.getElementById(config.selectElementId);
    const submitBtn = document.querySelector('button[type="submit"]');

    try {
        const deptResponse = await fetch(config.departmentsApiUrl, { headers: { 'Authorization': 'Bearer ' + token } });
        let departments = await deptResponse.json();

        if (!roles.includes("Admin")) {
            const staffResponse = await fetch(config.staffApiUrl, { headers: { 'Authorization': 'Bearer ' + token } });
            const staffList = await staffResponse.json();

            const myRecord = staffList.find(p => p.email === email || p.Email === email);

            if (myRecord) {
                const myDeptId = myRecord[config.foreignKeyField] || myRecord[config.foreignKeyField.charAt(0).toUpperCase() + config.foreignKeyField.slice(1)];
                departments = departments.filter(d => (d.id || d.Id) === myDeptId);
            } else {
                departments = [];
            }
        }

        selectElement.innerHTML = '';

        if (departments.length === 0) {
            selectElement.innerHTML = '<option value="">Brak przypisanej placówki (Brak uprawnień)</option>';
            if (submitBtn) submitBtn.disabled = true;
        } else {
            if (roles.includes("Admin")) {
                selectElement.innerHTML = '<option value="">Wybierz placówkę z listy...</option>';
            }

            departments.forEach(d => {
                const opt = document.createElement('option');
                opt.value = d.id || d.Id;
                opt.textContent = d.name || d.Name;
                selectElement.appendChild(opt);
            });
        }
    } catch (error) {
        console.error(`Błąd ładowania placówek z ${config.departmentsApiUrl}`, error);
    }
}