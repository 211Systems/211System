$(document).ready(function () {
    const token = localStorage.getItem('jwt');
    if (!token) {
        window.location.href = '/AuthView/Login';
        return;
    }
    const today = new Date();
    const lastWeek = new Date();
    lastWeek.setDate(today.getDate() - 7);
    
    document.getElementById('dateTo').valueAsDate = today;
    document.getElementById('dateFrom').valueAsDate = lastWeek;

const dataTable = $('#reportsTable').DataTable({
        language: {
            url: '//cdn.datatables.net/plug-ins/1.13.6/i18n/pl.json'
        },
        dom: '<"row mb-3"<"col-md-6"B><"col-md-6 text-right"f>>rt<"row"<"col-md-6"i><"col-md-6"p>>',
        buttons: [
            {
                extend: 'excelHtml5',
                text: '<i class="fas fa-file-excel mr-1"></i> Pobierz plik Excel (.xlsx)',
                className: 'btn btn-success font-weight-bold',
                title: 'Szczególowy_Raport_System211_' + new Date().toISOString().slice(0,10),
                exportOptions: {
                    columns: [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10] 
                }
            }
        ],
        columns: [
            { data: 'incidentNumber' },
            { data: 'date' },
            { data: 'type', render: function(data) { return `<b>${data}</b>`; } },
            { 
                data: 'severity',
                render: function (data) {
                    let color = data === 'Krytyczny' ? 'danger' : (data === 'Wysoki' ? 'warning' : (data === 'Średni' ? 'info' : 'secondary'));
                    return `<span class="badge badge-${color} p-2">${data}</span>`;
                }
            },
            { 
                data: 'status',
                render: function (data) {
                    let sColor = data === 'Zakończone' ? 'success' : (data === 'W toku' ? 'primary' : 'light text-dark');
                    return `<span class="badge bg-${sColor}">${data}</span>`;
                }
            },
            { data: 'address' },
            {
                data: 'weather',
                render: function(data) { return `<i class="fas fa-cloud-sun text-info mr-1"></i> ${data}`; }
            },
           { 
                data: 'police',
                render: function(data) { return data !== 'Brak' ? `<span class="text-primary font-weight-bold"><i class="fas fa-car-side"></i> ${data}</span>` : '<span class="text-muted">Brak</span>'; }
            },
            { 
                data: 'fire',
                render: function(data) { return data !== 'Brak' ? `<span class="text-danger font-weight-bold"><i class="fas fa-fire"></i> ${data}</span>` : '<span class="text-muted">Brak</span>'; }
            },
            { 
                data: 'medical',
                render: function(data) { return data !== 'Brak' ? `<span class="text-success font-weight-bold"><i class="fas fa-ambulance"></i> ${data}</span>` : '<span class="text-muted">Brak</span>'; }
            },
            { 
                data: 'description', 
                render: function(data) { return data && data.length > 40 ? data.substring(0, 40) + '...' : data; } 
            }
        ],
        order: [[1, 'desc']]
    });

    $('#btnLoadData').on('click', async function() {
        const from = $('#dateFrom').val();
        const to = $('#dateTo').val();

        if(!from || !to) {
            alert("Proszę wybrać zakres dat!");
            return;
        }

        try {
            const response = await fetch(`/api/Reports/data?from=${from}&to=${to}`, {
                headers: { 'Authorization': 'Bearer ' + token }
            });

            if(response.ok) {
                const data = await response.json();
                dataTable.clear();
                dataTable.rows.add(data);
                dataTable.draw();
            } else {
                alert("Błąd pobierania danych. Sprawdź konsole.");
            }
        } catch(e) {
            console.error("Błąd sieci:", e);
        }
    });

    $('#btnDownloadPdf').on('click', function() {
        const from = $('#dateFrom').val();
        const to = $('#dateTo').val();

        if(!from || !to) {
            alert("Proszę wybrać zakres dat!");
            return;
        }
        window.open(`/api/Reports/generate?from=${from}&to=${to}`, '_blank');
    });

    $('#btnLoadData').click();
});