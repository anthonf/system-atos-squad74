using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportAtos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlinharAoSqlOficial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_chamado_sla_categoria_sla_categoria_id",
                table: "chamado");

            migrationBuilder.DropForeignKey(
                name: "FK_interacao_usuario_usuario_id",
                table: "interacao");

            migrationBuilder.DropPrimaryKey(
                name: "PK_sla_categoria",
                table: "sla_categoria");

            migrationBuilder.RenameTable(
                name: "sla_categoria",
                newName: "sla_config");

            migrationBuilder.RenameColumn(
                name: "data",
                table: "log_auditoria",
                newName: "criado_em");

            migrationBuilder.RenameColumn(
                name: "usuario_id",
                table: "interacao",
                newName: "autor_id");

            migrationBuilder.RenameIndex(
                name: "IX_interacao_usuario_id",
                table: "interacao",
                newName: "IX_interacao_autor_id");

            migrationBuilder.RenameColumn(
                name: "sla_prazo",
                table: "chamado",
                newName: "sla_prazo_resolucao");

            migrationBuilder.RenameColumn(
                name: "sla_categoria_id",
                table: "chamado",
                newName: "sla_config_id");

            migrationBuilder.RenameIndex(
                name: "IX_chamado_sla_categoria_id",
                table: "chamado",
                newName: "IX_chamado_sla_config_id");

            migrationBuilder.RenameColumn(
                name: "url",
                table: "anexo",
                newName: "caminho");

            migrationBuilder.RenameColumn(
                name: "tempo_resposta",
                table: "sla_config",
                newName: "tempo_resposta_min");

            migrationBuilder.RenameColumn(
                name: "tempo_resolucao",
                table: "sla_config",
                newName: "tempo_resolucao_min");

            migrationBuilder.AlterColumn<string>(
                name: "papel",
                table: "usuario",
                type: "varchar(20)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar");

            migrationBuilder.AlterColumn<string>(
                name: "nome",
                table: "usuario",
                type: "varchar(150)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "usuario",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar");

            migrationBuilder.AddColumn<DateTime>(
                name: "atualizado_em",
                table: "usuario",
                type: "timestamp",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "criado_em",
                table: "usuario",
                type: "timestamp",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<int>(
                name: "usuario_id",
                table: "log_auditoria",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "campo_alterado",
                table: "log_auditoria",
                type: "varchar(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "acao",
                table: "log_auditoria",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar");

            migrationBuilder.AlterColumn<string>(
                name: "tipo",
                table: "interacao",
                type: "varchar(20)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar");

            migrationBuilder.AlterColumn<string>(
                name: "nome",
                table: "grupo_empresa",
                type: "varchar(150)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar");

            migrationBuilder.AddColumn<DateTime>(
                name: "criado_em",
                table: "grupo_empresa",
                type: "timestamp",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "grupo_pai_id",
                table: "grupo_empresa",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tipo",
                table: "grupo_empresa",
                type: "varchar(20)",
                nullable: false,
                defaultValue: "matriz");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "chamado",
                type: "varchar(30)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar");

            migrationBuilder.AlterColumn<string>(
                name: "produto",
                table: "chamado",
                type: "varchar(100)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar");

            migrationBuilder.AlterColumn<string>(
                name: "prioridade",
                table: "chamado",
                type: "varchar(20)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar");

            migrationBuilder.AlterColumn<string>(
                name: "codigo_publico",
                table: "chamado",
                type: "varchar(20)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar");

            migrationBuilder.AlterColumn<string>(
                name: "categoria",
                table: "chamado",
                type: "varchar(100)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar");

            migrationBuilder.AddColumn<int>(
                name: "agente_id",
                table: "chamado",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "aguardando_desde",
                table: "chamado",
                type: "timestamp",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "atualizado_em",
                table: "chamado",
                type: "timestamp",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "descricao",
                table: "chamado",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "sla_prazo_resposta",
                table: "chamado",
                type: "timestamp",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "titulo",
                table: "chamado",
                type: "varchar(200)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "nome_arquivo",
                table: "anexo",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar");

            migrationBuilder.AlterColumn<int>(
                name: "interacao_id",
                table: "anexo",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "caminho",
                table: "anexo",
                type: "varchar(500)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar");

            migrationBuilder.AddColumn<int>(
                name: "chamado_id",
                table: "anexo",
                type: "integer",
                nullable: true);

            // Anexos legados: herdam o chamado da interação antes de tornar NOT NULL.
            migrationBuilder.Sql(@"UPDATE anexo SET chamado_id = interacao.chamado_id FROM interacao WHERE anexo.interacao_id = interacao.id AND anexo.chamado_id IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "chamado_id",
                table: "anexo",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tipo_mime",
                table: "anexo",
                type: "varchar(100)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "produto",
                table: "sla_config",
                type: "varchar(100)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar");

            migrationBuilder.AlterColumn<string>(
                name: "prioridade",
                table: "sla_config",
                type: "varchar(20)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar");

            migrationBuilder.AlterColumn<string>(
                name: "categoria",
                table: "sla_config",
                type: "varchar(100)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar");

            migrationBuilder.AddColumn<bool>(
                name: "ativo",
                table: "sla_config",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_sla_config",
                table: "sla_config",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_log_auditoria_criado_em",
                table: "log_auditoria",
                column: "criado_em");

            migrationBuilder.CreateIndex(
                name: "IX_grupo_empresa_grupo_pai_id",
                table: "grupo_empresa",
                column: "grupo_pai_id");

            migrationBuilder.CreateIndex(
                name: "IX_chamado_agente_id",
                table: "chamado",
                column: "agente_id");

            migrationBuilder.CreateIndex(
                name: "IX_chamado_produto_categoria_status_grupo_empresa_id_criado_em",
                table: "chamado",
                columns: new[] { "produto", "categoria", "status", "grupo_empresa_id", "criado_em" });

            migrationBuilder.CreateIndex(
                name: "IX_chamado_sla_prazo_resolucao",
                table: "chamado",
                column: "sla_prazo_resolucao");

            migrationBuilder.CreateIndex(
                name: "IX_chamado_status",
                table: "chamado",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_anexo_chamado_id",
                table: "anexo",
                column: "chamado_id");

            migrationBuilder.CreateIndex(
                name: "IX_sla_config_produto_categoria_prioridade",
                table: "sla_config",
                columns: new[] { "produto", "categoria", "prioridade" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_anexo_chamado_chamado_id",
                table: "anexo",
                column: "chamado_id",
                principalTable: "chamado",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_chamado_sla_config_sla_config_id",
                table: "chamado",
                column: "sla_config_id",
                principalTable: "sla_config",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_chamado_usuario_agente_id",
                table: "chamado",
                column: "agente_id",
                principalTable: "usuario",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_grupo_empresa_grupo_empresa_grupo_pai_id",
                table: "grupo_empresa",
                column: "grupo_pai_id",
                principalTable: "grupo_empresa",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_interacao_usuario_autor_id",
                table: "interacao",
                column: "autor_id",
                principalTable: "usuario",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_anexo_chamado_chamado_id",
                table: "anexo");

            migrationBuilder.DropForeignKey(
                name: "FK_chamado_sla_config_sla_config_id",
                table: "chamado");

            migrationBuilder.DropForeignKey(
                name: "FK_chamado_usuario_agente_id",
                table: "chamado");

            migrationBuilder.DropForeignKey(
                name: "FK_grupo_empresa_grupo_empresa_grupo_pai_id",
                table: "grupo_empresa");

            migrationBuilder.DropForeignKey(
                name: "FK_interacao_usuario_autor_id",
                table: "interacao");

            migrationBuilder.DropIndex(
                name: "IX_log_auditoria_criado_em",
                table: "log_auditoria");

            migrationBuilder.DropIndex(
                name: "IX_grupo_empresa_grupo_pai_id",
                table: "grupo_empresa");

            migrationBuilder.DropIndex(
                name: "IX_chamado_agente_id",
                table: "chamado");

            migrationBuilder.DropIndex(
                name: "IX_chamado_produto_categoria_status_grupo_empresa_id_criado_em",
                table: "chamado");

            migrationBuilder.DropIndex(
                name: "IX_chamado_sla_prazo_resolucao",
                table: "chamado");

            migrationBuilder.DropIndex(
                name: "IX_chamado_status",
                table: "chamado");

            migrationBuilder.DropIndex(
                name: "IX_anexo_chamado_id",
                table: "anexo");

            migrationBuilder.DropPrimaryKey(
                name: "PK_sla_config",
                table: "sla_config");

            migrationBuilder.DropIndex(
                name: "IX_sla_config_produto_categoria_prioridade",
                table: "sla_config");

            migrationBuilder.DropColumn(
                name: "atualizado_em",
                table: "usuario");

            migrationBuilder.DropColumn(
                name: "criado_em",
                table: "usuario");

            migrationBuilder.DropColumn(
                name: "criado_em",
                table: "grupo_empresa");

            migrationBuilder.DropColumn(
                name: "grupo_pai_id",
                table: "grupo_empresa");

            migrationBuilder.DropColumn(
                name: "tipo",
                table: "grupo_empresa");

            migrationBuilder.DropColumn(
                name: "agente_id",
                table: "chamado");

            migrationBuilder.DropColumn(
                name: "aguardando_desde",
                table: "chamado");

            migrationBuilder.DropColumn(
                name: "atualizado_em",
                table: "chamado");

            migrationBuilder.DropColumn(
                name: "descricao",
                table: "chamado");

            migrationBuilder.DropColumn(
                name: "sla_prazo_resposta",
                table: "chamado");

            migrationBuilder.DropColumn(
                name: "titulo",
                table: "chamado");

            migrationBuilder.DropColumn(
                name: "chamado_id",
                table: "anexo");

            migrationBuilder.DropColumn(
                name: "tipo_mime",
                table: "anexo");

            migrationBuilder.DropColumn(
                name: "ativo",
                table: "sla_config");

            migrationBuilder.RenameTable(
                name: "sla_config",
                newName: "sla_categoria");

            migrationBuilder.RenameColumn(
                name: "criado_em",
                table: "log_auditoria",
                newName: "data");

            migrationBuilder.RenameColumn(
                name: "autor_id",
                table: "interacao",
                newName: "usuario_id");

            migrationBuilder.RenameIndex(
                name: "IX_interacao_autor_id",
                table: "interacao",
                newName: "IX_interacao_usuario_id");

            migrationBuilder.RenameColumn(
                name: "sla_prazo_resolucao",
                table: "chamado",
                newName: "sla_prazo");

            migrationBuilder.RenameColumn(
                name: "sla_config_id",
                table: "chamado",
                newName: "sla_categoria_id");

            migrationBuilder.RenameIndex(
                name: "IX_chamado_sla_config_id",
                table: "chamado",
                newName: "IX_chamado_sla_categoria_id");

            migrationBuilder.RenameColumn(
                name: "caminho",
                table: "anexo",
                newName: "url");

            migrationBuilder.RenameColumn(
                name: "tempo_resposta_min",
                table: "sla_categoria",
                newName: "tempo_resposta");

            migrationBuilder.RenameColumn(
                name: "tempo_resolucao_min",
                table: "sla_categoria",
                newName: "tempo_resolucao");

            migrationBuilder.AlterColumn<string>(
                name: "papel",
                table: "usuario",
                type: "varchar",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)");

            migrationBuilder.AlterColumn<string>(
                name: "nome",
                table: "usuario",
                type: "varchar",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(150)");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "usuario",
                type: "varchar",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)");

            migrationBuilder.AlterColumn<int>(
                name: "usuario_id",
                table: "log_auditoria",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "campo_alterado",
                table: "log_auditoria",
                type: "varchar",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "acao",
                table: "log_auditoria",
                type: "varchar",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AlterColumn<string>(
                name: "tipo",
                table: "interacao",
                type: "varchar",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)");

            migrationBuilder.AlterColumn<string>(
                name: "nome",
                table: "grupo_empresa",
                type: "varchar",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(150)");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "chamado",
                type: "varchar",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)");

            migrationBuilder.AlterColumn<string>(
                name: "produto",
                table: "chamado",
                type: "varchar",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)");

            migrationBuilder.AlterColumn<string>(
                name: "prioridade",
                table: "chamado",
                type: "varchar",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)");

            migrationBuilder.AlterColumn<string>(
                name: "codigo_publico",
                table: "chamado",
                type: "varchar",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)");

            migrationBuilder.AlterColumn<string>(
                name: "categoria",
                table: "chamado",
                type: "varchar",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)");

            migrationBuilder.AlterColumn<string>(
                name: "nome_arquivo",
                table: "anexo",
                type: "varchar",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)");

            migrationBuilder.AlterColumn<int>(
                name: "interacao_id",
                table: "anexo",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "url",
                table: "anexo",
                type: "varchar",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(500)");

            migrationBuilder.AlterColumn<string>(
                name: "produto",
                table: "sla_categoria",
                type: "varchar",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)");

            migrationBuilder.AlterColumn<string>(
                name: "prioridade",
                table: "sla_categoria",
                type: "varchar",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)");

            migrationBuilder.AlterColumn<string>(
                name: "categoria",
                table: "sla_categoria",
                type: "varchar",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_sla_categoria",
                table: "sla_categoria",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_chamado_sla_categoria_sla_categoria_id",
                table: "chamado",
                column: "sla_categoria_id",
                principalTable: "sla_categoria",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_interacao_usuario_usuario_id",
                table: "interacao",
                column: "usuario_id",
                principalTable: "usuario",
                principalColumn: "id");
        }
    }
}
