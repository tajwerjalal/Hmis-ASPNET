(function () {
    'use strict';

    // --- Print A4 (two-copy sheet) ---
    function printA4() {
        document.body.classList.add('print-a4');
        window.print();
    }

    window.onafterprint = function () {
        document.body.classList.remove('print-a4');
    };

    // --- Print Thermal 80mm ---
    function printThermal() {
        var src = document.getElementById('thermalBody');
        var frame = document.getElementById('thermalFrame');
        if (!src || !frame) return;

        var at = String.fromCharCode(64); // '@' — avoids Razor parser interception
        var pg = at + 'page';
        var css =
            '<style>'
            + pg + '{size:80mm auto;margin:0;}'
            + 'body{margin:0;width:80mm;font-family:"Consolas","Courier New",monospace;font-size:12px;color:#000;}'
            + 'div{line-height:1.4;}span{white-space:nowrap;}'
            + '</style>';

        var html =
            '<!DOCTYPE html><html><head>'
            + '<meta charset="utf-8"><title>Thermal Receipt</title>'
            + css
            + '</head><body>'
            + src.innerHTML
            + '</body></html>';

        frame.srcdoc = html;

        var fw = frame.contentWindow || frame;
        fw.focus();
        setTimeout(function () { fw.print(); }, 400);
    }

    // Expose print functions globally (called by inline onclick in cshtml)
    window.printA4 = printA4;
    window.printThermal = printThermal;

    // --- Payment modal ---
    var payAmount = 0;
    var payModalEl = document.getElementById('payModal');

    if (payModalEl) {
        payModalEl.addEventListener('show.bs.modal', function (ev) {
            var btn = ev.relatedTarget;
            payAmount = parseFloat(btn.getAttribute('data-amount')) || 0;

            document.getElementById('payIndentId').value = btn.getAttribute('data-bill-id');
            document.getElementById('payInvoice').textContent = btn.getAttribute('data-invoice');
            document.getElementById('payVisit').textContent = btn.getAttribute('data-visit');
            document.getElementById('payPatient').textContent = btn.getAttribute('data-patient');
            document.getElementById('payPhn').textContent = btn.getAttribute('data-phn');
            document.getElementById('payDept').textContent = btn.getAttribute('data-department');
            document.getElementById('payAmount').textContent = payAmount.toFixed(2);
            document.getElementById('payTendered').value = payAmount.toFixed(2);
            document.getElementById('payReference').value = '';
            document.getElementById('payMethod').value = 'Cash';
            document.getElementById('payValidationMsg').style.display = 'none';
            updateMethodFields();
        });
    }

    function updateMethodFields() {
        var v = document.getElementById('payMethod').value;
        var isCash = (v === 'Cash');
        document.getElementById('tenderBox').style.display = isCash ? '' : 'none';
        document.getElementById('refBox').style.display = isCash ? 'none' : '';
        document.getElementById('payValidationMsg').style.display = 'none';
        computeChange();
    }

    function computeChange() {
        var v = document.getElementById('payMethod').value;
        var t = parseFloat(document.getElementById('payTendered').value) || 0;
        var box = document.getElementById('payChangeBox');
        var retBox = document.getElementById('payReturnBox');
        var validMsg = document.getElementById('payValidationMsg');
        var submitBtn = document.getElementById('paySubmitBtn');

        if (v !== 'Cash') {
            box.textContent = '';
            retBox.textContent = '';
            validMsg.style.display = 'none';
            submitBtn.disabled = false;
            return;
        }

        if (t <= 0) {
            box.textContent = '';
            retBox.textContent = '';
            validMsg.textContent = 'Amount must be greater than zero';
            validMsg.style.display = 'block';
            submitBtn.disabled = true;
            return;
        }

        if (t < payAmount) {
            box.textContent = '';
            retBox.textContent = '';
            validMsg.textContent = 'Tendered amount is less than Rs ' + payAmount.toFixed(2);
            validMsg.style.display = 'block';
            submitBtn.disabled = true;
            return;
        }

        validMsg.style.display = 'none';
        submitBtn.disabled = false;

        var diff = t - payAmount;
        if (diff > 0) {
            box.textContent = 'Return: ' + diff.toFixed(2);
            box.className = 'fs-5 fw-bold change-positive';
            retBox.textContent = 'Patient gives Rs ' + t.toFixed(2) + ', return Rs ' + diff.toFixed(2);
        } else {
            box.textContent = 'Exact amount';
            box.className = 'fs-5 fw-bold';
            retBox.textContent = '';
        }
    }

    var methodSel = document.getElementById('payMethod');
    var tenderedInput = document.getElementById('payTendered');
    if (methodSel) methodSel.addEventListener('change', updateMethodFields);
    if (tenderedInput) tenderedInput.addEventListener('input', computeChange);
})();
