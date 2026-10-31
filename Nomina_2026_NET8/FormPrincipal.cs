using DocumentFormat.OpenXml.CustomProperties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nomina_2026_NET8;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace NOMINA_2025
{
    public partial class FormPrincipal : Form
    {
        // ── Estado ────────────────────────────────────────────────────────
        public int anioEmpresa;
        public decimal salarioMinimo;
        private TreeNode _nodoSeleccionadoAnterior;
        public static string EmpresaNombre { get; set; }
        private string _filtroActual;

        private FileSystemWatcher _watcherDirectorio;
        private System.Windows.Forms.Timer _timerRefreshArchivos;

        [Flags]
        public enum CapacidadesDirectorio { Ninguna = 0, Empresa = 1, Personal = 2, Nomina = 4, AcumuladosJSON = 8, AcumuladosNOM = 16 }

        private static readonly Dictionary<string, int> MesesOrden = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ENE"] = 1,
            ["FEB"] = 2,
            ["MAR"] = 3,
            ["ABR"] = 4,
            ["MAY"] = 5,
            ["JUN"] = 6,
            ["JUL"] = 7,
            ["AGO"] = 8,
            ["SEP"] = 9,
            ["OCT"] = 10,
            ["NOV"] = 11,
            ["DIC"] = 12
        };

        public FormPrincipal()
        {
            InitializeComponent();

            // Inicializar timer antes de que IniciarWatcherDirectorio lo use
            _timerRefreshArchivos = new System.Windows.Forms.Timer
            { Interval = 600 };
            _timerRefreshArchivos.Tick += (s, e) => { _timerRefreshArchivos.Stop(); RefrescarArchivosSilencioso(); };

            // Filtro por defecto
            _filtroActual = Nomina_2026_NET8.Properties.Settings.Default.MostrarFiltroArchivos ?? "*.NOM";

            RefrescarUnidades();
            CargarTreeViewDesdeRaiz();
        }

        // ── Load / Shown ──────────────────────────────────────────────────
        private void FormPrincipal_Load(object sender, EventArgs e)
        {
            // Sincronizar checks del menú con el filtro guardado
            tsMostrarJSON.Checked = _filtroActual == "*.JSON";
            tsMostrarNOM.Checked = _filtroActual != "*.JSON";

            // Verificar que existan las tablas fiscales
            VerificarTablasTarifa();
        }

        private void FormPrincipal_Shown(object sender, EventArgs e)
        {
            string rutaGuardada = Nomina_2026_NET8.Properties.Settings.Default.RutaSeleccionada;
            if (string.IsNullOrWhiteSpace(rutaGuardada)) return;

            string rutaValida = ObtenerRutaValidaMasCercana(rutaGuardada);

            if (rutaValida != null)
            {
                SeleccionarYExpandirRuta(rutaValida);

                if (!string.Equals(rutaValida, rutaGuardada, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show($"El directorio guardado ya no está disponible:\n{rutaGuardada}\n\n" + $"Se seleccionó el directorio accesible más cercano:\n{rutaValida}",
                        "Directorio no encontrado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                // Ni siquiera la raíz del drive existe (ej. unidad de red desconectada) → último recurso
                string escritorio = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if (Directory.Exists(escritorio))
                    SeleccionarYExpandirRuta(escritorio);
            }
        }

        private void FormPrincipal_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Guardar directorio actual
            if (treeViewDirectorios.SelectedNode != null)
            {
                Nomina_2026_NET8.Properties.Settings.Default.RutaSeleccionada = treeViewDirectorios.SelectedNode.Tag?.ToString();
                Nomina_2026_NET8.Properties.Settings.Default.Save();
            }

            Application.Exit();
        }

        // ── Eventos de ComboBox de unidades ───────────────────────────────
        private void cbUnidades_SelectionChangeCommitted(object sender, EventArgs e)
        {
            string drive = cbUnidades.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(drive))
                NavegaAUnidad(drive);
        }

        // ── Eventos de TreeView ───────────────────────────────────────────
        private void treeViewDirectorios_AfterSelect(object sender, TreeViewEventArgs e)
        {
            // Restaurar ícono del nodo anterior
            if (_nodoSeleccionadoAnterior != null && _nodoSeleccionadoAnterior != e.Node)
            {
                bool sigueExpandido = _nodoSeleccionadoAnterior.IsExpanded;
                string icono = sigueExpandido ? "FOLDERABIERTO" : "FOLDER";
                _nodoSeleccionadoAnterior.ImageKey = icono;
                _nodoSeleccionadoAnterior.SelectedImageKey = icono;
            }

            // Marcar el nodo actual como "abierto" visualmente
            e.Node.ImageKey = "FOLDERABIERTO";
            e.Node.SelectedImageKey = "FOLDERABIERTO";
            _nodoSeleccionadoAnterior = e.Node;

            txtBuscarRutaDirectorio.Clear();
            AplicarRutaSeleccionada(e.Node.Tag?.ToString());
        }

        private void treeViewDirectorios_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            var node = e.Node;
            node.ImageKey = "FOLDERABIERTO";
            node.SelectedImageKey = "FOLDERABIERTO";

            // Detecta dummy con Tag == null (consistente con LoadSubDirectories)
            if (node.Nodes.Count == 1 && node.Nodes[0].Tag == null)
            {
                node.Nodes.Clear();
                LoadSubDirectories(node);
            }
        }
        //Comentario
        private void treeViewDirectorios_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            // Si es el nodo seleccionado actualmente, que se mantenga "abierto"
            if (e.Node == treeViewDirectorios.SelectedNode) return;

            e.Node.ImageKey = "FOLDER";
            e.Node.SelectedImageKey = "FOLDER";
        }

        private void treeViewDirectorios_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            if (treeViewDirectorios == null || e.Node == null) return;

            if (e.Node == treeViewDirectorios.SelectedNode)
            {
                using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(81, 113, 232));
                e.Graphics.FillRectangle(brush, e.Bounds);
                TextRenderer.DrawText(e.Graphics, e.Node.Text, treeViewDirectorios.Font, e.Bounds, System.Drawing.Color.White, TextFormatFlags.GlyphOverhangPadding);
            }
            else
            {
                e.DrawDefault = true;
            }
        }

        private void treeViewDirectorios_KeyDown(object sender, KeyEventArgs e)
        {
            // Espacio se comporta igual que Enter: "entra" al nodo seleccionado.
            if (e.KeyCode != Keys.Enter && e.KeyCode != Keys.Space) return;

            var nodo = treeViewDirectorios.SelectedNode;
            if (nodo == null) return;

            if (!nodo.IsExpanded) nodo.Expand();

            if (nodo.Nodes.Count == 1 && nodo.Nodes[0].Tag == null)
            {
                nodo.Nodes.Clear();
                LoadSubDirectories(nodo);
            }

            AplicarRutaSeleccionada(nodo.Tag?.ToString());

            // Evita que WinForms le dé su comportamiento por defecto a la tecla
            // (con Enter puede intentar "aceptar" el diálogo; con Espacio, en teoría
            // no hace nada especial en un TreeView sin checkboxes, pero no lo dejamos suelto).
            e.Handled = true;
        }

        // ── Eventos de ListBox ────────────────────────────────────────────
        private void listBoxArchivos_DoubleClick(object sender, EventArgs e)
        {
            if (EsFilaSeparador(listBoxArchivos.SelectedItem)) return;
            AbrirArchivoSeleccionado();
        }

        private void listBoxArchivos_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (!EsFilaSeparador(listBoxArchivos.SelectedItem))
                    AbrirArchivoSeleccionado();
                e.Handled = true;
            }
        }

        // ── Eventos de búsqueda manual ────────────────────────────────────
        private void btnBuscarDirectorio_Click(object sender, EventArgs e) => BuscarRutaManual();

        private void txtBuscarRutaDirectorio_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                BuscarRutaManual();
                e.SuppressKeyPress = true;
            }
        }

        // ── Eventos de toolbar — Personal ─────────────────────────────────
        private void tsCRUDPersonal_Click(object sender, EventArgs e)
        {
            EjecutarConValidacionRuep((rutaTrabajo, rutaRUEP) => AbrirFormEdicionPersonal(rutaRUEP), "No se ha encontrado el archivo RUEP.dat.\n\n" + "¿Desea generarlo ahora?", false);
        }

        private void tsmImpresion_Click(object sender, EventArgs e)
        {
            //Aquí va la función de imprimir GridView de Form "FormEditarInfoPersonal" (Form 4 en versión VB).
        }

        // ── Eventos de toolbar — Nómina ───────────────────────────────────
        private void tsmIniciarCaptura_Click(object sender, EventArgs e)
        {
            EjecutarConValidacionRuep((rutaTrabajo, rutaRUEP) => AbrirFormCaptura(rutaTrabajo, rutaRUEP), "No se ha encontrado el archivo RUEP.dat.\n\n¿Desea generarlo para iniciar la captura de nómina?", true);
        }

        private void tsmImpresionNomina_Click(object sender, EventArgs e)
        {
            // Reservado para futura implementación de impresión de nómina
        }

        // ── Eventos de toolbar — Configuración ───────────────────────────
        private void tsDatosDeLaEmpresa_Click(object sender, EventArgs e)
        {
            EjecutarConValidacionRuep((rutaTrabajo, rutaRUEP) => AbrirFormEmpresaUnificado(rutaRUEP), "No se ha encontrado el archivo RUEP.dat.\n\n¿Desea generarlo para acceder a los datos de la empresa?", false);
        }

        private void tsMostrarTarifasImpuestos_Click(object sender, EventArgs e)
        {
            if (!File.Exists(TarifasRepository.Ruta))
                ForzarGeneracionTablas();

            if (!File.Exists(TarifasRepository.Ruta)) return;

            var formTablas = new FormImpuestosTablas();
            formTablas.ShowDialog();
        }

        private void tsAcercaDe_Click(object sender, EventArgs e)
        {
            FormVersion frmVersion = new FormVersion();
            frmVersion.ShowDialog();
        }

        // ── Eventos de toolbar — Mostrar ──────────────────────────────────
        private void tsMostrarJSON_Click(object sender, EventArgs e) => CambiarFiltroArchivos("*.JSON");
        private void tsMostrarNOM_Click(object sender, EventArgs e) => CambiarFiltroArchivos("*.NOM");

        // ── Eventos de toolbar — Acumulados ──────────────────────────────
        private void AcumJSON_Click(object sender, EventArgs e)
        {
            string ruta = lblRutaDirectorio.Text;
            if (!Directory.Exists(ruta))
            {
                MessageBox.Show("Debe seleccionar una carpeta válida.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var f = new FormAcumulados
            {
                RutaOrigen = ruta,
                ModoNOM = false
            };
            f.Show();
        }

        private void AcumNOM_Click(object sender, EventArgs e)
        {
            string ruta = lblRutaDirectorio.Text;
            if (!Directory.Exists(ruta))
            {
                MessageBox.Show("Debe seleccionar una carpeta válida.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var f = new FormAcumulados
            {
                RutaOrigen = ruta,
                ModoNOM = true
            };
            f.Show();
        }

        // ── Métodos privados — Navegación ─────────────────────────────────
        private void AplicarRutaSeleccionada(string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta) || !Directory.Exists(ruta))
                return;

            lblRutaDirectorio.Text = ruta;
            LimpiarLabels();
            LoadFiles(ruta);
            InterpretarYMostrarEmpre(ruta);
            AplicarPermisos(EvaluarDirectorio(ruta));
            IniciarWatcherDirectorio(ruta);

            // Guardar SIEMPRE que se aplique una ruta válida, no solo al capturar nómina
            Nomina_2026_NET8.Properties.Settings.Default.RutaSeleccionada = ruta;
            Nomina_2026_NET8.Properties.Settings.Default.Save();
        }

        private void BuscarRutaManual()
        {
            string ruta = txtBuscarRutaDirectorio.Text.Trim();
            if (string.IsNullOrWhiteSpace(ruta))
            {
                MessageBox.Show("Ingrese una ruta válida.");
                return;
            }

            ruta = Path.GetFullPath(ruta);
            if (!Directory.Exists(ruta))
            {
                MessageBox.Show("La ruta no existe.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                SeleccionarYExpandirRuta(ruta);
                AplicarRutaSeleccionada(ruta);
                treeViewDirectorios.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar la ruta: {ex.Message}");
            }
        }

        private void NavegaAUnidad(string drive)
        {
            // Si ya existe el nodo en el TreeView, seleccionarlo
            var nodoExistente = treeViewDirectorios.Nodes
                .Cast<TreeNode>()
                .FirstOrDefault(n => string.Equals(
                    n.Tag?.ToString(), drive,
                    StringComparison.OrdinalIgnoreCase));

            if (nodoExistente != null)
            {
                treeViewDirectorios.SelectedNode = nodoExistente;
                nodoExistente.EnsureVisible();

                if (nodoExistente.Nodes.Count == 1 &&
                    nodoExistente.Nodes[0].Tag == null)
                    LoadSubDirectories(nodoExistente);

                return;
            }

            // Si no existía, agregar y seleccionar
            var nodoNuevo = new TreeNode(drive)
            {
                Tag = drive,
                ImageKey = "FOLDER",
                SelectedImageKey = "FOLDER"
            };
            // 🔧 Dummy con Tag = null — consistente con treeViewDirectorios_BeforeExpand
            nodoNuevo.Nodes.Add(new TreeNode("Cargando...") { Tag = null });
            treeViewDirectorios.Nodes.Add(nodoNuevo);
            treeViewDirectorios.SelectedNode = nodoNuevo;
        }

        private void SeleccionarYExpandirRuta(string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta) || !Directory.Exists(ruta))
                return;

            string root = Path.GetPathRoot(ruta);
            TreeNode nodoActual = null;

            foreach (TreeNode n in treeViewDirectorios.Nodes)
            {
                if (string.Equals(n.Tag?.ToString(), root,
                    StringComparison.OrdinalIgnoreCase))
                {
                    nodoActual = n;
                    break;
                }
            }

            if (nodoActual == null) return;

            string[] partes = ruta.Substring(root.Length)
                .Split(new[] { Path.DirectorySeparatorChar },
                    StringSplitOptions.RemoveEmptyEntries);

            foreach (string parte in partes)
            {
                if (nodoActual.Nodes.Count == 1 &&
                    nodoActual.Nodes[0].Tag == null)
                {
                    nodoActual.Nodes.Clear();
                    LoadSubDirectories(nodoActual);
                }

                bool encontrado = false;
                foreach (TreeNode child in nodoActual.Nodes)
                {
                    if (string.Equals(child.Text, parte,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        nodoActual = child;
                        nodoActual.Expand();
                        encontrado = true;
                        break;
                    }
                }

                if (!encontrado) break;
            }

            treeViewDirectorios.SelectedNode = nodoActual;
            nodoActual?.EnsureVisible();

            // No dependemos únicamente de que asignar SelectedNode dispare AfterSelect
            // (comportamiento indirecto/implícito) — lo llamamos explícitamente.
            AplicarRutaSeleccionada(nodoActual?.Tag?.ToString());
        }

        // ── Métodos privados — Carga de UI ──────────────────────────────── 
        /// Carga todas las unidades de disco como nodos raíz del TreeView. Lazy loading: cada nodo tiene un dummy (Tag = null) hasta que se expande.
        private void CargarTreeViewDesdeRaiz()
        {
            treeViewDirectorios.Nodes.Clear();

            foreach (var drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed || d.DriveType == DriveType.Removable || d.DriveType == DriveType.Network))
            {
                var nodo = new TreeNode(drive.Name)
                {
                    Tag = drive.Name,
                    ImageKey = "FOLDER",
                    SelectedImageKey = "FOLDER"
                };
                // Dummy con Tag = null para lazy load consistente
                nodo.Nodes.Add(new TreeNode("Cargando...") { Tag = null });
                treeViewDirectorios.Nodes.Add(nodo);
            }
        }

        private void RefrescarUnidades()
        {
            string seleccion = cbUnidades.SelectedItem?.ToString();
            cbUnidades.Items.Clear();

            foreach (var drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed || d.DriveType == DriveType.Removable || d.DriveType == DriveType.Network))
                cbUnidades.Items.Add(drive.Name);

            if (seleccion != null && cbUnidades.Items.Contains(seleccion))
                cbUnidades.SelectedItem = seleccion;
            else if (cbUnidades.Items.Count > 0)
                cbUnidades.SelectedIndex = 0;
        }

        private void LoadSubDirectories(TreeNode node)
        {
            string path = node.Tag?.ToString();
            if (string.IsNullOrWhiteSpace(path)) return;

            node.Nodes.Clear();
            try
            {
                foreach (var dir in Directory.EnumerateDirectories(path))
                {
                    try
                    {
                        var attrs = File.GetAttributes(dir);
                        if (attrs.HasFlag(FileAttributes.Hidden) || attrs.HasFlag(FileAttributes.System))
                            continue;
                    }
                    catch { continue; } // sin permisos para leer atributos → la saltamos también

                    var child = new TreeNode(Path.GetFileName(dir))
                    {
                        Tag = dir,
                        ImageKey = "FOLDER",
                        SelectedImageKey = "FOLDER"
                    };
                    // Dummy con Tag = null — detectado por BeforeExpand
                    child.Nodes.Add(new TreeNode("Cargando...") { Tag = null });
                    node.Nodes.Add(child);
                }
            }
            catch { /* acceso denegado o ruta de red → ignorar */ }
        }

        private void LoadFiles(string path)
        {
            listBoxArchivos.BeginUpdate();
            try
            {
                listBoxArchivos.Items.Clear();
                if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                    return;

                IEnumerable<string> allFiles;
                try { allFiles = Directory.EnumerateFiles(path); }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("No tiene permisos para leer la carpeta seleccionada.", "Permisos insuficientes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                bool verJSON = string.Equals(_filtroActual, "*.JSON", StringComparison.OrdinalIgnoreCase);

                var nombres = allFiles.Where(f => !string.Equals(Path.GetFileName(f), "RUEP.dat", StringComparison.OrdinalIgnoreCase)).Where(f =>
                {
                    string name = Path.GetFileName(f);
                    return verJSON ? name.EndsWith(".json", StringComparison.OrdinalIgnoreCase) : name.EndsWith(".nom", StringComparison.OrdinalIgnoreCase);
                }).Select(Path.GetFileName).ToList();

                var clasificados = nombres.Select(ClasificarArchivo).ToList();
                var normales = clasificados.Where(a => a.EsNormal).OrderBy(a => a.Anio).ThenBy(a => a.Mes).ThenBy(a => a.Quincena).ToList();
                var especiales = clasificados.Where(a => !a.EsNormal).OrderBy(a => a.Numero).ThenBy(a => a.Concepto, StringComparer.CurrentCultureIgnoreCase).ToList();

                if (normales.Count > 0)
                    listBoxArchivos.Items.AddRange(normales.Select(a => (object)a.NombreArchivo).ToArray());

                if (normales.Count > 0 && especiales.Count > 0)
                    listBoxArchivos.Items.Add(SeparadorEspeciales);

                if (especiales.Count > 0)
                    listBoxArchivos.Items.AddRange(especiales.Select(a => (object)a.NombreArchivo).ToArray());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar archivos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                listBoxArchivos.EndUpdate();
            }
        }

        private void LimpiarLabels()
        {
            lblEmpresa.Text = string.Empty;
            lblAnio.Text = string.Empty;
            lblSalarioMinimo.Text = string.Empty;
            lblUMA.Text = string.Empty;

            EmpresaNombre = string.Empty;
            anioEmpresa = 0;
            salarioMinimo = 0m;
        }

        // Solo rama RUEP.dat — rama EMPRESA.dno eliminada (código muerto)
        private void InterpretarYMostrarEmpre(string directoryPath)
        {
            string rutaRUEP = Path.Combine(directoryPath, "RUEP.dat");

            // ====================================================
            // CASO 1: EXISTE RUEP.DAT
            // Lectura directa del formato RUEP V9
            // ====================================================

            if (File.Exists(rutaRUEP))
            {
                try
                {
                    // Descifrar y leer el JSON sin modificarlo.
                    JObject datos = CryptoService.LoadEncrypted<JObject>(rutaRUEP);

                    if (datos == null)
                        throw new InvalidDataException("No fue posible obtener los datos del RUEP.");

                    // PRUEBA TEMPORAL DE DESERIALIZACIÓN RUEP V9
                    var modeloV9 = datos.ToObject<RuepRoot>();

                    if (modeloV9 == null)
                        throw new InvalidDataException("No se pudo interpretar el modelo V9.");

                    if (modeloV9.ano_fiscal != datos.Value<int>("anio_fiscal"))
                        throw new InvalidDataException("Error al interpretar anio_fiscal.");

                    if (modeloV9.datos_fiscales?.registro_patronal !=
                        (string)datos["datos_fiscales"]?["registro_patronal_empresa"])
                    {
                        throw new InvalidDataException("Error al interpretar el registro patronal.");
                    }

                    if (modeloV9.personal == null || modeloV9.personal.Count == 0)
                        throw new InvalidDataException("No se pudieron interpretar los empleados.");

                    var empleado = modeloV9.personal[0];
                    var empleadoJson = datos["personal"][0];

                    if (empleado.imss != (string)empleadoJson["nss_imss"])
                        throw new InvalidDataException("Error al interpretar nss_imss.");

                    if (empleado.salario_integrado !=
                        (decimal?)empleadoJson["ingresos"]?["integrado"])
                    {
                        throw new InvalidDataException("Error al interpretar ingresos.integrado.");
                    }

                    if (empleado.historial_laboral?.Count !=
                        (empleadoJson["historial_laboral"] as JArray)?.Count)
                    {
                        throw new InvalidDataException("Error al interpretar historial_laboral.");
                    }

                    System.Diagnostics.Debug.WriteLine($"RUEP V9 interpretado correctamente. " + $"Empleados: {modeloV9.personal.Count}");

                    // Comprobar la estructura principal de V9.
                    if (datos["datos_fiscales"] is not JObject || datos["cfdi_nomina"] is not JObject || datos["personal"] is not JArray)
                    {
                        throw new InvalidDataException("El archivo no contiene la estructura esperada de RUEP V9.");
                    }

                    // ====================================================
                    // CAMPOS GENERALES DE EMPRESA - RUEP V9
                    // ====================================================

                    string nombreEmpresa = datos.Value<string>("nombre") ?? "";
                    int? anioFiscal = datos.Value<int?>("anio_fiscal");
                    decimal? salarioMinimoV9 = datos.Value<decimal?>("salario_minimo");
                    decimal? umaPorDiaV9 = datos.Value<decimal?>("uma_por_dia");
                    
                    // Validar campos indispensables.
                    if (string.IsNullOrWhiteSpace(nombreEmpresa) || !anioFiscal.HasValue || anioFiscal.Value <= 0 || !salarioMinimoV9.HasValue || !umaPorDiaV9.HasValue)
                    {
                        throw new InvalidDataException("Faltan datos generales obligatorios en RUEP V9.");
                    }

                    // ====================================================
                    // FORMATO MONETARIO
                    // ====================================================

                    var formato = new System.Globalization.NumberFormatInfo
                    {
                        CurrencySymbol = "$",
                        CurrencyDecimalDigits = 2
                    };

                    // ====================================================
                    // MOSTRAR DATOS EN FORMPRINCIPAL
                    // ====================================================

                    lblEmpresa.Text = nombreEmpresa;
                    lblAnio.Text = anioFiscal.Value.ToString();
                    lblSalarioMinimo.Text = salarioMinimoV9.Value.ToString("C", formato);
                    lblUMA.Text = umaPorDiaV9.Value.ToString("C", formato);

                    // ====================================================
                    // VARIABLES INTERNAS
                    // ====================================================

                    EmpresaNombre = nombreEmpresa;
                    anioEmpresa = anioFiscal.Value;
                    salarioMinimo = salarioMinimoV9.Value;
                }
                catch (Exception ex)
                {
                    LimpiarLabels();

                    lblEmpresa.Text = "Error al leer RUEP.dat";

                    MessageBox.Show("No fue posible interpretar RUEP V9.\n\n" + "Archivo:\n" + rutaRUEP + "\n\n" + "Detalle:\n" + ex.Message, "Error de lectura RUEP V9", MessageBoxButtons.OK, MessageBoxIcon.Error
                    );
                }

                return;
            }

            // ====================================================
            // CASO 2: NO EXISTE RUEP.DAT
            // Lectura provisional de encabezado VB6
            // ====================================================

            if (ExistenArchivosBaseParaRuep(directoryPath))
            {
                try
                {
                    var encabezado =
                        new RuepGenerator().LeerEncabezadoEmpresa(directoryPath);

                    var formato = new System.Globalization.NumberFormatInfo
                    {
                        CurrencySymbol = "$",
                        CurrencyDecimalDigits = 2
                    };

                    lblEmpresa.Text = encabezado.Nombre;
                    lblAnio.Text = encabezado.AnoFiscal.ToString();

                    lblSalarioMinimo.Text =
                        encabezado.SalarioMinimo.ToString("C", formato);

                    lblUMA.Text =
                        encabezado.Uma.ToString("C", formato);

                    EmpresaNombre = encabezado.Nombre;
                    anioEmpresa = encabezado.AnoFiscal;
                    salarioMinimo = encabezado.SalarioMinimo;
                }
                catch
                {
                    LimpiarLabels();
                }

                return;
            }

            // ====================================================
            // CASO 3: SIN INFORMACIÓN DE EMPRESA
            // ====================================================

            LimpiarLabels();
        }

        // ── Filtro de archivos ────────────────────────────────────────────
        private void CambiarFiltroArchivos(string filtro)
        {
            _filtroActual = filtro;
            tsMostrarJSON.Checked = filtro == "*.JSON";
            tsMostrarNOM.Checked = filtro != "*.JSON";

            Nomina_2026_NET8.Properties.Settings.Default.MostrarFiltroArchivos = filtro;
            Nomina_2026_NET8.Properties.Settings.Default.Save();

            if (!string.IsNullOrEmpty(lblRutaDirectorio.Text) && Directory.Exists(lblRutaDirectorio.Text))
                LoadFiles(lblRutaDirectorio.Text);
        }

        // ── Permisos de botones según directorio ──────────────────────────
        private CapacidadesDirectorio EvaluarDirectorio(string ruta)
        {
            if (!Directory.Exists(ruta))
                return CapacidadesDirectorio.Ninguna;

            bool existeRUEP = File.Exists(Path.Combine(ruta, "RUEP.dat"));

            // RUEP presente → habilita todo directamente
            if (existeRUEP)
                return CapacidadesDirectorio.Empresa | CapacidadesDirectorio.Personal | CapacidadesDirectorio.Nomina;

            // Fallback: evaluar por archivos binarios VB6
            bool existeEmpresa = File.Exists(Path.Combine(ruta, "EMPRESA.dno")) || File.Exists(Path.Combine(ruta, "empresa.dno"));
            bool existePersonal = File.Exists(Path.Combine(ruta, "PERSONAL.dno")) || File.Exists(Path.Combine(ruta, "personal.dno"));
            bool existeMaestro = File.Exists(Path.Combine(ruta, "MAESTRO.dno")) || File.Exists(Path.Combine(ruta, "maestro.dno"));

            var caps = CapacidadesDirectorio.Ninguna;

            if (existeEmpresa)
                caps |= CapacidadesDirectorio.Empresa;

            if (existeEmpresa && existePersonal && existeMaestro)
                caps |= CapacidadesDirectorio.Personal;

            if (caps.HasFlag(CapacidadesDirectorio.Personal))
                caps |= CapacidadesDirectorio.Nomina;

            return caps;
        }

        private void AplicarPermisos(CapacidadesDirectorio caps)
        {
            tsDatosDeLaEmpresa.Enabled = caps.HasFlag(CapacidadesDirectorio.Empresa);
            tsCRUDPersonal.Enabled = caps.HasFlag(CapacidadesDirectorio.Personal);
            tsmIniciarCaptura.Enabled = caps.HasFlag(CapacidadesDirectorio.Nomina);
            tsmImpresionNomina.Enabled = caps.HasFlag(CapacidadesDirectorio.Nomina);
            AcumJSON.Enabled = caps.HasFlag(CapacidadesDirectorio.Nomina);
            AcumNOM.Enabled = caps.HasFlag(CapacidadesDirectorio.Nomina);
        }

        // ── Apertura de archivos ──────────────────────────────────────────

        private void AbrirArchivoSeleccionado()
        {
            if (listBoxArchivos.SelectedItem == null) return;

            string nombreArchivo = listBoxArchivos.SelectedItem.ToString();
            string rutaFull = Path.Combine(lblRutaDirectorio.Text, nombreArchivo);

            bool esJson = nombreArchivo.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
            bool esNom = nombreArchivo.EndsWith(".nom", StringComparison.OrdinalIgnoreCase);

            if (!esJson && !esNom) return;

            if (!File.Exists(rutaFull))
            {
                MessageBox.Show($"El archivo {(esJson ? "JSON" : "NOM")} no existe.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                this.Visible = false;
                using var form = new FormCaptura
                {
                    RequiereRUEP = false,
                    ModoSoloLectura = true,
                    SalarioMinimo = this.salarioMinimo,
                    AnioEmpresa = this.anioEmpresa,
                    RutaArchivoACargar = rutaFull
                };
                form.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir archivo: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Visible = true;
            }
        }

        // ── Apertura de Forms hijos ───────────────────────────────────────
        private void AbrirFormCaptura(string rutaTrabajo, string rutaRUEP)
        {
            //IntentarSincronizarRUEP(rutaRUEP, mostrarAvisoSinConexion: true);

            this.Visible = false;
            using var formCaptura = new FormCaptura
            {
                RutaTrabajo = rutaTrabajo,
                RutaRUEP = rutaRUEP,
                AnioEmpresa = anioEmpresa,
                SalarioMinimo = salarioMinimo
            };
            formCaptura.ShowDialog();
            this.Visible = true;
        }

        private void AbrirFormEdicionPersonal(string rutaRUEP)
        {
            try
            {
                SyncResult sync = BDSyncService.SincronizarRUEP(rutaRUEP);

                if (sync.Discrepancias.Any())
                {
                    MessageBox.Show($"Se detectaron {sync.Discrepancias.Count} discrepancias.", "Sincronización", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error de sincronización");
            }

            this.Visible = false;

            using var form = new FormEDICIONCENTRALPERSONAL
            {
                RutaRUEP = rutaRUEP
            };

            form.ShowDialog();

            this.Visible = true;
        }

        private void AbrirFormEmpresaUnificado(string rutaRUEP)
        {
            using var form = new FormEmpresaUnificado
            { RutaArchivoEmpresa = rutaRUEP };
            form.ShowDialog();
        }

        // ── Validación de RUEP antes de abrir Forms ───────────────────────
        /// Patrón central para validar/generar RUEP.dat antes de ejecutar cualquier acción que lo requiera.
        private void EjecutarConValidacionRuep(Action<string, string> accionFinal, string mensajeGeneracion, bool preguntarContinuarDespuesGenerar = false)
        {
            string rutaTrabajo = lblRutaDirectorio.Text;

            if (!Directory.Exists(rutaTrabajo))
            {
                MessageBox.Show("Debe seleccionar un directorio válido.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string rutaRUEP = Path.Combine(rutaTrabajo, "RUEP.dat");

            // CASO 1: RUEP ya existe → usarlo directamente
            if (File.Exists(rutaRUEP))
            {
                accionFinal(rutaTrabajo, rutaRUEP);
                return;
            }

            // CASO 2: Sin archivos base para generar RUEP
            if (!ExistenArchivosBaseParaRuep(rutaTrabajo))
            {
                MessageBox.Show("No se encontró RUEP.dat y tampoco los archivos base necesarios.\n\nAsegúrate de tener el directorio de empresa completo.", "Archivos insuficientes",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // CASO 3: Preguntar si generar RUEP desde .dno
            if (MessageBox.Show(mensajeGeneracion, "RUEP.dat no encontrado", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                new RuepGenerator().Generar(rutaTrabajo);

                var sync = BDSyncService.SincronizarRUEP(rutaRUEP);

                if (sync.TieneDiscrepancias)
                {
                    MessageBox.Show(string.Join(Environment.NewLine + Environment.NewLine, sync.Discrepancias), "Discrepancias encontradas", MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                MessageBox.Show("RUEP.dat generado correctamente.\n\nSe recomienda verificar los datos del personal antes de continuar.", "RUEP generado", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Sync inmediato tras generar
                //IntentarSincronizarRUEP(rutaRUEP, mostrarAvisoSinConexion: true);

                if (preguntarContinuarDespuesGenerar && MessageBox.Show("¿Desea continuar?", "Continuar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                accionFinal(rutaTrabajo, rutaRUEP);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar RUEP.dat:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ExistenArchivosBaseParaRuep(string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta) || !Directory.Exists(ruta))
                return false;

            string[] requeridos = { "empresa.dno", "Empcomp.dno", "EMP_CFDI.DNO", "personal.dno", "PerOtre.dno", "Perscfdi.dno" };

            return requeridos.All(nombre => File.Exists(Path.Combine(ruta, nombre)));
        }

        // ── Tablas fiscales ───────────────────────────────────────────────
        private void VerificarTablasTarifa()
        {
            if (File.Exists(TarifasRepository.Ruta))
            {
                try { TarifasRepository.Obtener(); }
                catch { ForzarGeneracionTablas(); }
                return;
            }

            ForzarGeneracionTablas();
        }

        private void ForzarGeneracionTablas()
        {
            try
            {
                int anio = anioEmpresa > 0 ? anioEmpresa : DateTime.Now.Year;
                string mensaje = new TablasGenerator().Generar(anio);
                MessageBox.Show(mensaje, "Tablas Fiscales", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar tablas fiscales:\n\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── FileSystemWatcher ─────────────────────────────────────────────
        private void IniciarWatcherDirectorio(string ruta)
        {
            _watcherDirectorio?.Dispose();
            _watcherDirectorio = null;

            if (!Directory.Exists(ruta)) return;

            try
            {
                _watcherDirectorio = new FileSystemWatcher(ruta)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName,
                    Filter = "*.*",
                    IncludeSubdirectories = false,
                    EnableRaisingEvents = true
                };

                void DispararRefresh(object s, FileSystemEventArgs ev)
                {
                    if (IsDisposed || !IsHandleCreated) return;
                    BeginInvoke(() => { _timerRefreshArchivos.Stop(); _timerRefreshArchivos.Start(); });
                }

                _watcherDirectorio.Created += DispararRefresh;
                _watcherDirectorio.Deleted += DispararRefresh;
                _watcherDirectorio.Renamed += DispararRefresh;
            }
            catch { /* ruta inaccesible → ignorar */ }
        }

        private void RefrescarArchivosSilencioso()
        {
            string ruta = lblRutaDirectorio.Text;
            if (string.IsNullOrEmpty(ruta) || !Directory.Exists(ruta)) return;

            LoadFiles(ruta);

            var nodo = treeViewDirectorios.SelectedNode;
            if (nodo != null)
            {
                bool estabaExpandido = nodo.IsExpanded;
                nodo.Nodes.Clear();
                LoadSubDirectories(nodo);
                if (estabaExpandido) nodo.Expand();
            }
        }

        // ── Cierre ────────────────────────────────────────────────────────
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _watcherDirectorio?.Dispose();
            _timerRefreshArchivos?.Dispose();
            base.OnFormClosed(e);
        }

        private string ObtenerRutaValidaMasCercana(string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta)) return null;

            try
            {
                var dir = new DirectoryInfo(ruta);
                while (dir != null)
                {
                    if (dir.Exists) return dir.FullName;
                    dir = dir.Parent;
                }
            }
            catch { /* ruta malformada o inaccesible */ }

            return null;
        }

        // MES(3) + QUINCENA(1) + AÑO(4): ej. SEP22026.NOM, SEP12026.JSON
        private static readonly Regex RegexNominaNormal = new(@"^(?<mes>ENE|FEB|MAR|ABR|MAY|JUN|JUL|AGO|SEP|OCT|NOV|DIC)(?<q>[12])(?<anio>\d{4})\.(NOM|JSON)$", 
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // [NUMERO opcional][CONCEPTO]: ej. 1FINIQUITOS.NOM, 21FINIQUITOS.NOM, 3PTU.NOM
        private static readonly Regex RegexNominaEspecial = new(@"^(?<num>\d+)?(?<concepto>[A-ZÁÉÍÓÚÑ]+)\.(NOM|JSON)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private const string SeparadorEspeciales = "──────────────── ESPECIALES ────────────────";
        private class ArchivoNomina
        {
            public string NombreArchivo;
            public bool EsNormal;
            public int Anio, Mes, Quincena;
            public long Numero;
            public string Concepto;
        }

        private ArchivoNomina ClasificarArchivo(string nombreArchivo)
        {
            var mNormal = RegexNominaNormal.Match(nombreArchivo);
            if (mNormal.Success)
            {
                return new ArchivoNomina
                {
                    NombreArchivo = nombreArchivo,
                    EsNormal = true,
                    Mes = MesesOrden[mNormal.Groups["mes"].Value.ToUpperInvariant()],
                    Quincena = int.Parse(mNormal.Groups["q"].Value),
                    Anio = int.Parse(mNormal.Groups["anio"].Value)
                };
            }

            var mEspecial = RegexNominaEspecial.Match(nombreArchivo);
            long numero = 0;
            string concepto = Path.GetFileNameWithoutExtension(nombreArchivo);

            if (mEspecial.Success)
            {
                if (mEspecial.Groups["num"].Success)
                    long.TryParse(mEspecial.Groups["num"].Value, out numero);
                concepto = mEspecial.Groups["concepto"].Value;
            }

            return new ArchivoNomina { NombreArchivo = nombreArchivo, EsNormal = false, Numero = numero, Concepto = concepto };
        }

        private bool EsFilaSeparador(object item) => string.Equals(item as string, SeparadorEspeciales, StringComparison.Ordinal);

        private void listBoxArchivos_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!EsFilaSeparador(listBoxArchivos.SelectedItem)) return;

            int idx = listBoxArchivos.SelectedIndex;
            int siguiente = idx + 1;
            if (siguiente < listBoxArchivos.Items.Count)
                listBoxArchivos.SelectedIndex = siguiente;
            else if (idx - 1 >= 0)
                listBoxArchivos.SelectedIndex = idx - 1;
        }
    }
}