using DocumentFormat.OpenXml.CustomProperties;
using Newtonsoft.Json;
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
            // Aviso de conectividad — no bloquea
            //if (!BDSyncService.TestConexion(out _))
            //{
            //    MessageBox.Show("No hay conexión con la base de datos.\n\nPuedes seguir trabajando con los datos del RUEP.dat.\n" + "La sincronización se omitirá hasta que recuperes conexión.",
            //        "Sin conexión a BD", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //}

            // Restaurar directorio guardado
            string rutaGuardada = Nomina_2026_NET8.Properties.Settings.Default.RutaSeleccionada;
            if (string.IsNullOrWhiteSpace(rutaGuardada)) return;

            if (Directory.Exists(rutaGuardada))
                SeleccionarYExpandirRuta(rutaGuardada);
            else
            {
                MessageBox.Show($"El directorio guardado ya no está disponible:\n{rutaGuardada}\n\nDeberás seleccionarlo nuevamente.", "Directorio no encontrado", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

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
        private void listBoxArchivos_DoubleClick(object sender, EventArgs e) => AbrirArchivoSeleccionado();

        private void listBoxArchivos_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
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

            // Sync silencioso al navegar a un directorio con RUEP
            //string rutaRUEP = Path.Combine(ruta, "RUEP.dat");
            //if (File.Exists(rutaRUEP))
            //{
            //    IntentarSincronizarRUEP(rutaRUEP, mostrarAvisoSinConexion: false);
            //    InterpretarYMostrarEmpre(ruta);
            //}
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
                try
                {
                    allFiles = Directory.EnumerateFiles(path);
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("No tiene permisos para leer la carpeta seleccionada.", "Permisos insuficientes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                bool verJSON = string.Equals(_filtroActual, "*.JSON", StringComparison.OrdinalIgnoreCase);

                var files = allFiles.Where(f => !string.Equals(Path.GetFileName(f), "RUEP.dat", StringComparison.OrdinalIgnoreCase)).Where(f =>
                {
                    string name = Path.GetFileName(f);
                    return verJSON ? name.EndsWith(".json", StringComparison.OrdinalIgnoreCase) : name.EndsWith(".nom", StringComparison.OrdinalIgnoreCase);
                }).OrderBy(f => f, StringComparer.CurrentCultureIgnoreCase).Select(Path.GetFileName).ToArray();

                if (files.Length > 0)
                    listBoxArchivos.Items.AddRange(files);
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
        }

        // Solo rama RUEP.dat — rama EMPRESA.dno eliminada (código muerto)
        private void InterpretarYMostrarEmpre(string directoryPath)
        {
            string rutaRUEP = Path.Combine(directoryPath, "RUEP.dat");

            if (File.Exists(rutaRUEP))
            {
                try
                {
                    var svc = new RuepService(rutaRUEP);
                    var empresa = svc.DatosEmpresa;

                    var fmt = new System.Globalization.NumberFormatInfo
                    { CurrencySymbol = "$", CurrencyDecimalDigits = 2 };

                    lblEmpresa.Text = empresa.nombre;
                    EmpresaNombre = empresa.nombre;
                    lblAnio.Text = empresa.ano_fiscal.ToString();
                    lblSalarioMinimo.Text = empresa.salario_minimo.ToString("C", fmt);
                    lblUMA.Text = empresa.uma_por_dia.ToString("C", fmt);

                    salarioMinimo = empresa.salario_minimo;
                    anioEmpresa = empresa.ano_fiscal;
                }
                catch
                {
                    LimpiarLabels();
                }
                return;
            }

            if (ExistenArchivosBaseParaRuep(directoryPath))
            {
                try
                {
                    var encabezado = new RuepGenerator().LeerEncabezadoEmpresa(directoryPath);

                    var fmt = new System.Globalization.NumberFormatInfo
                    { CurrencySymbol = "$", CurrencyDecimalDigits = 2 };

                    lblEmpresa.Text = encabezado.Nombre;
                    EmpresaNombre = encabezado.Nombre;
                    lblAnio.Text = encabezado.AnoFiscal.ToString();
                    lblSalarioMinimo.Text = encabezado.SalarioMinimo.ToString("C", fmt);
                    lblUMA.Text = encabezado.Uma.ToString("C", fmt);

                    salarioMinimo = encabezado.SalarioMinimo;
                    anioEmpresa = encabezado.AnoFiscal;
                }
                catch
                {
                    LimpiarLabels();
                }
                return;
            }

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
            //IntentarSincronizarRUEP(rutaRUEP, mostrarAvisoSinConexion: true);

            this.Visible = false;
            using var form = new FormEDICIONCENTRALPERSONAL
            { RutaRUEP = rutaRUEP };
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
            string[] requeridos = { "empresa.dno", "Empcomp.dno", "personal.dno", "PerOtre.dno", "Perscfdi.dno", "Bnxcla.dno" };
            return requeridos.All(f => File.Exists(Path.Combine(ruta, f)));
        }

        // ── Sincronización con BD ─────────────────────────────────────────
        //private bool IntentarSincronizarRUEP(string rutaRUEP, bool mostrarAvisoSinConexion = false)
        //{
        //    if (!BDSyncService.TestConexion(out _))
        //    {
        //        if (mostrarAvisoSinConexion)
        //            MessageBox.Show("Sin conexión a la base de datos.\nSe trabajará con los datos locales del RUEP.dat.", "Sin conexión", MessageBoxButtons.OK, MessageBoxIcon.Information);
        //        return false;
        //    }
        //
        //    try
        //    {
        //        Cursor = Cursors.WaitCursor;
        //        SyncResult sync = BDSyncService.SincronizarRUEP(rutaRUEP);
        //        Cursor = Cursors.Default;
        //
        //        if (!sync.HuboCambios) return true;
        //
        //        var svc = new RuepService(rutaRUEP);
        //        var sinSalario = svc.ObtenerTodos().Where(e => e.SalarioDiario == 0m && string.IsNullOrWhiteSpace(e.FechaBaja) && !string.IsNullOrWhiteSpace(e.Nombre)).ToList();
        //
        //        string msg = $"Datos del personal actualizados desde la base de datos.\n\n" + $"• Empleados nuevos  : {sync.Nuevos}\n" + $"• Actualizados      : {sync.Actualizados}";
        //
        //        if (sinSalario.Count > 0)
        //            msg += $"\n\n⚠ {sinSalario.Count} empleado(s) sin salario " + $"registrado:\n• " + string.Join("\n• ", sinSalario.Take(5).Select(e => $"{e.ApellidoP} {e.ApellidoM} {e.Nombre}")) +
        //                (sinSalario.Count > 5 ? $"\n  ...y {sinSalario.Count - 5} más." : "");
        //
        //        MessageBox.Show(msg, "Sincronización BD", MessageBoxButtons.OK, MessageBoxIcon.Information);
        //
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Cursor = Cursors.Default;
        //        MessageBox.Show($"No se pudo sincronizar con la BD:\n\n{ex.Message}\n\nSe usarán los datos locales del RUEP.dat.", "Error de sincronización", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //        return false;
        //    }
        //}

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

        private void listBoxArchivos_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

    }
}