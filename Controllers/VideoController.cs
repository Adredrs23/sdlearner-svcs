using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    }
}
