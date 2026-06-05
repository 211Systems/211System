window.updateLayoutBasedOnAuth = function () {
    const token = localStorage.getItem('jwt');
    const currentPath = window.location.pathname.toLowerCase();
    const isAuthPage = currentPath.includes('/authview');

    if (!token && !isAuthPage && currentPath !== '/') {
        window.location.href = '/AuthView/Login';
        return;
    }

    if (token) {
        let decodedToken = null;
        try {
            const base64Url = token.split('.')[1];
            const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
            decodedToken = JSON.parse(window.atob(base64));
        } catch (e) {
            console.error("Błąd parsowania tokenu!");
            return;
        }

        if (document.getElementById('auth-logged-in')) document.getElementById('auth-logged-in').classList.remove('d-none');
        if (document.getElementById('auth-logged-out')) document.getElementById('auth-logged-out').classList.add('d-none');

        const email = decodedToken["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"] || "Użytkownik";

        let roleClaim = decodedToken["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || decodedToken.role || "Pracownik";
        let roles = Array.isArray(roleClaim) ? roleClaim : [roleClaim];

        if (document.getElementById('nav-user-name')) document.getElementById('nav-user-name').textContent = email.split('@')[0];
        if (document.getElementById('profile-email')) document.getElementById('profile-email').textContent = email;
        if (document.getElementById('profile-role')) document.getElementById('profile-role').textContent = roles.join(', ');

        if (currentPath.includes('/admin') && !roles.includes("Admin")) {
            alert("To jest strefa tylko dla Głównego Administratora systemu.");
            window.location.href = '/';
            return;
        }
        if (currentPath.includes('/police') && !roles.some(r => ["Admin", "Policjant", "Komendant", "Inspektor"].includes(r))) {
            alert("Brak uprawnień do tego obszaru.");
            window.location.href = '/';
            return;
        }
        if (currentPath.includes('/medic') && !roles.some(r => ["Admin", "Medyk", "Lekarz", "Kierownik Szpitala"].includes(r))) {
            alert("Brak uprawnień do tego obszaru.");
            window.location.href = '/';
            return;
        }
        if (currentPath.includes('/fire') && !roles.some(r => ["Admin", "Strazak", "Kapitan", "Naczelnik"].includes(r))) {
            alert("Brak uprawnień do tego obszaru.");
            window.location.href = '/';
            return;
        }
        const reportRoles = ["Admin", "Admin112", "Dyspozytor112", "Naczelnik", "Kapitan", "Komendant", "Inspektor", "Kierownik Szpitala"];
        const isReportPage = currentPath.includes('/report');

        if (isReportPage) {
            if (!roles.some(r => reportRoles.includes(r))) {
                alert("Brak uprawnień do modułu raportów.");
                window.location.href = '/';
                return;
            }
        } else if (currentPath.includes('/dispatch') && !roles.some(r => ["Admin", "Admin112", "Dyspozytor112"].includes(r))) {
            alert("Brak uprawnień do tego obszaru.");
            window.location.href = '/';
            return;
        }

        const homeLink = document.getElementById('nav-home-link');

        const shouldRedirectToDashboard = currentPath === '/' || currentPath === '/authview/login';

        const allMenus = [
            'menu-admin', 'menu-dispatch',
            'menu-medic-manager', 'menu-medic-worker', 'link-hospitals', 'link-ambulances', 'link-operations',
            'menu-police-manager', 'menu-police-worker', 'link-police-depts', 'link-police-cars', 'link-police-operations',
            'menu-fire-manager', 'menu-fire-worker', 'link-fire-depts', 'link-fire-trucks', 'link-fire-operations',
            'menu-reports'
        ];

        const showReports = () => {
            const el = document.getElementById('menu-reports');
            if (el) el.classList.remove('d-none');
        };

        if (roles.includes("Admin")) {
            allMenus.forEach(id => {
                const el = document.getElementById(id);
                if (el) el.classList.remove('d-none');
            });
            if (homeLink) homeLink.href = '/Admin/Home/Index';
            if (shouldRedirectToDashboard) window.location.href = '/Admin/Home/Index';
        }
        else if (roles.includes("Kierownik Szpitala")) {
            ['menu-medic-manager', 'menu-medic-worker', 'link-hospitals', 'link-ambulances', 'link-operations'].forEach(id => {
                if (document.getElementById(id)) document.getElementById(id).classList.remove('d-none');
            });
            showReports();
            if (homeLink) homeLink.href = '/Medic/Hospitals';
            if (shouldRedirectToDashboard) window.location.href = '/Medic/Hospitals';
        }
        else if (roles.includes("Lekarz")) {
            ['menu-medic-manager', 'menu-medic-worker', 'link-hospitals', 'link-operations'].forEach(id => {
                if (document.getElementById(id)) document.getElementById(id).classList.remove('d-none');
            });
            if (homeLink) homeLink.href = '/Medic/Hospitals';
            if (shouldRedirectToDashboard) window.location.href = '/Medic/Hospitals';
        }
        else if (roles.includes("Medyk")) {
            ['menu-medic-worker', 'link-operations'].forEach(id => {
                if (document.getElementById(id)) document.getElementById(id).classList.remove('d-none');
            });
            if (homeLink) homeLink.href = '/Medic/Operations';
            if (shouldRedirectToDashboard) window.location.href = '/Medic/Operations';
        }
        else if (roles.includes("Inspektor") || roles.includes("Komendant")) {
            ['menu-police-manager', 'menu-police-worker', 'link-police-depts', 'link-police-cars', 'link-police-operations'].forEach(id => {
                if (document.getElementById(id)) document.getElementById(id).classList.remove('d-none');
            });
            showReports();
            if (homeLink) homeLink.href = '/Police/Home';
            if (shouldRedirectToDashboard) window.location.href = '/Police/Home';
        }
        else if (roles.includes("Policjant")) {
            ['menu-police-worker', 'link-police-operations'].forEach(id => {
                if (document.getElementById(id)) document.getElementById(id).classList.remove('d-none');
            });
            if (homeLink) homeLink.href = '/Police/Operations';
            if (shouldRedirectToDashboard) window.location.href = '/Police/Operations';
        }
        else if (roles.includes("Naczelnik") || roles.includes("Kapitan")) {
            ['menu-fire-manager', 'menu-fire-worker', 'link-fire-depts', 'link-fire-trucks', 'link-fire-operations'].forEach(id => {
                if (document.getElementById(id)) document.getElementById(id).classList.remove('d-none');
            });
            showReports();
            if (homeLink) homeLink.href = '/Fire/Home';
            if (shouldRedirectToDashboard) window.location.href = '/Fire/Home';
        }
        else if (roles.includes("Strazak")) {
            ['menu-fire-worker', 'link-fire-operations'].forEach(id => {
                if (document.getElementById(id)) document.getElementById(id).classList.remove('d-none');
            });
            if (homeLink) homeLink.href = '/Fire/Operations';
            if (shouldRedirectToDashboard) window.location.href = '/Fire/Operations';
        }
        else if (roles.includes("Admin112") || roles.includes("Dyspozytor112")) {
            if (document.getElementById('menu-dispatch')) document.getElementById('menu-dispatch').classList.remove('d-none');
            showReports();
            if (homeLink) homeLink.href = '/Dispatch/Home/Index';
            if (shouldRedirectToDashboard) window.location.href = '/Dispatch/Home/Index';
        }
    } else {
        if (document.getElementById('auth-logged-in')) document.getElementById('auth-logged-in').classList.add('d-none');
        if (document.getElementById('auth-logged-out')) document.getElementById('auth-logged-out').classList.remove('d-none');
        if (document.getElementById('nav-user-name')) document.getElementById('nav-user-name').textContent = "Niezalogowany";
    }
};

document.addEventListener("DOMContentLoaded", window.updateLayoutBasedOnAuth);