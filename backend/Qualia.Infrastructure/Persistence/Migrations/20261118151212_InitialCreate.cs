using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Qualia.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "qualia");

            migrationBuilder.CreateTable(
                name: "administrador",
                schema: "qualia",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    correo = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_administrador", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "empresa",
                schema: "qualia",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fecha_registro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    minimo_muestra = table.Column<int>(type: "integer", nullable: false, defaultValue: 5)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_empresa", x => x.id);
                    table.CheckConstraint("ck_empresa_estado", "estado IN ('Activa', 'Suspendida')");
                    table.CheckConstraint("ck_empresa_minimo_muestra", "minimo_muestra > 0");
                });

            migrationBuilder.CreateTable(
                name: "plan",
                schema: "qualia",
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
                    table.PrimaryKey("pk_plan", x => x.id);
                    table.CheckConstraint("ck_plan_limites_positivos", "(limite_supervisores IS NULL OR limite_supervisores > 0) AND (limite_rubricas IS NULL OR limite_rubricas > 0)");
                    table.CheckConstraint("ck_plan_retencion_positiva", "retencion_dias > 0");
                    table.CheckConstraint("ck_plan_valores_no_negativos", "precio_mensual >= 0 AND minutos_incluidos >= 0 AND precio_excedente >= 0");
                });

            migrationBuilder.CreateTable(
                name: "auditoria_plataforma",
                schema: "qualia",
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
                    table.PrimaryKey("pk_auditoria_plataforma", x => x.id);
                    table.ForeignKey(
                        name: "fk_auditoria_plataforma_administrador_administrador_id",
                        column: x => x.administrador_id,
                        principalSchema: "qualia",
                        principalTable: "administrador",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_auditoria_plataforma_empresa_empresa_objetivo_id",
                        column: x => x.empresa_objetivo_id,
                        principalSchema: "qualia",
                        principalTable: "empresa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "campana",
                schema: "qualia",
                columns: table => new
                {
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_campana", x => new { x.empresa_id, x.id });
                    table.ForeignKey(
                        name: "fk_campana_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "qualia",
                        principalTable: "empresa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "usuario",
                schema: "qualia",
                columns: table => new
                {
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    correo = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    rol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuario", x => new { x.empresa_id, x.id });
                    table.CheckConstraint("ck_usuario_rol", "rol IN ('AdminEmpresa', 'Supervisor', 'Agente', 'Auditor')");
                    table.ForeignKey(
                        name: "fk_usuario_empresa_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "qualia",
                        principalTable: "empresa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "funcion_plan",
                schema: "qualia",
                columns: table => new
                {
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo_funcion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_funcion_plan", x => new { x.plan_id, x.codigo_funcion });
                    table.ForeignKey(
                        name: "fk_funcion_plan_planes_plan_id",
                        column: x => x.plan_id,
                        principalSchema: "qualia",
                        principalTable: "plan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "suscripcion",
                schema: "qualia",
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
                    table.PrimaryKey("ak_suscripciones_empresa_id_id", x => new { x.empresa_id, x.id });
                    table.CheckConstraint("ck_suscripcion_estado", "estado IN ('Prueba', 'Activa', 'EnMora', 'Vencida')");
                    table.CheckConstraint("ck_suscripcion_fechas", "fecha_vencimiento > fecha_inicio");
                    table.CheckConstraint("ck_suscripcion_gracia", "gracia_hasta IS NULL OR gracia_hasta >= fecha_vencimiento");
                    table.ForeignKey(
                        name: "fk_suscripcion_empresa_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "qualia",
                        principalTable: "empresa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_suscripcion_plan_plan_id",
                        column: x => x.plan_id,
                        principalSchema: "qualia",
                        principalTable: "plan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rubrica",
                schema: "qualia",
                columns: table => new
                {
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    campana_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rubrica", x => new { x.empresa_id, x.id });
                    table.CheckConstraint("ck_rubrica_estado", "estado IN ('Borrador', 'Activa', 'Retirada')");
                    table.CheckConstraint("ck_rubrica_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_rubrica_campana_empresa_id_campana_id",
                        columns: x => new { x.empresa_id, x.campana_id },
                        principalSchema: "qualia",
                        principalTable: "campana",
                        principalColumns: new[] { "empresa_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_rubrica_empresa_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "qualia",
                        principalTable: "empresa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "auditoria",
                schema: "qualia",
                columns: table => new
                {
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    accion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entidad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entidad_id = table.Column<Guid>(type: "uuid", nullable: true),
                    antes = table.Column<string>(type: "jsonb", nullable: true),
                    despues = table.Column<string>(type: "jsonb", nullable: true),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditoria", x => new { x.empresa_id, x.id });
                    table.ForeignKey(
                        name: "fk_auditoria_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "qualia",
                        principalTable: "empresa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_auditoria_usuario_empresa_id_usuario_id",
                        columns: x => new { x.empresa_id, x.usuario_id },
                        principalSchema: "qualia",
                        principalTable: "usuario",
                        principalColumns: new[] { "empresa_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "usuario_campana",
                schema: "qualia",
                columns: table => new
                {
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campana_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuario_campana", x => new { x.empresa_id, x.usuario_id, x.campana_id });
                    table.ForeignKey(
                        name: "fk_usuario_campana_campana_empresa_id_campana_id",
                        columns: x => new { x.empresa_id, x.campana_id },
                        principalSchema: "qualia",
                        principalTable: "campana",
                        principalColumns: new[] { "empresa_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_usuario_campana_empresa_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "qualia",
                        principalTable: "empresa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_usuario_campana_usuario_empresa_id_usuario_id",
                        columns: x => new { x.empresa_id, x.usuario_id },
                        principalSchema: "qualia",
                        principalTable: "usuario",
                        principalColumns: new[] { "empresa_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "periodo_consumo",
                schema: "qualia",
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
                    table.PrimaryKey("ak_periodos_consumo_empresa_id_id", x => new { x.empresa_id, x.id });
                    table.CheckConstraint("ck_periodo_consumo_fechas", "fecha_fin > fecha_inicio");
                    table.CheckConstraint("ck_periodo_consumo_minutos", "minutos_contratados >= 0");
                    table.ForeignKey(
                        name: "fk_periodo_consumo_empresa_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "qualia",
                        principalTable: "empresa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_periodo_consumo_suscripciones_empresa_id_suscripcion_id",
                        columns: x => new { x.empresa_id, x.suscripcion_id },
                        principalSchema: "qualia",
                        principalTable: "suscripcion",
                        principalColumns: new[] { "empresa_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "criterio_rubrica",
                schema: "qualia",
                columns: table => new
                {
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rubrica_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    peso = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    es_critico = table.Column<bool>(type: "boolean", nullable: false),
                    umbral_cumplimiento = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_criterio_rubrica", x => new { x.empresa_id, x.id });
                    table.CheckConstraint("ck_criterio_rubrica_peso", "peso > 0 AND peso <= 100");
                    table.CheckConstraint("ck_criterio_rubrica_umbral", "umbral_cumplimiento >= 0 AND umbral_cumplimiento <= 100");
                    table.ForeignKey(
                        name: "fk_criterio_rubrica_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "qualia",
                        principalTable: "empresa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_criterio_rubrica_rubrica_empresa_id_rubrica_id",
                        columns: x => new { x.empresa_id, x.rubrica_id },
                        principalSchema: "qualia",
                        principalTable: "rubrica",
                        principalColumns: new[] { "empresa_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "llamada",
                schema: "qualia",
                columns: table => new
                {
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rubrica_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    archivo_ruta = table.Column<string>(type: "text", nullable: false),
                    duracion_seg = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    fecha_llamada = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fecha_registro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_llamada", x => new { x.empresa_id, x.id });
                    table.CheckConstraint("ck_llamada_duracion", "duracion_seg > 0");
                    table.CheckConstraint("ck_llamada_estado", "estado IN ('Disponible', 'EnCola', 'Transcribiendo', 'Transcrita', 'Evaluando', 'EvaluadaPorIA', 'Fallida')");
                    table.ForeignKey(
                        name: "fk_llamada_empresa_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "qualia",
                        principalTable: "empresa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_llamada_rubrica_empresa_id_rubrica_id",
                        columns: x => new { x.empresa_id, x.rubrica_id },
                        principalSchema: "qualia",
                        principalTable: "rubrica",
                        principalColumns: new[] { "empresa_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_llamada_usuario_empresa_id_agente_id",
                        columns: x => new { x.empresa_id, x.agente_id },
                        principalSchema: "qualia",
                        principalTable: "usuario",
                        principalColumns: new[] { "empresa_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "consumo_llamada",
                schema: "qualia",
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
                    table.PrimaryKey("pk_consumo_llamada", x => new { x.empresa_id, x.id });
                    table.CheckConstraint("ck_consumo_llamada_estado", "estado IN ('Reservado', 'Facturado', 'Liberado')");
                    table.CheckConstraint("ck_consumo_llamada_valores", "minutos_facturables >= 0 AND minutos_reservados >= 0 AND tokens_transcriptor >= 0 AND tokens_entrada_evaluador >= 0 AND tokens_salida_evaluador >= 0 AND costo_estimado >= 0");
                    table.ForeignKey(
                        name: "fk_consumo_llamada_empresa_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "qualia",
                        principalTable: "empresa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_consumo_llamada_llamada_empresa_id_llamada_id",
                        columns: x => new { x.empresa_id, x.llamada_id },
                        principalSchema: "qualia",
                        principalTable: "llamada",
                        principalColumns: new[] { "empresa_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_consumo_llamada_periodos_consumo_empresa_id_periodo_id",
                        columns: x => new { x.empresa_id, x.periodo_id },
                        principalSchema: "qualia",
                        principalTable: "periodo_consumo",
                        principalColumns: new[] { "empresa_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "evaluacion_ia",
                schema: "qualia",
                columns: table => new
                {
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    llamada_id = table.Column<Guid>(type: "uuid", nullable: false),
                    modelo_usado = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    modo_ia = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    estado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    requiere_revision = table.Column<bool>(type: "boolean", nullable: false),
                    fecha_evaluacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_evaluacion_ia", x => new { x.empresa_id, x.id });
                    table.CheckConstraint("ck_evaluacion_ia_estado", "estado IN ('EvaluadaPorIA', 'ConfirmadaPorSupervisor', 'AjustadaPorSupervisor')");
                    table.CheckConstraint("ck_evaluacion_ia_modo", "modo_ia IN ('Real', 'Simulado')");
                    table.CheckConstraint("ck_evaluacion_ia_revision", "revision > 0");
                    table.ForeignKey(
                        name: "fk_evaluacion_ia_empresa_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "qualia",
                        principalTable: "empresa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_evaluacion_ia_llamada_empresa_id_llamada_id",
                        columns: x => new { x.empresa_id, x.llamada_id },
                        principalSchema: "qualia",
                        principalTable: "llamada",
                        principalColumns: new[] { "empresa_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "segmento_transcripcion",
                schema: "qualia",
                columns: table => new
                {
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    llamada_id = table.Column<Guid>(type: "uuid", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    hablante = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    inicio_seg = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    fin_seg = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    texto = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_segmento_transcripcion", x => new { x.empresa_id, x.id });
                    table.CheckConstraint("ck_segmento_transcripcion_orden", "orden > 0");
                    table.CheckConstraint("ck_segmento_transcripcion_tiempos", "inicio_seg >= 0 AND fin_seg >= inicio_seg");
                    table.ForeignKey(
                        name: "fk_segmento_transcripcion_empresa_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "qualia",
                        principalTable: "empresa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_segmento_transcripcion_llamada_empresa_id_llamada_id",
                        columns: x => new { x.empresa_id, x.llamada_id },
                        principalSchema: "qualia",
                        principalTable: "llamada",
                        principalColumns: new[] { "empresa_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "trabajo_procesamiento",
                schema: "qualia",
                columns: table => new
                {
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    llamada_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    intentos = table.Column<int>(type: "integer", nullable: false),
                    proximo_intento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ultimo_error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trabajo_procesamiento", x => new { x.empresa_id, x.id });
                    table.CheckConstraint("ck_trabajo_procesamiento_estado", "estado IN ('Pendiente', 'EnProceso', 'ReintentoPendiente', 'Completado', 'Fallido')");
                    table.CheckConstraint("ck_trabajo_procesamiento_intentos", "intentos >= 0");
                    table.ForeignKey(
                        name: "fk_trabajo_procesamiento_empresa_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "qualia",
                        principalTable: "empresa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_trabajo_procesamiento_llamada_empresa_id_llamada_id",
                        columns: x => new { x.empresa_id, x.llamada_id },
                        principalSchema: "qualia",
                        principalTable: "llamada",
                        principalColumns: new[] { "empresa_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "detalle_evaluacion",
                schema: "qualia",
                columns: table => new
                {
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    evaluacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criterio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cumple_criterio = table.Column<bool>(type: "boolean", nullable: false),
                    puntaje_criterio = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    observacion = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_detalle_evaluacion", x => new { x.empresa_id, x.id });
                    table.CheckConstraint("ck_detalle_evaluacion_puntaje", "puntaje_criterio >= 0 AND puntaje_criterio <= 100");
                    table.ForeignKey(
                        name: "fk_detalle_evaluacion_criterio_rubrica_empresa_id_criterio_id",
                        columns: x => new { x.empresa_id, x.criterio_id },
                        principalSchema: "qualia",
                        principalTable: "criterio_rubrica",
                        principalColumns: new[] { "empresa_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_detalle_evaluacion_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "qualia",
                        principalTable: "empresa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_detalle_evaluacion_evaluacion_ia_empresa_id_evaluacion_id",
                        columns: x => new { x.empresa_id, x.evaluacion_id },
                        principalSchema: "qualia",
                        principalTable: "evaluacion_ia",
                        principalColumns: new[] { "empresa_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "disputa",
                schema: "qualia",
                columns: table => new
                {
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    evaluacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    motivo = table.Column<string>(type: "text", nullable: false),
                    estado = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    resolucion = table.Column<string>(type: "text", nullable: true),
                    resuelta_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    creada_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resuelta_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_disputa", x => new { x.empresa_id, x.id });
                    table.CheckConstraint("ck_disputa_estado", "estado IN ('Abierta', 'Resuelta')");
                    table.CheckConstraint("ck_disputa_resolucion", "(estado = 'Abierta' AND resolucion IS NULL AND resuelta_por_id IS NULL AND resuelta_en IS NULL) OR (estado = 'Resuelta' AND resolucion IS NOT NULL AND resuelta_por_id IS NOT NULL AND resuelta_en IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_disputa_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "qualia",
                        principalTable: "empresa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_disputa_evaluacion_ia_empresa_id_evaluacion_id",
                        columns: x => new { x.empresa_id, x.evaluacion_id },
                        principalSchema: "qualia",
                        principalTable: "evaluacion_ia",
                        principalColumns: new[] { "empresa_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_disputa_usuario_empresa_id_resuelta_por_id",
                        columns: x => new { x.empresa_id, x.resuelta_por_id },
                        principalSchema: "qualia",
                        principalTable: "usuario",
                        principalColumns: new[] { "empresa_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "revision_humana",
                schema: "qualia",
                columns: table => new
                {
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    evaluacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supervisor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_revision = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    comentarios = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_revision_humana", x => new { x.empresa_id, x.id });
                    table.ForeignKey(
                        name: "fk_revision_humana_empresa_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "qualia",
                        principalTable: "empresa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_revision_humana_evaluacion_ia_empresa_id_evaluacion_id",
                        columns: x => new { x.empresa_id, x.evaluacion_id },
                        principalSchema: "qualia",
                        principalTable: "evaluacion_ia",
                        principalColumns: new[] { "empresa_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_revision_humana_usuario_empresa_id_supervisor_id",
                        columns: x => new { x.empresa_id, x.supervisor_id },
                        principalSchema: "qualia",
                        principalTable: "usuario",
                        principalColumns: new[] { "empresa_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "evidencia_criterio",
                schema: "qualia",
                columns: table => new
                {
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    detalle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    segmento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cita_textual = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_evidencia_criterio", x => new { x.empresa_id, x.detalle_id, x.segmento_id });
                    table.ForeignKey(
                        name: "fk_evidencia_criterio_detalle_evaluacion_empresa_id_detalle_id",
                        columns: x => new { x.empresa_id, x.detalle_id },
                        principalSchema: "qualia",
                        principalTable: "detalle_evaluacion",
                        principalColumns: new[] { "empresa_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_evidencia_criterio_empresa_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "qualia",
                        principalTable: "empresa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_evidencia_criterio_segmento_transcripcion_empresa_id_segmen",
                        columns: x => new { x.empresa_id, x.segmento_id },
                        principalSchema: "qualia",
                        principalTable: "segmento_transcripcion",
                        principalColumns: new[] { "empresa_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "criterio_revisado",
                schema: "qualia",
                columns: table => new
                {
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    revision_id = table.Column<Guid>(type: "uuid", nullable: false),
                    detalle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    puntaje_ajustado = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    observacion = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_criterio_revisado", x => new { x.empresa_id, x.id });
                    table.CheckConstraint("ck_criterio_revisado_puntaje", "puntaje_ajustado >= 0 AND puntaje_ajustado <= 100");
                    table.ForeignKey(
                        name: "fk_criterio_revisado_detalle_evaluacion_empresa_id_detalle_id",
                        columns: x => new { x.empresa_id, x.detalle_id },
                        principalSchema: "qualia",
                        principalTable: "detalle_evaluacion",
                        principalColumns: new[] { "empresa_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_criterio_revisado_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "qualia",
                        principalTable: "empresa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_criterio_revisado_revision_humana_empresa_id_revision_id",
                        columns: x => new { x.empresa_id, x.revision_id },
                        principalSchema: "qualia",
                        principalTable: "revision_humana",
                        principalColumns: new[] { "empresa_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "evidencia_revision",
                schema: "qualia",
                columns: table => new
                {
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criterio_revisado_id = table.Column<Guid>(type: "uuid", nullable: false),
                    segmento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cita_textual = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_evidencia_revision", x => new { x.empresa_id, x.criterio_revisado_id, x.segmento_id });
                    table.ForeignKey(
                        name: "fk_evidencia_revision_criterio_revisado_empresa_id_criterio_re",
                        columns: x => new { x.empresa_id, x.criterio_revisado_id },
                        principalSchema: "qualia",
                        principalTable: "criterio_revisado",
                        principalColumns: new[] { "empresa_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_evidencia_revision_empresa_empresa_id",
                        column: x => x.empresa_id,
                        principalSchema: "qualia",
                        principalTable: "empresa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_evidencia_revision_segmento_transcripcion_empresa_id_segmen",
                        columns: x => new { x.empresa_id, x.segmento_id },
                        principalSchema: "qualia",
                        principalTable: "segmento_transcripcion",
                        principalColumns: new[] { "empresa_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "qualia",
                table: "plan",
                columns: new[] { "id", "activo", "limite_rubricas", "limite_supervisores", "minutos_incluidos", "nombre", "precio_excedente", "precio_mensual", "retencion_dias" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-4000-8000-000000000001"), true, 1, 2, 1000.00m, "Básico", 0.05m, 29.00m, 365 },
                    { new Guid("10000000-0000-4000-8000-000000000002"), true, null, 10, 5000.00m, "Pro", 0.04m, 99.00m, 365 },
                    { new Guid("10000000-0000-4000-8000-000000000003"), true, null, null, 20000.00m, "Enterprise", 0.03m, 299.00m, 365 }
                });

            migrationBuilder.InsertData(
                schema: "qualia",
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
                name: "ix_administrador_correo",
                schema: "qualia",
                table: "administrador",
                column: "correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_auditoria_empresa_id_fecha",
                schema: "qualia",
                table: "auditoria",
                columns: new[] { "empresa_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ix_auditoria_empresa_id_usuario_id",
                schema: "qualia",
                table: "auditoria",
                columns: new[] { "empresa_id", "usuario_id" });

            migrationBuilder.CreateIndex(
                name: "ix_auditoria_plataforma_admin_fecha",
                schema: "qualia",
                table: "auditoria_plataforma",
                columns: new[] { "administrador_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ix_auditoria_plataforma_empresa_fecha",
                schema: "qualia",
                table: "auditoria_plataforma",
                columns: new[] { "empresa_objetivo_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ix_consumo_llamada_empresa_periodo",
                schema: "qualia",
                table: "consumo_llamada",
                columns: new[] { "empresa_id", "periodo_id" });

            migrationBuilder.CreateIndex(
                name: "uq_consumo_llamada_empresa_llamada",
                schema: "qualia",
                table: "consumo_llamada",
                columns: new[] { "empresa_id", "llamada_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_criterio_revisado_empresa_id_detalle_id",
                schema: "qualia",
                table: "criterio_revisado",
                columns: new[] { "empresa_id", "detalle_id" });

            migrationBuilder.CreateIndex(
                name: "ix_criterio_revisado_empresa_id_revision_id_detalle_id",
                schema: "qualia",
                table: "criterio_revisado",
                columns: new[] { "empresa_id", "revision_id", "detalle_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_criterio_rubrica_empresa_id_rubrica_id",
                schema: "qualia",
                table: "criterio_rubrica",
                columns: new[] { "empresa_id", "rubrica_id" });

            migrationBuilder.CreateIndex(
                name: "ix_detalle_evaluacion_empresa_id_criterio_id",
                schema: "qualia",
                table: "detalle_evaluacion",
                columns: new[] { "empresa_id", "criterio_id" });

            migrationBuilder.CreateIndex(
                name: "ix_detalle_evaluacion_empresa_id_evaluacion_id_criterio_id",
                schema: "qualia",
                table: "detalle_evaluacion",
                columns: new[] { "empresa_id", "evaluacion_id", "criterio_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_disputa_empresa_id_estado_creada_en",
                schema: "qualia",
                table: "disputa",
                columns: new[] { "empresa_id", "estado", "creada_en" });

            migrationBuilder.CreateIndex(
                name: "ix_disputa_empresa_id_resuelta_por_id",
                schema: "qualia",
                table: "disputa",
                columns: new[] { "empresa_id", "resuelta_por_id" });

            migrationBuilder.CreateIndex(
                name: "ux_disputa_abierta_por_evaluacion",
                schema: "qualia",
                table: "disputa",
                columns: new[] { "empresa_id", "evaluacion_id" },
                unique: true,
                filter: "estado = 'Abierta'");

            migrationBuilder.CreateIndex(
                name: "ix_empresa_codigo",
                schema: "qualia",
                table: "empresa",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_evaluacion_ia_empresa_id_llamada_id",
                schema: "qualia",
                table: "evaluacion_ia",
                columns: new[] { "empresa_id", "llamada_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_evidencia_criterio_empresa_id_segmento_id",
                schema: "qualia",
                table: "evidencia_criterio",
                columns: new[] { "empresa_id", "segmento_id" });

            migrationBuilder.CreateIndex(
                name: "ix_evidencia_revision_empresa_id_segmento_id",
                schema: "qualia",
                table: "evidencia_revision",
                columns: new[] { "empresa_id", "segmento_id" });

            migrationBuilder.CreateIndex(
                name: "ix_llamada_empresa_id_agente_id",
                schema: "qualia",
                table: "llamada",
                columns: new[] { "empresa_id", "agente_id" });

            migrationBuilder.CreateIndex(
                name: "ix_llamada_empresa_id_estado_rubrica_id",
                schema: "qualia",
                table: "llamada",
                columns: new[] { "empresa_id", "estado", "rubrica_id" });

            migrationBuilder.CreateIndex(
                name: "ix_llamada_empresa_id_rubrica_id",
                schema: "qualia",
                table: "llamada",
                columns: new[] { "empresa_id", "rubrica_id" });

            migrationBuilder.CreateIndex(
                name: "ix_periodo_consumo_empresa_suscripcion",
                schema: "qualia",
                table: "periodo_consumo",
                columns: new[] { "empresa_id", "suscripcion_id" });

            migrationBuilder.CreateIndex(
                name: "uq_periodo_consumo_empresa_fin",
                schema: "qualia",
                table: "periodo_consumo",
                columns: new[] { "empresa_id", "fecha_fin" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_plan_nombre",
                schema: "qualia",
                table: "plan",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_revision_humana_empresa_id_evaluacion_id_fecha_revision",
                schema: "qualia",
                table: "revision_humana",
                columns: new[] { "empresa_id", "evaluacion_id", "fecha_revision" });

            migrationBuilder.CreateIndex(
                name: "ix_revision_humana_empresa_id_supervisor_id",
                schema: "qualia",
                table: "revision_humana",
                columns: new[] { "empresa_id", "supervisor_id" });

            migrationBuilder.CreateIndex(
                name: "ix_rubrica_empresa_id_campana_id_version",
                schema: "qualia",
                table: "rubrica",
                columns: new[] { "empresa_id", "campana_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_rubrica_activa_por_campana",
                schema: "qualia",
                table: "rubrica",
                columns: new[] { "empresa_id", "campana_id" },
                unique: true,
                filter: "estado = 'Activa'");

            migrationBuilder.CreateIndex(
                name: "ix_segmento_transcripcion_empresa_id_llamada_id_orden",
                schema: "qualia",
                table: "segmento_transcripcion",
                columns: new[] { "empresa_id", "llamada_id", "orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_suscripcion_plan_id",
                schema: "qualia",
                table: "suscripcion",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "uq_suscripcion_vigente_empresa",
                schema: "qualia",
                table: "suscripcion",
                column: "empresa_id",
                unique: true,
                filter: "estado IN ('Prueba', 'Activa', 'EnMora')");

            migrationBuilder.CreateIndex(
                name: "ix_trabajo_procesamiento_empresa_id_llamada_id",
                schema: "qualia",
                table: "trabajo_procesamiento",
                columns: new[] { "empresa_id", "llamada_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_trabajo_procesamiento_estado_proximo_intento",
                schema: "qualia",
                table: "trabajo_procesamiento",
                columns: new[] { "estado", "proximo_intento" });

            migrationBuilder.CreateIndex(
                name: "ix_usuario_empresa_id_correo",
                schema: "qualia",
                table: "usuario",
                columns: new[] { "empresa_id", "correo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuario_campana_empresa_id_campana_id",
                schema: "qualia",
                table: "usuario_campana",
                columns: new[] { "empresa_id", "campana_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auditoria",
                schema: "qualia");

            migrationBuilder.DropTable(
                name: "auditoria_plataforma",
                schema: "qualia");

            migrationBuilder.DropTable(
                name: "consumo_llamada",
                schema: "qualia");

            migrationBuilder.DropTable(
                name: "disputa",
                schema: "qualia");

            migrationBuilder.DropTable(
                name: "evidencia_criterio",
                schema: "qualia");

            migrationBuilder.DropTable(
                name: "evidencia_revision",
                schema: "qualia");

            migrationBuilder.DropTable(
                name: "funcion_plan",
                schema: "qualia");

            migrationBuilder.DropTable(
                name: "trabajo_procesamiento",
                schema: "qualia");

            migrationBuilder.DropTable(
                name: "usuario_campana",
                schema: "qualia");

            migrationBuilder.DropTable(
                name: "administrador",
                schema: "qualia");

            migrationBuilder.DropTable(
                name: "periodo_consumo",
                schema: "qualia");

            migrationBuilder.DropTable(
                name: "criterio_revisado",
                schema: "qualia");

            migrationBuilder.DropTable(
                name: "segmento_transcripcion",
                schema: "qualia");

            migrationBuilder.DropTable(
                name: "suscripcion",
                schema: "qualia");

            migrationBuilder.DropTable(
                name: "detalle_evaluacion",
                schema: "qualia");

            migrationBuilder.DropTable(
                name: "revision_humana",
                schema: "qualia");

            migrationBuilder.DropTable(
                name: "plan",
                schema: "qualia");

            migrationBuilder.DropTable(
                name: "criterio_rubrica",
                schema: "qualia");

            migrationBuilder.DropTable(
                name: "evaluacion_ia",
                schema: "qualia");

            migrationBuilder.DropTable(
                name: "llamada",
                schema: "qualia");

            migrationBuilder.DropTable(
                name: "rubrica",
                schema: "qualia");

            migrationBuilder.DropTable(
                name: "usuario",
                schema: "qualia");

            migrationBuilder.DropTable(
                name: "campana",
                schema: "qualia");

            migrationBuilder.DropTable(
                name: "empresa",
                schema: "qualia");
        }
    }
}
