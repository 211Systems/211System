function setupApiFormSubmit(config) {
    const form = document.getElementById(config.formId);
    if (!form) return;

    form.addEventListener('submit', async function (e) {
        e.preventDefault();

        const token = localStorage.getItem('jwt');
        const errorMsg = document.getElementById('form-error');

        if (errorMsg) errorMsg.classList.add('d-none');

        const requestData = config.buildPayload();

        try {
            const response = await fetch(config.apiUrl, {
                method: 'POST',
                headers: {
                    'Authorization': 'Bearer ' + token,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(requestData)
            });

            if (response.ok) {
                window.location.href = config.redirectUrl;
            } else {
                const errorData = await response.json();
                if (errorMsg) {
                    errorMsg.textContent = errorData.message || config.defaultErrorMsg || "Błąd podczas zapisywania.";
                    errorMsg.classList.remove('d-none');
                }
            }
        } catch (error) {
            if (errorMsg) {
                errorMsg.textContent = "Błąd połączenia z serwerem.";
                errorMsg.classList.remove('d-none');
            }
            console.error(error);
        }
    });
}