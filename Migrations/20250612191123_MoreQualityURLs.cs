using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sdlearner_svcs.Migrations
{
    /// <inheritdoc />
    public partial class MoreQualityURLs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "video_1080p_url",
                table: "video",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "video_144p_url",
                table: "video",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "video_1080p_url",
                table: "video");

            migrationBuilder.DropColumn(
                name: "video_144p_url",
                table: "video");
        }
    }
}
