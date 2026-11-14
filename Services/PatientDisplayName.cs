using HmisWeb.Models;

namespace HmisWeb.Services
{
    /// <summary>
    /// Single place that decides how a patient's name is shown on screen and
    /// on printed documents.
    ///
    /// Why it exists
    ///   Legacy patients were imported into PATIENT/PERSON but a large number
    ///   of them have no PERSON row (or an empty name), so screens that read
    ///   Person.NameWithInitials showed "Unnamed" even though the patient was
    ///   found by PHN. The indent screens now enrich the Patient with the
    ///   legacy name from the Patient table and call this helper, so every screen
    ///   resolves the name with the same fallback chain.
    ///
    /// Fallback order
    ///   1. Person.NameWithInitials
    ///   2. Person.Name
    ///   3. LegacyName (Patient.NAME)
    ///   4. Person's other name fields
    ///   5. "Unnamed"
    ///
    /// Developer : AI Assistant
    /// Created   : 28-Oct-2026 09:10
    /// </summary>
    public static class PatientDisplayName
    {
        public static string Of(Patient patient)
        {
            if (patient == null) return "Unnamed";

            var person = patient.Person;
            return FirstNonEmpty(
                person?.NameWithInitials,
                person?.Name,
                patient.LegacyName,
                person?.FullName,
                person?.Surname,
                person?.LastName,
                "Unnamed");
        }

        /// <summary>
        /// Name for a PERSON row on its own (consultant, staff member, user).
        /// Same fallback chain but without a Patient wrapper.
        /// </summary>
        public static string OfPerson(Person person, string fallback = "")
        {
            if (person == null) return fallback;
            return FirstNonEmpty(
                person.NameWithInitials,
                person.Name,
                person.FullName,
                person.Surname,
                person.LastName,
                fallback);
        }

        /// <summary>Name plus PHN, e.g. "SILVA S. (PHN 146359)".</summary>
        public static string WithPhn(Patient patient)
        {
            var name = Of(patient);
            var phn = patient?.Phn;
            if (string.IsNullOrWhiteSpace(phn) && patient?.PatientId != null)
                phn = patient.PatientId.Value.ToString();
            return string.IsNullOrWhiteSpace(phn) ? name : $"{name} (PHN {phn})";
        }

        private static string FirstNonEmpty(params string[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
            }
            return "";
        }
    }
}
