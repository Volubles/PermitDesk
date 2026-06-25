namespace Voluble.PermitDesk.Web.Lantmateriet;

/// <summary>
/// The "Sökande" half of a Lantmäteriet spridningstillstånd application — the
/// same values for every ärende we file. Lives in appsettings so the rest of
/// the workflow doesn't have to remember the org number / address every time.
/// </summary>
public class ApplicantOptions
{
    public const string SectionName = "Lantmateriet:Applicant";

    public string CompanyName { get; set; } = string.Empty;
    public string OrganizationNumber { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = "Sverige";
}
