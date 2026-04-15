window.parseJwt = window.parseJwt || function (token) {
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        return JSON.parse(window.atob(base64));
    } catch (e) { return null; }
};

window.logout = function () {
    localStorage.removeItem('jwt');
    window.location.href = '/AuthView/Login';
};
function initClock() {
    const clock = document.getElementById('live-clock');
    if (!clock) return;

    function updateClock() {
        clock.textContent = new Date().toLocaleTimeString('pl-PL');
    }
    setInterval(updateClock, 1000);
    updateClock();
}

function setAvatarImage(url) {
    if (!url) return;
    const navImg = document.getElementById('nav-user-avatar');
    const navIcon = document.getElementById('nav-user-icon');
    const profImg = document.getElementById('profile-user-avatar');
    const profIcon = document.getElementById('profile-user-icon');

    if (navImg) { navImg.src = url; navImg.classList.remove('d-none'); }
    if (navIcon) { navIcon.classList.add('d-none'); }
    if (profImg) { profImg.src = url; profImg.classList.remove('d-none'); }
    if (profIcon) { profIcon.classList.add('d-none'); }
}

function initUserContext() {
    const token = localStorage.getItem('jwt');
    const authLoggedIn = document.getElementById('auth-logged-in');
    const authLoggedOut = document.getElementById('auth-logged-out');

    if (!token) {
        if (authLoggedOut) authLoggedOut.classList.remove('d-none');
        if (authLoggedIn) authLoggedIn.classList.add('d-none');
        return;
    }

    const decoded = window.parseJwt(token);
    if (!decoded) return;

    if (authLoggedIn) authLoggedIn.classList.remove('d-none');
    if (authLoggedOut) authLoggedOut.classList.add('d-none');

    const email = decoded["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"] || decoded.email || decoded.unique_name || "Nieznany Email";
    const profEmail = document.getElementById('profile-email');
    const navName = document.getElementById('nav-user-name');

    const userName = email.split('@')[0];

    if (profEmail) profEmail.textContent = email;
    if (navName) navName.textContent = userName;

    let role = decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || decoded.role || "Brak";
    const rolesArray = Array.isArray(role) ? role : [role];
    const profRole = document.getElementById('profile-role');
    if (profRole) profRole.textContent = rolesArray.join(', ');

    const avatarUrl = decoded["AvatarUrl"];
    if (avatarUrl) {
        setAvatarImage(avatarUrl);
    } else {
        const fallbackAvatar = `https://ui-avatars.com/api/?name=${userName}&background=random&color=fff`;
        setAvatarImage(fallbackAvatar);
    }

    const show = (id) => { const el = document.getElementById(id); if (el) el.classList.remove('d-none'); };

    if (rolesArray.includes("Admin")) {
        show('menu-admin'); show('menu-dispatch'); show('nav-admin-cpr-container');
        show('nav-admin-centers-container'); show('menu-medic-manager');
        show('menu-medic-worker'); show('link-hospitals'); show('link-ambulances');
        show('link-operations'); show('menu-police-manager'); show('link-police-depts');
        show('link-police-cars'); show('menu-police-worker'); show('link-police-operations');
        show('menu-fire-manager'); show('link-fire-depts'); show('link-fire-trucks');
        show('menu-fire-worker'); show('link-fire-operations');
    }
    else if (rolesArray.includes("Admin112")) {
        show('menu-dispatch'); show('nav-admin-cpr-container'); show('nav-admin-centers-container');
    }
    else if (rolesArray.includes("Dyspozytor112")) {
        show('menu-dispatch');
    }
    else if (rolesArray.includes("Kierownik Szpitala")) {
        show('menu-medic-manager'); show('menu-medic-worker'); show('link-hospitals');
        show('link-ambulances'); show('link-operations');
    }
    else if (rolesArray.includes("Lekarz")) {
        show('menu-medic-manager'); show('menu-medic-worker'); show('link-hospitals'); show('link-operations');
    }
    else if (rolesArray.includes("Medyk")) {
        show('menu-medic-worker'); show('link-operations');
    }
 
    else if (rolesArray.includes("Komendant")) {
        show('menu-police-manager'); show('link-police-depts'); show('link-police-cars');
        show('menu-police-worker'); show('link-police-operations');
    }
    else if (rolesArray.includes("Inspektor")) {
        show('menu-police-manager'); show('link-police-depts'); show('link-police-cars');
        show('menu-police-worker'); show('link-police-operations');
    }
    else if (rolesArray.includes("Policjant")) {
        show('menu-police-worker'); show('link-police-operations');
    }

    else if (rolesArray.includes("Naczelnik") || rolesArray.includes("Kapitan")) {
        show('menu-fire-manager'); show('link-fire-depts'); show('link-fire-trucks');
        show('menu-fire-worker'); show('link-fire-operations');
    }
    else if (rolesArray.includes("strazak")) { 
        show('menu-fire-worker'); show('link-fire-operations');
    }
}

function initAvatarUpload() {
    const avatarInput = document.getElementById('avatarUploadInput');
    if (!avatarInput) return;

    avatarInput.addEventListener('change', async function (e) {
        const file = e.target.files[0];
        if (!file) return;

        const reader = new FileReader();
        reader.onload = (event) => setAvatarImage(event.target.result);
        reader.readAsDataURL(file);

        const token = localStorage.getItem('jwt');
        if (!token) {
            alert("Brak autoryzacji. Zaloguj się ponownie.");
            return;
        }

        const formData = new FormData();
        formData.append('file', file);

        try {
            const profEmail = document.getElementById('profile-email');
            if (profEmail) profEmail.textContent = "Wgrywanie zdjęcia...";

            const response = await fetch('/api/Profile/upload-avatar', {
                method: 'POST',
                headers: { 'Authorization': `Bearer ${token}` },
                body: formData
            });

            if (response.ok) {
                const result = await response.json();
                setAvatarImage(result.avatarUrl);
                const decoded = window.parseJwt(token);
                if (profEmail) profEmail.textContent = decoded.email || decoded.unique_name || "Zaktualizowano!";
            } else {
                const err = await response.json();
                alert(err.message || "Błąd podczas wgrywania pliku.");
                location.reload();
            }
        } catch (error) {
            console.error("Błąd sieci:", error);
            alert("Błąd połączenia z serwerem.");
            location.reload();
        }
    });
}

document.addEventListener("DOMContentLoaded", function () {
    initClock();
    initUserContext();
    initAvatarUpload();
});