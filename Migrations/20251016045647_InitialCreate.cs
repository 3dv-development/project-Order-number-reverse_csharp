using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ProjectOrderNumberSystem.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_number = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    category = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    staff_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    staff_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    case_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    project_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    client_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    budget = table.Column<int>(type: "integer", nullable: false),
                    deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    remarks = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "edit_history",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    editor_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    editor_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    edit_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    changes = table.Column<string>(type: "text", nullable: true),
                    edited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_edit_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_edit_history_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_edit_history_project_id",
                table: "edit_history",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_employee_id",
                table: "employees",
                column: "employee_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_projects_project_number",
                table: "projects",
                column: "project_number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "edit_history");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "projects");
        }
    }
}
