# Lab Service Indent (Invoice) — Discovery Notes

- **Date / Time:** 26-Oct-2026 11:45
- **Project:** `Hmis-ASPNET` (ASP.NET Core 8 Razor Pages, EF Core 8, SQL Server)
- **UI stack:** Bootstrap 5 + Bootstrap Icons + Font Awesome 6, plain JavaScript, PdfSharpCore PDFs
- **Author:** `[TAJWER JALAL]`
- **Scope of this note:** Step 0 discovery for the new single-screen `/lab/indent` page. No application code was changed to produce this note.

---

## 1. Current `/lab/indent` implementation

| Item | Finding |
|---|---|
| View | `Pages/Lab/Indent.cshtml` — `@page "/lab/indent"` |
| Page model | `Pages/Lab/Indent.cshtml.cs` — `HmisWeb.Pages.Lab.IndentModel` |
| Flow | 3 steps: POST patient search → select patient → open a Bootstrap modal to add lab services → POST save. |
| State | Patient id in session `LabIndentPatientId`; cart JSON in session `LabIndentCart`. |
| Billing on save | `BILL` + `BILLITEM`. OPD = `BillType.OpdBill` / `BillClassType.BilledBill`, `Paid = false`. Active IPD = `BillType.InwardServiceBill` / `BillClassType.PreBill`, `PatientEncounterId` + `ReferenceNumber = BHT`. |
| Lab master | `SERVICES` through `DeptService` where `DEPT = '01'`. Line stores `BillItem.ItemId1 = null`, `BillItem.ItemId = service.Code`, `BillItem.DeptId = '01'`. |
| Patient search | `ICommonService.SearchPatientsAsync` — `Contains` over name/NIC/mobile/PHN, top 50, POST round-trip, no live suggestions. |
| Test search | `OnGetSearchItemsAsync` / `OnPostSearchItemsAsync` page handlers — top 50, no debounce, no cancellation. |
| Print | A4 PDF via PdfSharpCore (`OnGetPrintInvoice`) + on-page invoice card (`OnGetViewInvoice`). |
| Numbering | No indent number at all. A `BILL` row id is used as the reference. |
| Idempotency | None. |
| Lab worklist | **Not created.** `INVESTIGATIONORDER` is never written, so Sample Collection, Test Result Entry and the Indent Report do not see these indents. |
| Discount | Per-line discount is posted and trusted; no approver field; no server-side recomputation. |

### Gaps against the new specification

1. No live patient suggestions / keyboard flow.
2. No server-side, indexed, debounced test search (max 20 rows, stale-request cancellation).
3. No permanent entry row, no keyboard add/remove, no urgent flag.
4. No concession approver, no server-side discount limit, no master-price re-read.
5. No concurrency-safe indent number.
6. No idempotency token / double-submit protection.
7. No `INVESTIGATIONORDER` rows (lab worklist broken for these indents).
8. No legacy OPD/IPD indent record (`OBIL_MST/DET`, `IBIL_MST/DET`).

---

## 2. Tables and entities (verified in the codebase)

### Patient / person
| Table | Model | Relevant columns |
|---|---|---|
| `PATIENT` | `Models/Patient.cs` | `Id`, `PatientId` (numeric registration / PHN shown on receipts), `Phn`, `PersonId`, `CreatedInstitutionId`, audit |
| `PERSON` | `Models/Person.cs` | `Name`, `FullName` (`TNAME`), `NameWithInitials` (`SNAME`), `Nic`, `Mobile`, `Phone`, `Dob`, `Sex`, audit |
| `PATIENTENCOUNTER` | `Models/PatientEncounter.cs` | `EncounterType` (`Opd`/`Inward`), `BhtNo`, `DateOfAdmission`, `DateOfDischarge`, `OpdDoctorId`, `CreditCompanyId`, `DepartmentId` |
| `WARD` / `BED_POS` / `PATIENTROOM` | `Models/Ward.cs`, `BedPosition.cs`, `PatientRoom.cs` | Current ward/bed for an active IPD encounter |
| `COMPANYEMPLOYEE` | `Models/CompanyEmployee.cs` | `PersonId` → `CompanyId` (panel membership) |
| `COMPANY` | `Models/CompanyMaster.cs` | Keyless legacy company register (name) |
| `INSTITUTION` | `Models/Institution.cs` | Panel company name/printing data |
| `STAFF` / `CONSULTANT` / `DOCTOR` | `Models/Staff.cs`, `Consultant.cs`, `Doctor.cs` | Consultant list; encounter links by `OpdDoctorId` |

### Services / prices
| Table | Model | Relevant columns |
|---|---|---|
| `SERVICES` | `Models/DeptService.cs` | `DEPT`, `CODE`, `DESC` → `Name`, `SNAME` → `Sname`, `SRATE` → `Rate`, `BILLABLE`, `ACTIVE`, `RETIRED`, `PRES` → `Requirements`, `CATEGORYID`. Lab = `DEPT='01'`. |
| `ITEM` | `Models/Item.cs` | Billing mirror rows `INV-LB-{code}` created by `DeptBillingMirrorService`; needed only for company-rates lookups. |
| `COMPANYSERVICERATES` | `Models/CompanyServiceRate.cs` | `CompanyId`, `ItemId`, `Rate` — panel rates are keyed to the `ITEM` mirror, not to `SERVICES.Id`. |

### Bills / indents
| Table | Model | Purpose |
|---|---|---|
| `BILL` / `BILLITEM` | `Models/Bill.cs`, `BillItem.cs` | App billing ledger. **Cashier Receipt collects unpaid `BILL` rows.** |
| `OBIL_MST` / `OBIL_DET` | `Models/Legacy/ObilMst.cs`, `ObilDet.cs` | Legacy **OPD** indent header/detail (`DEPT`, `SLIPNO`, `SLIPDT`, `TYPE`, `NTOTAL`, `STAT`, `BILL`, `BDATE`; detail `CODE`, `QTY`, `RATE`, `NAMT`, `DESC`, `REM`, `CONCODE`, audit, `Retired`). Indexed `(HosCode, Dept)` and `(Dept, SlipNo)`. |
| `IBIL_MST` / `IBIL_DET` | `Models/Legacy/IbilMst.cs`, `IbilDet.cs` | Legacy **IPD** indent header/detail (same shape). |
| `OPDBILL` / `IPDBILL` / `BCONTROL` | `Models/Legacy/OpdBillHeader.cs`, `Bcontrol.cs` | Generated final bills; `BCONTROL.OPDBILL` / `IPDBILL` counters. Not required at indent time. |
| `INVESTIGATIONORDER` | `Models/InvestigationOrder.cs` | **The app's lab worklist.** Read by `Lab/SampleCollection`, `Lab/TestResultEntry`, `Lab/IndentReport`. |
| `CRCVD` / `CRCVDDET` / `CCONTROL` | `Models/CashierModule.cs` | Cash receipts and their counter (`CCONTROL.RECNO`). |
| `OPDBILLAUDIT` / `IPDBILLAUDIT` | `Models/Legacy/OpdBillAudit.cs`, `IpdBillAudit.cs` | Legacy billing audit trails. |

### Key business facts
- Department codes (`Services/DeptServiceScopes.cs`): `01` Laboratory, `02` X-ray/CT, `03` Ultrasound, `04` Dental, `06` OT, `07` Room & bed, `08` Ward procedures, `11` Visits.
- Lab services = `SERVICES.DEPT = '01'`; code `CODE`, name `DESC`, short `SNAME`, rate `SRATE`.
- Active IPD = `PatientEncounter.EncounterType == EncounterType.Inward && DateOfDischarge == null`.
- Cashier Receipt (`Pages/Cashier/Receipt.cshtml.cs`) lists `BILL` rows where `!Paid`, `NetTotal > 0`, `NetTotal - PaidAmount > 0.004`.
- The app runs on `https://localhost:7100`; `HmisWeb.exe` must be stopped before `dotnet build` (file lock).

---

## 3. Existing services and patterns to reuse

| Service / pattern | Use |
|---|---|
| `ICommonService.SearchPatientsAsync` | Baseline patient lookup (kept for the classic page; the new page uses its own paged query). |
| `ICommonService.GetPatientCompanyIdAsync` | Panel company of a person (`COMPANYEMPLOYEE`). |
| `ICommonService.GetAppliedRateAsync(DeptService, …)` | **X-ray only** — hard-codes `INV-XR-` mirror. Lab needs its own `INV-LB-` resolver. |
| `IAppSession` | `LoggedUser`, `LoggedSite`, `LoggedDepartment`, `WebUserId`, `HasPrivilege`. |
| `OpdBillQueryService` / `OpdBillLineService` | Legacy OPD `OBIL` / `OPDBILL` path. `OpdBillLineService.GuardEditableAsync` flips `REGISTER.BILLGEN = 1` on first write (looks inverted); **not reused as-is**. |
| `IpdBillLineService` / `IpdBillWorkflowService` | Legacy IPD `IBIL` / `IPDBILL` path. |
| `DeptBillingMirrorService` | Creates `INV-LB-{code}` `ITEM` rows so `CompanyServiceRates` can be applied. |
| Number allocation | No dedicated service. Existing pattern: `MAX(SLIPNO) + 1` per department (`Opd/Billing.GetNextSlipNoAsync`, `Inward/Billing`); `BILL.InvoiceNumber = "IN-{id:D6}"` after insert. |
| Audit | Columns `USERID` / `UDATE` / `UTIME`, `CreatedAt` / `CreaterId`; `OPDBILLAUDIT` / `IPDBILLAUDIT` for legacy billing. No generic audit service. |
| Print | PdfSharpCore A4 PDF in the classic page; no thermal receipt. |
| Tests | **No test project and no test framework in the solution.** |

---

## 4. Smallest proposed changes (no schema change unless approved)

1. **Indent number** — allocate `MAX(TRY_CAST(SLIPNO AS bigint)) + 1` for `OBIL_MST.DEPT='01'` inside the save transaction, guarded by `sp_getapplock` (`LAB_INDENT_01`) so two receptionists never share a number. Store `SLIPNO = n:D9`; display `LI-{n:D6}`.
2. **Idempotency** — cache the successful result in session under `LabIndentResult:{token}`; a repeated POST with the same token returns the cached result. No schema change. (A DB table is the alternative; see open decisions.)
3. **New-patient age** — `PERSON` has no age column. Store an approximate `Dob = today.AddYears(-age)` and display the computed age. No schema change. (Adding `AGE` columns is the alternative.)
4. **OPD billing** — write `OBIL_MST` + `OBIL_DET` as the indent record **and** `BILL` + `BILLITEM` as the collectable unpaid bill, stamping `OBIL_MST.BILL = BILL.Id` and `BDATE = now` so the GENBILL batch cannot re-bill the same slips.
5. **IPD billing** — write `IBIL_MST` + `IBIL_DET` and a `BILL` (`InwardServiceBill`, `PreBill`) linked to the active encounter, stamping `IBIL_MST.BILL = BILL.Id`; the Cashier Inpatient screen already works off `BILL`.
6. **Lab worklist** — create one `INVESTIGATIONORDER` row per line (`PatientId`, `PatientEncounterId`, `InvestigationName`, `Description`, `Ordered = true`, `SampleCollected = false`, `Completed = false`, `DepartmentId` = lab department). `ItemId = null` because lab services are not `ITEM` rows.
7. **Panel rate** — resolve the `INV-LB-{code}` `ITEM` id, look up `COMPANYSERVICERATES`, otherwise use `SERVICES.SRATE`. Server always recomputes; browser prices are ignored.
8. **Print** — compact 80 mm thermal HTML receipt with `window.print()`; keep the old A4 PDF for the classic page.
9. **UI** — Bootstrap 5 + vanilla JS, `fetch` + `AbortController`, 200 ms debounce, no new libraries.

---

## 5. Risks / notes

- **Legacy `REGISTER` rows** — `OBIL_MST.HOS_CODE` is the legacy registration number. A walk-in lab patient created on this screen has `PATIENT`/`PERSON` rows but no `REGISTER` row, so the legacy `/opd/Billing` list (which filters `REGISTER.TYPE='O'`) will not show the `OBIL` row. The app `BILL` is the screen the cashier uses, so collection still works. This is documented as a known limitation.
- **`InvestigationOrder.ItemId`** is a FK to `ITEM`; lab services from `SERVICES` have no `ITEM` id, so it stays `null`. Sample type/container fall back to the name-based defaults in `SampleCollection`.
- **Serialised saves** — the `sp_getapplock` guard makes lab saves one-at-a-time. At the stated volume (15–20 patients at rush hour) this is negligible and guarantees unique indent numbers and unique `PATIENT.PatientId`.
- **Git state** — the project has many staged/unstaged changes (including the current `Lab/Indent` and `Xr/Indent` edits). Nothing will be reverted.
- **Developer name** — confirmed: `[TAJWER JALAL]` (project convention).

---

## 6. Legacy source review — `Labsys`, `US`, `billsys` (added 26-Oct-2026 12:20)

Reviewed the VFP sources and the Billsys review pack. Extracted source text is in
`workspace/lab-indent/legacy/` (not part of the app).

### 6.1 The legacy process, in order

| Step | Legacy form / program | Tables written | Key rules |
|---|---|---|---|
| 1. OPD patient file | `billsys/FORMS/BS0101` (Outdoor Patient) | `REGISTER` (pre-existing), `PATIENT`, `COMPANY` read | One `REGISTER` row per OPD file, `TYPE='O'`; `BILLGEN=0` until the bill is generated. |
| 2. Department indent | `BS010110` Lab, `BS010120` X-Ray, `BS010130` US, `BS010140` Dental, `BS010150` Procedures | `OBIL_MST` + `OBIL_DET` | `HOS_CODE = REGISTER.REGNO`; `DEPT` = `01/02/03/04/11`; `SLIPNO` `999-999999` (manual); `SLIPDT`; `TYPE='A'`; `STAT='E'`; `BILL=0`; detail `CODE` (5-char service code), `QTY`, `RATE`, `NAMT=QTY*RATE`, `RID='HM'`, `DESC`, `FLAG='B'`. |
| 3. OPD bill | `BS0101` → `GENBILL` (Administration, menu `bs0425`) | `BCONTROL`, `OPDBILL`, `OBIL_MST`, `REGISTER` | Select `REGISTER.TYPE='O'` + `OBIL_MST.BILL=0` in the date range, group by `REGNO`, sum `NTOTAL` (`TYPE='R'` negative); `BCONTROL.OPDBILL+1`; insert `OPDBILL`; stamp `OBIL_MST.BILL/BDATE`; `REGISTER.BILLGEN=1`, `TBILLAMT=total`. |
| 4. OPD payment | Cashier module (CASH.DBC / legacy Accounts) | `CRCVD` + `CRCVDDET`; `REGISTER.TRCVD/TPYMT/TBILLAMT/CASHDISC/DISCBY/SETTLE/SETTLEDT`; `OPDBILL.STAT/RDATE/RUSERID/RTIME` | No payment is taken on the department indent screen. The cashier receipt references `REGNO` and `DEPT`/`INDENTNO`. |
| 1i. IPD patient file | `BS0110` (Indoor Patient) | `REGISTER` (`TYPE='I'`, pre-existing), `PATIENT` read | Ward/bed from `REGISTER.BEDNO`; file open until discharge. |
| 2i. IPD department indent | `BS011001..BS011070` (Pharmacy/Lab/X-Ray/US/Dental/Procedures) | `IBIL_MST` + `IBIL_DET` | Same shape as OPD, `HOS_CODE = REGISTER.REGNO`; `BILL=0`; charges accumulate on the IPD account. |
| 3i. IPD bill | `GENBILLI` (menu `bs0460`) | `BCONTROL`, `IPDBILL`, `IBIL_MST`, `REGISTER` | Same as OPD but no slip-date range restriction in the legacy code; stamps `IBIL_MST.BILL`. |
| 4i. IPD payment | Cashier / discharge settlement | `CRCVD`/`CRCVDDET`, `REGISTER`, `IPDBILL` | Settled at/after discharge; no payment at indent. |

### 6.2 Standalone Labsys/US indent app (the department-only path)

- `US/FORMS/US0101` (Laboratory / Ultrasound indent) writes **`COLLECT`** (header) + **`TRANS`** (lines), not `OBIL`.
  - `COLLECT`: `INDENTNO` (`999-999999`, manual), `DATE`, `TIME`, `CID` (company), `REGNO`, `TYPE` (`OPD`/`IPD`), `PCODE`, `CONCODE`, `SEX`, `AGE`, `ATYPE`, `NAME`, `BEDNO`, `PHONE`, `CONS` (concession), `CONSBY` (approved by), `CREDITBY` (default `CASH`), `CREDITNO`, `BALANCE`, `REFUND`, `RDATE`, `TOTAL`, `NET = TOTAL - CONS`, `CREDIT`, `POSTED`.
  - `TRANS`: `INDENTNO`, `SID` (service id), `TESTNO`, `CHARGES`, `CDATE`, `URGENT='Y'`, `RID`, `SNAME`, `CONCODE`.
- `US/FORMS/INDPOST` posts `COLLECT` + `TRANS` into `OBIL_MST/DET` (when `TYPE` starts with `O`) or `IBIL_MST/DET` (otherwise):
  - `OBIL_MST`: `DEPT`, `SLIPNO = COLLECT.INDENTNO`, `HOS_CODE = COLLECT.REGNO`, `SLIPDT = COLLECT.DATE`, `TYPE='A'`, `NTOTAL = COLLECT.TOTAL`, `STAT='P'`.
  - `OBIL_DET`: `CODE = TRANS.SID`, `QTY=1`, `RATE = TRANS.CHARGES`, `NAMT = TRANS.CHARGES`, `RID`, `FLAG='B'`, `CONCODE`, `CONPAY`, `DESC = TRANS.SNAME`.
  - Marks `COLLECT.POSTED='P'`; skips if the slip number already exists for the file; skips if `REGISTER.BILLGEN` is true.
- `US/FORMS/OPDPOST` is the panel/company variant of the same post.
- The legacy indent number is **manual**: `US0101.newrecord` builds `pre1 + DD + '000000'` (decade digit + day + six zeroes) and the user types the six-digit sequence. There is no auto counter. The new page will auto-generate the sequence safely and keep the same 9-character shape.

### 6.3 What exists in the ASP.NET app today

| Legacy piece | App equivalent | Status |
|---|---|---|
| `REGISTER` (OPD/IPD file) | `Models/Legacy/RegisterLegacy` | Read by `/opd/Billing` and `/inward/Billing`; **not created** by the app's patient registration (which writes `PATIENT` + `PATIENTENCOUNTER`). |
| `OBIL_MST/DET`, `IBIL_MST/DET` | Models + `OpdBillLineService` / `IpdBillLineService` | Present. `OpdBillLineService` creates `OBIL_MST` with `TYPE='N'`, `STAT='E'`, `BILL=0` and details with `NAMT=QTY*RATE`, `Retired=false`. **`GuardEditableAsync` flips `REGISTER.BILLGEN=1` on the first add and then rejects the second add** — the guard is inverted; not reused as-is. |
| `GENBILL` / `GENBILLI` | `OpdBillGenerateService` / `IpdBillGenerateService` | Implemented exactly (BCONTROL + OPDPBILL/IPDBILL + stamp + REGISTER), but **not called by any page** (no caller found). |
| `OPDBILL` / `IPDBILL` | `OpdBillHeader` / `IpdBillHeader` | Present. |
| Cashier `CRCVD`/`CRCVDDET` | `Pages/Cashier/Receipt` | **Reads `BILL`, not `OPDBILL`/`OBIL`**; writes `CRCVD`/`CRCVDDET` as a side effect. |
| App cashier ledger | `BILL`/`BILLITEM` | The working cashier path today (`Receipt`, `Inpatient`, `Refunds`). |
| Lab worklist | `INVESTIGATIONORDER` | Present; the legacy indent screens do not write it. |

### 6.4 Consequence — the one architectural fork

The legacy OPD/IPD process and the app's current cashier use **different ledgers**:

- Legacy: `REGISTER` → `OBIL`/`IBIL` → `OPDBILL`/`IPDBILL` → cashier `CRCVD`.
- App cashier: `BILL`/`BILLITEM` → `CRCVD`.

Following the legacy **exactly** means the new page writes only `OBIL`/`IBIL`, creates a `REGISTER` file for new patients, and leaves payment to `/opd/Billing` + `/inward/Billing` + the legacy cashier. The app's `/cashier/Receipt` will **not** show those charges.

The alternative is the **bridge**: write `OBIL`/`IBIL` (legacy record, panel/company/legacy reports) **and** `BILL`/`BILLITEM` (app cashier), stamping `OBIL_MST.BILL` / `IBIL_MST.BILL` with the app `BILL.Id` so the legacy `GENBILL`/`GENBILLI` cannot re-bill the same lines. This keeps the app cashier working and keeps the legacy record, but skips the `OPDBILL`/`IPDBILL` header (legacy bill registers will not see these lab-only bills).

### 6.5 Discount / concession

- The integrated OPD/IPD subforms (`BS010110` etc.) have **no discount or concession field**; `OBIL_DET` has only `RATE` and `NAMT`.
- The standalone `US0101` has `CONS`/`CONSBY` on `COLLECT`, and `NET = TOTAL - CONS`. The app has no `COLLECT`/`TRANS` models.
- Smallest no-schema mapping for the new page: set `OBIL_DET.RATE = gross rate`, `OBIL_DET.NAMT = net amount`, `OBIL_MST.NTOTAL = sum(net)`, and write `Disc <amount> approved by <name>` into `OBIL_DET.REM`; keep the gross/concession/net breakdown on the page/receipt and in the app audit. This makes the legacy bill net and preserves the approver in the legacy detail remark.
- `Cash now` is **not** part of either legacy indent screen. The cashier collects after bill generation. Keep the toggle OFF/hidden by default, matching the legacy rule.
