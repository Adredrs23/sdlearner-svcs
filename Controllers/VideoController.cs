using System.Security.Claims;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SDLearnerSVCs.Data;
using SDLearnerSVCs.Models;
using SDLearnerSVCs.VideoDTO;


namespace SDLearnerSVCs.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class VideoController : ControllerBase
    {

        private readonly IAmazonS3 _s3Client;
        private readonly AppDbContext _dbContext;

        public VideoController(AppDbContext appDbContext)
        {

            this._dbContext = appDbContext;

            _s3Client = new AmazonS3Client("minioadmin", "minioadmin",
                new AmazonS3Config
                {
                    ServiceURL = "http://localhost:9000",
                    ForcePathStyle = true,
                    UseHttp = true
                });
        }

        [HttpGet]
        public IActionResult getString()
        {
            return Ok("Jello World");
        }

        [HttpPost("initiate-upload")]
        public async Task<IActionResult> InitiateUpload([FromBody] VideoUploadRequest request)
        {
            var videoId = Guid.NewGuid().ToString();
            var key = $"{request.UserId}/{videoId}/{request.FileName}";

            var requestUrl = _s3Client.GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = "raw-uploads",
                Key = key,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddMinutes(15),
                Protocol = Protocol.HTTP
            });

            var video = new VideoMetadata
            {
                Id = Guid.Parse(videoId),
                UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown",
                FileName = request.FileName,
                S3Key = key,
                UploadTime = DateTime.UtcNow,
                Status = "pending"
            };

            _dbContext.Videos.Add(video);
            await _dbContext.SaveChangesAsync();

            return Ok(new { videoId, uploadUrl = requestUrl });
        }


        [HttpPost("confirm-upload")]
        public async Task<IActionResult> ConfirmUpload([FromBody] ConfirmUploadRequest request)
        {

            var video = await _dbContext.Videos.FindAsync(request.VideoId);
            // var video = await _dbContext.Videos.FirstOrDefaultAsync(v => v.Id == request.VideoId);
            if (video == null)
            {
                return NotFound("Video not found");
            }

            video.Status = "uploaded";
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Upload confirmed" });
        }
    }
}
