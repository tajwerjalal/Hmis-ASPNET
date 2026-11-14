using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Xr
{
    /// <summary>
    /// X-ray / CT scan indent (route /xr/indent).
    ///
    /// What it does
    ///   * Uses the shared department indenting process
    ///     (Pages/Shared/DepartmentIndentPageModel) - the same flow as
    ///     /us/indent: patient search, IPD detection, service popup, cart and
    ///     one-transaction save.
    ///   * Catalogue: [SERVICES] rows owned by DEPT 02 (DeptServiceScopes.XrayDept).
    ///   * Extra step: every indented line is also written to [XR_ORDER] so the
    ///     X-ray worklist / sample collection screens pick it up.
    ///
    /// Why it exists
    ///   * The previous X-ray page had its own inline service grid. Mirroring
    ///     Ultrasound keeps the cashier and ward experience identical across
    ///     departments and keeps every catalogue read on [SERVICES].
    ///
    /// Business rules / validations
    ///   * All shared rules from DepartmentIndentPageModel apply (patient and
    ///     one line required, dept-scoped services only, rate re-resolution,
    ///     discount clamping, IPD vs OPD billing).
    ///   * Orders are only created for lines that saved successfully.
    ///
    /// Parameters / return values
    ///   * Inherited handlers; save returns a redirect with a TempData message.
    ///
    /// Developer : AI Assistant
    /// Created   : 27-Oct-2026 11:35
    /// Change Log: 27-Oct-2026 11:35 - rebuilt on the shared Ultrasound
    ///             indenting process; order creation kept in this page.
    /// </summary>
    public class IndentModel : Shared.DepartmentIndentPageModel
    {
        public IndentModel(ApplicationDbContext db, ICommonService common, IAppSession appSession)
            : base(db, common, appSession)
        {
        }

        public override string ServiceDept => DeptServiceScopes.XrayDept;
        public override string ModuleName => "X-ray";
        public override string ModuleShortName => "XR";
        public override string RoutePath => "/Xr/Indent";
        public override string InvoicePrintRoute => "/Xr/Reports/IndentPrintout";
        public override string PatientSessionKey => "XrIndentPatientId";
        public override string CartSessionKey => "XrIndentCart";

        /// <summary>
        /// Billing alone leaves the scan room with nothing to work from, so every
        /// indented line also becomes an X-ray order that X-ray Collection Entry
        /// can pick up.
        /// </summary>
        protected override async Task CreateModuleOrdersAsync(Bill bill, List<IndentCartItem> cart,
            Patient patient, PatientEncounter encounter, DateTime now, long? userId, long? institutionId)
        {
            var serviceIds = cart.Select(c => c.ItemId).Where(id => id > 0).Distinct().ToList();
            if (serviceIds.Count == 0)
                return;

            // X-ray services now come from SERVICES (DeptService, dept='02'),
            // not XRSERVICE - XrServiceId FK is dropped, so leave it null.
            var services = await Db.DeptServices
                .Where(s => serviceIds.Contains(s.Id) && s.Dept == ServiceDept)
                .ToDictionaryAsync(s => s.Id);

            // Resolve the X-ray department entity for XrOrder.DepartmentId FK
            var xrDept = await Db.Departments
                .Where(d => d.Code == ServiceDept)
                .FirstOrDefaultAsync();

            foreach (var c in cart)
            {
                if (c.ItemId <= 0) continue;

                services.TryGetValue(c.ItemId, out var service);

                Db.XrOrders.Add(new XrOrder
                {
                    PatientId = patient.Id,
                    PatientEncounterId = encounter?.Id,
                    XrServiceId = null,
                    InstitutionId = institutionId,
                    DepartmentId = xrDept?.Id,
                    BillId = bill.Id,
                    Qty = c.Qty,
                    OrderName = string.IsNullOrWhiteSpace(c.ItemName) ? (service?.Name ?? c.ItemName) : c.ItemName,
                    Instructions = service?.Requirements ?? string.Empty,
                    Ordered = true,
                    SampleCollected = false,
                    Completed = false,
                    Retired = false,
                    CreaterId = userId,
                    CreatedAt = now
                });
            }

            await Db.SaveChangesAsync();
        }
    }
}
