namespace SDLearnerSVCs.Models;


public class VideoMetadata
{
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public string FileName { get; set; }
    public string S3Key { get; set; }
    public DateTime UploadTime { get; set; }
    public string Status { get; set; } = "pending"; // e.g., pending, processing, complete

}