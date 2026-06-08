/**
 * Kompatybilność markupu i API Bootstrap 4 → Bootstrap 5.
 */
(function () {
    function applyBootstrapCompat(root) {
        root = root || document;

        root.querySelectorAll('[data-toggle]').forEach(function (el) {
            var value = el.getAttribute('data-toggle');
            if (value && !el.hasAttribute('data-bs-toggle')) {
                el.setAttribute('data-bs-toggle', value);
            }
        });

        root.querySelectorAll('[data-target]').forEach(function (el) {
            var value = el.getAttribute('data-target');
            if (value && !el.hasAttribute('data-bs-target')) {
                el.setAttribute('data-bs-target', value);
            }
        });

        root.querySelectorAll('[data-dismiss]').forEach(function (el) {
            var value = el.getAttribute('data-dismiss');
            if (value && !el.hasAttribute('data-bs-dismiss')) {
                el.setAttribute('data-bs-dismiss', value);
            }
        });
    }

    window.applyBootstrapCompat = applyBootstrapCompat;
    applyBootstrapCompat();

    document.addEventListener('DOMContentLoaded', function () {
        applyBootstrapCompat();

        document.querySelectorAll('#main-navigation .dropdown-toggle').forEach(function (toggle) {
            bootstrap.Dropdown.getOrCreateInstance(toggle, {
                popperConfig: function (defaultConfig) {
                    defaultConfig.strategy = 'fixed';
                    return defaultConfig;
                }
            });
        });
    });

    // Dynamicznie dodane modale (innerHTML) — zamknięcie bez migracji atrybutów
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-dismiss="modal"]');
        if (!btn || btn.hasAttribute('data-bs-dismiss')) return;

        var modalEl = btn.closest('.modal');
        if (!modalEl) return;

        var instance = bootstrap.Modal.getInstance(modalEl)
            || bootstrap.Modal.getOrCreateInstance(modalEl);
        instance.hide();
    });
})();

// jQuery .modal('show'|'hide') używane w wielu widokach (BS4 API)
(function ($) {
    if (!$ || $.fn.modal) return;

    $.fn.modal = function (config) {
        return this.each(function () {
            if (typeof config === 'string') {
                var instance = bootstrap.Modal.getOrCreateInstance(this);
                if (config === 'show') instance.show();
                else if (config === 'hide') instance.hide();
                else if (config === 'toggle') instance.toggle();
                else if (config === 'dispose') {
                    var existing = bootstrap.Modal.getInstance(this);
                    if (existing) existing.dispose();
                }
            } else {
                bootstrap.Modal.getOrCreateInstance(this, config || {});
            }
        });
    };
})(window.jQuery);
