document.addEventListener("DOMContentLoaded", updateLayoutBasedOnAuth);

window.addEventListener("pageshow", function (event) {
    if (event.persisted || !localStorage.getItem('jwt')) {
        updateLayoutBasedOnAuth();
    }
});

function updateLayoutBasedOnAuth() {
    const token = localStorage.getItem('jwt');
    const currentPath = window.location.pathname.toLowerCase();
    const isAuthPage = currentPath.includes('/authview');

    if (!token && !isAuthPage && currentPath !== '/') {
        window.location.href = '/AuthView/Login';
        return;
    }

    if (token) {
        const decodedToken = parseJwt(token);

        document.getElementById('auth-logged-in').classList.remove('d-none');
        document.getElementById('auth-logged-out').classList.add('d-none');

        const email = decodedToken["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"] || "Użytkownik";
        const role = decodedToken["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || "Pracownik";

        document.getElementById('nav-user-name').textContent = email.split('@')[0];
        document.getElementById('profile-email').textContent = email;
        document.getElementById('profile-role').textContent = role;

        // Ochrona tras
        if (currentPath.includes('/admin') && role !== "Admin") {
            alert("To jest strefa zarezerwowana tylko dla Głównego Administratora systemu!");
            window.location.href = '/';
            return;
        }
        if (currentPath.includes('/police') && !["Admin", "Policjant", "Komendant", "Inspektor"].includes(role)) {
            alert("Brak uprawnień do tego obszaru!");
            window.location.href = '/';
            return;
        }
        if (currentPath.includes('/medic') && !["Admin", "Medyk", "Lekarz", "Kierownik Szpitala"].includes(role)) {
            alert("Brak uprawnień do tego obszaru!");
            window.location.href = '/';
            return;
        }
        if (currentPath.includes('/fire') && !["Admin", "Strazak", "Kapitan", "Naczelnik"].includes(role)) {
            alert("Brak uprawnień do tego obszaru!");
            window.location.href = '/';
            return;
        }
        if (currentPath.includes('/dispatch') && !["Admin", "Admin112", "Dyspozytor112"].includes(role)) {
            alert("Brak uprawnień do tego obszaru!");
            window.location.href = '/';
            return;
        }

        const homeLink = document.getElementById('nav-home-link');

        // Odkrywanie menu
        if (role === "Admin") {
            if (document.getElementById('menu-admin')) document.getElementById('menu-admin').classList.remove('d-none');
            if (homeLink) homeLink.href = '/Admin/Home/Index';
        }
        else if (role === "Kierownik Szpitala") {
            if (document.getElementById('menu-medic-manager')) document.getElementById('menu-medic-manager').classList.remove('d-none');
            if (document.getElementById('menu-medic-worker')) document.getElementById('menu-medic-worker').classList.remove('d-none');
            if (homeLink) homeLink.href = '/Medic/Home/Index';
        }
        else if (role === "Medyk" || role === "Lekarz") {
            if (document.getElementById('menu-medic-worker')) document.getElementById('menu-medic-worker').classList.remove('d-none');
            if (homeLink) homeLink.href = '/Medic/Home/Index';
        }
        else if (role === "Inspektor" || role === "Komendant") {
            if (document.getElementById('menu-police-manager')) document.getElementById('menu-police-manager').classList.remove('d-none');
            if (document.getElementById('menu-police-worker')) document.getElementById('menu-police-worker').classList.remove('d-none');
            if (homeLink) homeLink.href = '/Police/Home/Index';
        }
        else if (role === "Policjant") {
            if (document.getElementById('menu-police-worker')) document.getElementById('menu-police-worker').classList.remove('d-none');
            if (homeLink) homeLink.href = '/Police/Home/Index';
        }
        else if (role === "Strazak" || role === "Kapitan" || role === "Naczelnik") {
            if (homeLink) homeLink.href = '/Fire/Home/Index';
        }
        // TUTAJ WSTAWIAMY ZAKTUALIZOWANY FRAGMENT:
        else if (role === "Admin112" || role === "Dyspozytor112") {
            if (document.getElementById('menu-dispatch')) document.getElementById('menu-dispatch').classList.remove('d-none');
            if (homeLink) homeLink.href = '/Dispatch/Home/Index';
            if (!currentPath.includes('/dispatch') && currentPath !== '/') window.location.href = '/Dispatch/Home/Index';
        }

    } else {
        document.getElementById('auth-logged-in').classList.add('d-none');
        document.getElementById('auth-logged-out').classList.remove('d-none');
        document.getElementById('nav-user-name').textContent = "Niezalogowany";
    }
}

function logout() {
    localStorage.removeItem('jwt');
    window.location.href = '/AuthView/Login';
}

function parseJwt(token) {
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(window.atob(base64).split('').map(function (c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));

        return JSON.parse(jsonPayload);
    } catch (e) {
        return null;
    }
}