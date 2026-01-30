namespace Api.Modules.Activity.Models;

public sealed class Activity
{
    public string Id { get; set; } = string.Empty;
    public string AffiliationId { get; set; } = string.Empty;
    public string WorldId { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public List<string> CoCreatorIds { get; set; } = new();
    public List<string> SignatureIds { get; set; } = new();
}

public static class ActivityStatuses
{
    public const string Published = "Published";
    public const string PendingMultiSig = "PendingMultiSig";
}
