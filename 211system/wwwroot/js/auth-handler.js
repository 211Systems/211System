window.parseJwt = function (token) {
    try {
        return JSON.parse(window.atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
    } catch (e) {
        return null;
    }
};

window.getUserRoles = function (decoded) {
    if (!decoded) return [];
    const claim = decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || decoded["role"];
    return Array.isArray(claim) ? claim : [claim].filter(Boolean);
};

window.ensureValidToken = async function () {
    try {
        const currentToken = localStorage.getItem('jwt');
        if (!currentToken) return null;

        const res = await fetch('/api/Auth/refresh-token', {
            method: 'GET',
            headers: {
                'Authorization': 'Bearer ' + currentToken,
                'Content-Type': 'application/json'
            }
        });

        if (res.ok) {
            const data = await res.json();
            localStorage.setItem('jwt', data.token);
            window.jwtToken = data.token;
            return data.token;
        } else {
            console.warn("Serwer odrzucił odświeżenie tokenu. Wymagane ponowne logowanie.");
            localStorage.removeItem('jwt');
            return null;
        }
    } catch (e) {
        console.error("Błąd sieci podczas weryfikacji tokenu:", e);
        return null;
    }
};

window.refreshAvatarToken = async function () {
    const token = await window.ensureValidToken();
    if (token) {
        window.location.reload();
    }
};