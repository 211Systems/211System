(function () {
    const MAX_ATTACHMENTS = 10;

    function getToken() {
        return window.jwtToken || localStorage.getItem('jwt') || '';
    }

    function escapeHtml(text) {
        if (!text) return '';
        const d = document.createElement('div');
        d.textContent = text;
        return d.innerHTML;
    }

    function formatSize(bytes) {
        if (!bytes || bytes < 1024) return (bytes || 0) + ' B';
        if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
        return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
    }

    function isImage(contentType, fileName) {
        if (contentType && contentType.startsWith('image/')) return true;
        const ext = (fileName || '').split('.').pop().toLowerCase();
        return ['jpg', 'jpeg', 'png', 'webp', 'gif'].includes(ext);
    }

    function fileIcon(fileName, contentType) {
        const ext = (fileName || '').split('.').pop().toLowerCase();
        if (isImage(contentType, fileName)) return 'fa-image';
        if (ext === 'pdf') return 'fa-file-pdf';
        if (ext === 'docx') return 'fa-file-word';
        return 'fa-file';
    }

    async function fetchAttachments(incidentId) {
        const token = getToken();
        if (!token) return [];
        const res = await fetch(`/api/Attachment/incident/${incidentId}`, {
            headers: { 'Authorization': 'Bearer ' + token }
        });
        if (!res.ok) return [];
        return await res.json();
    }

    function buildListHtml(attachments, compact) {
        if (!attachments.length) {
            return '<p class="text-muted mb-0 small">Brak załączników do tego zgłoszenia.</p>';
        }

        const thumbStyle = compact
            ? 'max-height:80px;max-width:120px;object-fit:cover;'
            : 'max-height:140px;max-width:100%;object-fit:contain;';

        return `<div class="list-group list-group-flush border rounded">` +
            attachments.map(a => {
                const url = escapeHtml(a.url || '');
                const name = escapeHtml(a.fileName || 'plik');
                const icon = fileIcon(a.fileName, a.contentType);
                const img = isImage(a.contentType, a.fileName)
                    ? `<div class="mt-2"><a href="${url}" target="_blank" rel="noopener noreferrer"><img src="${url}" alt="${name}" class="img-thumbnail" style="${thumbStyle}"></a></div>`
                    : '';
                return `<div class="list-group-item bg-light">
                    <div class="d-flex justify-content-between align-items-start flex-wrap">
                        <div>
                            <i class="fas ${icon} text-secondary mr-1"></i>
                            <a href="${url}" target="_blank" rel="noopener noreferrer" class="font-weight-bold">${name}</a>
                            <div class="text-muted small">${formatSize(a.fileSizeBytes)}</div>
                        </div>
                        <a href="${url}" target="_blank" rel="noopener noreferrer" class="btn btn-xs btn-outline-primary btn-sm mt-1">
                            <i class="fas fa-external-link-alt"></i> Otwórz
                        </a>
                    </div>
                    ${img}
                </div>`;
            }).join('') +
            `</div>`;
    }

    function ensureModal() {
        if (document.getElementById('incidentAttachmentsModal')) return;
        const wrap = document.createElement('div');
        wrap.innerHTML = `
        <div class="modal fade" id="incidentAttachmentsModal" tabindex="-1" role="dialog" aria-hidden="true">
            <div class="modal-dialog modal-lg" role="document">
                <div class="modal-content">
                    <div class="modal-header bg-info text-white">
                        <h5 class="modal-title"><i class="fas fa-paperclip"></i> Załączniki zgłoszenia <span id="ia-modal-num"></span></h5>
                        <button type="button" class="close text-white" data-dismiss="modal" aria-label="Zamknij"><span>&times;</span></button>
                    </div>
                    <div class="modal-body" id="ia-modal-body">Ładowanie...</div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-dismiss="modal">Zamknij</button>
                    </div>
                </div>
            </div>
        </div>`;
        document.body.appendChild(wrap.firstElementChild);
    }

    async function renderInto(container, incidentId, options) {
        const el = typeof container === 'string' ? document.getElementById(container) : container;
        if (!el) return;
        const compact = options && options.compact;
        el.innerHTML = '<small class="text-muted"><i class="fas fa-spinner fa-spin"></i> Ładowanie załączników...</small>';
        const list = await fetchAttachments(incidentId);
        el.innerHTML =
            `<h6 class="mb-2"><i class="fas fa-paperclip text-info"></i> Załączniki <span class="badge badge-info">${list.length}</span></h6>` +
            buildListHtml(list, compact);
        return list;
    }

    async function showModal(incidentId, incidentLabel) {
        ensureModal();
        document.getElementById('ia-modal-num').textContent = incidentLabel ? '— ' + incidentLabel : '';
        const body = document.getElementById('ia-modal-body');
        body.innerHTML = '<p class="text-muted"><i class="fas fa-spinner fa-spin"></i> Ładowanie...</p>';
        $('#incidentAttachmentsModal').modal('show');
        const list = await fetchAttachments(incidentId);
        body.innerHTML = buildListHtml(list, false);
    }

    function jsString(value) {
        return (value || '').replace(/\\/g, '\\\\').replace(/'/g, "\\'");
    }

    function badgeHtml(incidentId, count, incidentLabel) {
        if (!count) return '';
        const label = jsString(incidentLabel);
        return `<button type="button" class="btn btn-link btn-sm p-0 ml-1 align-baseline text-info"
            onclick="window.IncidentAttachments.showModal('${incidentId}', '${label}')"
            title="Pokaż ${count} załącznik(ów)">
            <i class="fas fa-paperclip"></i> ${count}
        </button>`;
    }

    async function uploadBatch(incidentId, fileList) {
        if (!fileList || !fileList.length) return { ok: true };
        if (fileList.length > MAX_ATTACHMENTS) {
            return { ok: false, message: `Maksymalnie ${MAX_ATTACHMENTS} załączników na zgłoszenie.` };
        }
        const fd = new FormData();
        fd.append('incidentId', incidentId);
        for (let i = 0; i < fileList.length; i++) {
            fd.append('files', fileList[i]);
        }
        const res = await fetch('/api/Attachment/upload-batch', {
            method: 'POST',
            headers: { 'Authorization': 'Bearer ' + getToken() },
            body: fd
        });
        if (!res.ok) {
            const err = await res.json().catch(() => ({}));
            return { ok: false, message: err.message || 'Błąd przesyłania załączników.' };
        }
        return { ok: true };
    }

    window.IncidentAttachments = {
        fetch: fetchAttachments,
        renderInto,
        showModal,
        badgeHtml,
        uploadBatch,
        buildListHtml,
        MAX_ATTACHMENTS
    };
})();
