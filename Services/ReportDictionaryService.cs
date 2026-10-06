using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HmisWeb.Data;
using HmisWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Services
{
    public class DictionaryLookupResult
    {
        public string Word { get; set; } = "";
        public string Phonetic { get; set; } = "";
        public string PartOfSpeech { get; set; } = "";
        public string Definition { get; set; } = "";
        public string Example { get; set; } = "";
        public string Source { get; set; } = "";
        public bool Found { get; set; }
    }

    public class MedicalTermResult
    {
        public string Term { get; set; } = "";
        public string Definition { get; set; } = "";
        public string Category { get; set; } = "";
        public string Synonyms { get; set; } = "";
        public bool Found { get; set; }
    }

    /// <summary>
    /// Supplies words to the report editor's spell checker and supplies terms to the
    /// Dictionary / Medical Dictionary modals. The medical source is the local
    /// MEDICALTERM table so no patient information is ever sent to an external service.
    /// </summary>
    public interface IReportDictionaryService
    {
        Task<DictionaryLookupResult> LookupGeneralAsync(string word);
        Task<List<MedicalTermResult>> LookupMedicalAsync(string term, int maxResults = 25);
        Task<List<string>> GetMedicalTermsAsync(int maxResults = 400);
    }

    public class ReportDictionaryService : IReportDictionaryService
    {
        // Free, key-less public dictionary. Swapped out behind this single field if the
        // hospital later prefers an offline or licensed provider.
        private const string GeneralDictionaryEndpoint =
            "https://api.dictionaryapi.dev/api/v2/entries/en/{0}";

        private readonly ApplicationDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;

        public ReportDictionaryService(ApplicationDbContext db, IHttpClientFactory httpClientFactory)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Plain-English definition from the free dictionaryapi.dev service.
        /// Only the single word the user selected is sent, never report content.
        /// </summary>
        public async Task<DictionaryLookupResult> LookupGeneralAsync(string word)
        {
            var term = (word ?? "").Trim().ToLowerInvariant();
            var result = new DictionaryLookupResult { Word = term, Source = "dictionaryapi.dev" };

            if (term.Length < 2) return result;

            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = GeneralDictionaryEndpoint + Uri.EscapeDataString(term);
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode) return result;

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array) return result;

                var entry = doc.RootElement[0];
                result.Found = true;
                result.Phonetic = entry.TryGetProperty("phonetic", out var ph) ? ph.GetString() ?? "" : "";
                result.Phonetic = result.Phonetic ?? "";
                if (string.IsNullOrWhiteSpace(result.Phonetic)
                    && entry.TryGetProperty("phonetics", out var phs) && phs.ValueKind == JsonValueKind.Array)
                {
                    foreach (var p in phs.EnumerateArray())
                    {
                        if (p.TryGetProperty("text", out var t) && !string.IsNullOrWhiteSpace(t.GetString()))
                        {
                            result.Phonetic = t.GetString();
                            break;
                        }
                    }
                }

                var defs = new List<string>();
                if (entry.TryGetProperty("meanings", out var meanings) && meanings.ValueKind == JsonValueKind.Array)
                {
                    foreach (var m in meanings.EnumerateArray())
                    {
                        var pos = m.TryGetProperty("partOfSpeech", out var p2) ? p2.GetString() : null;
                        if (!string.IsNullOrWhiteSpace(pos) && string.IsNullOrWhiteSpace(result.PartOfSpeech))
                            result.PartOfSpeech = char.ToUpper(pos[0]) + pos.Substring(1);

                        if (!m.TryGetProperty("definitions", out var defsArr)) continue;
                        foreach (var d in defsArr.EnumerateArray())
                        {
                            var text = d.TryGetProperty("definition", out var dt) ? dt.GetString() : null;
                            if (string.IsNullOrWhiteSpace(text)) continue;

                            if (string.IsNullOrWhiteSpace(result.Definition) && !string.IsNullOrWhiteSpace(pos))
                                defs.Add($"{char.ToUpper(pos[0]) + pos.Substring(1)}: {text}");
                            else
                                defs.Add(text);

                            if (string.IsNullOrWhiteSpace(result.Example)
                                && d.TryGetProperty("example", out var ex))
                                result.Example = ex.GetString();
                        }
                    }
                }
                result.Definition = string.Join("; ", defs.Take(4));
            }
            catch
            {
                // Dictionary is a convenience only - never let it break report writing.
                return result;
            }

            return result;
        }

        /// <summary>
        /// Searches the local MEDICALTERM table. Works with no Internet connection and
        /// never transmits anything about the patient.
        /// </summary>
        public async Task<List<MedicalTermResult>> LookupMedicalAsync(string term, int maxResults = 25)
        {
            var text = (term ?? "").Trim();

            var query = _db.MedicalTerms.AsNoTracking().Where(t => !t.Retired);
            if (!string.IsNullOrWhiteSpace(text))
                query = query.Where(t => t.Term.Contains(text) || t.Synonyms.Contains(text));

            var rows = await query
                .OrderBy(t => t.Term)
                .Take(Math.Clamp(maxResults, 1, 100))
                .ToListAsync();

            return rows.Select(t => new MedicalTermResult
            {
                Term = t.Term,
                Definition = t.Definition,
                Category = t.Category,
                Synonyms = t.Synonyms,
                Found = true
            }).ToList();
        }

        /// <summary>Term list used to teach the spell checker the local medical vocabulary.</summary>
        public async Task<List<string>> GetMedicalTermsAsync(int maxResults = 400)
        {
            var rows = await _db.MedicalTerms.AsNoTracking()
                .Where(t => !t.Retired)
                .OrderBy(t => t.Term)
                .Take(maxResults)
                .Select(t => t.Term)
                .ToListAsync();

            return rows;
        }
    }
}