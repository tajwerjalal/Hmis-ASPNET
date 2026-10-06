using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Us.Services
{
    public abstract class UsServiceFormModel : PageModel
    {
        protected const string CodePrefix = "USS-";

        protected readonly ApplicationDbContext Db;
        protected readonly IAppSession AppSession;

        protected UsServiceFormModel(ApplicationDbContext db, IAppSession appSession)
        {
            Db = db;
            AppSession = appSession;
        }

        public List<UsServiceCategory> Categories { get; set; } = new();

        protected async Task LoadCategoriesAsync()
        {
            Categories = await Db.UsServiceCategories
                .Where(c => !c.Retired)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Ultrasound services are always owned by the radiology department; the user
        /// cannot change it, so it is resolved here instead of being posted.
        /// </summary>
        protected async Task<long?> ResolveFixedDepartmentIdAsync()
        {
            var radiology = await Db.Departments
                .FirstOrDefaultAsync(d => !d.Retired && d.DepartmentType == DepartmentType.Radiology);

            if (radiology != null)
                return radiology.Id;

            var byName = await Db.Departments
                .FirstOrDefaultAsync(d => !d.Retired && d.Name == "Radiology");

            return byName?.Id;
        }

        /// <summary>
        /// Builds the next unused sequential service code. The highest existing number is
        /// used rather than string ordering so the sequence keeps working past 999.
        /// </summary>
        protected async Task<string> GenerateNextCodeAsync()
        {
            var existingCodes = await Db.UsServices
                .Where(s => s.Code != null && s.Code.StartsWith(CodePrefix))
                .Select(s => s.Code)
                .ToListAsync();

            var nextNumber = existingCodes
                .Select(c =>
                {
                    var suffix = c.Substring(CodePrefix.Length);
                    return int.TryParse(suffix, out var number) ? number : 0;
                })
                .DefaultIfEmpty(0)
                .Max() + 1;

            return CodePrefix + nextNumber.ToString("D3");
        }

        protected static string Trimmed(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        protected void CopyEditableFields(UsService target, UsService source)
        {
            target.Name = Trimmed(source.Name);
            target.Sname = Trimmed(source.Sname);
            target.Description = Trimmed(source.Description);
            target.PrintName = Trimmed(source.PrintName);
            target.Requirements = Trimmed(source.Requirements);
            target.Rate = source.Rate;
            target.RateForeigner = source.RateForeigner;
            target.DiscountAllowed = source.DiscountAllowed;
            target.CategoryId = source.CategoryId;
            target.ItemType = source.ItemType;
            target.Priority = source.Priority;
            target.Billable = source.Billable;
            target.Active = source.Active;
        }
    }
}
