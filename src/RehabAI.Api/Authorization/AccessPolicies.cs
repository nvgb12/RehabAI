namespace RehabAI.Api.Authorization;

public static class AccessPolicies
{
    public const string ActivePatient = "ActivePatient";
    public const string ActiveDoctor = "ActiveDoctor";
    public const string ActiveAdmin = "ActiveAdmin";
    public const string ActiveDoctorStaffOrAdmin = "ActiveDoctorStaffOrAdmin";

    public const string PatientRole = "Patient";
    public const string DoctorRole = "Doctor";
    public const string AdminRole = "Admin";
    public const string AuthorizedInternalStaffRole = "AuthorizedInternalStaff";
    public const string SupportStaffRole = "SupportStaff";
    public const string VerificationAdminRole = "VerificationAdmin";
}
