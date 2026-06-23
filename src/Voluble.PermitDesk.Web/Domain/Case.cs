namespace Voluble.PermitDesk.Web.Domain;

public class Case
{
    public Guid Id { get; set; }
    public required string CaseNumber { get; set; }
    public required string Title { get; set; }
    public required string Address { get; set; }
    public required string Municipality { get; set; }
    public string? BrokerName { get; set; }
    public string? PhotographerName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public CaseStatus Status { get; set; } = CaseStatus.Draft;
    public PublicationStatus PublicationStatus { get; set; } = PublicationStatus.NotAllowed;
    public string? LantmaterietReferenceNumber { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
