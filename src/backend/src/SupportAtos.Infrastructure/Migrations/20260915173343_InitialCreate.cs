using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SupportAtos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "grupo_empresa",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "varchar", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grupo_empresa", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sla_categoria",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    produto = table.Column<string>(type: "varchar", nullable: false),
                    categoria = table.Column<string>(type: "varchar", nullable: false),
                    prioridade = table.Column<string>(type: "varchar", nullable: false),
                    tempo_resposta = table.Column<int>(type: "integer", nullable: false),
                    tempo_resolucao = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sla_categoria", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuario",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "varchar", nullable: false),
                    email = table.Column<string>(type: "varchar", nullable: false),
                    grupo_empresa_id = table.Column<int>(type: "integer", nullable: false),
                    papel = table.Column<string>(type: "varchar", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario", x => x.id);
                    table.ForeignKey(
                        name: "FK_usuario_grupo_empresa_grupo_empresa_id",
                        column: x => x.grupo_empresa_id,
                        principalTable: "grupo_empresa",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "chamado",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo_publico = table.Column<string>(type: "varchar", nullable: false),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    grupo_empresa_id = table.Column<int>(type: "integer", nullable: false),
                    sla_categoria_id = table.Column<int>(type: "integer", nullable: false),
                    produto = table.Column<string>(type: "varchar", nullable: false),
                    categoria = table.Column<string>(type: "varchar", nullable: false),
                    status = table.Column<string>(type: "varchar", nullable: false),
                    prioridade = table.Column<string>(type: "varchar", nullable: false),
                    sla_prazo = table.Column<DateTime>(type: "timestamp", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp", nullable: false),
                    fechado_em = table.Column<DateTime>(type: "timestamp", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chamado", x => x.id);
                    table.ForeignKey(
                        name: "FK_chamado_grupo_empresa_grupo_empresa_id",
                        column: x => x.grupo_empresa_id,
                        principalTable: "grupo_empresa",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_chamado_sla_categoria_sla_categoria_id",
                        column: x => x.sla_categoria_id,
                        principalTable: "sla_categoria",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_chamado_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "interacao",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    chamado_id = table.Column<int>(type: "integer", nullable: false),
                    tipo = table.Column<string>(type: "varchar", nullable: false),
                    mensagem = table.Column<string>(type: "text", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interacao", x => x.id);
                    table.ForeignKey(
                        name: "FK_interacao_chamado_chamado_id",
                        column: x => x.chamado_id,
                        principalTable: "chamado",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_interacao_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "log_auditoria",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    chamado_id = table.Column<int>(type: "integer", nullable: false),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    acao = table.Column<string>(type: "varchar", nullable: false),
                    campo_alterado = table.Column<string>(type: "varchar", nullable: true),
                    valor_anterior = table.Column<string>(type: "text", nullable: true),
                    valor_novo = table.Column<string>(type: "text", nullable: true),
                    data = table.Column<DateTime>(type: "timestamp", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_log_auditoria", x => x.id);
                    table.ForeignKey(
                        name: "FK_log_auditoria_chamado_chamado_id",
                        column: x => x.chamado_id,
                        principalTable: "chamado",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_log_auditoria_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "anexo",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    interacao_id = table.Column<int>(type: "integer", nullable: false),
                    nome_arquivo = table.Column<string>(type: "varchar", nullable: false),
                    url = table.Column<string>(type: "varchar", nullable: false),
                    tamanho_bytes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_anexo", x => x.id);
                    table.ForeignKey(
                        name: "FK_anexo_interacao_interacao_id",
                        column: x => x.interacao_id,
                        principalTable: "interacao",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_anexo_interacao_id",
                table: "anexo",
                column: "interacao_id");

            migrationBuilder.CreateIndex(
                name: "IX_chamado_codigo_publico",
                table: "chamado",
                column: "codigo_publico",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chamado_grupo_empresa_id",
                table: "chamado",
                column: "grupo_empresa_id");

            migrationBuilder.CreateIndex(
                name: "IX_chamado_sla_categoria_id",
                table: "chamado",
                column: "sla_categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_chamado_usuario_id",
                table: "chamado",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_interacao_chamado_id",
                table: "interacao",
                column: "chamado_id");

            migrationBuilder.CreateIndex(
                name: "IX_interacao_usuario_id",
                table: "interacao",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_log_auditoria_chamado_id",
                table: "log_auditoria",
                column: "chamado_id");

            migrationBuilder.CreateIndex(
                name: "IX_log_auditoria_usuario_id",
                table: "log_auditoria",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_usuario_email",
                table: "usuario",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuario_grupo_empresa_id",
                table: "usuario",
                column: "grupo_empresa_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "anexo");

            migrationBuilder.DropTable(
                name: "log_auditoria");

            migrationBuilder.DropTable(
                name: "interacao");

            migrationBuilder.DropTable(
                name: "chamado");

            migrationBuilder.DropTable(
                name: "sla_categoria");

            migrationBuilder.DropTable(
                name: "usuario");

            migrationBuilder.DropTable(
                name: "grupo_empresa");
        }
    }
}
