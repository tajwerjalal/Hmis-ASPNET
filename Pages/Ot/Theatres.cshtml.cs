using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Ot
{
    /// <summary>
    /// Operation Theatre master setup (theatre / room list). Every OT case, the
    /// day board, the register and the utilisation report are keyed off this
    /// list, so it is maintained here. Additive page - nothing existing changed.
    /// </summary>
    public class TheatresModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public TheatresModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        public List<OtTheatre> Items { get; set; } = new();
        public string ErrorMessage { get; set; } = "";
        public string Success { get; set; } = "";

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public long? Id { get; set; }
            public string Code { get; set; } = "";
            public string Name { get; set; } = "";
            public string TheatreType { get; set; } = "Major";
            public string Location { get; set; } = "";
            public bool Active { get; set; } = true;
        }

        private static readonly string[] Types = { "Major", "Minor", "Day Care", "Emergency" };

        public async Task OnGetAsync(long? id)
        {
            if (id.HasValue)
            {
                var existing = await _db.OtTheatres.FindAsync(id.Value);
                if (existing != null && !existing.Retired)
                {
                    Input = new InputModel
                    {
                        Id = existing.Id,
                        Code = existing.Code,
                        Name = existing.Name,
                        TheatreType = existing.TheatreType,
                        Location = existing.Location,
                        Active = existing.Active
                    };
                }
            }

            await LoadListAsync();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            Success = "";

            if (string.IsNullOrWhiteSpace(Input.Name))
            {
                ErrorMessage = "Theatre name is required.";
                await LoadListAsync();
                return Page();
            }

            // The code is optional, but when given it must be unique.
            var code = (Input.Code ?? "").Trim();
            if (code.Length > 0)
            {
                var dup = await _db.OtTheatres
                    .AnyAsync(t => t.Code == code && t.Id != (Input.Id ?? 0) && !t.Retired);
                if (dup)
                {
                    ErrorMessage = "A theatre with code '" + code + "' already exists.";
                    await LoadListAsync();
                    return Page();
                }
            }

            if (Input.Id.HasValue && Input.Id.Value > 0)
            {
                var t = await _db.OtTheatres.FindAsync(Input.Id.Value);
                if (t == null)
                {
                    ErrorMessage = "Theatre not found.";
                    await LoadListAsync();
                    return Page();
                }

                t.Code = code;
                t.Name = Input.Name.Trim();
                t.TheatreType = Types.Contains(Input.TheatreType) ? Input.TheatreType : "Major";
                t.Location = (Input.Location ?? "").Trim();
                t.Active = Input.Active;
                t.EditerId = _session.WebUserId;
                t.EditedAt = DateTime.Now;

                Success = "Theatre '" + t.Name + "' updated.";
            }
            else
            {
                var t = new OtTheatre
                {
                    Code = code,
                    Name = Input.Name.Trim(),
                    TheatreType = Types.Contains(Input.TheatreType) ? Input.TheatreType : "Major",
                    Location = (Input.Location ?? "").Trim(),
                    Active = Input.Active,
                    CreaterId = _session.WebUserId,
                    CreatedAt = DateTime.Now
                };
                _db.OtTheatres.Add(t);

                Success = "Theatre '" + t.Name + "' added.";
            }

            await _db.SaveChangesAsync();
            Input = new InputModel();
            ErrorMessage = "";
            await LoadListAsync();
            return Page();
        }

        /// <summary>Soft delete - keeps any historical cases pointing at the theatre.</summary>
        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            var t = await _db.OtTheatres.FindAsync(id);
            if (t != null && !t.Retired)
            {
                var used = await _db.OtCases.AnyAsync(c => c.TheatreId == id && !c.Retired);
                if (used)
                {
                    // Retire instead of remove so the register still resolves the name.
                    t.Retired = true;
                    t.Active = false;
                    t.RetirerId = _session.WebUserId;
                    t.RetiredAt = DateTime.Now;
                    t.RetireComments = "Retired - still used by existing OT cases";
                    Success = "Theatre '" + t.Name + "' retired (existing cases keep it listed).";
                }
                else
                {
                    _db.OtTheatres.Remove(t);
                    Success = "Theatre '" + t.Name + "' deleted.";
                }
                await _db.SaveChangesAsync();
            }

            await LoadListAsync();
            return RedirectToPage();
        }

        private async Task LoadListAsync()
        {
            Items = await _db.OtTheatres
                .Where(t => !t.Retired)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }
    }
}
