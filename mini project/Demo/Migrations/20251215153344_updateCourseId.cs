using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demo.Migrations
{
    /// <inheritdoc />
    public partial class updateCourseId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CoursePhotos_Courses_CourseId",
                table: "CoursePhotos");

            migrationBuilder.AlterColumn<string>(
                name: "CourseId",
                table: "CoursePhotos",
                type: "nvarchar(4)",
                maxLength: 4,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(4)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CoursePhotos_Courses_CourseId",
                table: "CoursePhotos",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "CourseId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CoursePhotos_Courses_CourseId",
                table: "CoursePhotos");

            migrationBuilder.AlterColumn<string>(
                name: "CourseId",
                table: "CoursePhotos",
                type: "nvarchar(4)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4)",
                oldMaxLength: 4);

            migrationBuilder.AddForeignKey(
                name: "FK_CoursePhotos_Courses_CourseId",
                table: "CoursePhotos",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "CourseId");
        }
    }
}
