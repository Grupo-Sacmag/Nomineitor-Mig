using Microsoft.VisualBasic;
using System;
using System.Windows.Forms;

namespace Nomina_2026_NET8
{
    public class DatosInicioCfdiNomina
    {
        public long FolioAnterior { get; set; }
        public string Serie { get; set; }
        public int Consecutivo { get; set; }

        public string MetodoPagoClave { get; set; }
        public string MetodoPagoDescripcion { get; set; }

        public string RegistroPatronal { get; set; }
        public string RiesgoImss { get; set; }
    }

    public static class CapturaCfdiNominaVB6
    {
        public static DatosInicioCfdiNomina Capturar(ConfiguracionCfdiNomina config)
        {
            config ??= new ConfiguracionCfdiNomina();

            // ---------------------------------------------------------
            // FOLIO ANTERIOR
            // ---------------------------------------------------------
            string entradaFolio = Interaction.InputBox("Ingresa el numero de Folio Anterior", "CFDI NOMINA", config.folio.ToString());

            long folio = 0;

            if (!long.TryParse(entradaFolio, out folio))
                folio = config.folio;

            // ---------------------------------------------------------
            // SERIE
            // ---------------------------------------------------------
            string serie = Interaction.InputBox("Ingrese la serie", "CFDI NOMINA", config.serie ?? "");

            // ---------------------------------------------------------
            // CONSECUTIVO
            // ---------------------------------------------------------
            string entradaConsecutivo = Interaction.InputBox("Numero de nomina consecutivo ", "CFDI NOMINA", (config.consecutivo + 1).ToString());

            int consecutivo;

            if (!int.TryParse(entradaConsecutivo, out consecutivo))
                consecutivo = config.consecutivo + 1;

            // ---------------------------------------------------------
            // MÉTODO DE PAGO
            // ---------------------------------------------------------
            string claveMetodo;
            string descripcionMetodo;

            while (true)
            {
                claveMetodo = Interaction.InputBox("Metodo de Pago (utiliza la clave numerica)\r\n" +
                    "01\tEFECTIVO\r\n" +
                    "02\tCHEQUE NOMINATIVO\r\n" +
                    "03\tTRANSFERENCIA ELECTRONICA\r\n" +
                    "28\tTARJETA DE DEBITO",
                    "CFDI NOMINA",
                    "28");

                switch (claveMetodo)
                {
                    case "01":
                        // Conservamos incluso el typo del VB6.
                        descripcionMetodo = "01 Eectivo";
                        break;

                    case "02":
                        descripcionMetodo = "02 Cheque nominativo";
                        break;

                    case "03":
                        descripcionMetodo = "03 TRANSFERENCIA";
                        break;

                    case "28":
                        descripcionMetodo = "28 Tarjeta de Débito";
                        break;

                    default:
                        MessageBox.Show("LA CLAVE NO EXISTE", "CFDI NOMINA", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                        continue;
                }

                break;
            }

            // ---------------------------------------------------------
            // REGISTRO PATRONAL
            // ---------------------------------------------------------
            string registroPatronal = Interaction.InputBox("Registro Patronal Imss ", "CFDI NOMINA", config.registro_patronal ?? "");

            // ---------------------------------------------------------
            // RIESGO PATRONAL
            // ---------------------------------------------------------
            string riesgoFinal;

            while (true)
            {
                string entradaRiesgo = Interaction.InputBox("Riesgo Patronal(utiliza la clave numerica)\r\n" +
                    "1\tCLASE I\r\n" +
                    "2\tCLASE II\r\n" +
                    "3\tCLASE III\r\n" +
                    "4\tCLASE IV\r\n" +
                    "5\tCLASE V\r\n",
                    "CFDI NOMINA",
                    "1");

                switch (entradaRiesgo)
                {
                    case "1":
                        riesgoFinal = "1-Clase I";
                        break;

                    case "2":
                        riesgoFinal = "2-Clase II";
                        break;

                    case "3":
                        riesgoFinal = "3-Clase III";
                        break;

                    case "4":
                        riesgoFinal = "4-Clase IV";
                        break;

                    case "5":
                        riesgoFinal = "5-Clase V";
                        break;

                    default:
                        MessageBox.Show("LA CLAVE NO EXISTE", "CFDI NOMINA", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                        continue;
                }

                break;
            }

            return new DatosInicioCfdiNomina
            {
                FolioAnterior = folio,
                Serie = serie,
                Consecutivo = consecutivo,

                MetodoPagoClave = claveMetodo,
                MetodoPagoDescripcion = descripcionMetodo,

                RegistroPatronal = registroPatronal,
                RiesgoImss = riesgoFinal
            };
        }
    }
}