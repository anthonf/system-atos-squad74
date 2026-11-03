using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SupportAtos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPortalExternalId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "codigo_externo",
                table: "usuario",
                type: "varchar",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "codigo_externo",
                table: "grupo_empresa",
                type: "varchar",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "provisionamento_audit",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo_externo_usuario = table.Column<string>(type: "varchar", nullable: false),
                    codigo_externo_grupo = table.Column<string>(type: "varchar", nullable: false),
                    acao = table.Column<string>(type: "varchar", nullable: false),
                    data = table.Column<DateTime>(type: "timestamp", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provisionamento_audit", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_usuario_codigo_externo",
                table: "usuario",
                column: "codigo_externo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_grupo_empresa_codigo_externo",
                table: "grupo_empresa",
                column: "codigo_externo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "provisionamento_audit");

            migrationBuilder.DropIndex(
                name: "IX_usuario_codigo_externo",
                table: "usuario");

            migrationBuilder.DropIndex(
                name: "IX_grupo_empresa_codigo_externo",
                table: "grupo_empresa");

            migrationBuilder.DropColumn(
                name: "codigo_externo",
                table: "usuario");

            migrationBuilder.DropColumn(
                name: "codigo_externo",
                table: "grupo_empresa");
        }
    }
}
