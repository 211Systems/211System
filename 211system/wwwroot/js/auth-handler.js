window.parseJwt = function (token) {
    try {
        return JSON.parse(window.atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
    } catch (e) { return null; }
};

window.ensureValidToken = async function () {
    try {
        const res = await fetch('/api/Auth/refresh-token');

        if (res.ok) {
            const data = await res.json();
            localStorage.setItem('jwt', data.token);
            window.jwtToken = data.token;
            return data.token;
        } else {
            console.warn("Serwer odrzucił odświeżenie tokenu. Status:", res.status);
            localStorage.removeItem('jwt');
            return null;
        }
    } catch (e) {
        console.error("Błąd sieci w auth-handler:", e);
        return null;
    }
};

window.refreshAvatarToken = async function () {
    const token = await window.ensureValidToken();
    if (token) {
        window.location.reload();
    }
};