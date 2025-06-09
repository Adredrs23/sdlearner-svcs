using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sdlearner_svcs.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedVideoMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Videos",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "S3Key",
                table: "Videos",
                newName: "s3key");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Videos",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Videos",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "UploadTime",
                table: "Videos",
                newName: "upload_time");

            migrationBuilder.RenameColumn(
                name: "FileName",
                table: "Videos",
                newName: "file_name");

            migrationBuilder.AddColumn<string>(
                name: "thumbnail_url",
                table: "Videos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "video_480p_url",
                table: "Videos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "video_720p_url",
                table: "Videos",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "thumbnail_url",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "video_480p_url",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "video_720p_url",
                table: "Videos");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Videos",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "s3key",
                table: "Videos",
                newName: "S3Key");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Videos",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "Videos",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "upload_time",
                table: "Videos",
                newName: "UploadTime");

            migrationBuilder.RenameColumn(
                name: "file_name",
                table: "Videos",
                newName: "FileName");
        }
    }
}
