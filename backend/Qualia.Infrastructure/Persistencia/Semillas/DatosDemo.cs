namespace Qualia.Infrastructure.Persistencia.Semillas;

/// <summary>
/// Identificadores fijos de los datos de demostración.
/// Dos empresas para verificar aislamiento: Norte y Sur.
/// </summary>
public static class DatosDemo
{
    public static class Norte
    {
        public static readonly Guid Empresa = Guid.Parse("10000000-0000-0000-0000-000000000001");
        public static readonly Guid Admin = Guid.Parse("10000000-0000-0000-0001-000000000001");
        public static readonly Guid Supervisora = Guid.Parse("10000000-0000-0000-0001-000000000002");
        public static readonly Guid SupervisorSinCampana = Guid.Parse("10000000-0000-0000-0001-000000000003");
        public static readonly Guid AgenteAndres = Guid.Parse("10000000-0000-0000-0001-000000000004");
        public static readonly Guid AgenteAlicia = Guid.Parse("10000000-0000-0000-0001-000000000005");
        public static readonly Guid Auditor = Guid.Parse("10000000-0000-0000-0001-000000000006");
        public static readonly Guid Campana = Guid.Parse("10000000-0000-0000-0002-000000000001");
        public static readonly Guid Rubrica = Guid.Parse("10000000-0000-0000-0003-000000000001");
        public static readonly Guid RubricaBorrador = Guid.Parse("10000000-0000-0000-0003-000000000002");

        /// <summary>Llamadas 1–6 (Disponible, rúbrica publicada). Impares: Andrés; pares: Alicia.</summary>
        public static readonly Guid[] Llamadas =
        [
            Guid.Parse("10000000-0000-0000-0005-000000000001"),
            Guid.Parse("10000000-0000-0000-0005-000000000002"),
            Guid.Parse("10000000-0000-0000-0005-000000000003"),
            Guid.Parse("10000000-0000-0000-0005-000000000004"),
            Guid.Parse("10000000-0000-0000-0005-000000000005"),
            Guid.Parse("10000000-0000-0000-0005-000000000006"),
        ];

        /// <summary>Llamada ligada a una rúbrica en Borrador (para probar RUBRICA_SIN_PUBLICAR).</summary>
        public static readonly Guid LlamadaRubricaBorrador = Guid.Parse("10000000-0000-0000-0005-000000000099");
    }

    public static class Sur
    {
        public static readonly Guid Empresa = Guid.Parse("20000000-0000-0000-0000-000000000001");
        public static readonly Guid Supervisor = Guid.Parse("20000000-0000-0000-0001-000000000002");
        public static readonly Guid Agente = Guid.Parse("20000000-0000-0000-0001-000000000004");
        public static readonly Guid Campana = Guid.Parse("20000000-0000-0000-0002-000000000001");
        public static readonly Guid Rubrica = Guid.Parse("20000000-0000-0000-0003-000000000001");

        public static readonly Guid[] Llamadas =
        [
            Guid.Parse("20000000-0000-0000-0005-000000000001"),
            Guid.Parse("20000000-0000-0000-0005-000000000002"),
            Guid.Parse("20000000-0000-0000-0005-000000000003"),
        ];
    }
}
