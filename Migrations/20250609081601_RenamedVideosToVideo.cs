using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sdlearner_svcs.Migrations
{
    /// <inheritdoc />
    public partial class RenamedVideosToVideo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Videos",
                table: "Videos");

            migrationBuilder.RenameTable(
                name: "Videos",
                newName: "video");

            migrationBuilder.AddPrimaryKey(
                name: "PK_video",
                table: "video",
                column: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_video",
                table: "video");

            migrationBuilder.RenameTable(
                name: "video",
                newName: "Videos");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Videos",
                table: "Videos",
                column: "id");
        }
    }
}
