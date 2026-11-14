/*
 * ============================================================================
 * File........: wwwroot/js/lab-testresult.js
 * Purpose.....: Client controller for the LB0110 Test Result Entry page. Handles
 *               the List/Entry tabs, TESTTR row selection, the READTR and
 *               culture grids, Insert-key pick lists and the AJAX save.
 * Inputs......: Partials rendered by TestResultEntry.cshtml(.cs) and the
 *               antiforgery token rendered on the page.
 * Outputs.....: fetch calls to the named page handlers (List, Entry, Readings,
 *               Culture, ResultChoices, Remarks, Organisms, GrowthRemarks,
 *               Medicines, AllMedicines, LoadAllGroup, RemoveBlank, Save).
 * Business
 *   rules.....: 1. Result entry (READTR, notes, culture) is enabled as soon as
 *                  an indent/test is opened; the patient header, Age Type,
 *                  Sr.No and Report Date stay read-only until Edit is pressed.
 *               2. Insert opens the pick list for the focused box; empty lists
 *                  show the legacy error text.
 *               3. Enter moves to the next result cell; double-click on Normal
 *                  Range copies the range into Result.
 *               4. Only result lines with a value (and medicine lines with at
 *                  least one A/B/C sensitivity) are posted; Save drops blank
 *                  lines again on the server.
 *               5. The first time the Culture tab is opened for a test while
 *                  result entry is enabled, ?handler=Culture&edit=true is
 *                  called so TESTTR.CULTURE is set and the CULTURE row is
 *                  created; later tab switches keep unsaved lines.
 * Developer...: Tajwer Jalal
 * Date/Time...: 2026-11-02 15:12
 * ============================================================================
 */
(function () {
    'use strict';

    var app = {
        edit: false,          // header / TESTTR metadata edit mode (Edit button)
        resultEdit: false,    // READTR / notes / culture entry (always on for an entry)
        cultureEnsuredFor: '',// labNo|tid|testNo whose CULTURE row is already ensured
        medicineTimer: null,
        listLoaded: false
    };

    // ------------------------------------------------------------------
    // small helpers
    // ------------------------------------------------------------------
    function $(id) { return document.getElementById(id); }
    function qs(sel, root) { return (root || document).querySelector(sel); }
    function qsa(sel, root) {
        return Array.prototype.slice.call((root || document).querySelectorAll(sel));
    }
    function enc(value) { return encodeURIComponent(value == null ? '' : value); }
    function esc(value) {
        return String(value == null ? '' : value).replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    }
    function token() {
        var el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }
    function showAlert(type, message) {
        // #lbAlert is outside the entry pane, so a success message survives the
        // auto-advance to the next pending test.
        var host = $('lbAlert') || $('entryAlert');
        if (!host) return;
        host.innerHTML = '<div class="alert alert-' + type + ' alert-dismissible py-2 fade show">' +
            esc(message) +
            '<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button></div>';
        host.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }

    /*
     * ====================================================================
     * Function....: showSaveAlert
     * Purpose.....: SweetAlert2 success/failure dialog for the Save action.
     *               Keeps the inline #lbAlert message as a persistent record
     *               and as the fallback when the SweetAlert CDN is missing.
     * Inputs......: type (success|danger), message, optional printUrl.
     * Outputs.....: SweetAlert modal; "Print Report" opens printUrl.
     * Developer...: Tajwer Jalal
     * Date/Time...: 2026-11-03 12:16
     * ====================================================================
     */
    function showSaveAlert(type, message, printUrl) {
        showAlert(type, message);
        if (!window.Swal || !window.Swal.fire) return;

        var isSuccess = type === 'success';
        window.Swal.fire({
            title: isSuccess ? 'Saved' : 'Save failed',
            text: message || '',
            icon: isSuccess ? 'success' : 'error',
            confirmButtonText: printUrl ? 'Print Report' : 'OK',
            showCancelButton: !!printUrl,
            cancelButtonText: 'Close',
            reverseButtons: true,
            allowOutsideClick: false
        }).then(function (result) {
            if (printUrl && result.isConfirmed) {
                var win = window.open(printUrl, '_blank');
                if (!win) window.location.href = printUrl;
            }
        });
    }
    function formData(values) {
        var body = new URLSearchParams();
        Object.keys(values).forEach(function (key) {
            body.append(key, values[key] == null ? '' : values[key]);
        });
        return body;
    }
    function entryValue(id) {
        var el = $(id);
        return el ? el.value : '';
    }
    function hiddenValues() {
        return {
            labNo: entryValue('entryLabNo'),
            tid: entryValue('entryTid'),
            testNo: entryValue('entryTestNo'),
            gid: entryValue('entryGid')
        };
    }
    function currentTestRow() {
        var tid = entryValue('entryTid');
        var testNo = entryValue('entryTestNo');
        return qsa('#testGrid .test-row').find(function (row) {
            return (row.dataset.tid || '') === tid && (row.dataset.testno || '') === testNo;
        });
    }

    // ------------------------------------------------------------------
    // edit mode
    // ------------------------------------------------------------------
    function canEnterResults() {
        return app.resultEdit || app.edit;
    }
    function isResultField(el) {
        if (el.id === 'testRemarks') return true;
        return !!(el.closest && el.closest('#resultHost, #cultureHost'));
    }
    function applyEditModeTo(root) {
        qsa('input, select, textarea', root).forEach(function (el) {
            if (el.type === 'hidden' || el.dataset.always === '1') return;
            el.disabled = isResultField(el) ? !canEnterResults() : !app.edit;
        });
    }
    function applyActionButtons() {
        ['btnAddReading', 'btnDeleteReading', 'btnLoadAllGroup', 'btnRemoveBlank',
            'btnAddMedicine', 'btnDeleteMedicine', 'btnLoadAllMedicines',
            'btnRemoveBlankMedicines', 'btnInsertRemark'].forEach(function (id) {
                var el = $(id);
                if (el) el.disabled = !canEnterResults();
            });
        qsa('.btn-pick-species, .btn-pick-growth').forEach(function (el) {
            el.disabled = !canEnterResults();
        });
    }
    function setEditMode(on) {
        app.edit = !!on;
        // READTR, notes and culture are enterable without pressing Edit.
        app.resultEdit = true;
        var pane = $('entryPane');
        if (!pane) return;
        applyEditModeTo(pane);
        qsa('input[data-always="1"]', pane).forEach(function (el) { el.disabled = true; });
        if ($('btnEditEntry')) $('btnEditEntry').disabled = app.edit;
        if ($('btnSaveEntry')) $('btnSaveEntry').disabled = !canEnterResults();
        if ($('btnCancelEntry')) $('btnCancelEntry').disabled = !canEnterResults();
        applyActionButtons();
        if (app.edit) {
            if ($('ageType')) $('ageType').focus();
        }
    }
    function cultureTabActive() {
        var tab = $('cultureTab');
        return tab && tab.classList.contains('active');
    }

    // ------------------------------------------------------------------
    // TABS
    // ------------------------------------------------------------------
    function showTab(buttonId) {
        var btn = $(buttonId);
        if (btn && window.bootstrap) {
            bootstrap.Tab.getOrCreateInstance(btn).show();
        }
    }
    function findIndent() {
        var alreadyActive = $('listTabBtn') && $('listTabBtn').classList.contains('active');
        showTab('listTabBtn');
        if (alreadyActive && !app.listLoaded) loadList(1);
        var input = $('listLabNo');
        if (input) { input.focus(); input.select(); }
    }

    // ------------------------------------------------------------------
    // LIST TAB
    // ------------------------------------------------------------------
    function loadList(page) {
        var form = $('listSearchForm');
        if (!form) return;

        var body = new FormData(form);
        var labNo = (body.get('labNoFilter') || '').toString().trim();
        if (/^\d{9}$/.test(labNo)) { openEntry(labNo); return; }

        body.set('pageNumber', page || 1);
        body.set('pageSize', $('listPageSize') ? $('listPageSize').value : 25);
        if (!body.has('pendingOnly')) body.set('pendingOnly', 'false');

        fetch('/lab/TestResultEntry?handler=List&' + new URLSearchParams(body).toString())
            .then(function (r) { return r.text(); })
            .then(function (html) { $('listHost').innerHTML = html; app.listLoaded = true; })
            .catch(function (err) { showAlert('danger', 'List search failed: ' + err.message); });
    }

    function bindListEvents() {
        var host = $('listHost');
        if (!host) return;

        host.addEventListener('submit', function (e) {
            if (e.target && e.target.id === 'listSearchForm') {
                e.preventDefault();
                loadList(1);
            }
        });
        host.addEventListener('click', function (e) {
            var open = e.target.closest('.indent-open') || e.target.closest('.indent-row');
            if (open) {
                e.preventDefault();
                openEntry(open.dataset.labno);
                return;
            }
            var page = e.target.closest('.list-page');
            if (page && !page.closest('.disabled')) {
                e.preventDefault();
                loadList(parseInt(page.dataset.page, 10) || 1);
                return;
            }
            if (e.target.closest('#btnClearListFilters')) {
                e.preventDefault();
                if ($('listLabNo')) $('listLabNo').value = '';
                if ($('listHrno')) $('listHrno').value = '';
                if ($('listName')) $('listName').value = '';
                if ($('listPendingOnly')) $('listPendingOnly').checked = false;
                loadList(1);
            }
        });
        host.addEventListener('change', function (e) {
            if (e.target && e.target.id === 'listPageSize') loadList(1);
        });
    }

    // ------------------------------------------------------------------
    // ENTRY TAB
    // ------------------------------------------------------------------
    function openEntry(labNo, tid, testNo) {
        var url = '/lab/TestResultEntry?handler=Entry&labNo=' + enc(labNo) +
            '&tid=' + enc(tid || '') + '&testNo=' + enc(testNo || '');

        fetch(url)
            .then(function (r) { return r.text(); })
            .then(function (html) {
                $('entryHost').innerHTML = html;
                showTab('entryTabBtn');
                var stateUrl = '/lab/TestResultEntry?labNo=' + enc(labNo);
                if (tid) stateUrl += '&tid=' + enc(tid) + '&testNo=' + enc(testNo || '');
                history.pushState({}, '', stateUrl);

                // Results are enterable immediately. The header/test metadata
                // stays read-only until Edit is pressed.
                app.cultureEnsuredFor = '';
                setEditMode(false);
            })
            .catch(function (err) { showAlert('danger', 'Could not open the indent: ' + err.message); });
    }

    function loadReadings() {
        var v = hiddenValues();
        if (!v.labNo || !v.tid) return;
        var url = '/lab/TestResultEntry?handler=Readings&labNo=' + enc(v.labNo) +
            '&tid=' + enc(v.tid) + '&testNo=' + enc(v.testNo) + '&gid=' + enc(v.gid);
        fetch(url)
            .then(function (r) { return r.text(); })
            .then(function (html) {
                $('resultHost').innerHTML = html;
                applyEditModeTo($('resultHost'));
                applyActionButtons();
            })
            .catch(function (err) { showAlert('danger', 'Could not load results: ' + err.message); });
    }

    function loadCulture(edit) {
        var v = hiddenValues();
        if (!v.labNo || !v.tid) return;
        var key = v.labNo + '|' + v.tid + '|' + v.testNo;
        var url = '/lab/TestResultEntry?handler=Culture&labNo=' + enc(v.labNo) +
            '&tid=' + enc(v.tid) + '&testNo=' + enc(v.testNo) + '&gid=' + enc(v.gid) +
            '&edit=' + (edit ? 'true' : 'false');
        fetch(url)
            .then(function (r) { return r.text(); })
            .then(function (html) {
                $('cultureHost').innerHTML = html;
                app.cultureEnsuredFor = edit ? key : '';
                applyEditModeTo($('cultureHost'));
                applyActionButtons();
            })
            .catch(function (err) { showAlert('danger', 'Could not load culture: ' + err.message); });
    }

    function selectTest(row) {
        if (!row) return;
        var v = hiddenValues();
        var tid = row.dataset.tid || '';
        var testNo = row.dataset.testno || '';
        var gid = row.dataset.gid || row.dataset.group || '';

        $('entryTid').value = tid;
        $('entryTestNo').value = testNo;
        $('entryGid').value = gid;
        $('origRemarks').value = row.dataset.remarks || '';
        $('origReportDate').value = row.dataset.repdt || '';
        $('origSrNo').value = row.dataset.srno || '0';
        $('testRemarks').value = row.dataset.remarks || '';

        var pane = $('entryPane');
        if (pane) { pane.dataset.tid = tid; pane.dataset.testno = testNo; pane.dataset.gid = gid; }

        qsa('#testGrid .test-row').forEach(function (r) {
            r.classList.toggle('table-primary', r === row);
        });

        loadReadings();

        // Ensure the culture row only when the culture tab is actually open;
        // merely selecting a test must not create a CULTURE row.
        loadCulture(cultureTabActive());
    }

    function bindEntryEvents() {
        var host = $('entryHost');
        if (!host) return;

        host.addEventListener('click', function (e) {
            if (e.target.closest('#btnEditEntry')) { setEditMode(true); return; }
            if (e.target.closest('#btnSaveEntry')) { saveEntry(); return; }
            if (e.target.closest('#btnCancelEntry')) { cancelEntry(); return; }
            if (e.target.closest('#btnFindEntry')) { findIndent(); return; }
            if (e.target.closest('#btnPrintEntry')) { printCurrentResult(); return; }
            if (e.target.closest('#btnInsertRemark')) { openRemarks(); return; }
            if (e.target.closest('#btnAddReading')) { addReadingRow(); return; }
            if (e.target.closest('#btnDeleteReading')) { deleteReadingRow(); return; }
            if (e.target.closest('#btnLoadAllGroup')) { loadAllGroup(); return; }
            if (e.target.closest('#btnRemoveBlank')) { removeBlankReadings(); return; }
            if (e.target.closest('#btnAddMedicine')) { addMedicineRow('', ''); return; }
            if (e.target.closest('#btnDeleteMedicine')) { deleteMedicineRow(); return; }
            if (e.target.closest('#btnLoadAllMedicines')) { loadAllMedicines(); return; }
            if (e.target.closest('#btnRemoveBlankMedicines')) { removeBlankMedicines(); return; }

            var speciesBtn = e.target.closest('.btn-pick-species');
            if (speciesBtn) {
                var speciesInput = qs('.culture-species[data-index="' + speciesBtn.dataset.index + '"]');
                openPickList('Organisms', '/lab/TestResultEntry?handler=Organisms', speciesInput, 'No organisms available', true);
                return;
            }
            var growthBtn = e.target.closest('.btn-pick-growth');
            if (growthBtn) {
                var growthInput = qs('.culture-growth[data-index="' + growthBtn.dataset.index + '"]');
                openPickList('Growth remarks', '/lab/TestResultEntry?handler=GrowthRemarks', growthInput, 'No growth remarks available', true);
                return;
            }

            var testRow = e.target.closest('#testGrid .test-row');
            if (testRow) {
                // Clicking an input inside the row that is already being edited
                // must not reload the grid and discard the typed notes.
                var sameRow = (testRow.dataset.tid || '') === entryValue('entryTid') &&
                    (testRow.dataset.testno || '') === entryValue('entryTestNo');
                if (!sameRow) selectTest(testRow);
            }
        });

        host.addEventListener('keydown', function (e) {
            if (e.key === 'Insert' || e.keyCode === 45) {
                var value = e.target.closest ? e.target.closest('.reading-value') : null;
                if (value) { e.preventDefault(); openResultChoices(value); return; }
                if (e.target.id === 'testRemarks') { e.preventDefault(); openRemarks(); return; }
                if (e.target.classList && e.target.classList.contains('culture-species')) {
                    e.preventDefault();
                    openPickList('Organisms', '/lab/TestResultEntry?handler=Organisms', e.target, 'No organisms available', true);
                    return;
                }
                if (e.target.classList && e.target.classList.contains('culture-growth')) {
                    e.preventDefault();
                    openPickList('Growth remarks', '/lab/TestResultEntry?handler=GrowthRemarks', e.target, 'No growth remarks available', true);
                    return;
                }
            }

            if (e.key === 'Enter') {
                if (e.target.classList && e.target.classList.contains('reading-code')) {
                    e.preventDefault();
                    var row = e.target.closest('tr');
                    var nextValue = row ? qs('.reading-value', row) : null;
                    if (nextValue) nextValue.focus();
                    return;
                }
                if (e.target.classList && e.target.classList.contains('reading-value')) {
                    e.preventDefault();
                    var values = qsa('#resultBody .reading-value');
                    var index = values.indexOf(e.target);
                    if (index >= 0 && index < values.length - 1) values[index + 1].focus();
                    else { addReadingRow(); var added = qsa('#resultBody .reading-value').pop(); if (added) added.focus(); }
                }
            }
        });

        host.addEventListener('input', function (e) {
            if (e.target.classList && e.target.classList.contains('reading-code')) {
                e.target.value = (e.target.value || '').toUpperCase();
            }
            if (e.target.id === 'medicineSearch') {
                scheduleMedicineSearch(e.target.value);
            }
        });

        host.addEventListener('dblclick', function (e) {
            var range = e.target.closest('.reading-range');
            if (!range || !canEnterResults()) return;
            var row = range.closest('tr');
            var value = row ? qs('.reading-value', row) : null;
            var text = (range.textContent || '').trim();
            if (value && text) { value.value = text; value.focus(); }
        });
    }

    // ------------------------------------------------------------------
    // RESULT GRID
    // ------------------------------------------------------------------
    function addReadingRow() {
        if (!canEnterResults()) return;
        var body = $('resultBody');
        if (!body) return;
        var empty = $('resultEmptyRow');
        if (empty) empty.remove();

        var tr = document.createElement('tr');
        tr.className = 'reading-row';
        tr.dataset.code = '';
        tr.dataset.range = '';
        tr.innerHTML =
            '<td><input class="form-control form-control-sm reading-code" maxlength="2" value="" style="text-transform:uppercase" /></td>' +
            '<td><div class="reading-name">(new reading)</div></td>' +
            '<td><input class="form-control form-control-sm reading-value" maxlength="30" value="" title="Press Insert for the standard result list" /></td>' +
            '<td class="reading-unit small"></td>' +
            '<td class="reading-range small"></td>';
        body.appendChild(tr);
        applyEditModeTo(tr);
        var code = qs('.reading-code', tr);
        if (code) code.focus();
    }

    function deleteReadingRow() {
        if (!canEnterResults()) return;
        var active = document.activeElement;
        var row = active && active.closest ? active.closest('#resultBody tr.reading-row') : null;
        if (!row) {
            var rows = qsa('#resultBody tr.reading-row');
            row = rows[rows.length - 1];
        }
        if (row) row.remove();
        ensureResultEmptyState();
    }

    function ensureResultEmptyState() {
        var body = $('resultBody');
        if (!body) return;
        if (qsa('#resultBody tr.reading-row').length === 0) {
            body.innerHTML = '<tr id="resultEmptyRow"><td colspan="5" class="text-center text-muted py-3">' +
                'No readings. Press <b>Add Line</b> or <b>Load All Group</b>.</td></tr>';
        }
    }

    function removeBlankReadings() {
        if (!canEnterResults()) return;
        qsa('#resultBody tr.reading-row').forEach(function (row) {
            var value = qs('.reading-value', row);
            if (!value || !value.value.trim()) row.remove();
        });
        ensureResultEmptyState();
    }

    function loadAllGroup() {
        if (!canEnterResults()) return;
        if (!confirm('Reload all readings for this group? Unsaved changes will be lost.')) return;
        var v = hiddenValues();
        fetch('/lab/TestResultEntry?handler=LoadAllGroup', {
            method: 'POST',
            headers: { 'RequestVerificationToken': token() },
            body: formData(v)
        })
            .then(function (r) { return r.text(); })
            .then(function (html) {
                $('resultHost').innerHTML = html;
                applyEditModeTo($('resultHost'));
                applyActionButtons();
            })
            .catch(function (err) { showAlert('danger', 'Load All Group failed: ' + err.message); });
    }

    function openResultChoices(input) {
        if (!canEnterResults()) return;
        var row = input.closest('tr');
        var codeInput = row ? qs('.reading-code', row) : null;
        var code = (codeInput ? codeInput.value.trim() : '') || (row ? row.dataset.code : '');
        var table = $('resultGrid');
        var gid = table ? table.dataset.gid : entryValue('entryGid');
        var url = '/lab/TestResultEntry?handler=ResultChoices&gid=' + enc(gid) + '&readId=' + enc(code);
        fetch(url)
            .then(function (r) { return r.json(); })
            .then(function (items) {
                openPickList('Result choices - ' + code, items, function (value) {
                    input.value = value;
                    input.focus();
                }, 'No default results available', true);
            })
            .catch(function (err) { showAlert('danger', 'Result choices failed: ' + err.message); });
    }

    // ------------------------------------------------------------------
    // NOTES
    // ------------------------------------------------------------------
    function openRemarks() {
        if (!canEnterResults()) return;
        var tid = entryValue('entryTid');
        fetch('/lab/TestResultEntry?handler=Remarks&tid=' + enc(tid))
            .then(function (r) { return r.json(); })
            .then(function (items) {
                openPickList('Remarks - ' + tid, items, function (value) {
                    if ($('testRemarks')) $('testRemarks').value = value;
                }, 'No Remarks Available', true);
            })
            .catch(function (err) { showAlert('danger', 'Remarks lookup failed: ' + err.message); });
    }

    // ------------------------------------------------------------------
    // CULTURE
    // ------------------------------------------------------------------
    function sensOptions(selected) {
        return ['', 'S', 'I', 'R'].map(function (value) {
            return '<option value="' + value + '"' + (value === selected ? ' selected' : '') + '>' +
                (value || '') + '</option>';
        }).join('');
    }

    function addMedicineRow(code, name) {
        if (!canEnterResults()) return;
        var body = $('cultureBody');
        if (!body) return;
        var empty = $('cultureEmptyRow');
        if (empty) empty.remove();

        var tr = document.createElement('tr');
        tr.className = 'medicine-row';
        tr.dataset.code = code || '';
        tr.innerHTML =
            '<td><input class="form-control form-control-sm medicine-code" maxlength="20" value="' + esc(code) + '" /></td>' +
            '<td class="medicine-name">' + esc(name || '') + '</td>' +
            '<td><select class="form-select form-select-sm medicine-s1">' + sensOptions('') + '</select></td>' +
            '<td><select class="form-select form-select-sm medicine-s2">' + sensOptions('') + '</select></td>' +
            '<td><select class="form-select form-select-sm medicine-s3">' + sensOptions('') + '</select></td>';
        body.appendChild(tr);
        applyEditModeTo(tr);
        var codeInput = qs('.medicine-code', tr);
        if (codeInput && !code) codeInput.focus();
    }

    function deleteMedicineRow() {
        if (!canEnterResults()) return;
        var active = document.activeElement;
        var row = active && active.closest ? active.closest('#cultureBody tr.medicine-row') : null;
        if (!row) {
            var rows = qsa('#cultureBody tr.medicine-row');
            row = rows[rows.length - 1];
        }
        if (row) row.remove();
        ensureCultureEmptyState();
    }

    function ensureCultureEmptyState() {
        var body = $('cultureBody');
        if (!body) return;
        if (qsa('#cultureBody tr.medicine-row').length === 0) {
            body.innerHTML = '<tr id="cultureEmptyRow"><td colspan="5" class="text-center text-muted py-3">' +
                'No medicine lines. Press <b>Add</b> or <b>Load All</b>.</td></tr>';
        }
    }

    function removeBlankMedicines() {
        if (!canEnterResults()) return;
        qsa('#cultureBody tr.medicine-row').forEach(function (row) {
            var a = qs('.medicine-s1', row);
            var b = qs('.medicine-s2', row);
            var c = qs('.medicine-s3', row);
            var blank = (!a || !a.value) && (!b || !b.value) && (!c || !c.value);
            if (blank) row.remove();
        });
        ensureCultureEmptyState();
    }

    function loadAllMedicines() {
        if (!canEnterResults()) return;
        if (!confirm('Add every medicine not yet on this test? (maximum 500, use search for the rest)')) return;
        var v = hiddenValues();
        var url = '/lab/TestResultEntry?handler=AllMedicines&labNo=' + enc(v.labNo) +
            '&tid=' + enc(v.tid) + '&testNo=' + enc(v.testNo);
        fetch(url)
            .then(function (r) { return r.json(); })
            .then(function (items) {
                var existing = qsa('#cultureBody tr.medicine-row').map(function (row) {
                    var code = qs('.medicine-code', row);
                    return code ? code.value : '';
                });
                (items || []).forEach(function (item) {
                    if (existing.indexOf(item.code) < 0) addMedicineRow(item.code, item.name);
                });
                showAlert('info', (items || []).length + ' medicine line(s) added (maximum 500).');
            })
            .catch(function (err) { showAlert('danger', 'Load All medicines failed: ' + err.message); });
    }

    function scheduleMedicineSearch(term) {
        clearTimeout(app.medicineTimer);
        app.medicineTimer = setTimeout(function () { searchMedicines(term); }, 250);
    }

    function searchMedicines(term) {
        var host = $('medicineSuggest');
        if (!host) return;
        term = (term || '').trim();
        if (term.length === 0) { host.style.display = 'none'; host.innerHTML = ''; return; }

        fetch('/lab/TestResultEntry?handler=Medicines&term=' + enc(term))
            .then(function (r) { return r.json(); })
            .then(function (items) {
                host.innerHTML = '';
                if (!items || items.length === 0) { host.style.display = 'none'; return; }
                items.forEach(function (item) {
                    var btn = document.createElement('button');
                    btn.type = 'button';
                    btn.className = 'list-group-item list-group-item-action py-1 small';
                    btn.innerHTML = '<span class="font-monospace me-2">' + esc(item.code) + '</span>' + esc(item.name);
                    btn.addEventListener('click', function () {
                        addMedicineRow(item.code, item.name);
                        if ($('medicineSearch')) $('medicineSearch').value = '';
                        host.style.display = 'none';
                        host.innerHTML = '';
                    });
                    host.appendChild(btn);
                });
                host.style.display = '';
            })
            .catch(function () { host.style.display = 'none'; });
    }

    function openMedicinePicker(input) {
        // Not used: medicine search is a type-ahead. Kept for the Insert key on the ID box.
        if (!input) return;
        var term = input.value || '';
        searchMedicines(term);
    }

    // ------------------------------------------------------------------
    // GENERIC PICK LIST
    // ------------------------------------------------------------------
    function openPickList(title, source, target, emptyMessage, isJson) {
        var modalEl = $('pickListModal');
        if (!modalEl) return;
        $('pickListTitle').textContent = title;
        var message = $('pickListMessage');
        message.style.display = 'none';
        message.textContent = '';
        var host = $('pickListItems');
        host.innerHTML = '';

        // Callers either pass an URL to fetch or an already loaded array.
        var loading = Array.isArray(source)
            ? Promise.resolve(source)
            : fetch(source).then(function (r) { return isJson ? r.json() : r.text(); });

        loading
            .then(function (items) {
                if (!items || items.length === 0) {
                    message.textContent = emptyMessage || 'No items available';
                    message.style.display = '';
                } else {
                    items.forEach(function (item) {
                        var btn = document.createElement('button');
                        btn.type = 'button';
                        btn.className = 'list-group-item list-group-item-action';
                        btn.textContent = (typeof item === 'string') ? item : (item.name || item.code || '');
                        btn.addEventListener('click', function () {
                            var value = (typeof item === 'string') ? item : (item.name || item.code || '');
                            if (typeof target === 'function') {
                                target(value, item);
                            } else if (target) {
                                target.value = value;
                                target.focus();
                            }
                            bootstrap.Modal.getOrCreateInstance(modalEl).hide();
                        });
                        host.appendChild(btn);
                    });
                }
                bootstrap.Modal.getOrCreateInstance(modalEl).show();
            })
            .catch(function (err) { showAlert('danger', 'Lookup failed: ' + err.message); });
    }

    // ------------------------------------------------------------------
    // SAVE / CANCEL
    // ------------------------------------------------------------------
    function collectReadings() {
        return qsa('#resultBody tr.reading-row').map(function (row) {
            var code = qs('.reading-code', row);
            var value = qs('.reading-value', row);
            return {
                code: code ? code.value.trim() : '',
                value: value ? value.value.trim() : ''
            };
        }).filter(function (row) {
            // Only lines that actually have a result are posted/saved.
            return row.value !== '';
        });
    }

    function collectCulture() {
        var species = {};
        var growth = {};
        qsa('.culture-species').forEach(function (el) { species[el.dataset.index] = el.value.trim(); });
        qsa('.culture-growth').forEach(function (el) { growth[el.dataset.index] = el.value.trim(); });

        var medicines = qsa('#cultureBody tr.medicine-row').map(function (row) {
            var code = qs('.medicine-code', row);
            var name = qs('.medicine-name', row);
            var s1 = qs('.medicine-s1', row);
            var s2 = qs('.medicine-s2', row);
            var s3 = qs('.medicine-s3', row);
            return {
                code: code ? code.value.trim() : '',
                name: name ? name.textContent.trim() : '',
                s1: s1 ? s1.value : '',
                s2: s2 ? s2.value : '',
                s3: s3 ? s3.value : ''
            };
        }).filter(function (row) {
            // A medicine line is only saved when at least one sensitivity
            // (A/B/C) has been entered.
            return row.code !== '' && (row.s1 !== '' || row.s2 !== '' || row.s3 !== '');
        });

        return {
            growth1: growth['1'] || '',
            growth2: growth['2'] || '',
            growth3: growth['3'] || '',
            species1: species['1'] || '',
            species2: species['2'] || '',
            species3: species['3'] || '',
            medicines: medicines
        };
    }

    /*
     * ====================================================================
     * Function....: printCurrentResult
     * Purpose.....: Open the standalone printable report for the selected
     *               test (READTR results + culture).
     * Developer...: Tajwer Jalal
     * Date/Time...: 2026-11-03 12:16
     * ====================================================================
     */
    function printCurrentResult() {
        var v = hiddenValues();
        if (!v.labNo || !v.tid) {
            showAlert('warning', 'Select a test first.');
            return;
        }
        var url = '/lab/TestResultPrint?labNo=' + enc(v.labNo) +
            '&tid=' + enc(v.tid) + '&testNo=' + enc(v.testNo) + '&print=1';
        var win = window.open(url, '_blank');
        if (!win) window.location.href = url;
    }

    function saveEntry() {
        if (!canEnterResults()) return;
        var row = currentTestRow();
        var srNo = row ? qs('.test-srno', row) : null;
        var repoDt = row ? qs('.test-repodt', row) : null;

        var payload = {
            labNo: entryValue('entryLabNo'),
            tid: entryValue('entryTid'),
            testNo: entryValue('entryTestNo'),
            aType: entryValue('ageType'),
            remarks: entryValue('testRemarks'),
            reportDate: repoDt ? repoDt.value : '',
            srNo: srNo ? (parseInt(srNo.value, 10) || 0) : 0,
            originalRemarks: entryValue('origRemarks'),
            originalReportDate: entryValue('origReportDate'),
            originalSrNo: parseInt(entryValue('origSrNo'), 10) || 0,
            readings: collectReadings(),
            culture: collectCulture()
        };

        if (!payload.labNo || !payload.tid) { showAlert('warning', 'Select a test first.'); return; }

        // Print the test that was just saved, not the next pending test that
        // the page auto-advances to after a successful save.
        var printUrl = '/lab/TestResultPrint?labNo=' + enc(payload.labNo) +
            '&tid=' + enc(payload.tid) + '&testNo=' + enc(payload.testNo) + '&print=1';

        fetch('/lab/TestResultEntry?handler=Save', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token()
            },
            body: JSON.stringify(payload)
        })
            .then(function (r) {
                return r.json().then(function (data) { return { ok: r.ok, data: data }; });
            })
            .then(function (result) {
                if (!result.ok || !result.data || !result.data.success) {
                    showSaveAlert('danger', (result.data && result.data.message) || 'Save failed.');
                    return;
                }
                showSaveAlert('success', result.data.message || 'Saved.', printUrl);
                if (result.data.nextUrl) {
                    var url = new URL(result.data.nextUrl, window.location.origin);
                    openEntry(url.searchParams.get('labNo'), url.searchParams.get('tid'), url.searchParams.get('testNo'));
                }
            })
            .catch(function (err) { showSaveAlert('danger', 'Save failed: ' + err.message); });
    }

    function cancelEntry() {
        var v = hiddenValues();
        if (!v.labNo) return;
        openEntry(v.labNo, v.tid, v.testNo);
    }

    // ------------------------------------------------------------------
    // BOOT
    // ------------------------------------------------------------------
    document.addEventListener('DOMContentLoaded', function () {
        bindListEvents();
        bindEntryEvents();

        if ($('btnFindIndent')) $('btnFindIndent').addEventListener('click', findIndent);

        document.addEventListener('shown.bs.tab', function (e) {
            if (e.target && e.target.id === 'listTabBtn' && !app.listLoaded) {
                loadList(1);
            }
            if (e.target && e.target.id === 'cultureTabBtn' && canEnterResults()) {
                var v = hiddenValues();
                var key = v.labNo + '|' + v.tid + '|' + v.testNo;
                if (v.labNo && v.tid && app.cultureEnsuredFor !== key) {
                    loadCulture(true);
                }
            }
        });

        document.addEventListener('click', function (e) {
            if (!e.target.closest('#medicineSearch') && !e.target.closest('#medicineSuggest')) {
                var suggest = $('medicineSuggest');
                if (suggest) { suggest.style.display = 'none'; }
            }
        });

        // The server may render the Entry tab active (direct link / scanner).
        app.listLoaded = document.querySelectorAll('#listHost .indent-row').length > 0;
        var active = window.LB0110_ACTIVE_TAB;
        if (active === 'entry' && $('entryPane')) {
            setEditMode(false);
        } else {
            applyActionButtons();
        }
    });
})();
