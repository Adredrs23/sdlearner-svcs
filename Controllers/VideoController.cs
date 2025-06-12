using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
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
        private readonly IConfiguration _configuration;

        private readonly IAmazonS3 _s3Client;
        private readonly AppDbContext _dbContext;

        public VideoController(AppDbContext appDbContext, IConfiguration configuration)
        {
            _configuration = configuration;

            _dbContext = appDbContext;

            _s3Client = new AmazonS3Client(configuration["Minio:AccessKey"], configuration["Minio:SecretKey"],
                new AmazonS3Config
                {
                    ServiceURL = configuration["Minio:Endpoint"] ?? "http://localhost:9000",
                    ForcePathStyle = true,
                    UseHttp = true
                });
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
            if (video == null)
            {
                return NotFound("Video not found");
            }

            video.Status = "uploaded";
            await _dbContext.SaveChangesAsync();

            // var factory = new ConnectionFactory() { HostName = "localhost" };
            var factory = new ConnectionFactory()
            {
                HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
                UserName = _configuration["RabbitMQ:UserName"] ?? "Guest",
                Password = _configuration["RabbitMQ:Password"] ?? "Guest",
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672")
            };

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            // Declare the queue (idempotent)
            await channel.QueueDeclareAsync(queue: "video-processing",
                                durable: true,
                                exclusive: false,
                                autoDelete: false,
                                arguments: null);

            // Send a message (JSON encoded)
            var message = JsonSerializer.Serialize(new { videoId = video.Id });
            var body = Encoding.UTF8.GetBytes(message);
            var properties = new BasicProperties
            {
                Persistent = true, // Makes message durable
                ContentType = "application/json"
            };

            await channel.BasicPublishAsync(exchange: "",
                                routingKey: "video-processing",
                                mandatory: true,
                                basicProperties: properties,
                                body: body);

            return Ok(new { message = "Upload confirmed" });
        }

        [HttpGet("{id}/play")]
        public async Task<IActionResult> GetPresignedUrls(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var video = await _dbContext.Videos.FindAsync(id);
            if (video == null || video.UserId != userId)
                return Unauthorized();

            var baseKey = $"{video.UserId}/{video.Id}";

            var response = new
            {
                thumbnailUrl = GeneratePresignedUrl("processed-videos", $"{baseKey}/thumb.jpg"),
                video480pUrl = GeneratePresignedUrl("processed-videos", $"{baseKey}/video_480p.mp4"),
                video720pUrl = GeneratePresignedUrl("processed-videos", $"{baseKey}/video_720p.mp4"),
            };

            return Ok(response);
        }


        [HttpGet("user")]
        public async Task<IActionResult> GetUserVideos()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            var videos = await _dbContext.Videos
                .Where(v => v.UserId == userId)
                .Select(v => new
                {
                    v.Id,
                    v.FileName,
                    v.ThumbnailUrl,
                    v.Status
                })
                .ToListAsync();

            return Ok(videos);
        }

        [HttpGet("preview-url")]
        public async Task<IActionResult> GetPreviewUrl([FromQuery] string key)
        {

            var url = GeneratePresignedUrl(key.Split('/')[0], string.Join('/', key.Split('/').Skip(1)));
            Console.WriteLine("url", url);
            return Ok(new { url });
        }

        private string GeneratePresignedUrl(string bucket, string key)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucket,
                Key = key,
                Expires = DateTime.UtcNow.AddMinutes(15),
                Protocol = Protocol.HTTP
            };

            return _s3Client.GetPreSignedURL(request);
        }
    }
}
