using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Qualia.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSuscripciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "auditoria_plataforma",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    administrador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_objetivo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    accion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    antes = table.Column<string>(type: "jsonb", nullable: true),
                    despues = table.Column<string>(type: "jsonb", nullable: true),
                    fecha = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auditoria_plataforma", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "plan",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    precio_mensual = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    minutos_incluidos = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    precio_excedente = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    limite_supervisores = table.Column<int>(type: "integer", nullable: true),
                    limite_rubricas = table.Column<int>(type: "integer", nullable: true),
                    retencion_dias = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan", x => x.id);
                    table.CheckConstraint("ck_plan_limites_positivos", "(limite_supervisores IS NULL OR limite_supervisores > 0) AND (limite_rubricas IS NULL OR limite_rubricas > 0)");
                    table.CheckConstraint("ck_plan_retencion_positiva", "retencion_dias > 0");
                    table.CheckConstraint("ck_plan_valores_no_negativos", "precio_mensual >= 0 AND minutos_incluidos >= 0 AND precio_excedente >= 0");
                });

            migrationBuilder.CreateTable(
                name: "funcion_plan",
                columns: table => new
                {
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo_funcion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_funcion_plan", x => new { x.plan_id, x.codigo_funcion });
                    table.ForeignKey(
                        name: "FK_funcion_plan_plan_plan_id",
                        column: x => x.plan_id,
                        principalTable: "plan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "suscripcion",
                columns: table => new
                {
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_inicio = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    fecha_vencimiento = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    estado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    permite_excedente = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    gracia_hasta = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suscripcion", x => new { x.empresa_id, x.id });
                    table.CheckConstraint("ck_suscripcion_estado", "estado IN ('Prueba', 'Activa', 'EnMora', 'Vencida')");
                    table.CheckConstraint("ck_suscripcion_fechas", "fecha_vencimiento > fecha_inicio");
                    table.CheckConstraint("ck_suscripcion_gracia", "gracia_hasta IS NULL OR gracia_hasta >= fecha_vencimiento");
                    table.ForeignKey(
                        name: "FK_suscripcion_plan_plan_id",
                        column: x => x.plan_id,
                        principalTable: "plan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "periodo_consumo",
                columns: table => new
                {
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    suscripcion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_inicio = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    fecha_fin = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    minutos_contratados = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_periodo_consumo", x => new { x.empresa_id, x.id });
                    table.CheckConstraint("ck_periodo_consumo_fechas", "fecha_fin > fecha_inicio");
                    table.CheckConstraint("ck_periodo_consumo_minutos", "minutos_contratados >= 0");
                    table.ForeignKey(
                        name: "FK_periodo_consumo_suscripcion_empresa_id_suscripcion_id",
                        columns: x => new { x.empresa_id, x.suscripcion_id },
                        principalTable: "suscripcion",
                        principalColumns: new[] { "empresa_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "consumo_llamada",
                columns: table => new
                {
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    llamada_id = table.Column<Guid>(type: "uuid", nullable: false),
                    periodo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    minutos_facturables = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    tokens_transcriptor = table.Column<int>(type: "integer", nullable: false),
                    tokens_entrada_evaluador = table.Column<int>(type: "integer", nullable: false),
                    tokens_salida_evaluador = table.Column<int>(type: "integer", nullable: false),
                    costo_estimado = table.Column<decimal>(type: "numeric(12,6)", precision: 12, scale: 6, nullable: false),
                    minutos_reservados = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consumo_llamada", x => new { x.empresa_id, x.id });
                    table.CheckConstraint("ck_consumo_llamada_estado", "estado IN ('Reservado', 'Facturado', 'Liberado')");
                    table.CheckConstraint("ck_consumo_llamada_valores", "minutos_facturables >= 0 AND minutos_reservados >= 0 AND tokens_transcriptor >= 0 AND tokens_entrada_evaluador >= 0 AND tokens_salida_evaluador >= 0 AND costo_estimado >= 0");
                    table.ForeignKey(
                        name: "FK_consumo_llamada_periodo_consumo_empresa_id_periodo_id",
                        columns: x => new { x.empresa_id, x.periodo_id },
                        principalTable: "periodo_consumo",
                        principalColumns: new[] { "empresa_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "plan",
                columns: new[] { "id", "activo", "limite_rubricas", "limite_supervisores", "minutos_incluidos", "nombre", "precio_excedente", "precio_mensual", "retencion_dias" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-4000-8000-000000000001"), true, 1, 2, 1000.00m, "Básico", 0.05m, 29.00m, 365 },
                    { new Guid("10000000-0000-4000-8000-000000000002"), true, null, 10, 5000.00m, "Pro", 0.04m, 99.00m, 365 },
                    { new Guid("10000000-0000-4000-8000-000000000003"), true, null, null, 20000.00m, "Enterprise", 0.03m, 299.00m, 365 }
                });

            migrationBuilder.InsertData(
                table: "funcion_plan",
                columns: new[] { "codigo_funcion", "plan_id" },
                values: new object[,]
                {
                    { "EVALUACION", new Guid("10000000-0000-4000-8000-000000000001") },
                    { "SCORECARD_BASICO", new Guid("10000000-0000-4000-8000-000000000001") },
                    { "COMPARACION_CAMPANAS", new Guid("10000000-0000-4000-8000-000000000002") },
                    { "DISPUTAS", new Guid("10000000-0000-4000-8000-000000000002") },
                    { "EVALUACION", new Guid("10000000-0000-4000-8000-000000000002") },
                    { "SCORECARD_BASICO", new Guid("10000000-0000-4000-8000-000000000002") },
                    { "TENDENCIA", new Guid("10000000-0000-4000-8000-000000000002") },
                    { "COMPARACION_CAMPANAS", new Guid("10000000-0000-4000-8000-000000000003") },
                    { "DISPUTAS", new Guid("10000000-0000-4000-8000-000000000003") },
                    { "EVALUACION", new Guid("10000000-0000-4000-8000-000000000003") },
                    { "SCORECARD_BASICO", new Guid("10000000-0000-4000-8000-000000000003") },
                    { "TENDENCIA", new Guid("10000000-0000-4000-8000-000000000003") }
                });

            migrationBuilder.CreateIndex(
                name: "ix_auditoria_plataforma_admin_fecha",
                table: "auditoria_plataforma",
                columns: new[] { "administrador_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ix_auditoria_plataforma_empresa_fecha",
                table: "auditoria_plataforma",
                columns: new[] { "empresa_objetivo_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ix_consumo_llamada_empresa_periodo",
                table: "consumo_llamada",
                columns: new[] { "empresa_id", "periodo_id" });

            migrationBuilder.CreateIndex(
                name: "uq_consumo_llamada_empresa_llamada",
                table: "consumo_llamada",
                columns: new[] { "empresa_id", "llamada_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_periodo_consumo_empresa_id_suscripcion_id",
                table: "periodo_consumo",
                columns: new[] { "empresa_id", "suscripcion_id" });

            migrationBuilder.CreateIndex(
                name: "uq_periodo_consumo_empresa_fin",
                table: "periodo_consumo",
                columns: new[] { "empresa_id", "fecha_fin" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_plan_nombre",
                table: "plan",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_suscripcion_plan_id",
                table: "suscripcion",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "uq_suscripcion_vigente_empresa",
                table: "suscripcion",
                column: "empresa_id",
                unique: true,
                filter: "estado IN ('Prueba', 'Activa', 'EnMora')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auditoria_plataforma");

            migrationBuilder.DropTable(
                name: "consumo_llamada");

            migrationBuilder.DropTable(
                name: "funcion_plan");

            migrationBuilder.DropTable(
                name: "periodo_consumo");

            migrationBuilder.DropTable(
                name: "suscripcion");

            migrationBuilder.DropTable(
                name: "plan");
        }
    }
}
