// ============================================================================
// Developer : Tajwer Jalal
// Date      : 19-Oct-2026
// Time      : 15:40
// Purpose   : Shared list + popup behaviour for every pharmacy setup screen.
//
// WHY A SHARED HELPER
//   The brief is that every setup and transaction form works the same way:
//   a list on the page, and a data-entry popup opened either by clicking a
//   row or by pressing Add New. Writing that once means Supplier, Manufacturer,
//   Chart of Accounts, Generics and Categories cannot drift apart, and the
//   delete confirmation cannot be forgotten on one of them.
//
//   It also carries the two fixes found while building Medicine / Product:
//   the popup opens even if the Bootstrap JS bundle is missing (it drives
//   Bootstrap's own CSS classes directly), and the delete button stops
//   propagation so it never opens the edit popup underneath the confirmation.
// ============================================================================
(function (global) {
    'use strict';

    function esc(s) {
        return String(s === null || s === undefined ? '' : s)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    /**
     * Wraps a Bootstrap modal. Prefers the real Bootstrap API (focus trap,
     * Escape, backdrop) and falls back to driving the CSS classes directly
     * so a missing JS bundle degrades instead of doing nothing at all.
     */
    function makeModal(modalEl) {
        var api = (typeof bootstrap !== 'undefined' && bootstrap.Modal)
            ? new bootstrap.Modal(modalEl)
            : null;

        function backdrop(on) {
            var b = document.querySelector('.modal-backdrop');
            if (on) {
                if (!b) {
                    b = document.createElement('div');
                    b.className = 'modal-backdrop fade show';
                    document.body.appendChild(b);
                }
            } else if (b) {
                b.remove();
            }
        }

        function open() {
            if (api) { api.show(); return; }
            modalEl.classList.add('show');
            modalEl.style.display = 'block';
            modalEl.setAttribute('aria-hidden', 'false');
            document.body.classList.add('modal-open');
            backdrop(true);
        }

        function close() {
            if (api) { api.hide(); return; }
            modalEl.classList.remove('show');
            modalEl.style.display = 'none';
            modalEl.setAttribute('aria-hidden', 'true');
            document.body.classList.remove('modal-open');
            backdrop(false);
        }

        modalEl.querySelectorAll('[data-bs-dismiss="modal"]').forEach(function (el) {
            el.addEventListener('click', close);
        });

        if (!api) {
            document.addEventListener('keydown', function (e) {
                if (e.key === 'Escape' && modalEl.classList.contains('show')) { close(); }
            });
        }

        return { open: open, close: close };
    }

    /**
     * Confirm then submit. Never deletes without asking: if SweetAlert is
     * unavailable this falls back to window.confirm rather than proceeding.
     */
    function confirmDelete(opts, onConfirm) {
        if (global.Swal && global.Swal.fire) {
            global.Swal.fire({
                title: opts.title,
                html: '<div style="text-align:left">' +
                      '<div><strong>' + esc(opts.code) + '</strong>' +
                      (opts.name ? ' &mdash; ' + esc(opts.name) : '') + '</div>' +
                      (opts.note ? '<div class="small text-muted mt-2">' + opts.note + '</div>' : '') +
                      '</div>',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Delete',
                cancelButtonText: 'Cancel',
                confirmButtonColor: '#dc3545',
                reverseButtons: true,
                customClass: { popup: 'hmis-swal' }
            }).then(function (r) { if (r.isConfirmed) { onConfirm(); } });
        } else if (global.confirm('Delete ' + opts.code + (opts.name ? ' - ' + opts.name : '') + '?')) {
            onConfirm();
        }
    }

    /**
     * Wire a list + popup screen.
     *
     * cfg:
     *   data        id of the <script type="application/json"> holding the rows
     *   modal       id of the modal element
     *   form        id of the form inside the modal
     *   title       id of the modal title element
     *   addBtn      id of the Add New button
     *   rowSelector CSS selector for list rows
     *   delSelector CSS selector for delete buttons
     *   deleteForm  id of the hidden form used to post a delete
     *   deleteId    id of the hidden input that carries the row id
     *   fields      array of field names; the input ids are Edit_<name>
     *   idField     the field that holds the primary key (default 'Id')
     *   label       noun used in the titles, e.g. 'supplier'
     *   newDefaults object of values applied when Add New is pressed
     *   deleteTitle / deleteNote   confirmation wording
     *   onFill      optional (item) => void, for extra panels
     *   onNew       optional () => void
     */
    function init(cfg) {
        var dataEl = document.getElementById(cfg.data);
        if (!dataEl) { return; }

        var rows;
        try {
            rows = JSON.parse(dataEl.textContent || '[]');
        } catch (e) {
            console.error('pharmacy crud: could not parse row data', e);
            return;
        }

        var modalEl = document.getElementById(cfg.modal);
        if (!modalEl) { return; }

        var modal = makeModal(modalEl);
        var titleEl = document.getElementById(cfg.title);
        var formEl = document.getElementById(cfg.form);
        var idField = cfg.idField || 'Id';
        var fields = cfg.fields || [];

        function setField(name, value) {
            var el = document.getElementById('Edit_' + name);
            if (!el) { return; }
            if (el.type === 'checkbox') {
                el.checked = !!value;
            } else {
                el.value = (value === null || value === undefined) ? '' : value;
            }
        }

        function fill(item) {
            fields.forEach(function (f) {
                setField(f, item[f.charAt(0).toLowerCase() + f.slice(1)]);
            });
        }

        function clear() {
            fields.forEach(function (f) { setField(f, ''); });
            setField(idField, 0);
            if (cfg.newDefaults) {
                Object.keys(cfg.newDefaults).forEach(function (k) {
                    setField(k, cfg.newDefaults[k]);
                });
            }
        }

        var addBtn = document.getElementById(cfg.addBtn);
        if (addBtn) {
            addBtn.addEventListener('click', function () {
                clear();
                if (titleEl) { titleEl.textContent = 'Add ' + cfg.label; }
                if (cfg.onNew) { cfg.onNew(); }
                modal.open();
                var first = formEl ? formEl.querySelector('input:not([type=hidden]), select, textarea') : null;
                if (first) { first.focus(); }
            });
        }

        document.querySelectorAll(cfg.rowSelector).forEach(function (tr) {
            tr.addEventListener('click', function () {
                var id = parseInt(tr.getAttribute('data-id'), 10);
                var item = null;
                for (var i = 0; i < rows.length; i++) {
                    if (rows[i].id === id) { item = rows[i]; break; }
                }
                if (!item) { return; }
                fill(item);
                if (titleEl) { titleEl.textContent = 'Edit ' + cfg.label + ' - ' + (item.code || item.id); }
                if (cfg.onFill) { cfg.onFill(item); }
                modal.open();
            });
        });

        // The delete button sits inside the row, so without stopPropagation
        // every delete would also open the edit popup underneath it.
        document.querySelectorAll(cfg.delSelector).forEach(function (btn) {
            btn.addEventListener('click', function (e) {
                e.stopPropagation();

                var id = btn.getAttribute('data-id');
                var code = btn.getAttribute('data-code') || '';
                var name = btn.getAttribute('data-name') || '';

                confirmDelete({
                    title: cfg.deleteTitle || ('Delete this ' + cfg.label + '?'),
                    code: code,
                    name: name,
                    note: cfg.deleteNote || ''
                }, function () {
                    var idInput = document.getElementById(cfg.deleteId);
                    var delForm = document.getElementById(cfg.deleteForm);
                    if (idInput && delForm) {
                        idInput.value = id;
                        delForm.submit();
                    }
                });
            });
        });

        // Server-opened fallback (?handler=Edit) - the modal is already
        // rendered with .show, so nothing needs doing here.
        return { open: modal.open, close: modal.close, fill: fill };
    }

    global.PhCrud = { init: init, esc: esc };
})(window);
