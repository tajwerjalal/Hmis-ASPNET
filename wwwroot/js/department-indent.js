// ---------------------------------------------------------------------------
// Shared department indent helpers (X-ray, Dental, Operation Theatre, CNP).
//
// Why this file exists
//   The shared partial Pages/Shared/_DepartmentIndent.cshtml replaces the old
//   per-module script blocks. Keeping the behaviour here means the toggle and
//   the "re-open the service popup after a search" logic cannot drift between
//   departments.
//
// What it does
//   * toggleNewPatient(open) shows / hides the walk-in registration panel.
//   * After a service search or an add, the modal is re-opened when the page
//     renders with data-open-on-load="true".
//
// Developer : AI Assistant
// Created   : 27-Oct-2026 11:25
// ---------------------------------------------------------------------------
(function () {
    'use strict';

    window.toggleNewPatient = function (open) {
        var el = document.getElementById('newPatientSection');
        if (el) {
            el.style.display = open ? '' : 'none';
        }
    };

    document.addEventListener('DOMContentLoaded', function () {
        var modalEl = document.getElementById('deptServiceModal');
        if (!modalEl) return;
        if (modalEl.getAttribute('data-open-on-load') !== 'true') return;

        if (window.bootstrap && window.bootstrap.Modal) {
            window.bootstrap.Modal.getOrCreateInstance(modalEl).show();
        }
    });
})();
