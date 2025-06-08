namespace SDLearnerSVCs.VideoDTO;


public class VideoUploadRequest
{
    public string UserId { get; set; }  // Use Keycloak-sub if integrated
    public string FileName { get; set; }
}

public class ConfirmUploadRequest
{
    public Guid VideoId { get; set; }
}
