// CalculadoraNomina.cs — VERSION LIMPIA para .NET 8
using System;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace Nomina_2026_NET8
{
    // ═══════════════════════════════════════════════════════════════════
    // CONSTANTES COMPARTIDAS
    // ═══════════════════════════════════════════════════════════════════

    internal static class ConstantesNomina
    {
        public static int AntiguedadPorDiaExacto(string fechaAltaTexto, DateTime fechaCorte)
        {
            if (!DateTime.TryParseExact(fechaAltaTexto, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fechaAlta))
            {
                return 0;
            }

            return AntiguedadPorDiaExacto(fechaAlta, fechaCorte);
        }

        public static int AntiguedadPorDiaExacto(DateTime fechaAlta, DateTime fechaCorte)
        {
            fechaAlta = fechaAlta.Date;
            fechaCorte = fechaCorte.Date;

            if (fechaCorte < fechaAlta)
                return 0;

            int anios = fechaCorte.Year - fechaAlta.Year;

            // Si todavía no llegó el aniversario exacto, no cuenta como año cumplido.
            if (fechaAlta.AddYears(anios) > fechaCorte)
                anios--;

            return Math.Max(0, anios);
        }

        /// Compatibilidad con el VB6 probado: antig = año fiscal + 2 - año de ingreso.
        /// Ejemplo:
        /// año fiscal 2026, alta 04/11/2024:
        /// antig = 2026 + 2 - 2024 = 4
        /// factor = 1.0534
        public static int AntiguedadBaseImssVB6(string fechaAlta, int anoFiscal)
        {
            if (string.IsNullOrWhiteSpace(fechaAlta))
                return 1;

            string[] partes = fechaAlta.Split('/');

            if (partes.Length < 3 || !int.TryParse(partes[2], out int aoingr))
                return 1;

            if (aoingr < 100)
            {
                aoingr = aoingr > 50 ? aoingr + 1900 : aoingr + 2000;
            }

            if (aoingr < 1900)
                return 1;

            int antig = anoFiscal + 2 - aoingr;

            return Math.Max(1, antig);
        }

        /// Factor de integración salarial máximo (30+ años de antigüedad). Usado como fallback en SDI cuando no se conoce la antigüedad exacta.
        public const decimal FactorSDI = 1.0630m;

        /// Tabla directa de factor de integración. Recibe el año/base IMSS ya calculado.
        public static decimal FactorSDIPorAnioBase(int anioBaseIMSS)
        {
            anioBaseIMSS = Math.Max(1, anioBaseIMSS);

            return anioBaseIMSS switch
            {
                1 => 1.0493m,
                2 => 1.0507m,
                3 => 1.0521m,
                4 => 1.0534m,
                5 => 1.0548m,
                <= 10 => 1.0562m,
                <= 15 => 1.0575m,
                <= 20 => 1.0589m,
                <= 25 => 1.0603m,
                <= 30 => 1.0616m,
                _ => 1.0630m,
            };
        }

        /// NOM-005 literal: años cumplidos + 1.
        public static decimal FactorSDIPorAntiguedad(int aniosCumplidos)
        {
            int anioBaseIMSS = Math.Max(1, aniosCumplidos + 1);
            return FactorSDIPorAnioBase(anioBaseIMSS);
        }

        /// Calcula antigüedad en años igual que el VB6: empresa.ao + 2 - año_de_fecha_alta. Si el año no es parseable o es menor a 1900, retorna 1 (mínimo).
        [Obsolete("Usar AntiguedadPorDiaExacto(fechaAlta, fechaCorte). NOM-004/NOM-005 eliminan la lógica año fiscal + 2.")]
        public static int AntiguedadVB6(string fechaAlta, int anoFiscal)
        {
            if (string.IsNullOrWhiteSpace(fechaAlta)) return 1;

            string[] partes = fechaAlta.Split('/');
            if (partes.Length < 3 || !int.TryParse(partes[2], out int aoingr) || aoingr < 1900)
                return 1;

            int antig = anoFiscal + 2 - aoingr;
            return antig < 1 ? 1 : antig;
        }

        public static int AntiguedadAltaEmpleadoVB6(DateTime fechaAlta, int anoFiscal)
        {
            int antiguedad = anoFiscal - fechaAlta.Year;

            if (antiguedad < 1)
                antiguedad = 1;

            return antiguedad;
        }

        public static int AntiguedadAltaEmpleadoVB6(string fechaAltaTexto, int anoFiscal)
        {
            if (!DateTime.TryParseExact(fechaAltaTexto, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fechaAlta))
            {
                return 1;
            }

            return AntiguedadAltaEmpleadoVB6(fechaAlta, anoFiscal);
        }

        /// <summary>
        /// Determina si un motivo de nómina especial corresponde a PTU o Aguinaldo.
        /// Equivalente a checar() Case 5 (Aguinaldo) / Case 6 (PTU) del VB6: son los
        /// únicos dos conceptos que en el original reparten el importe capturado entre
        /// gravado y exento contra un tope (Text3/"exento"). Cualquier otro motivo
        /// (Liquidación, Finiquito, Bono, Vacaciones, "OTROS...") pasa sin repartir,
        /// igual que el Case 4 genérico de VB6 ("dato_sal = dato_ent").
        /// </summary>
        public static bool EsMotivoPtuOAguinaldo(string motivo)
        {
            if (string.IsNullOrWhiteSpace(motivo)) return false;
            string m = motivo.Trim().ToUpperInvariant();
            return m.StartsWith("PTU") || m.StartsWith("AGUIN");
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // CLASE 1: ResultadoISR
    // ═══════════════════════════════════════════════════════════════════

    public class ResultadoISR
    {
        public decimal IngresoGravable { get; set; }
        public decimal ImpuestoCalculado { get; set; }
        public decimal SubsidioCausado { get; set; }
        public decimal ImpuestoNeto { get; set; }
        public decimal PorcentajeAplicado { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════
    // CLASE 2: CalculadoraISR
    // ═══════════════════════════════════════════════════════════════════

    public class CalculadoraISR
    {
        private readonly TablasTarifaRoot _tablas;

        public CalculadoraISR(TablasTarifaRoot tablas)
        {
            _tablas = tablas ?? throw new ArgumentNullException(nameof(tablas));
        }

        // ── Primera quincena ──────────────────────────────────────────
        // Tabla ISR:      ISR_113     (Tab08Kin.ISR)
        // Tabla Subsidio: SUBSIDIO_114(Tab08kin.SUB)
        public ResultadoISR CalcularQuincenal(decimal ingresoGravable)
        {
            decimal impuesto = 0m, subsidio = 0m, porc = 0m;

            var rISR = BuscarEnTabla(_tablas.tablas.ISR_113, ingresoGravable);
            if (rISR != null)
                (impuesto, porc) = AplicarTarifa(rISR, ingresoGravable);

            var rSub = BuscarEnTabla(_tablas.tablas.SUBSIDIO_114, ingresoGravable);
            if (rSub != null)
                subsidio = rSub.cuotaFija;

            decimal neto = Math.Max(Math.Round(impuesto - subsidio, 2,
                MidpointRounding.AwayFromZero), 0m);

            return new ResultadoISR
            {
                IngresoGravable = Math.Round(ingresoGravable, 2, MidpointRounding.AwayFromZero),
                ImpuestoCalculado = Math.Round(impuesto, 2, MidpointRounding.AwayFromZero),
                SubsidioCausado = Math.Round(subsidio, 2, MidpointRounding.AwayFromZero),
                ImpuestoNeto = neto,
                PorcentajeAplicado = porc
            };
        }

        // ── Segunda quincena ──────────────────────────────────────────
        // Tabla ISR:      ISR_MENSUAL      (Tab08Mes.ISR)
        // Tabla Subsidio: SUBSIDIO_MENSUAL (Tab08Mes.SUB)
        public ResultadoISR CalcularSegundaQuincena(decimal ingresoGravable)
        {
            if (_tablas.tablas.ISR_MENSUAL == null || !_tablas.tablas.ISR_MENSUAL.Any())
                throw new InvalidOperationException("La tabla ISR_MENSUAL no está cargada.\nRegenera TablasTarifa.dat con TablasGenerator.");

            decimal ispt = 0m, subsidio = 0m, porc = 0m;

            var rango = BuscarEnTabla(_tablas.tablas.ISR_MENSUAL, ingresoGravable);
            if (rango != null)
                (ispt, porc) = AplicarTarifa(rango, ingresoGravable);

            var rSub = BuscarEnTabla(_tablas.tablas.SUBSIDIO_MENSUAL, ingresoGravable);
            if (rSub != null)
                subsidio = rSub.cuotaFija;

            return new ResultadoISR
            {
                IngresoGravable = Math.Round(ingresoGravable, 2, MidpointRounding.AwayFromZero),
                ImpuestoCalculado = Math.Round(ispt, 2, MidpointRounding.AwayFromZero),
                SubsidioCausado = Math.Round(subsidio, 2, MidpointRounding.AwayFromZero),
                ImpuestoNeto = Math.Round(Math.Max(ispt - subsidio, 0m), 2, MidpointRounding.AwayFromZero),
                PorcentajeAplicado = porc
            };
        }

        // ── ISR bruto sin subsidio ────────────────────────────────────
        // 🔧 Renombrado de CalcularISRMensualBruto → CalcularISRBruto
        //    (el nombre "Mensual" era confuso — recibe cualquier base gravable)
        public decimal CalcularISRBruto(decimal baseGravable)
        {
            if (_tablas.tablas.ISR_MENSUAL == null) return 0m;

            var rango = BuscarEnTabla(_tablas.tablas.ISR_MENSUAL, baseGravable);
            if (rango == null) return 0m;

            var (isr, _) = AplicarTarifa(rango, baseGravable);
            return Math.Max(isr, 0m);
        }

        // ── ISR diferencial para nómina especial ──────────────────────
        public decimal CalcularISREspecial(decimal totalGravable, decimal salarioDiario)
        {
            if (totalGravable <= 0m) return 0m;

            decimal equivalenteMensual = totalGravable / 365m * 30.4m;
            decimal sueldoMensual = salarioDiario * 30m;

            decimal isrBase = CalcularISRBruto(sueldoMensual);
            decimal isrTotal = CalcularISRBruto(sueldoMensual + equivalenteMensual);

            decimal diferencia = isrTotal - isrBase;
            if (diferencia <= 0m) return 0m;

            decimal tasaMarginal = diferencia / equivalenteMensual;
            return Math.Max(Math.Round(totalGravable * tasaMarginal, 2, MidpointRounding.AwayFromZero), 0m);
        }

        // ── Helpers privados ──────────────────────────────────────────

        // 🔧 Método extraído para eliminar el patrón duplicado en Quincenal/Segunda
        private static RangoTarifa BuscarEnTabla(System.Collections.Generic.List<RangoTarifa> tabla, decimal ingreso) => tabla?.FirstOrDefault(t => ingreso >= t.limInf && ingreso <= t.limSup);

        private static (decimal impuesto, decimal porcentaje) AplicarTarifa(RangoTarifa rango, decimal ingreso)
        {
            decimal exc = ingreso - rango.limInf;
            decimal imp = Math.Round(exc * (rango.porcentaje / 100m), 4, MidpointRounding.AwayFromZero);
            decimal impuesto = Math.Round(rango.cuotaFija + imp, 4, MidpointRounding.AwayFromZero);
            return (impuesto, rango.porcentaje);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // CLASE 3: CalculadoraIMSS
    // ═══════════════════════════════════════════════════════════════════

    public class CalculadoraIMSS
    {
        private readonly decimal _baseImss;

        public CalculadoraIMSS(decimal baseImss)
        {
            _baseImss = baseImss;
        }

        public decimal CalcularCuotaObrera(decimal integrado, int diasTrabajados)
        {
            if (integrado <= 0m || diasTrabajados <= 0)
                return 0m;

            decimal salint1 = integrado;
            decimal salint2;
            decimal enfymat = 0m;
            decimal otrascu = 0m;

            if (integrado > (_baseImss * 3m))
            {
                salint1 = integrado > (_baseImss * 25m) ? _baseImss * 25m : integrado;
                enfymat = (salint1 - (_baseImss * 3m)) * (0.4m / 100m);
            }

            enfymat += salint1 * ((0.25m + 0.375m) / 100m);
            salint2 = integrado > (_baseImss * 25m) ? _baseImss * 25m : integrado;

            if (integrado > ((_baseImss * 1.0561m) + 0.01m))
            {
                otrascu = salint2 * ((0.625m + 1.125m) / 100m);
            }

            decimal seguro = diasTrabajados * (enfymat + otrascu);

            return Math.Round(seguro, 2, MidpointRounding.AwayFromZero);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // CLASE 4: ServicioCalculoNomina
    // ═══════════════════════════════════════════════════════════════════

    public class ServicioCalculoNomina
    {
        private readonly int _anioFiscal;
        private readonly decimal _uma;
        private readonly decimal _salarioMinimo;
        private readonly CalculadoraISR _isrCalc;
        private readonly CalculadoraIMSS _imssCalc;

        public ServicioCalculoNomina(decimal uma, decimal salarioMinimo, int anioActual, TablasTarifaRoot tablas)
        {
            _uma = uma;
            _salarioMinimo = salarioMinimo;
            _anioFiscal = anioActual;
            _isrCalc = new CalculadoraISR(tablas);

            // Para igualar el VB6 probado, la base de topes IMSS debe ser UMA.
            // En tu ejemplo: 117.31.
            _imssCalc = new CalculadoraIMSS(uma);
        }

        public ResultadoNominaCompleto CalcularNomina(EmpleadoCompleto emp, DatosCapturados d, DateTime fechaCorteNomina)
        {
            // 1. Salario del periodo
            decimal salario = Math.Round(d.SalarioDiario * d.DiasTrabajados, 2);

            // 1.b Horas normales
            decimal salarioHora = Math.Round(d.SalarioDiario / 8m, 4, MidpointRounding.AwayFromZero);
            decimal ingresoHorasNormales = !d.EsNominaEspecial ? Math.Round(d.HorasNormales * salarioHora, 2) : d.HorasNormales;   // ya viene en pesos cuando es especial

            // 2. Otras
            // PTU/Aguinaldo (checar() Case 5/6 en VB6) conservan 2 decimales porque
            // ya vienen repartidas gravado/exento contra un tope en pesos con centavos
            // (ver AplicarRepartoExentoEspecial en FormCaptura). El resto de conceptos
            // ("Otras" tarifa-por-día en nómina normal, o cualquier otro motivo especial
            // tipo Liquidación/Finiquito/Bono) se sigue redondeando a pesos enteros,
            // igual que siempre.
            bool esPtuOAguinaldo = d.EsNominaEspecial && ConstantesNomina.EsMotivoPtuOAguinaldo(d.MotivoEspecial);
            decimal otras = esPtuOAguinaldo
                ? Math.Round(d.OtrasPercepciones, 2, MidpointRounding.AwayFromZero)
                : Math.Round(d.OtrasPercepciones, 0, MidpointRounding.AwayFromZero);

            // 3. SDI para IMSS calculado desde la fila de nómina, como VB6
            decimal integrado = CalcularSalarioDiarioIntegradoIMSS(emp, d, fechaCorteNomina, salario, otras);

            // 4. Total ingresos
            decimal totalIngr = Math.Round(salario + d.HorasNormales + d.HorasDobles + d.HorasTriples + d.OF + d.PrimaVacacional + otras + d.PercepcionExenta, 2);

            // 5. Ingreso gravable
            decimal gravable = totalIngr - d.PercepcionExenta;

            // 6. ISR / Subsidio / IMSS
            decimal isrMostrado;
            decimal subsidioMostrado;
            decimal subsidioCausado;
            decimal imss;

            if (d.EsNominaEspecial)
            {
                isrMostrado = _isrCalc.CalcularISREspecial(gravable, emp.SalarioDiario);
                subsidioMostrado = 0m;
                subsidioCausado = 0m;
                imss = 0m;
            }
            else
            {
                ResultadoISR r = d.EsSegundaQuincena ? _isrCalc.CalcularSegundaQuincena(gravable) : _isrCalc.CalcularQuincenal(gravable);

                // VB6:
                // columna 12 / ISPT contiene el impuesto antes de restar
                // el subsidio que aparece aparte en columna 13.
                isrMostrado = Math.Round(r.ImpuestoCalculado + r.SubsidioCausado, 2, MidpointRounding.AwayFromZero);

                // Lo que se muestra en FormCaptura/SubEmp.
                // VB6 posteriormente hace * -1 dentro de reng().
                subsidioMostrado = r.SubsidioCausado != 0m ? Math.Round(-r.SubsidioCausado, 2, MidpointRounding.AwayFromZero) : 0m;

                // ESTE ES nom_com.subdio / subc13.
                subsidioCausado = r.SubsidioCausado;

                imss = _imssCalc.CalcularCuotaObrera(integrado, d.DiasTrabajados);
            }

            // 7. Deducciones y neto
            decimal deduc = Math.Round(isrMostrado + subsidioMostrado + imss + d.Prestamos + d.FONACOT + d.PensionAlimenticia + d.INFONAVIT, 2);

            decimal neto = Math.Round(totalIngr - deduc, 2);

            return new ResultadoNominaCompleto
            {
                SalarioPeriodo = salario,
                TotalIngresos = totalIngr,
                ISR = isrMostrado,
                Subsidio = subsidioMostrado,
                IMSS = imss,
                TotalDeducciones = deduc,
                Neto = neto,
                SalarioIntegrado = integrado,
                SubsidioCausado = subsidioCausado
            };
        }

        private decimal CalcularSalarioDiarioIntegradoIMSS(EmpleadoCompleto emp, DatosCapturados d, DateTime fechaCorteNomina, decimal salarioPeriodo, decimal otrasPeriodo)
        {
            if (emp == null || d == null || d.DiasTrabajados <= 0)
                return 0m;

            /*
                VB6:
                For late = 3 To 9
                    sum(1) = sum(1) + ConNom1.TextMatrix(li, late)
                Next

                integrado = (sum(1) - p_vacacional + exe_nto) / diaseg * facto
            */

            decimal sumaColumnas3a9 = salarioPeriodo + d.HorasNormales + d.HorasDobles + d.HorasTriples + d.OF + d.PrimaVacacional + otrasPeriodo;
            decimal primaVacacional = d.PrimaVacacional;
            decimal exentoIntegrable = 0m;

            if (d.PercepcionExenta > 0m)
            {
                // VB6 usa empresa.sm. Para igualar tu VB6 probado,
                // ese valor corresponde a UMA: 117.31.
                exentoIntegrable = d.PercepcionExenta - (_uma * d.DiasTrabajados * 0.4m);

                if (exentoIntegrable < 0m)
                    exentoIntegrable = 0m;
            }

            // Compatibilidad con VB6 probado:
            // empresa.ao + 2 - añoAlta
            int anioBaseImss = ConstantesNomina.AntiguedadBaseImssVB6(emp.FechaAlta, _anioFiscal);

            decimal factor = ConstantesNomina.FactorSDIPorAnioBase(anioBaseImss);
            decimal baseDiaria = (sumaColumnas3a9 - primaVacacional + exentoIntegrable) / d.DiasTrabajados;
            decimal integrado = baseDiaria * factor;

            // Currency en VB6 trabaja con 4 decimales.
            integrado = Math.Round(integrado, 4, MidpointRounding.AwayFromZero);

            return integrado;
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // CLASES DE APOYO
    // ═══════════════════════════════════════════════════════════════════

    public class DatosCapturados
    {
        public bool EsSegundaQuincena { get; set; }
        public bool EsNominaEspecial { get; set; }

        /// <summary>
        /// Motivo de la nómina especial seleccionado en cbMotivo (p.ej. "PTU",
        /// "AGUINALDO", "LIQUIDACION"...). Vacío/null cuando EsNominaEspecial es false.
        /// Se usa únicamente para decidir si "Otras" conserva 2 decimales (PTU/Aguinaldo,
        /// ya repartidos gravado/exento) o se redondea a pesos enteros (cualquier otro
        /// caso) — ver ConstantesNomina.EsMotivoPtuOAguinaldo.
        /// </summary>
        public string MotivoEspecial { get; set; }

        public int DiasTrabajados { get; set; }
        public decimal SalarioDiario { get; set; }
        public decimal HorasNormales { get; set; }
        public decimal HorasDobles { get; set; }
        public decimal HorasTriples { get; set; }
        public decimal OF { get; set; }
        public decimal PrimaVacacional { get; set; }
        public decimal OtrasPercepciones { get; set; }
        public decimal PercepcionExenta { get; set; }
        public decimal Prestamos { get; set; }
        public decimal FONACOT { get; set; }
        public decimal PensionAlimenticia { get; set; }
        public decimal INFONAVIT { get; set; }
    }

    public class ResultadoNominaCompleto
    {
        public decimal SalarioPeriodo { get; set; }
        public decimal TotalIngresos { get; set; }

        public decimal ISR { get; set; }

        // Equivalente al valor mostrado en SubEmp.
        // En nómina normal normalmente queda NEGATIVO.
        public decimal Subsidio { get; set; }

        // Equivalente VB6:
        // nom_com.subdio
        // reng() -> subc13
        // Plantilla -> SE_SCAUSADO
        public decimal SubsidioCausado { get; set; }

        public decimal IMSS { get; set; }
        public decimal TotalDeducciones { get; set; }
        public decimal Neto { get; set; }
        public decimal SalarioIntegrado { get; set; }
    }
}