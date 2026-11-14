using System;

namespace HmisWeb.Models.Legacy
{
    /// <summary>
    /// Combined patient information from the Patient table joined with REGISTER.
    /// Returned by <see cref="ICommonService.GetPatientLegacyInfoAsync(string)"/>.
    /// Mirrors the VFP bs0110/bs0120 lookup query.
    /// </summary>
    public class PatientLegacyInfo
    {
        /// <summary>PATIENT_LEGACY.CODE</summary>
        public string Code { get; set; }

        /// <summary>PATIENT_LEGACY.NAME</summary>
        public string Name { get; set; }

        /// <summary>PATIENT_LEGACY.SEX</summary>
        public string Sex { get; set; }

        /// <summary>PATIENT_LEGACY.AGE</summary>
        public string Age { get; set; }

        /// <summary>PATIENT_LEGACY.ATYPE (admission type)</summary>
        public string AType { get; set; }

        /// <summary>PATIENT_LEGACY.ADDRESS</summary>
        public string Address { get; set; }

        /// <summary>PATIENT_LEGACY.PHONE1</summary>
        public string Phone1 { get; set; }

        /// <summary>PATIENT_LEGACY.PHONE2</summary>
        public string Phone2 { get; set; }

        /// <summary>PATIENT_LEGACY.PHONE3</summary>
        public string Phone3 { get; set; }

        /// <summary>PATIENT_LEGACY.RDATE (varchar — legacy stores '19000101' style text)</summary>
        public string RDate { get; set; }

        /// <summary>PATIENT_LEGACY.NIC</summary>
        public string Nic { get; set; }

        /// <summary>PATIENT_LEGACY.REGDATE (varchar — legacy stores '19000101' style text)</summary>
        public string RegDate { get; set; }

        /// <summary>PATIENT_LEGACY.CASTE</summary>
        public string Caste { get; set; }

        /// <summary>REGISTER.GNAME (guardian name)</summary>
        public string GName { get; set; }

        /// <summary>REGISTER.GRELATION (guardian relation)</summary>
        public string GRelation { get; set; }

        /// <summary>REGISTER.DIAGNOSE (diagnosis)</summary>
        public string Diagnose { get; set; }

        /// <summary>REGISTER.BEDNO</summary>
        public string BedNo { get; set; }

        /// <summary>REGISTER.REGNO (registration number)</summary>
        public string RegNo { get; set; }

        /// <summary>REGISTER.RDATE (register date)</summary>
        public DateTime? RegisterRDate { get; set; }

        /// <summary>REGISTER.RTIME (register time)</summary>
        public string RTime { get; set; }

        /// <summary>REGISTER.TYPE ('I' or 'O')</summary>
        public string RegType { get; set; }
    }
}