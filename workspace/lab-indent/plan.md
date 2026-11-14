# Lab Service Indent (Invoice) — Implementation Plan

- **Date / Time:** 26-Oct-2026 11:50
- **Author:** `[TAJWER JALAL]`
- **Status:** awaiting approval — no application code has been changed yet.
- **Related:** `workspace/lab-indent/discovery-notes.md`

---

## 1. Route / coexistence

| Screen | Route | File |
|---|---|---|
| New single-screen invoice | `/lab/indent` | **new** `Pages/Lab/IndentInvoice.cshtml` + `.cshtml.cs` |
| Existing 3-step screen (kept) | `/lab/indent-classic` | **edited** `Pages/Lab/Indent.cshtml` (route template only) |
| Thermal receipt | `/lab/indent-receipt` | **new** `Pages/Lab/IndentReceipt.cshtml` + `.cshtml.cs` |

- The old page keeps its file, model and behaviour; only its `@page` route changes to `/lab/indent-classic`.
- The old page gets a header remark + a small banner link to the new screen.
- The new page gets a "Classic screen" link in the same place.
- The sidebar `asp-page="/lab/Indent"` link is repointed to the new page so the menu opens the new screen.

---

## 2. Files to create

| File | Purpose |
|---|---|
| `Pages/Lab/IndentInvoice.cshtml` | Single-screen Razor view: title bar, patient header, permanent entry row + line grid, footer totals/payment/actions. No page scroll at 1366×768 for 8 lines (flex layout, grid scrolls internally). |
| `Pages/Lab/IndentInvoice.cshtml.cs` | `IndentInvoiceModel`. Handlers: page GET (next indent preview + consultants), `SearchPatients(q)`, `Patient(id)`, `SearchTests(q)`, `Save` (JSON), `CheckDuplicate(q)`. |
| `Pages/Lab/IndentReceipt.cshtml` | Compact 80 mm thermal receipt markup (auto `window.print()`), print-only CSS. |
| `Pages/Lab/IndentReceipt.cshtml.cs` | Loads the saved bill/indent for the receipt and renders it. |
| `Services/Lab/LabIndentQueryService.cs` | Read side: patient suggestions (max 7, exact match), patient header (company, unpaid balance, active IPD ward/bed/consultant), test search (max 20 + `hasMore`, multi-word ranking), next indent preview. |
| `Services/Lab/LabIndentService.cs` | Write side: one transaction for patient creation (if new), indent number, `OBIL`/`IBIL` indent, `BILL`/`BILLITEM`, `INVESTIGATIONORDER`, optional cash receipt, audit, idempotency. |
| `workspace/lab-indent/sql/20261026_lab_indent_indexes.sql` | Proposed indexes only — **not executed automatically**. |
| `workspace/lab-indent/README.md` | Deliverable: how to run, settings, route toggle, known limitations. |
| `workspace/lab-indent/test-checklist.md` | Manual acceptance checklist + expected results. |
| `workspace/lab-indent/discovery-notes.md` | Already written. |
| `workspace/lab-indent/plan.md` | This file. |

## 3. Files to modify

| File | Change |
|---|---|
| `Pages/Lab/Indent.cshtml` | `@page "/lab/indent"` → `@page "/lab/indent-classic"`; add "Modified by / Date / What changed" remark; add banner link to `/lab/indent`. |
| `Pages/Lab/Indent.cshtml.cs` | Add "Modified by / Date / What changed" remark only (no behaviour change). |
| `Pages/Shared/_Layout.cshtml` | Sidebar Lab Service Indent link → `asp-page="/lab/IndentInvoice"`. |
| `Program.cs` | Register `ILabIndentService` and `ILabIndentQueryService` (scoped). |
| `appsettings.json` | Add `"LabIndent": { "CashNowEnabled": false }`. |
| `Data/ApplicationDbContext.cs` | **No change** unless a DB idempotency table is approved. |

---

## 4. Save transaction (one DB transaction)

1. Re-validate: patient (existing or new data), ≥ 1 test, no duplicate test codes, rates re-read from `SERVICES`, discount `0..charge`, approver when concession > 0, active IPD still admitted, payment mode permitted.
2. `sp_getapplock 'LAB_INDENT_01'` (transaction owner) → serialises indent number + new `PATIENT.PatientId` allocation.
3. If new patient: `PERSON` + `PATIENT` (approximate `Dob` from age, `CreatedInstitutionId`, audit).
4. Indent number: `MAX(TRY_CAST(SLIPNO AS bigint)) + 1` for `OBIL_MST.DEPT='01'`; `SLIPNO = n:D9`, display `LI-{n:D6}`.
5. Insert `OBIL_MST`/`OBIL_DET` (OPD) or `IBIL_MST`/`IBIL_DET` (IPD), stamped `BILL = <new BILL id>` and `BDATE = now` after the bill is inserted, so GENBILL cannot re-bill it.
6. Insert `BILL` + `BILLITEM` (server-computed amounts, `ItemId1 = null`, `ItemId = service code`, `DeptId = '01'`, `Priority = Urgent` when flagged).
7. Insert one `INVESTIGATIONORDER` per line (`Ordered = true`, `SampleCollected = false`, `Completed = false`, `ItemId = null`, `DepartmentId` = lab department).
8. If `CashNowEnabled` and OPD + "Cash now": post cash receipt (`CCONTROL` counter → `CRCVD`/`CRCVDDET`, mark bill paid). Otherwise OPD stays unpaid for the cashier; IPD posts to the encounter account.
9. Audit: legacy `USERID`/`UDATE`/`UTIME` + app `CreaterId`/`CreatedAt`; commit.
10. Any failure → full rollback; JSON error with field-level messages (no blocking alert).

## 5. Idempotency

- Client generates a GUID per page load and sends it with Save.
- Server caches the success payload in session under `LabIndentResult:{token}` (10 min); a repeated POST returns the cached payload.
- Save button is disabled while the request is in flight.
- Limitation: protects double-click/retry in the same browser session. Cross-browser/DB-level dedupe would need a new table (see decisions).

## 6. Search / performance

- Patient: server-side `LIKE` over `PATIENT.PatientId`, `PATIENT.Phn`, `PERSON.Name`, `PERSON.FullName`, `PERSON.NameWithInitials`, `PERSON.Mobile`, `PERSON.Nic`; top 7; exact-match flag for Enter auto-select.
- Tests: server-side `SERVICES` `DEPT='01'`, `BILLABLE`, `ACTIVE`, `!RETIRED`; multi-word AND across `CODE`/`DESC`/`SNAME`; prefix ranked first; top 20 + `hasMore`; already-added codes excluded server-side.
- 200 ms debounce, `AbortController` cancels stale requests, loading spinner.
- SQL script proposes indexes for `PERSON`/`PATIENT`/`SERVICES`; no full catalog is ever shipped to the browser.

## 7. UI / keyboard

- Autofocus patient search; 7 live suggestions; Up/Down/Enter; exact Reg no/CNIC selects on Enter.
- Permanent last entry row; test suggestions; Enter adds; Esc closes; Delete/trash removes selected line.
- Per-line urgent toggle, discount input (`0..charge`), charge read-only, net computed.
- Footer: concession approver (required when concession > 0), payment (OPD: "Pay at cashier" default; "Cash now" only when enabled; IPD hides payment), gross/concession/net, Clear, Save and print.
- After save: clear, green confirmation, open receipt, refocus patient search.
- F2 = patient search, Ctrl+S = Save and print; hints in title bar.
- Money: right-aligned, thousands separators, decimal-safe formatting.

## 8. Billing-table decision (recommended)

| Case | Recommended | Alternative |
|---|---|---|
| OPD | `OBIL_MST`/`OBIL_DET` + `BILL`/`BILLITEM` (unpaid, `OBIL.BILL` stamped) | `BILL` only (simplest, loses the legacy indent record) |
| IPD | `IBIL_MST`/`IBIL_DET` + `BILL` (`InwardServiceBill`, stamped) | `BILL` only (matches today's classic page) |

The recommended option satisfies the earlier requirement ("keep bill detail in `OBIL_DET`/`OBIL_MST` for OPD") **and** keeps the Cashier Receipt working, without double billing (the `BILL` stamp removes the slips from GENBILL).

## 9. Known limitations to document

- Walk-in lab patients have no legacy `REGISTER` row, so `/opd/Billing` (which filters `REGISTER.TYPE='O'`) will not list their `OBIL` slips; the app `BILL` is the cashier-facing record.
- `PERSON` has no age column → approximate `Dob` is stored for new patients.
- `INVESTIGATIONORDER` has no indent-number or bill-id column → orders link by patient/encounter/time; the receipt uses the bill/indent.
- No test framework exists in the solution → manual checklist, no new test project unless approved.

---

## 10. Open decisions (need answers before coding)

1. **Developer name** for every header remark — `[TAJWER JALAL]` or another name?
2. **Billing tables** — recommended `OBIL`/`IBIL` + `BILL`, or `BILL` only?
3. **Idempotency** — session-based (no schema change) or a new DB table + migration?
4. **New-patient age** — approximate `Dob` (no schema change) or add `AGE` columns (schema change)?
5. **Cash now** — implement the paid path now behind the setting, or ship it disabled and untested?
