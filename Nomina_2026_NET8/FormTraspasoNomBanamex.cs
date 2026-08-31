using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ClosedXML.Excel;

namespace Nomina_2026_NET8
{
    public partial class FormTraspasoNomBanamex : Form
    {
        // ── Datos recibidos desde FormCaptura ─────────────────────────────
        private List<DatoDispersionBancaria> _empleados;
        private string _nombreEmpresa;
        private int _mesElegido;
        private int _anio;
        private bool _esPrimeraQuincena;
        private string _etiquetaQuincena;
        private RuepRoot _datosEmpresa;

        // ── Estado interno ────────────────────────────────────────────────
        private string _rutaConfigBanco;
        private List<DatoDispersionBancaria> _empleadosConCuenta;
        private bool _previsualizacionLista = false;

        public FormTraspasoNomBanamex()
        {
            InitializeComponent();
            ConfigurarMenuExportacionExcel();
        }

        private void FormTraspasoNomBanamex_Load(object sender, EventArgs e)
        {
            CargarConfigBancoDesdeArchivo();

            if (_empleados?.Count > 0)
                CargarPrimerTabla();
            else
                btnGenerar.Enabled = false;
        }

        // ── Eventos ───────────────────────────────────────────────────────
        private void btnCerrar_Click(object sender, EventArgs e) => Close();
        private void tsGenerarTXT_Click(object sender, EventArgs e) => GenerarArchivo();
        private void tsCopiarTabla1_Click(object sender, EventArgs e) => CopiarGrid(dgvDatosEmpleadosEntrada);
        private void tsCopiarTabla2_Click(object sender, EventArgs e) => CopiarGrid(dgvEspejo);
        private void btnGenerar_Click(object sender, EventArgs e) => GenerarArchivo();

        // ── API pública ───────────────────────────────────────────────────
        public void CargarDatos(List<DatoDispersionBancaria> empleados, string nombreEmpresa, int mesElegido, int anio, bool esPrimeraQuincena, 
            string etiquetaQuincena, RuepRoot datosEmpresa = null, string rutaTrabajo = null)
        { 
            _empleados = empleados ?? new List<DatoDispersionBancaria>();
            _nombreEmpresa = (nombreEmpresa ?? "").Trim();
            _mesElegido = mesElegido;
            _anio = anio;
            _esPrimeraQuincena = esPrimeraQuincena;
            _datosEmpresa = datosEmpresa;

            // Simplificado — el llamador siempre pasa la etiqueta
            _etiquetaQuincena = etiquetaQuincena ?? $"{AbrevMes(mesElegido)}-{(esPrimeraQuincena ? 1 : 2)}Q";
            _rutaConfigBanco = string.IsNullOrEmpty(rutaTrabajo) ? Path.Combine(Application.StartupPath, "config_banco.json") : Path.Combine(rutaTrabajo, "config_banco.json");

            // Fecha de pago por defecto
            int diaPago = esPrimeraQuincena ? 15 : DateTime.DaysInMonth(anio, mesElegido);
            string mesStr = mesElegido.ToString("D2");
            string anioStr = (anio % 100).ToString("D2");
            mskdFecha.Text = $"{diaPago}{mesStr}{anioStr}";
            saveFileDialog1.FileName = $"quin_{AbrevMes(mesElegido)}{(esPrimeraQuincena ? 1 : 2)}{anio}.txt";
        }

        // ── Primera tabla ─────────────────────────────────────────────────
        private void CargarPrimerTabla()
        {
            dgvDatosEmpleadosEntrada.Rows.Clear();
            _previsualizacionLista = false;
            btnGenerar.Enabled = false;

            _empleadosConCuenta = _empleados.Where(e => !string.IsNullOrWhiteSpace(e.CuentaTarjeta) && e.Importe > 0m).ToList();
            int sinCuenta = _empleados.Count - _empleadosConCuenta.Count;

            if (_empleadosConCuenta.Count == 0)
            {
                MessageBox.Show($"Ningún empleado tiene cuenta bancaria o importe > $0.\n" + $"Excluidos: {sinCuenta}", "Sin registros", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            dgvDatosEmpleadosEntrada.SuspendLayout();
            int consecutivo = 0;

            foreach (var emp in _empleadosConCuenta)
            {
                consecutivo++;
                int r = dgvDatosEmpleadosEntrada.Rows.Add();
                var row = dgvDatosEmpleadosEntrada.Rows[r];

                row.Cells["NumCuenta"].Value = emp.CuentaTarjeta;
                row.Cells["NombreEmpleado"].Value = emp.Nombre;
                row.Cells["ApellidoPaterno"].Value = emp.ApellidoP;
                row.Cells["ApellidoMaterno"].Value = emp.ApellidoM;
                row.Cells["Importe"].Value =
                    emp.Importe.ToString("N2", new CultureInfo("es-MX"));
                row.Cells["Trabajado"].Value =
                    emp.NumeroRegistro.ToString("D5");
                row.Cells["RefAlfanumerica"].Value = " ";
                row.Cells["ConceptoPago"].Value = _etiquetaQuincena;
                row.Cells["MismoDia"].Value = "Mismo dia";
                row.Cells["Cons"].Value = consecutivo.ToString("D5");
            }

            dgvDatosEmpleadosEntrada.ResumeLayout();

            decimal total = _empleadosConCuenta.Sum(e => e.Importe);
            _previsualizacionLista = true;
            btnGenerar.Enabled = true;

            lblResumen.Text = $"Empleados: {consecutivo}   |   " + $"Total: {total.ToString("C2", new CultureInfo("es-MX"))}   |   " + $"Sin cuenta: {sinCuenta}";
            PreVisualizarSegundaTabla();
        }

        // ── Generación del archivo ────────────────────────────────────────
        // Lógica extraída de btnGenerar_Click para que tsGenerarTXT también la use
        private void GenerarArchivo()
        {
            if (!_previsualizacionLista)
            {
                MessageBox.Show("Primero presiona 'Previsualizar'.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!ValidarCamposBanco()) return;
            if (saveFileDialog1.ShowDialog() != DialogResult.OK) return;

            try
            {
                string claveCliente = mskdNumCliente.Text.Trim();
                string nomEmpresa = mskdNombreCliente.Text.Trim();
                int diaPago = _esPrimeraQuincena ? 15 : DateTime.DaysInMonth(_anio, _mesElegido);
                decimal totalImporte = _empleadosConCuenta.Sum(e => e.Importe);
                int totalEmp = _empleadosConCuenta.Count;

                string sucursal = _datosEmpresa?.datos_fiscales?.banamex?.sucursal?.Trim() ?? "";
                string cuentaEmpresa = _datosEmpresa?.datos_fiscales?.banamex?.cuenta?.Trim() ?? "";

                if (totalImporte <= 0m)
                {
                    MessageBox.Show("El importe total es $0.00.", "Importe cero", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using var writer = new StreamWriter(saveFileDialog1.FileName, false, Encoding.ASCII);

                writer.WriteLine(ConstruirLinea1(claveCliente, nomEmpresa, diaPago));
                writer.WriteLine(ConstruirLinea21001(sucursal, cuentaEmpresa, totalImporte));

                decimal sumaVerif = 0m;
                foreach (var emp in _empleadosConCuenta)
                {
                    sumaVerif += emp.Importe;
                    writer.WriteLine(ConstruirLinea30001(emp));
                }

                writer.WriteLine(ConstruirLinea4001(totalEmp, sumaVerif));

                GuardarConfigBanco();

                MessageBox.Show($"Archivo generado:\n\n{saveFileDialog1.FileName}\n\n" + $"Registros: {totalEmp}\n" + $"Total: {totalImporte.ToString("C2", new CultureInfo("es-MX"))}",
                    "Generación Exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar archivo:\n\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Segunda tabla (espejo) ────────────────────────────────────────
        private void PreVisualizarSegundaTabla()
        {
            dgvEspejo.Rows.Clear();
            dgvEspejo.SuspendLayout();

            if (_empleadosConCuenta?.Count == 0)
            {
                dgvEspejo.ResumeLayout();
                return;
            }

            string nomEmpresa = mskdNombreCliente.Text.Trim();
            string sucursal = _datosEmpresa?.datos_fiscales?.banamex?.sucursal?.Trim() ?? "----";
            string cuentaEmpresa = _datosEmpresa?.datos_fiscales?.banamex?.cuenta?.Trim() ?? "----";
            decimal totalImporte = _empleadosConCuenta.Sum(e => e.Importe);

            // Fila 1: encabezado del lote
            AgregarFilaEspejo(new FilaEspejo
            {
                M = "1",
                Nombre = nomEmpresa,
                Importe = totalImporte.ToString("N2")
            });

            // Fila 21001: cuenta empresa
            AgregarFilaEspejo(new FilaEspejo
            {
                M = "21001",
                BAN = "002",
                IN = "01",
                SUC = sucursal.PadLeft(4, '0'),
                CUENTA = cuentaEmpresa.PadLeft(20, '0'),
                Nombre = nomEmpresa,
                M2 = "MXP",
                Importe = totalImporte.ToString("N2")
            });

            // Filas 30001: una por empleado
            foreach (var emp in _empleadosConCuenta)
            {
                string tipo = DetectarTipoCuenta(emp.CuentaTarjeta);
                string sucEmp = tipo == "01" ? emp.CuentaTarjeta.Split('-')[0].Trim() : "-";
                string ctaEmp = tipo == "01" ? emp.CuentaTarjeta.Split('-')[1].Trim() : emp.CuentaTarjeta.Trim();

                AgregarFilaEspejo(new FilaEspejo
                {
                    M = "30001",
                    BAN = "002",
                    TC = tipo,
                    PRO = "NOMINA",
                    IN = "01",
                    SUC = sucEmp,
                    CUENTA = ctaEmp,
                    PER = _etiquetaQuincena,
                    Nombre = emp.Nombre,
                    Alias = $"{emp.ApellidoP} {emp.ApellidoM} {emp.Nombre}".Trim(),
                    M2 = "MXP",
                    Importe = emp.Importe.ToString("N2"),
                    P = "M"
                });
            }

            // Fila 4001: totales
            AgregarFilaEspejo(new FilaEspejo
            {
                M = "4001",
                Importe = totalImporte.ToString("N2"),
                Vacio4 = _empleadosConCuenta.Count.ToString()
            });

            dgvEspejo.ResumeLayout();
        }

        // DTO para AgregarFilaEspejo — reemplaza los 18 parámetros posicionales
        private class FilaEspejo
        {
            public string M { get; init; } = "-";
            public string BAN { get; init; } = "-";
            public string TC { get; init; } = "-";
            public string PRO { get; init; } = "-";
            public string IN { get; init; } = "-";
            public string SUC { get; init; } = "-";
            public string CUENTA { get; init; } = "-";
            public string PER { get; init; } = "-";
            public string Nombre { get; init; } = "-";
            public string Alias { get; init; } = "-";
            public string M2 { get; init; } = "-";
            public string Importe { get; init; } = "-";
            public string P { get; init; } = "-";
            public string RFC { get; init; } = "-";
            public string Vacio1 { get; init; } = "";
            public string Vacio2 { get; init; } = "";
            public string Vacio3 { get; init; } = "";
            public string Vacio4 { get; init; } = "";
        }

        private void AgregarFilaEspejo(FilaEspejo f)
        {
            int r = dgvEspejo.Rows.Add();
            var row = dgvEspejo.Rows[r];

            row.Cells["M"].Value = f.M;
            row.Cells["BAN"].Value = f.BAN;
            row.Cells["TC"].Value = f.TC;
            row.Cells["PRO"].Value = f.PRO;
            row.Cells["IN"].Value = f.IN;
            row.Cells["SUC"].Value = f.SUC;
            row.Cells["CUENTA"].Value = f.CUENTA;
            row.Cells["PER"].Value = f.PER;
            row.Cells["Nombre"].Value = f.Nombre;
            row.Cells["Alias"].Value = f.Alias;
            row.Cells["M2"].Value = f.M2;
            row.Cells["Importe2"].Value = f.Importe;
            row.Cells["P"].Value = f.P;
            row.Cells["RFC"].Value = f.RFC;
            row.Cells["Vacio1"].Value = f.Vacio1;
            row.Cells["Vacio2"].Value = f.Vacio2;
            row.Cells["Vacio3"].Value = f.Vacio3;
            row.Cells["Vacio4"].Value = f.Vacio4;
        }

        // ── Constructores de registros ────────────────────────────────────
        private string ConstruirLinea1(
            string claveCliente, string nomEmpresa, int diaPago)
        {
            string cte = claveCliente.PadLeft(12, '0')[^12..]; // 🔧 rango .NET 8
            string mes = _mesElegido.ToString("D2");
            string fecha = $"{diaPago}{mes}{(_anio % 100):D2}";

            int numQna = _esPrimeraQuincena ? (_mesElegido * 2 - 1) : (_mesElegido * 2);
            string qna = numQna.ToString("D4");

            string emp36 = (nomEmpresa.Length > 35 ? nomEmpresa[..35] : nomEmpresa).PadRight(36);

            return $"1{cte}{fecha}{qna}{emp36}QUIN{new string(' ', 16)}05" + $"{new string(' ', 40)}B00";
        }

        private string ConstruirLinea21001(
            string sucursal, string cuenta, decimal importe)
        {
            SepararImporte(importe, out string imp16, out string c2);
            string suc = sucursal.PadLeft(4, '0')[^4..];
            string cta = cuenta.PadLeft(20, '0')[^20..];
            return $"21001{imp16}{c2}01{suc}{cta}{new string(' ', 20)}";
        }

        private string ConstruirLinea30001(DatoDispersionBancaria emp)
        {
            SepararImporte(emp.Importe, out string imp16, out string c2);

            string tipo = DetectarTipoCuenta(emp.CuentaTarjeta);
            string claveban;

            if (tipo == "01")
            {
                int pos = emp.CuentaTarjeta.IndexOf('-');

                if (pos < 0 || pos == emp.CuentaTarjeta.Length - 1)
                    throw new InvalidOperationException($"La cuenta '{emp.CuentaTarjeta}' no tiene el formato esperado SUCURSAL-CUENTA.");

                string ctaPart = emp.CuentaTarjeta[(pos + 1)..].Trim();
                claveban = SoloDigitos(ctaPart).PadLeft(7, '0')[^7..];
            }
            else
            {
                claveban = SoloDigitos(emp.CuentaTarjeta).PadLeft(20, '0')[^20..];
            }

            string referencia = $"{new string(' ', 8)}01";

            string nombreCompleto = $"{emp.Nombre} {emp.ApellidoP} {emp.ApellidoM}".Trim();
            string nomban = NormalizarTextoBanco(nombreCompleto, 55);

            if (tipo == "01")
            {
                return $"30001{imp16}{c2}01{claveban}{new string(' ', 30)}" + $"{referencia}{nomban}NOMINA{new string(' ', 34)}" + $"NOMINA{new string(' ', 18)}000000000100";
            }

            return $"30001{imp16}{c2}03{claveban}{new string(' ', 30)}" + $"{referencia}{nomban}NOMINA{new string(' ', 58)}" + $"000000000100";
        }

        private string ConstruirLinea4001(int numEmp, decimal importe)
        {
            SepararImporte(importe, out string imp16, out string c2);
            string psabon = numEmp.ToString("D6");
            return $"4001{psabon}{imp16}{c2}000001{imp16}{c2}";
        }

        // ── Configuración bancaria ────────────────────────────────────────
        private void CargarConfigBancoDesdeArchivo()
        {
            if (!string.IsNullOrEmpty(_rutaConfigBanco) &&
                File.Exists(_rutaConfigBanco))
            {
                try
                {
                    var cfg = JsonConvert.DeserializeObject<ConfigBancoGuardado>(
                        File.ReadAllText(_rutaConfigBanco, Encoding.UTF8));

                    if (cfg != null)
                    {
                        mskdNumCliente.Text = cfg.NumCliente ?? "";
                        mskdNombreCliente.Text = cfg.NomEmpresa ?? "";
                        mskdNumUsuario.Text = cfg.NumUsuario ?? "";
                        txtSecuencial.Text = cfg.Secuencial ?? "0001";
                        return;
                    }
                }
                catch { /* caer al pre-llenado desde RUEP */ }
            }

            // Pre-llenar desde RuepRoot si no hay config guardada
            if (_datosEmpresa != null)
            {
                var bnx = _datosEmpresa.datos_fiscales?.banamex;
                mskdNumCliente.Text = bnx?.cliente ?? "";
                string nom = _datosEmpresa.nombre ?? "";
                mskdNombreCliente.Text = nom.Length > 36 ? nom[..36] : nom;
                mskdNumUsuario.Text = bnx?.cuenta ?? "";
            }

            txtSecuencial.Text = "0001";
        }

        private void GuardarConfigBanco()
        {
            if (string.IsNullOrEmpty(_rutaConfigBanco)) return;
            try
            {
                File.WriteAllText(_rutaConfigBanco, JsonConvert.SerializeObject(new ConfigBancoGuardado
                    {
                        NumCliente = mskdNumCliente.Text.Trim(),
                        NomEmpresa = mskdNombreCliente.Text.Trim(),
                        NumUsuario = mskdNumUsuario.Text.Trim(),
                        Secuencial = txtSecuencial.Text.Trim()
                    }, Formatting.Indented), Encoding.UTF8);
            }
            catch { /* silencioso — no es crítico */ }
        }

        // ── Helpers ───────────────────────────────────────────────────────
        private static string DetectarTipoCuenta(string cuenta) => (cuenta ?? "").Contains('-') ? "01" : "03";      
        // Aritmética entera directa — más segura que manipular strings de decimales.
        private static void SepararImporte(decimal importe, out string imp16, out string c2)
        {
            // Redondear a 2 decimales y convertir a centavos enteros
            long centavos = (long)Math.Round(importe * 100m, 0, MidpointRounding.AwayFromZero);
            long pesos = centavos / 100;
            long resto = centavos % 100;

            imp16 = pesos.ToString().PadLeft(16, '0');
            c2 = resto.ToString("D2");
        }

        private static string AbrevMes(int mes)
        {
            string abrev = CultureInfo.GetCultureInfo("es-ES").DateTimeFormat.GetAbbreviatedMonthName(mes).ToUpper();
            return abrev.Length > 3 ? abrev[..3] : abrev;
        }

        // Unificado — antes había dos métodos CopiarTabla1 y CopiarTabla2
        private static void CopiarGrid(DataGridView dgv)
        {
            if (dgv == null || dgv.Rows.Count == 0)
            {
                MessageBox.Show("No hay datos para copiar.", "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string texto = ConstruirTextoTabuladoParaExcel(dgv, incluirEncabezados: true);

            if (string.IsNullOrWhiteSpace(texto))
            {
                MessageBox.Show("No se pudo construir el texto para Excel.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Clipboard.SetText(texto, TextDataFormat.Text);

            MessageBox.Show(
                "Información copiada correctamente.\n\nPega en Excel con Ctrl + V.",
                "Copiado para Excel",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private static string ConstruirTextoTabuladoParaExcel(DataGridView dgv, bool incluirEncabezados)
        {
            var sb = new StringBuilder();

            var columnas = dgv.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).OrderBy(c => c.DisplayIndex).ToList();

            if (incluirEncabezados)
            {
                sb.AppendLine(string.Join("\t", columnas.Select(c => LimpiarCampoExcel(c.HeaderText))));
            }

            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.IsNewRow) continue;

                var valores = columnas.Select(col =>
                {
                    string valor = row.Cells[col.Index].Value?.ToString() ?? "";
                    return FormatearCampoExcel(valor, col.Name);
                });

                sb.AppendLine(string.Join("\t", valores));
            }

            return sb.ToString();
        }

        private static string FormatearCampoExcel(string valor, string nombreColumna)
        {
            valor = LimpiarCampoExcel(valor);

            if (EsColumnaTextoCritica(nombreColumna))
                return ForzarTextoExcel(valor);

            return valor;
        }

        private static bool EsColumnaTextoCritica(string nombreColumna)
        {
            if (string.IsNullOrWhiteSpace(nombreColumna)) return false;

            string col = nombreColumna.Trim().ToUpperInvariant();

            return col is
                "NUMCUENTA" or
                "CUENTA" or
                "CLABE" or
                "RFC" or
                "SUC" or
                "BAN" or
                "TC" or
                "IN" or
                "M" or
                "M2" or
                "TRABAJADO" or
                "CONS";
        }

        private static string ForzarTextoExcel(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return "";

            valor = valor.Replace("\"", "\"\"");

            // Excel lo pega como texto y conserva ceros a la izquierda.
            return $"=\"{valor}\"";
        }

        private bool ValidarCamposBanco()
        {
            if (string.IsNullOrWhiteSpace(mskdNumCliente.Text))
            {
                MessageBox.Show("Ingresa el # de Cliente (12 dígitos).", "Campo requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                mskdNumCliente.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(mskdNombreCliente.Text))
            {
                MessageBox.Show("Ingresa el Nombre de Cliente.", "Campo requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                mskdNombreCliente.Focus();
                return false;
            }

            return true;
        }

        // ── Clase de configuración bancaria ──────────────────────────────
        private class ConfigBancoGuardado
        {
            public string NumCliente { get; set; }
            public string NomEmpresa { get; set; }
            public string NumUsuario { get; set; }
            public string Secuencial { get; set; }
        }

        private static string SoloDigitos(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return "";

            return new string(valor.Where(char.IsDigit).ToArray());
        }

        private static string NormalizarTextoBanco(string valor, int longitud)
        {
            valor = valor ?? "";

            valor = valor.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ").Trim().ToUpperInvariant();

            if (valor.Length > longitud)
                valor = valor[..longitud];

            return valor.PadRight(longitud);
        }

        private void GenerarArchivoTabuladoExcel()
        {
            if (!_previsualizacionLista)
            {
                MessageBox.Show("Primero genera la previsualización.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Title = "Guardar archivo tabulado para Excel",
                Filter = "Archivo tabulado (*.tsv)|*.tsv|Texto (*.txt)|*.txt",
                FileName = $"Banamex_Excel_{_etiquetaQuincena}_{_anio}.tsv",
                DefaultExt = "tsv"
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                string contenido = ConstruirTextoTabuladoParaExcel(dgvEspejo, incluirEncabezados: true);

                File.WriteAllText(sfd.FileName, contenido, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

                MessageBox.Show($"Archivo tabulado generado correctamente:\n\n{sfd.FileName}", "Exportación Excel", MessageBoxButtons.OK, MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar archivo tabulado:\n\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error
                );
            }
        }

        private void ConfigurarMenuExportacionExcel()
        {
            if (tsOpciones.DropDownItems["tsExportarExcelBanamex"] != null)
                return;

            tsOpciones.DropDownItems.Add(new ToolStripSeparator());

            var item = new ToolStripMenuItem
            {
                Name = "tsExportarExcelBanamex",
                Text = "Exportar a Excel (.xlsx)"
            };

            item.Click += (s, e) => ExportarBanamexAExcel();

            tsOpciones.DropDownItems.Add(item);
        }

        private void ExportarBanamexAExcel()
        {
            if (!_previsualizacionLista)
            {
                MessageBox.Show("Primero genera la previsualización.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information
                );
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Title = "Exportar Banamex a Excel",
                Filter = "Excel (*.xlsx)|*.xlsx",
                FileName = $"Banamex_{_etiquetaQuincena}_{_anio}.xlsx",
                DefaultExt = "xlsx",
                AddExtension = true,
                OverwritePrompt = true
            };

            if (sfd.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                using var workbook = new XLWorkbook();

                var hojaEntrada = workbook.Worksheets.Add("Datos empleados");
                ExportarGridAHojaExcel(dgvDatosEmpleadosEntrada, hojaEntrada);

                var hojaLayout = workbook.Worksheets.Add("Layout Banamex");
                ExportarGridAHojaExcel(dgvEspejo, hojaLayout);

                workbook.SaveAs(sfd.FileName);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(sfd.FileName)
                {
                    UseShellExecute = true
                });

                MessageBox.Show($"Archivo Excel generado correctamente:\n\n{sfd.FileName}", "Exportación exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al exportar a Excel:\n\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportarGridAHojaExcel(DataGridView dgv, IXLWorksheet sheet)
        {
            var columnas = dgv.Columns
                .Cast<DataGridViewColumn>()
                .Where(c => c.Visible)
                .OrderBy(c => c.DisplayIndex)
                .ToList();

            int colExcel = 1;

            foreach (var col in columnas)
            {
                var cell = sheet.Cell(1, colExcel);
                cell.Value = col.HeaderText;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                colExcel++;
            }

            int rowExcel = 2;

            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.IsNewRow) continue;

                colExcel = 1;

                foreach (var col in columnas)
                {
                    string valor = row.Cells[col.Index].Value?.ToString() ?? "";
                    valor = LimpiarCampoExcel(valor);

                    var cell = sheet.Cell(rowExcel, colExcel);

                    if (EsColumnaTextoCriticaExcel(col.Name))
                    {
                        cell.Style.NumberFormat.Format = "@";
                        cell.Value = valor;
                    }
                    else if (EsColumnaImporteExcel(col.Name) && TryParseDecimalMexicano(valor, out decimal importe))
                    {
                        cell.Value = importe;
                        cell.Style.NumberFormat.Format = "#,##0.00";
                    }
                    else
                    {
                        cell.Value = valor;
                    }

                    colExcel++;
                }

                rowExcel++;
            }

            sheet.Columns().AdjustToContents();

            foreach (var col in columnas.Select((value, index) => new { value, index }))
            {
                if (EsColumnaTextoCriticaExcel(col.value.Name))
                {
                    int excelColumn = col.index + 1;
                    sheet.Column(excelColumn).Style.NumberFormat.Format = "@";
                }
            }
        }

        private static bool EsColumnaTextoCriticaExcel(string nombreColumna)
        {
            if (string.IsNullOrWhiteSpace(nombreColumna)) return false;

            string col = nombreColumna.Trim().ToUpperInvariant();

            return col is
                "NUMCUENTA" or
                "CUENTA" or
                "CLABE" or
                "RFC" or
                "SUC" or
                "BAN" or
                "TC" or
                "IN" or
                "M" or
                "M2" or
                "TRABAJADO" or
                "CONS";
        }

        private static bool EsColumnaImporteExcel(string nombreColumna)
        {
            if (string.IsNullOrWhiteSpace(nombreColumna)) return false;

            string col = nombreColumna.Trim().ToUpperInvariant();

            return col is "IMPORTE" or "IMPORTE2";
        }

        private static bool TryParseDecimalMexicano(string valor, out decimal resultado)
        {
            valor = (valor ?? "").Replace("$", "").Trim();

            return decimal.TryParse(valor, NumberStyles.Number, new CultureInfo("es-MX"), out resultado);
        }

        private static string LimpiarCampoExcel(string valor)
        {
            if (string.IsNullOrEmpty(valor)) return "";

            return valor.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ").Trim();
        }
    }
}
