# LB0110 — Test Result Entry (Web Port)

```
============================================================================
File........: Pages/Lab/TestResultEntry.README.md
Purpose.....: Implementation notes for the web port of the legacy Labsys
              VFP form lb0110.scx (Test Result Entry), route /lab/TestResultEntry.
Inputs......: HMIS database (REPORT, TESTTR, READTR, CULTURE, CUL_TR,
              SERVICES and the migrated master tables).
Outputs.....: This document (no runtime effect).
Developer...: Tajwer Jalal
Date/Time...: 2026-11-02 13:30
============================================================================
```

## 1. What was replaced

`Labsys/FORMS/LB0110O.SCX` (the editable copy of `LB0110.SCX`) is reproduced on the
HMIS web application at:

```
/lab/TestResultEntry
```

The screen keeps the two-page VFP layout:

| VFP page | Web tab | Behaviour |
| --- | --- | --- |
| Page 1 — browse | **List** | Read-only paged `REPORT` grid; search by Lab No, Registration No, Patient Name; click/double-click a row to open it. |
| Page 2 — entry | **Entry** | Read-only header, `TESTTR` test grid, Additional Notes, Test Result sub-tab, Update Culture sub-tab. |

A 9-digit Lab No typed in the List search box opens the indent directly (scanner
fast path). Everything else stays on the List tab.

## 2. Files

### New

| File | Role |
| --- | --- |
| `Models/Lab/LabViewModels.cs` | View models for header, tests, readings, culture, save request/result. |
| `Services/Lab/LabSchemaService.cs` | Cached `sys.objects` probe for the optional legacy tables. |
| `Services/Lab/NormalRangeService.cs` | Exact port of the VFP `udfrange` function. |
| `Services/Lab/LabResultService.cs` | List/entry/readings/result choices/remarks/save transaction. |
| `Services/Lab/CultureService.cs` | `CULTURE` + `CUL_TR`, organisms/growth pick lists, medicine search. |
| `Pages/Lab/TestResultEntry.cshtml` | Page shell, top tabs, alert areas, scripts. |
| `Pages/Lab/TestResultEntry.cshtml.cs` | Thin PageModel with named AJAX handlers. |
| `Pages/Lab/TestResultPrint.cshtml` | Standalone A4 printable test report (patient header, results, culture, remarks). |
| `Pages/Lab/TestResultPrint.cshtml.cs` | Print report PageModel; loads the saved test through `LabResultService`. |
| `Pages/Lab/Partial/_ListGrid.cshtml` | List tab. |
| `Pages/Lab/Partial/_EntryPane.cshtml` | Entry header, test grid, notes, lower sub-tabs, Print button. |
| `Pages/Lab/Partial/_ResultGrid.cshtml` | `READTR` grid. |
| `Pages/Lab/Partial/_CulturePane.cshtml` | Species/growth blocks and medicine sensitivity grid. |
| `Pages/Lab/Partial/_PickListModal.cshtml` | Shared Insert-key pick-list modal. |
| `wwwroot/js/lab-testresult.js` | Tabs, edit mode, Insert keys, grid actions, AJAX save, SweetAlert feedback, Print action. |
| `wwwroot/lib/sweetalert2/sweetalert2.all.min.js` | Local SweetAlert2 bundle so save dialogs work without internet access. |

### Changed

| File | Change |
| --- | --- |
| `Program.cs` | Registers `LabSchemaService`, `NormalRangeService`, `CultureService`, `LabResultService` as scoped services. |

`CULTURE` and `CUL_TR` are created by the existing
`workspace/scripts/AddLabsysCultureTables.sql`; this port does **not** run any DDL and
does **not** add an EF migration.

## 3. Legacy VFP → C# mapping

| Legacy `lb0110` routine / control | Web implementation |
| --- | --- |
| `init` / form load | `TestResultEntryModel.OnGetAsync` |
| List browse / `find` | `OnGetListAsync` → `LabResultService.GetIndentPageAsync` |
| Scan a Lab No | 9-digit fast path → `IndentExistsAsync` → `OnGetEntryAsync` |
| `loadindent` | `OnGetEntryAsync` → `GetEntryAsync` |
| `loadtests` | `GetTestsAsync` (TESTTR + SERVICES) |
| `loadreadings` / group load | `OnGetReadingsAsync` → `GetReadingsAsync` |
| `udfrange` (normal range) | `NormalRangeService.ResolveAsync` |
| `loadculture` | `OnGetCultureAsync` → `CultureService.GetAsync` |
| `validrec` | `LabResultService.SaveAsync` + `CultureService.SaveAsync` |
| Insert (VFP keycode 22) | JS Insert key → `openRemarks` / `openResultChoices` / `openPickList` |
| Enter = next field | JS `keydown` handler on `.reading-value` |
| Add / Delete line | JS `addReadingRow` / `deleteReadingRow` |
| Load All Group | `OnPostLoadAllGroupAsync` |
| Remove Blank | JS `removeBlankReadings` + `OnPostRemoveBlankAsync` |
| Notes box | `#testRemarks` ↔ `TESTTR.REMARKS` |
| Age type combo | `#ageType` ↔ `REPORT.ATYPE` |
| Culture flag | `CultureService.EnsureAsync` / `SaveAsync` → `TESTTR.CULTURE` |

## 4. Schema fallbacks

The legacy DBF masters were not migrated with the OLTP data. `LabSchemaService`
checks `sys.objects` once per process and each service picks the best available
source:

| Legacy table | Used when present | Modern fallback |
| --- | --- | --- |
| `READINGS` | reading master by `GID` | `TESTDATA` joined to `CATEGORY` where `CATEGORY.Code LIKE '{group}-%'` |
| `RESULTS` | standard result choices | distinct previous `READTR.VALUE` for the same `GID` + `ID` |
| `NRANGE` | `udfrange` ranges | the reading master `Range` column |
| `SERGRP` | group names | `CATEGORY.Sname` (the group code, e.g. `BB`) |
| `TSREMARK` | test remarks | `TESTREMARK` |
| `ORGANISMS` | species list | `ORGANISM` |
| `GRREMARK` | growth remarks | `GROWTHDESCRIPTION` |
| `MADIMS` | medicine list | `PHITEM` where `ITEMTYPE='M'` and not retired |

No table is created or altered at runtime.

## 5. Normal range (`udfrange` port)

`NormalRangeService` reproduces the VFP logic:

1. Convert the patient age to hours: `HOUR × 1`, `DAY × 24`, `MONTH × 720`,
   `YEAR × 8640`.
2. Select the first `NRANGE` row for the group/reading/sex whose `HRRANGE` is
   greater than or equal to the age in hours.
3. If no row matches, fall back to the reading master's `Range` text.

Sex is normalised to `M`/`F`; unknown sex is compared as an empty value.

## 6. Save transaction (legacy `validrec`)

`LabResultService.SaveAsync` performs the following in **one** SQL transaction:

1. Read the current `TESTTR` row for `LABNO + TID + TESTNO`.
2. **Concurrency check** (no `rowversion` exists): compare the posted original
   `REPODT`, `REMARKS` and `SRNO` with the current row. A mismatch returns HTTP
   `409` and no data is written.
3. Update `REPORT.ATYPE` (the only editable header field).
4. Call `CultureService.SaveAsync`:
   - if growth/species are all blank → `DELETE CULTURE` and return `false`
     (rule 3: empty culture is removed and the flag is cleared);
   - otherwise upsert the single `CULTURE` row;
   - always `DELETE` + re-insert `CUL_TR`, skipping lines whose A/B/C are all
     blank (rule 2 / Remove Blank).
5. Update `TESTTR`: `REMARKS`, `REPODT`, `SRNO`, `CULTURE`, audit fields. The
   `UPDATE` re-checks the original values; `0` rows affected → `409`.
6. `DELETE` + re-insert `READTR`. Blank `VALUE` rows are not inserted
   (rule 1 / Remove Blank).
7. Commit, then find the next pending test (`REPODT IS NULL`) and return its URL
   so the page can auto-advance.

The save drops blank lines rather than keeping them, which matches the legacy
"Remove Blank" behaviour and the three cleanup rules in the specification.

## 7. Handlers

| Verb | Handler | Purpose |
| --- | --- | --- |
| GET | `?handler=List` | List tab partial (`_ListGrid`). |
| GET | `?handler=Entry` | Entry tab partial (`_EntryPane`). |
| GET | `?handler=Readings` | `_ResultGrid` for one test. |
| GET | `?handler=Culture` | `_CulturePane`; `edit=true` also ensures the CULTURE row and sets `TESTTR.CULTURE=1`. |
| GET | `?handler=ResultChoices` | Standard result JSON list. |
| GET | `?handler=Remarks` | Test remark JSON list. |
| GET | `?handler=Organisms` | Species JSON list. |
| GET | `?handler=GrowthRemarks` | Growth remark JSON list. |
| GET | `?handler=Medicines` | Medicine type-ahead JSON list. |
| GET | `?handler=AllMedicines` | Medicines not yet on the test (max 500). |
| POST | `?handler=Save` | JSON save; returns `{ success, message, nextUrl }`. |
| POST | `?handler=LoadAllGroup` | Rebuilds `_ResultGrid` with one row per group reading. |
| POST | `?handler=RemoveBlank` | Server-side blank `READTR` cleanup + filtered grid. |
| POST | `?handler=EnsureCulture` | Creates the CULTURE row / sets the flag. |

All POSTs are protected by the Razor Pages antiforgery token. JSON requests send
the token in the `RequestVerificationToken` header; form posts include the hidden
`__RequestVerificationToken` field.

## 8. Keyboard and grid behaviour

- **Result entry is ready immediately**: the result grid, notes box and culture
  pane are editable as soon as an indent/test is opened. **Edit** enables the
  read-only header fields (Age Type, per-test Sr.No and Report Date); **Save**
  and **Cancel** are available immediately.
- **Insert** in a Result cell opens result choices; with no choices the modal
  shows `No default results available`.
- **Insert** in the notes box opens `TESTREMARK`; with no remarks it shows
  `No Remarks Available`.
- **Insert** in Species A/B/C opens `ORGANISM`/`ORGANISMS`; Insert in Growth
  1/2/3 opens `GROWTHDESCRIPTION`/`GRREMARK`.
- **Enter** moves to the next result cell and adds a line at the end.
- **Double-click Normal Range** copies the range into Result.
- **Add Line / Delete Line** are client-side; **Remove Blank** removes result
  lines with an empty value. **Load All Group** reloads one row per reading of
  the test's group (confirmation prompt because it discards unsaved rows).
- Opening **Update Culture** for the first time on a test ensures the `CULTURE`
  row and sets `TESTTR.CULTURE=1` (the activation rule). Switching back to the
  tab keeps unsaved species/growth/medicine lines.
- After a successful save the page auto-advances to the next pending test and
  keeps the success message in the page-level alert area.
- **Save feedback** uses a local SweetAlert2 dialog: a green *Saved* dialog on
  success and a red *Save failed* dialog (for validation errors or the `409`
  concurrency message) on failure. The success dialog offers **Print Report**;
  it opens `/lab/TestResultPrint` for the test that was just saved (not the next
  test loaded by auto-advance) and opens the browser print dialog. The inline
  page-level alert remains as a persistent record and as a fallback if the
  SweetAlert bundle cannot load.
- **Print** in the Entry header opens the same printable report at any time for
  the selected test. The report page is standalone (`Layout = null`), prints
  only `READTR` rows with a value, includes the culture block and remarks when
  present, and has its own Print / Close buttons.

## 9. Assumptions and limitations

1. **Age Type storage.** `REPORT.ATYPE` is stored in the same convention as the
   existing data: a single letter (`Y`/`M`/`D`/`H`) when the column already uses
   that convention, otherwise the full `YEAR`/`MONTH`/`DAY`/`HOUR` label. This
   keeps existing reports unchanged. Sample data uses `Y`.
2. **Group names.** `SERGRP` was not migrated, so the group name is the group
   code from `CATEGORY.Sname` (`BB`, `B3`, ...).
3. **Readings/result choices.** `READINGS`/`RESULTS` were not migrated; readings
   come from `TESTDATA`, result choices from previous `READTR.VALUE` values.
   Tests whose group has no `CATEGORY` link (e.g. group `B3`) fall back to the
   `TESTDATA` row whose name best matches the test description, so the legacy
   unit and normal range are still shown. If no significant word matches, one
   free-text row `01` named "Result" is used. An existing free-text `01` value
   is displayed on the matched master row.
4. **Un-traceable unit.** `Untracable Unit` is shown only when the reading id is
   not in the master; a master row with an empty unit stays blank.
5. **Medicine Load All.** `MADIMS` had only a few rows; `PHITEM` has thousands.
   "Load All" is capped at 500 lines. The type-ahead search reaches the rest.
6. **Authorization.** The Lab module currently relies on the application's
   session/auth middleware. `[Authorize]` is intentionally not applied to this
   single page because the other Lab pages are not protected either; add it to
   the whole module in one change (the PageModel header documents this).
7. **No DDL.** The port reads/writes only existing tables. `CULTURE`/`CUL_TR`
   must already exist (see `workspace/scripts/AddLabsysCultureTables.sql`).
8. **No changes to existing reports** or other modules.

## 10. Build and run

```powershell
# Build
dotnet build "Hmis-ASPNET\HmisWeb.csproj" -c Debug

# Live app (HTTPS 7100, HTTP 5153)
Start-Process -FilePath "Hmis-ASPNET\bin\Debug\net8.0\HmisWeb.exe" `
  -ArgumentList '--urls "https://localhost:7100;http://localhost:5153"' `
  -WorkingDirectory "Hmis-ASPNET"
```

Connection string: `DefaultConnection` → `HMIS` on
`TAJWER-PC\SQLSERVER2017`.

Smoke checks:

```
GET /lab/TestResultEntry
GET /lab/TestResultEntry?labNo=230000001
GET /lab/TestResultEntry?handler=Readings&labNo=230000001&tid=01129&testNo=001&gid=BB
GET /lab/TestResultEntry?handler=Culture&labNo=230000001&tid=01129&testNo=001&gid=BB&edit=false
GET /lab/TestResultEntry?handler=Organisms
GET /lab/TestResultPrint?labNo=230000001&tid=01129&testNo=001
GET /lab/TestResultPrint?labNo=230000001&tid=01129&testNo=001&print=1
POST /lab/TestResultEntry?handler=Save
```

## 11. Verification performed

- List tab returns the sample report and pages correctly.
- Entry tab returns the read-only header, two `TESTTR` lines and 17 `BB`
  reading rows.
- Result grid, notes and culture are editable immediately; Age Type, Sr.No and
  Report Date remain read-only until **Edit** is pressed.
- Filling one result and saving posted a one-row `readings` payload and stored
  exactly one `READTR` row; blank lines were not sent or saved.
- Group `B3` (no `CATEGORY`-linked master) resolves the test description to a
  `TESTDATA` row; `01052` returns row `04` `24 Hrs Urinary Calcium`, unit
  `mg/24 Hrs`, range `50-400` instead of the blank free-text row.
- Result choices and remarks return the legacy empty-list messages.
- Organisms (16), growth remarks (15), medicine search (12 for `para`) respond.
- Medicine Load All is capped at 500.
- Save writes `READTR`, `CULTURE`, `CUL_TR`, `TESTTR` report date / remarks /
  culture flag and `REPORT.ATYPE`, then auto-advances.
- Changing `TESTTR` externally and saving with stale originals returns `409`.
- Save success shows the SweetAlert2 *Saved* dialog with **Print Report**; the
  report opens for the saved test (`01129/001`) and auto-prints with `print=1`.
- A stale-original save shows the red *Save failed* SweetAlert with the `409`
  message and writes nothing.
- `/lab/TestResultPrint` returns `200` and renders the patient header, test
  name, entered result rows, unit, normal range and printed timestamp; only
  non-blank result rows are printed.
- Test writes were cleaned up afterwards. The sample indent still has its
  pre-existing `READTR` row `01129/001/01 = 520` (user/test data, left
  untouched); `CULTURE` and `CUL_TR` contain zero rows for the sample indent,
  `TESTTR.REPODT` is `NULL`, `TESTTR.CULTURE = 0` and `REPORT.ATYPE` is `Y`.

## 12. Legacy lab master migration and report functions

Run `workspace/scripts/AddLabsysLabMasterTables.sql` on the HMIS database (it is
idempotent and does not touch `REPORT`, `TESTTR`, `READTR`, `CULTURE` or
`CUL_TR`). It creates and populates:

- `READINGS` from `E:\\Projects\\HMC\\Master\\lab_exOLD\\DATA\\READINGS.DBF`
  (312 rows, unit + normal range). Used by `NormalRangeService` and the
  `TestDataReport`.
- `MADIMS` from the legacy `MADIMS.DBF` (49 rows) plus every active `PHITEM`
  medicine (about 4,924 rows), so the culture screen keeps its full list.
- `SERGRP` from `Master\\SERGRP.DBF` (23 laboratory groups) plus any group
  that only exists in `CATEGORY`.
- `NRANGE` (empty, because the available `NRANGE.DBF` contained only test
  rows; `udfrange` falls back to `READINGS.RANGE` exactly like VFP).
- `RESULTS` (empty; `LabResultService` falls back to historical `READTR`
  values).

It also creates the report helper functions:

- `dbo.udfrange(gid, id, sex, age, atype)` - port of VFP `udfrange`.
- `dbo.medicine(labno, tid, testno)` - port of VFP `medicine`.
- `dbo.period(type + allt(str(hrno)))` - port of VFP `period`.
- `dbo.F_REC(labno, reportId)` and `dbo.L_REC(labno, reportId)` - ports of
  VFP `F_REC` / `L_REC`.

The C# equivalents live in `Services/Lab/LabReportFunctionService.cs`
(`GetMedicineTextAsync`, `GetPeriodAsync`, `IsFirstRecordAsync`,
`IsLastRecordAsync`) and are registered in `Program.cs`.

Quick checks:

```sql
SELECT dbo.udfrange('B3','04','MALE',75,'YEAR');   -- 50-400 mg/24hrs.
SELECT dbo.udfrange('B1','01','MALE',75,'YEAR');   -- 0-1.0
SELECT dbo.period('I0');                            -- From 30/10/2026 To 30/10/2026
SELECT dbo.F_REC('230000001', 4);                  -- 1
SELECT dbo.L_REC('230000001', 4);                  -- 1
```
