using gitosfiesta.Models;
using gitosfiesta.Services;
using System.Collections.ObjectModel;

namespace gitosfiesta;

public partial class ListaEventoPage : ContentPage
{
    private readonly Supabase.Client _supabaseClient;
    private readonly ContratoApiService _contratoApiService;
    private List<EventoModel> _todosLosEventos = new();
    private EventoModel? _eventoActual;
    private bool _isInitialized = false;
    private bool _isProcessing = false;
    private long? _idEventoInicial = null; // Variable para recordar si venimos desde el Dashboard 
    private List<UsuarioModelObj> _listaStaffObjetos = new();
    private List<string> _nombresStaffGlobal = new();
    private readonly List<string> _rolesMeserosBase = new() { "Principal / Barra", "Atención a Mesas", "Apoyo General" };
    private bool _isUpdatingPickers = false;
    private bool _eventoTieneStaffAsignado = false;

    public ObservableCollection<EventoModel> EventosFiltrados { get; set; } = new();
    public ObservableCollection<AbonoModel> HistorialModalAbonos { get; set; } = new();
    public ObservableCollection<StaffAsignadoViewItem> StaffAsignadoList { get; set; } = new();

    private decimal _saldoPendienteModalActual = 0;


    // Constructor 1: Para cuando entras desde el menú general
    public ListaEventoPage(Supabase.Client supabaseClient)
    {
        InitializeComponent();
        _supabaseClient = supabaseClient;
        _contratoApiService = App.Current?.Handler?.MauiContext?.Services
            .GetService<ContratoApiService>()
            ?? new ContratoApiService(new HttpClient
            {
                BaseAddress = new Uri("https://gitosfiesta-api.onrender.com/")
            });
        ColeccionEventos.ItemsSource = EventosFiltrados;
        ColeccionModalHistorial.ItemsSource = HistorialModalAbonos;
        ColeccionStaffAsignado.ItemsSource = StaffAsignadoList;

        DateTime hoy = DateTime.Today;
        DatePickerDesde.Date = hoy.AddDays(-15);
        DatePickerHasta.Date = hoy.AddDays(15);
    }

    // Constructor 2: NUEVO - Para cuando abres un evento específico desde el Dashboard (Inicio)
    // Constructor 2: Para cuando abres un evento específico desde el Dashboard (Inicio)
    public ListaEventoPage(Supabase.Client supabaseClient, long eventoId)
    {
        InitializeComponent();
        _supabaseClient = supabaseClient;
        _contratoApiService = App.Current?.Handler?.MauiContext?.Services
            .GetService<ContratoApiService>()
            ?? new ContratoApiService(new HttpClient
            {
                BaseAddress = new Uri("https://gitosfiesta-api.onrender.com/")
            });
        _idEventoInicial = eventoId; // <-- ¡Aquí faltaba guardar esta variable!

        ColeccionEventos.ItemsSource = EventosFiltrados;
        ColeccionModalHistorial.ItemsSource = HistorialModalAbonos;
        ColeccionStaffAsignado.ItemsSource = StaffAsignadoList;

        // Carga directamente el detalle del evento específico por su ID
        _ = CargarDetallePorIdAsync(eventoId);
    }

    private async Task CargarDetallePorIdAsync(long idEvento)
    {
        try
        {
            var response = await _supabaseClient
                .From<EventoModel>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, idEvento)
                .Get();

            var evento = response.Models?.FirstOrDefault();
            if (evento != null)
            {
                _eventoActual = evento;

                // Calculamos el total abonado igual que al hacer clic en la lista
                decimal totalAbonado = 0;
                try
                {
                    var responseAbonos = await _supabaseClient
                        .From<AbonoModel>()
                        .Filter("evento_id", Supabase.Postgrest.Constants.Operator.Equals, evento.Id)
                        .Get();

                    totalAbonado = (decimal)(responseAbonos.Models ?? new List<AbonoModel>()).Sum(a => a.Monto);
                }
                catch
                {
                    totalAbonado = 0;
                }

                // Verificamos staff y pintamos los detalles en pantalla
                await VerificarStaffAsignadoAsync(evento.Id);
                PintarDetallesEvento(evento, totalAbonado);
                ModoEdicionActivo(false);

                if (BtnModificarEvento != null)
                {
                    BtnModificarEvento.IsVisible = true;
                }

                // Ocultamos la lista y mostramos el panel de detalles con la información lista
                PanelLista.IsVisible = false;
                PanelDetalle.IsVisible = true;
            }
            else
            {
                await DisplayAlert("Aviso", "No se encontró el evento solicitado.", "Aceptar");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al cargar detalle: {ex.Message}");
        }
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Si entramos directos desde el Dashboard con un ID, cargamos ese detalle y evitamos resetear la vista
        if (_idEventoInicial.HasValue)
        {
            long idACargar = _idEventoInicial.Value;
            _idEventoInicial = null; // Limpiamos para que futuras navegaciones funcionen normal
            await CargarDetallePorIdAsync(idACargar);
            return;
        }

        // Comportamiento normal para la lista general
        PanelDetalle.IsVisible = false;
        PanelLista.IsVisible = true;
        _eventoActual = null;
        ModoEdicionActivo(false);

        if (PickerFiltro != null) PickerFiltro.SelectedIndex = 2;
        if (PickerEstatus != null && PickerEstatus.SelectedIndex == -1) PickerEstatus.SelectedIndex = 0;

        // Al entrar a la lista se muestran todos los eventos; el usuario puede acotar después.
        if (DatePickerDesde != null) DatePickerDesde.Date = new DateTime(2000, 1, 1);
        if (DatePickerHasta != null) DatePickerHasta.Date = new DateTime(2100, 12, 31);

        _isInitialized = true;
        await CargarEventosAsync();
    }

    private async Task CargarEventosAsync()
    {
        try
        {
            var query = _supabaseClient
                .From<EventoModel>()
                .Where(x => x.enable == true);

            if (SessionContext.Rol?.ToUpper() == "STAFF")
            {
                var asignaciones = await _supabaseClient
                    .From<EventoPersonalModel>()
                    .Filter("usuario_id", Supabase.Postgrest.Constants.Operator.Equals, SessionContext.UsuarioId)
                    .Get();

                var idsAsignados = (asignaciones.Models ?? new List<EventoPersonalModel>())
                    .Select(x => x.EventoId)
                    .ToList();

                _todosLosEventos = (await query.Get()).Models?
                    .Where(e => idsAsignados.Contains(e.Id))
                    .OrderBy(e => e.fecha_evento)
                    .ToList() ?? new List<EventoModel>();
            }
            else
            {
                var response = await query.Get();
                _todosLosEventos = response.Models?
                    .OrderBy(e => e.fecha_evento)
                    .ToList() ?? new List<EventoModel>();
            }

            AplicarFiltrosCombinados();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al cargar eventos: {ex.Message}");
            await DisplayAlert("Error", "No se pudieron cargar los eventos.", "Aceptar");
        }
    }

    private void OnFiltroCombinadoChanged(object sender, EventArgs e)
    {
        if (!_isInitialized) return;
        AplicarFiltrosCombinados();
    }

    private async void OnEventoSelected(object sender, SelectionChangedEventArgs e)
    {
        if (_isProcessing)
            return;

        var evento = e.CurrentSelection?.FirstOrDefault() as EventoModel;
        if (evento == null)
            return;

        // CollectionView no vuelve a lanzar SelectionChanged si queda seleccionado el mismo elemento.
        if (sender is CollectionView collectionView)
            collectionView.SelectedItem = null;

        _isProcessing = true;
        try
        {
            _eventoActual = evento;

            decimal totalAbonado = 0;
            var responseAbonos = await _supabaseClient
                .From<AbonoModel>()
                .Filter("evento_id", Supabase.Postgrest.Constants.Operator.Equals, evento.Id)
                .Get();

            totalAbonado = (decimal)(responseAbonos.Models ?? new List<AbonoModel>()).Sum(a => a.Monto);
            await VerificarStaffAsignadoAsync(evento.Id);
            PintarDetallesEvento(evento, totalAbonado);
            ModoEdicionActivo(false);

            PanelLista.IsVisible = false;
            PanelDetalle.IsVisible = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al abrir el evento {evento.Id}: {ex.Message}");
            await DisplayAlert("Error", "No se pudo abrir el evento seleccionado.", "Aceptar");
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private async Task VerificarStaffAsignadoAsync(long eventoId)
    {
        try
        {
            var response = await _supabaseClient
                .From<EventoPersonalModel>()
                .Filter("evento_id", Supabase.Postgrest.Constants.Operator.Equals, eventoId)
                .Get();

            var asignaciones = response.Models ?? new List<EventoPersonalModel>();
            _eventoTieneStaffAsignado = asignaciones.Count > 0;

            ActualizarBotonStaffUI();
        }
        catch (Exception)
        {
            _eventoTieneStaffAsignado = false;
            ActualizarBotonStaffUI();
        }
    }

    private void ActualizarBotonStaffUI()
    {
        if (BtnAsignarPersonal != null)
        {
            if (_eventoTieneStaffAsignado)
            {
                BtnAsignarPersonal.Text = "Staff Asignado";
                BtnAsignarPersonal.BackgroundColor = Color.FromArgb("#2B6CB0");
            }
            else
            {
                BtnAsignarPersonal.Text = "Asignar Personal";
                BtnAsignarPersonal.BackgroundColor = Color.FromArgb("#319795");
            }
        }
    }

    private async void OnVerContratoClicked(object sender, EventArgs e)
    {
        if (_eventoActual == null)
        {
            await DisplayAlert("Aviso", "Selecciona un evento antes de ver el contrato.", "Aceptar");
            return;
        }

        try
        {
            if (sender is Button button)
                button.IsEnabled = false;

            var responseTerminos = await _supabaseClient
                .From<MiscModel>()
                .Where(x => x.categoria == "TERMINO_CONTRATO" && x.enable == true)
                .Get();

            var terminos = responseTerminos.Models?
                .OrderBy(x => int.TryParse(x.value1, out int numero) ? numero : 0)
                .ToList() ?? new List<MiscModel>();

            var pdfBytes = await _contratoApiService.GenerarPdfAsync(
                _eventoActual,
                terminos,
                _eventoActual.firma);

            var ruta = Path.Combine(
                FileSystem.CacheDirectory,
                $"contrato-{_eventoActual.Id}.pdf");

            await File.WriteAllBytesAsync(ruta, pdfBytes);

            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                Title = "Abrir contrato",
                File = new ReadOnlyFile(ruta)
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Error al abrir contrato",
                $"No se pudo generar el contrato desde Render: {ex.Message}",
                "Aceptar");
        }
        finally
        {
            if (sender is Button button)
                button.IsEnabled = true;
        }
    }

    private async void OnVerComprobanteAbonoClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is AbonoModel abonoSeleccionado)
        {
            if (_eventoActual == null) return;

            try
            {
                var responseAbonos = await _supabaseClient
                    .From<AbonoModel>()
                    .Filter("evento_id", Supabase.Postgrest.Constants.Operator.Equals, _eventoActual.Id)
                    .Get();

                var listaAbonos = responseAbonos.Models?.OrderBy(a => a.FechaPago).ToList() ?? new List<AbonoModel>();

                decimal saldoAcumulado = _eventoActual.precio;
                decimal saldoAnterior = saldoAcumulado;

                foreach (var a in listaAbonos)
                {
                    if (a.Id == abonoSeleccionado.Id) break;
                    saldoAcumulado -= (decimal)a.Monto;
                }
                saldoAnterior = saldoAcumulado;

                var pdfBytes = await _contratoApiService.GenerarAbonoPdfAsync(
                    _eventoActual,
                    abonoSeleccionado,
                    saldoAnterior);

                string fileName = $"Comprobante_AB-{abonoSeleccionado.Id}.pdf";
                string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
                File.WriteAllBytes(filePath, pdfBytes);

                await Launcher.OpenAsync(new OpenFileRequest
                {
                    Title = "Comprobante de Abono - Gitos Fiesta",
                    File = new ReadOnlyFile(filePath)
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo generar el comprobante: {ex.Message}", "Aceptar");
            }
        }
    }

    // ==========================================
    // LÓGICA DE EDICIÓN DEL EVENTO EN LA MISMA VISTA
    // ==========================================
    private async void OnModificarEventoClicked(object sender, EventArgs e)
    {
        if (_eventoActual == null)
            return;

        ModoEdicionActivo(true);
    }

    private async void OnGuardarModificacionClicked(object sender, EventArgs e)
    {
        if (_eventoActual == null) return;

        if (string.IsNullOrWhiteSpace(TxtEditCliente.Text) ||
            string.IsNullOrWhiteSpace(TxtEditTelefono.Text) ||
            !int.TryParse(TxtEditPersonas.Text, out int numPersonas) || numPersonas <= 0)
        {
            await DisplayAlert("Datos requeridos", "Ingresa información válida para Cliente, Teléfono y Personas.", "Aceptar");
            return;
        }

        try
        {
            _eventoActual.nombre_cliente = TxtEditCliente.Text.Trim();
            _eventoActual.telefono_cliente = TxtEditTelefono.Text.Trim();
            _eventoActual.tipo_evento = TxtEditTipoEvento.Text.Trim();
            _eventoActual.fecha_evento = DatePickerEditFechaEvento.Date;
            _eventoActual.hora_evento = TimePickerEditHoraEvento.Time;
            _eventoActual.numero_personas = numPersonas;
            _eventoActual.menu = TxtEditMenu.Text?.Trim();
            _eventoActual.promocion = TxtEditPromocion.Text?.Trim();
            _eventoActual.observaciones = TxtEditObservaciones.Text?.Trim();

            await _supabaseClient
                .From<EventoModel>()
                .Where(x => x.Id == _eventoActual.Id)
                .Set(x => x.nombre_cliente, _eventoActual.nombre_cliente)
                .Set(x => x.telefono_cliente, _eventoActual.telefono_cliente)
                .Set(x => x.tipo_evento, _eventoActual.tipo_evento)
                .Set(x => x.fecha_evento, _eventoActual.fecha_evento)
                .Set(x => x.hora_evento, _eventoActual.hora_evento)
                .Set(x => x.numero_personas, _eventoActual.numero_personas)
                .Set(x => x.menu, _eventoActual.menu)
                .Set(x => x.promocion, _eventoActual.promocion)
                .Set(x => x.observaciones, _eventoActual.observaciones)
                .Update();

            await DisplayAlert("¡Éxito!", "El evento se ha actualizado correctamente.", "Aceptar");

            decimal totalAbonado = (decimal)HistorialModalAbonos.Sum(a => a.Monto);
            PintarDetallesEvento(_eventoActual, totalAbonado);
            ModoEdicionActivo(false);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error al actualizar", $"Ocurrió un error guardando los datos: {ex.Message}", "Aceptar");
        }
    }

    private void OnCancelarEdicionClicked(object sender, EventArgs e)
    {
        ModoEdicionActivo(false);
    }

    private void ModoEdicionActivo(bool enEdicion)
    {
        LblDetalleCliente.IsVisible = !enEdicion;
        TxtEditCliente.IsVisible = enEdicion;

        LblDetalleTelefono.IsVisible = !enEdicion;
        TxtEditTelefono.IsVisible = enEdicion;

        LblDetalleTipoEvento.IsVisible = !enEdicion;
        TxtEditTipoEvento.IsVisible = enEdicion;

        LblDetalleFechaEvento.IsVisible = !enEdicion;
        DatePickerEditFechaEvento.IsVisible = enEdicion;

        LblDetalleHora.IsVisible = !enEdicion;
        TimePickerEditHoraEvento.IsVisible = enEdicion;

        LblDetallePersonas.IsVisible = !enEdicion;
        TxtEditPersonas.IsVisible = enEdicion;

        LblDetalleMenu.IsVisible = !enEdicion;
        TxtEditMenu.IsVisible = enEdicion;

        LblDetallePromocion.IsVisible = !enEdicion;
        TxtEditPromocion.IsVisible = enEdicion;

        LblDetalleObservaciones.IsVisible = !enEdicion;
        TxtEditObservaciones.IsVisible = enEdicion;

        BtnModificarEvento.IsVisible = !enEdicion && (_eventoActual != null && (_eventoActual.fecha_evento.Date - DateTime.Today).Days > 10);
        BtnGuardarModificacion.IsVisible = enEdicion;
        BtnCancelarEdicion.IsVisible = enEdicion;
    }

    private void PickerModalMetodo_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (sender is Picker picker && picker.SelectedIndex != -1)
        {
            picker.InvalidateMeasure();
        }
    }
    private void PintarDetallesEvento(EventoModel evento, decimal totalAbonado = 0)
    {
        LblDetalleFolio.Text = evento.folio ?? "---";
        LblDetalleFechaContrato.Text = evento.fecha_contrato.ToString("dd/MM/yyyy HH:mm");
        LblDetalleCliente.Text = evento.nombre_cliente;
        LblDetalleTelefono.Text = evento.telefono_cliente;
        LblDetalleTipoEvento.Text = evento.tipo_evento;
        LblDetalleFechaEvento.Text = evento.fecha_evento.ToString("dd/MM/yyyy");

        DateTime horaTemporal = DateTime.Today.Add(evento.hora_evento);
        LblDetalleHora.Text = horaTemporal.ToString("hh:mm tt");

        LblDetallePersonas.Text = evento.numero_personas.ToString();
        LblDetalleEstatus.Text = evento.estatus;
        LblDetalleMenu.Text = evento.menu;
        LblDetallePrecio.Text = $"${evento.precio:F2}";
        LblDetalleAnticipo.Text = $"${evento.anticipo:F2}";
        LblDetalleAbonos.Text = $"${totalAbonado:F2}";
        LblDetalleSaldo.Text = $"${evento.Saldo:F2}";

        LblDetallePromocion.Text = string.IsNullOrEmpty(evento.promocion) ? "Ninguna" : evento.promocion;
        LblDetalleObservaciones.Text = string.IsNullOrEmpty(evento.observaciones) ? "Ninguna" : evento.observaciones;
    }

    private async void OnRegresarClicked(object sender, EventArgs e)
    {
        ModoEdicionActivo(false);
        PanelDetalle.IsVisible = false;
        PanelLista.IsVisible = true;
        _eventoActual = null;

        // Ampliamos el rango de fechas al regresar para asegurarnos de mostrar todos los eventos vigentes o recientes
        if (DatePickerDesde != null && DatePickerHasta != null)
        {
            DateTime hoy = DateTime.Today;
            DatePickerDesde.Date = hoy.AddMonths(-3); // Rango más amplio hacia atrás
            DatePickerHasta.Date = hoy.AddMonths(6); // Rango más amplio hacia adelante
        }

        if (PickerFiltro != null) PickerFiltro.SelectedIndex = 0; // Cambiar a "Todos" o el índice general
        if (PickerEstatus != null) PickerEstatus.SelectedIndex = 0; // Todos los estatus

        // Recargamos los eventos desde Supabase para repoblar la lista general
        await CargarEventosAsync();
    }

    // ==========================================
    // MÓDULO DE ASIGNACIÓN DE PERSONAL (STAFF)
    // ==========================================
    private async void OnAsignarPersonalClicked(object sender, EventArgs e)
    {
        if (_eventoActual == null) return;

        if (_eventoTieneStaffAsignado)
        {
            await MostrarStaffAsignadoModalAsync();
            return;
        }

        try
        {
            var responseStaff = await _supabaseClient
                .From<Usuario>()
                .Filter("access_type", Supabase.Postgrest.Constants.Operator.Equals, "STAFF")
                .Get();

            var listaStaff = responseStaff.Models ?? new List<Usuario>();

            if (listaStaff.Count == 0)
            {
                await DisplayAlert("Aviso", "No se encontraron usuarios registrados con el rol de STAFF.", "Aceptar");
                return;
            }

            _listaStaffObjetos = listaStaff.Select(u => new UsuarioModelObj { Id = u.Id, NombreCompleto = $"{u.Nombre} {u.ApellidoPaterno}" }).ToList();
            _nombresStaffGlobal = _listaStaffObjetos.Select(u => u.NombreCompleto).ToList();

            int numPersonas = _eventoActual.numero_personas;
            bool esGrupoGrande = numPersonas >= 60;

            if (esGrupoGrande)
            {
                LblPersonalRequeridoInfo.Text = $"Requerimiento ({numPersonas} personas): 4 Meseros y 1 Cocinero";
                LblTituloMeseros.Text = "🍽️ Asignar 4 Meseros y sus Roles";
                GridMesero4.IsVisible = true;
            }
            else
            {
                LblPersonalRequeridoInfo.Text = $"Requerimiento ({numPersonas} personas): 3 Meseros y 1 Cocinero";
                LblTituloMeseros.Text = "🍽️ Asignar 3 Meseros y sus Roles";
                GridMesero4.IsVisible = false;
            }

            _isUpdatingPickers = true;
            PickerCocaine.SelectedIndex = -1;
            PickerMesero1.SelectedIndex = -1;
            PickerMesero2.SelectedIndex = -1;
            PickerMesero3.SelectedIndex = -1;
            PickerMesero4.SelectedIndex = -1;
            PickerRol1.SelectedIndex = -1;
            PickerRol2.SelectedIndex = -1;
            PickerRol3.SelectedIndex = -1;
            PickerRol4.SelectedIndex = -1;
            _isUpdatingPickers = false;

            ActualizarOpcionesStaffPickers();
            ActualizarOpcionesRolPickers();

            PickerCocaine.SelectedIndexChanged -= OnStaffPickerChanged;
            PickerMesero1.SelectedIndexChanged -= OnStaffPickerChanged;
            PickerMesero2.SelectedIndexChanged -= OnStaffPickerChanged;
            PickerMesero3.SelectedIndexChanged -= OnStaffPickerChanged;
            PickerMesero4.SelectedIndexChanged -= OnStaffPickerChanged;

            PickerCocaine.SelectedIndexChanged += OnStaffPickerChanged;
            PickerMesero1.SelectedIndexChanged += OnStaffPickerChanged;
            PickerMesero2.SelectedIndexChanged += OnStaffPickerChanged;
            PickerMesero3.SelectedIndexChanged += OnStaffPickerChanged;
            PickerMesero4.SelectedIndexChanged += OnStaffPickerChanged;

            PickerRol1.SelectedIndexChanged -= OnRolPickerChanged;
            PickerRol2.SelectedIndexChanged -= OnRolPickerChanged;
            PickerRol3.SelectedIndexChanged -= OnRolPickerChanged;
            PickerRol4.SelectedIndexChanged -= OnRolPickerChanged;

            PickerRol1.SelectedIndexChanged += OnRolPickerChanged;
            PickerRol2.SelectedIndexChanged += OnRolPickerChanged;
            PickerRol3.SelectedIndexChanged += OnRolPickerChanged;
            PickerRol4.SelectedIndexChanged += OnRolPickerChanged;

            ModalAsignarPersonal.IsVisible = true;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo cargar el personal de staff: {ex.Message}", "Aceptar");
        }
    }

    private async Task MostrarStaffAsignadoModalAsync()
    {
        try
        {
            var response = await _supabaseClient
                .From<EventoPersonalModel>()
                .Filter("evento_id", Supabase.Postgrest.Constants.Operator.Equals, _eventoActual!.Id)
                .Get();

            var asignaciones = response.Models ?? new List<EventoPersonalModel>();
            StaffAsignadoList.Clear();

            foreach (var item in asignaciones)
            {
                var userResp = await _supabaseClient
                    .From<Usuario>()
                    .Where(u => u.Id == item.UsuarioId)
                    .Get();

                var user = userResp.Models.FirstOrDefault();
                string nombre = user != null ? $"{user.Nombre} {user.ApellidoPaterno}" : "Desconocido";

                StaffAsignadoList.Add(new StaffAsignadoViewItem
                {
                    Puesto = item.Puesto,
                    Rol = string.IsNullOrEmpty(item.Rol) ? "Sin Rol Específico" : item.Rol,
                    NombreCompleto = nombre
                });
            }

            ModalVerStaff.IsVisible = true;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo consultar el staff asignado: {ex.Message}", "Aceptar");
        }
    }

    private void OnCerrarModalVerStaffClicked(object sender, EventArgs e)
    {
        ModalVerStaff.IsVisible = false;
    }

    private void OnStaffPickerChanged(object sender, EventArgs e)
    {
        if (_isUpdatingPickers) return;
        ActualizarOpcionesStaffPickers();
    }

    private void ActualizarOpcionesStaffPickers()
    {
        _isUpdatingPickers = true;

        string? cocineroSel = PickerCocaine.SelectedItem?.ToString();
        string? mesero1Sel = PickerMesero1.SelectedItem?.ToString();
        string? mesero2Sel = PickerMesero2.SelectedItem?.ToString();
        string? mesero3Sel = PickerMesero3.SelectedItem?.ToString();
        string? mesero4Sel = GridMesero4.IsVisible ? PickerMesero4.SelectedItem?.ToString() : null;

        RefrescarItemsPicker(PickerCocaine, cocineroSel, new[] { mesero1Sel, mesero2Sel, mesero3Sel, mesero4Sel });
        RefrescarItemsPicker(PickerMesero1, mesero1Sel, new[] { cocineroSel, mesero2Sel, mesero3Sel, mesero4Sel });
        RefrescarItemsPicker(PickerMesero2, mesero2Sel, new[] { cocineroSel, mesero1Sel, mesero3Sel, mesero4Sel });
        RefrescarItemsPicker(PickerMesero3, mesero3Sel, new[] { cocineroSel, mesero1Sel, mesero2Sel, mesero4Sel });

        if (GridMesero4.IsVisible)
        {
            RefrescarItemsPicker(PickerMesero4, mesero4Sel, new[] { cocineroSel, mesero1Sel, mesero2Sel, mesero3Sel });
        }

        _isUpdatingPickers = false;
    }

    private void RefrescarItemsPicker(Picker picker, string? seleccionActual, IEnumerable<string?> excluidos)
    {
        var excluidosLimpios = excluidos.Where(x => !string.IsNullOrEmpty(x)).ToList();

        var disponibles = _nombresStaffGlobal
            .Where(nombre => nombre == seleccionActual || !excluidosLimpios.Contains(nombre))
            .ToList();

        picker.ItemsSource = disponibles;

        if (seleccionActual != null && disponibles.Contains(seleccionActual))
        {
            picker.SelectedItem = seleccionActual;
        }
    }

    private void OnRolPickerChanged(object sender, EventArgs e)
    {
        if (_isUpdatingPickers) return;
        ActualizarOpcionesRolPickers();
    }

    private void ActualizarOpcionesRolPickers()
    {
        _isUpdatingPickers = true;

        bool esGrupoGrande = _eventoActual != null && _eventoActual.numero_personas >= 60;
        int totalMeseros = esGrupoGrande ? 4 : 3;

        string? r1 = PickerRol1.SelectedItem?.ToString();
        string? r2 = PickerRol2.SelectedItem?.ToString();
        string? r3 = PickerRol3.SelectedItem?.ToString();
        string? r4 = GridMesero4.IsVisible ? PickerRol4.SelectedItem?.ToString() : null;

        RefrescarItemsRolPicker(PickerRol1, r1, new[] { r2, r3, r4 }, totalMeseros);
        RefrescarItemsRolPicker(PickerRol2, r2, new[] { r1, r3, r4 }, totalMeseros);
        RefrescarItemsRolPicker(PickerRol3, r3, new[] { r1, r2, r4 }, totalMeseros);

        if (GridMesero4.IsVisible)
        {
            RefrescarItemsRolPicker(PickerRol4, r4, new[] { r1, r2, r3 }, totalMeseros);
        }

        _isUpdatingPickers = false;
    }

    private void RefrescarItemsRolPicker(Picker picker, string? seleccionActual, IEnumerable<string?> otrosSeleccionados, int totalMeseros)
    {
        var listaOtros = otrosSeleccionados.Where(x => !string.IsNullOrEmpty(x)).ToList();
        var rolesDisponibles = new List<string>();

        int asignadosEnOtros = listaOtros.Count;
        int espaciosDisponiblesParaRepetir = (totalMeseros - 1) - asignadosEnOtros;

        foreach (var rol in _rolesMeserosBase)
        {
            int conteoEnOtros = listaOtros.Count(x => x == rol);

            bool esElMismo = (rol == seleccionActual);
            bool puedeRepetirse = conteoEnOtros < 2 && (conteoEnOtros == 0 || espaciosDisponiblesParaRepetir > 0);

            if (esElMismo || puedeRepetirse)
            {
                rolesDisponibles.Add(rol);
            }
        }

        picker.ItemsSource = rolesDisponibles;

        if (seleccionActual != null && rolesDisponibles.Contains(seleccionActual))
        {
            picker.SelectedItem = seleccionActual;
        }
    }

    private void OnCerrarModalPersonalClicked(object sender, EventArgs e)
    {
        ModalAsignarPersonal.IsVisible = false;
    }

    private async void OnGuardarPersonalClicked(object sender, EventArgs e)
    {
        if (_eventoActual == null) return;

        if (PickerCocaine.SelectedIndex == -1)
        {
            await DisplayAlert("Falta Cocinero", "Debes asignar un cocinero al evento.", "Aceptar");
            return;
        }

        int numPersonas = _eventoActual.numero_personas;
        bool esGrupoGrande = numPersonas >= 60;

        if (PickerMesero1.SelectedIndex == -1 || PickerRol1.SelectedIndex == -1 ||
            PickerMesero2.SelectedIndex == -1 || PickerRol2.SelectedIndex == -1 ||
            PickerMesero3.SelectedIndex == -1 || PickerRol3.SelectedIndex == -1)
        {
            await DisplayAlert("Faltan Meseros", "Por favor completa la asignación y el rol de los 3 meseros principales.", "Aceptar");
            return;
        }

        if (esGrupoGrande && (PickerMesero4.SelectedIndex == -1 || PickerRol4.SelectedIndex == -1))
        {
            await DisplayAlert("Falta Mesero 4", "Para 60 o más personas, es obligatorio asignar el cuarto mesero y su rol.", "Aceptar");
            return;
        }

        try
        {
            long cocineroId = ObtenerIdStaffPorNombre(PickerCocaine.SelectedItem?.ToString());
            long mesero1Id = ObtenerIdStaffPorNombre(PickerMesero1.SelectedItem?.ToString());
            long mesero2Id = ObtenerIdStaffPorNombre(PickerMesero2.SelectedItem?.ToString());
            long mesero3Id = ObtenerIdStaffPorNombre(PickerMesero3.SelectedItem?.ToString());
            long? mesero4Id = esGrupoGrande ? ObtenerIdStaffPorNombre(PickerMesero4.SelectedItem?.ToString()) : null;

            var existingRows = await _supabaseClient
                .From<EventoPersonalModel>()
                .Filter("evento_id", Supabase.Postgrest.Constants.Operator.Equals, _eventoActual.Id)
                .Get();

            foreach (var row in existingRows.Models ?? new List<EventoPersonalModel>())
            {
                await _supabaseClient.From<EventoPersonalModel>().Where(x => x.Id == row.Id).Delete();
            }

            await _supabaseClient.From<EventoPersonalModel>().Insert(new EventoPersonalModel
            {
                EventoId = _eventoActual.Id,
                UsuarioId = cocineroId,
                Puesto = "Cocinero",
                Rol = null,
                Enable = true
            });

            await _supabaseClient.From<EventoPersonalModel>().Insert(new EventoPersonalModel
            {
                EventoId = _eventoActual.Id,
                UsuarioId = mesero1Id,
                Puesto = "Mesero 1",
                Rol = PickerRol1.SelectedItem?.ToString(),
                Enable = true
            });

            await _supabaseClient.From<EventoPersonalModel>().Insert(new EventoPersonalModel
            {
                EventoId = _eventoActual.Id,
                UsuarioId = mesero2Id,
                Puesto = "Mesero 2",
                Rol = PickerRol2.SelectedItem?.ToString(),
                Enable = true
            });

            await _supabaseClient.From<EventoPersonalModel>().Insert(new EventoPersonalModel
            {
                EventoId = _eventoActual.Id,
                UsuarioId = mesero3Id,
                Puesto = "Mesero 3",
                Rol = PickerRol3.SelectedItem?.ToString(),
                Enable = true
            });

            if (esGrupoGrande && mesero4Id.HasValue)
            {
                await _supabaseClient.From<EventoPersonalModel>().Insert(new EventoPersonalModel
                {
                    EventoId = _eventoActual.Id,
                    UsuarioId = mesero4Id.Value,
                    Puesto = "Mesero 4",
                    Rol = PickerRol4.SelectedItem?.ToString(),
                    Enable = true
                });
            }

            await DisplayAlert("¡Éxito!", "El personal y sus roles fueron guardados correctamente en la base de datos.", "Aceptar");
            ModalAsignarPersonal.IsVisible = false;

            await VerificarStaffAsignadoAsync(_eventoActual.Id);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error al guardar", $"No se pudo guardar la asignación: {ex.Message}", "Aceptar");
        }
    }

    private long ObtenerIdStaffPorNombre(string? nombreCompleto)
    {
        if (string.IsNullOrEmpty(nombreCompleto)) return 0;
        var match = _listaStaffObjetos.FirstOrDefault(u => u.NombreCompleto == nombreCompleto);
        return match?.Id ?? 0;
    }

    private void OnAgregarComentariosClicked(object sender, EventArgs e)
    {
        if (_eventoActual == null) return;
        TxtModalComentarios.Text = _eventoActual.observaciones ?? string.Empty;
        ModalComentarios.IsVisible = true;
    }

    private async void OnGuardarComentarioModalClicked(object sender, EventArgs e)
    {
        if (_eventoActual == null) return;
        string nuevoComentario = TxtModalComentarios.Text?.Trim() ?? string.Empty;

        try
        {
            _eventoActual.observaciones = nuevoComentario;

            await _supabaseClient
                .From<EventoModel>()
                .Where(x => x.Id == _eventoActual.Id)
                .Set(x => x.observaciones, nuevoComentario)
                .Update();

            decimal totalAbonado = (decimal)HistorialModalAbonos.Sum(a => a.Monto);
            PintarDetallesEvento(_eventoActual, totalAbonado);
            ModalComentarios.IsVisible = false;

            await DisplayAlert("Éxito", "Observaciones actualizadas correctamente.", "Aceptar");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudieron guardar los comentarios: {ex.Message}", "Aceptar");
        }
    }

    private void OnCancelarComentarioModalClicked(object sender, EventArgs e)
    {
        ModalComentarios.IsVisible = false;
    }

    private async void OnRealizarPagoClicked(object sender, EventArgs e)
    {
        if (_eventoActual == null) return;

        try
        {
            var responseAbonos = await _supabaseClient
                .From<AbonoModel>()
                .Filter("evento_id", Supabase.Postgrest.Constants.Operator.Equals, _eventoActual.Id)
                .Get();

            var listaAbonos = responseAbonos.Models?.OrderBy(a => a.FechaPago).ToList() ?? new List<AbonoModel>();

            HistorialModalAbonos.Clear();
            foreach (var abono in listaAbonos)
            {
                HistorialModalAbonos.Add(abono);
            }

            decimal totalAbonado = (decimal)listaAbonos.Sum(a => a.Monto);
            decimal costoTotal = _eventoActual.precio;
            decimal anticipo = _eventoActual.anticipo;

            _saldoPendienteModalActual = _eventoActual.Saldo;

            LblModalCostoTotal.Text = $"${costoTotal:F2}";
            LblModalAnticipo.Text = $"${anticipo:F2}";
            LblModalAbonosAcumulados.Text = $"${totalAbonado:F2}";
            LblModalSaldoPendiente.Text = $"${_saldoPendienteModalActual:F2}";

            TxtModalMontoAbono.Text = string.Empty;

            // Establecemos 0 por defecto para que seleccione "Efectivo" automáticamente al abrir el modal
            PickerModalMetodo.SelectedIndex = 0;

            ModalAbonos.IsVisible = true;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo cargar el estado de cuenta: {ex.Message}", "Aceptar");
        }
    }
    private async void OnRegistrarPagoModalClicked(object sender, EventArgs e)
    {
        if (_eventoActual == null) return;

        if (_isProcessing) return;

        if (!decimal.TryParse(TxtModalMontoAbono.Text, out decimal montoNuevo) || montoNuevo <= 0)
        {
            await DisplayAlert("Dato inválido", "Ingresa una cantidad válida mayor a cero.", "Aceptar");
            return;
        }

        if (PickerModalMetodo.SelectedIndex == -1)
        {
            await DisplayAlert("Falta Método", "Por favor selecciona un método de pago.", "Aceptar");
            return;
        }

        if (montoNuevo > (decimal)_saldoPendienteModalActual)
        {
            await DisplayAlert("Monto excedido", $"No puedes abonar más del saldo pendiente (${_saldoPendienteModalActual:F2}).", "Aceptar");
            return;
        }

        _isProcessing = true;

        try
        {
            // 1. Guardar el nuevo abono
            var nuevoAbono = new AbonoModel
            {
                EventoId = _eventoActual.Id,
                Monto = montoNuevo,
                FechaPago = DateTime.Now,
                MetodoPago = PickerModalMetodo.SelectedItem.ToString(),
                Enable = true
            };

            await _supabaseClient.From<AbonoModel>().Insert(nuevoAbono);

            // 2. Calcular el nuevo saldo restante
            decimal nuevoSaldoRestante = (decimal)_saldoPendienteModalActual - montoNuevo;
            if (nuevoSaldoRestante < 0) nuevoSaldoRestante = 0;

            // 3. Actualizar el estatus y el saldo en el objeto del evento
            _eventoActual.Saldo = nuevoSaldoRestante;

            if (nuevoSaldoRestante <= 0)
            {
                _eventoActual.estatus = "Liquidado";
            }

            // Actualización segura con el objeto completo en Supabase
            await _supabaseClient.From<EventoModel>()
                .Where(x => x.Id == _eventoActual.Id)
                .Update(_eventoActual);

            // 4. Mostrar alerta de éxito adaptada según si se liquidó o fue abono parcial
            if (nuevoSaldoRestante <= 0)
            {
                await DisplayAlert("¡Cuenta Liquidada!", "El pago se ha registrado y el evento ha quedado totalmente pagado.", "Aceptar");
            }
            else
            {
                await DisplayAlert("Abono Exitoso", $"Se ha registrado el pago de ${montoNuevo:F2} correctamente.", "Aceptar");
            }

            // =========================================================================
            // MANTENERNOS SIEMPRE EN EL MODAL Y REFRESCAR DATOS AL INSTANTE
            // =========================================================================

            // Consultar los abonos actualizados de Supabase
            var responseAbonos = await _supabaseClient
                .From<AbonoModel>()
                .Filter("evento_id", Supabase.Postgrest.Constants.Operator.Equals, _eventoActual.Id)
                .Get();

            var listaAbonos = responseAbonos.Models?.OrderBy(a => a.FechaPago).ToList() ?? new List<AbonoModel>();

            HistorialModalAbonos.Clear();
            foreach (var abono in listaAbonos)
            {
                HistorialModalAbonos.Add(abono);
            }

            // Recalcular montos en el modal
            decimal totalAbonado = (decimal)listaAbonos.Sum(a => a.Monto);
            _saldoPendienteModalActual = _eventoActual.Saldo;

            // Refrescar etiquetas visuales del modal
            LblModalAbonosAcumulados.Text = $"${totalAbonado:F2}";
            LblModalSaldoPendiente.Text = $"${_saldoPendienteModalActual:F2}";
            TxtModalMontoAbono.Text = string.Empty;

            // Refrescar también los detalles de la tarjeta de fondo
            PintarDetallesEvento(_eventoActual, totalAbonado);

            // Si ya se liquidó, actualizamos también la lista general de eventos en segundo plano por si deciden cerrar el modal después
            if (nuevoSaldoRestante <= 0)
            {
                await CargarEventosAsync();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error al guardar", $"Hubo un problema registrando el pago: {ex.Message}", "Aceptar");
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private void OnCerrarModalAbonosClicked(object sender, EventArgs e)
    {
        ModalAbonos.IsVisible = false;
    }

    private void AplicarFiltrosCombinados()
    {
        if (_todosLosEventos == null) return;

        IEnumerable<EventoModel> resultado = _todosLosEventos;

        if (DatePickerDesde != null && DatePickerHasta != null)
        {
            DateTime fechaInicio = DatePickerDesde.Date.Date;
            DateTime fechaFin = DatePickerHasta.Date.Date;
            resultado = resultado.Where(e => e.fecha_evento.Date >= fechaInicio && e.fecha_evento.Date <= fechaFin);
        }

        string seleccionVigencia = PickerFiltro?.SelectedItem?.ToString() ?? "Todos";
        DateTime hoy = DateTime.Today;

        if (seleccionVigencia.Contains("Vigentes"))
        {
            resultado = resultado.Where(e => e.fecha_evento.Date >= hoy);
        }
        else if (seleccionVigencia.Contains("Caducados"))
        {
            resultado = resultado.Where(e => e.fecha_evento.Date < hoy);
        }

        string estatusSeleccionado = PickerEstatus?.SelectedItem?.ToString() ?? "Todos los estatus";
        if (!estatusSeleccionado.Contains("Todos"))
        {
            resultado = resultado.Where(e => e.estatus != null && e.estatus.Equals(estatusSeleccionado, StringComparison.OrdinalIgnoreCase));
        }

        string textoBusqueda = TxtBuscarNombre?.Text?.Trim().ToLower() ?? "";
        if (!string.IsNullOrEmpty(textoBusqueda))
        {
            resultado = resultado.Where(e => e.nombre_cliente != null && e.nombre_cliente.ToLower().Contains(textoBusqueda));
        }

        EventosFiltrados.Clear();
        foreach (var ev in resultado)
        {
            EventosFiltrados.Add(ev);
        }
    }

    private async void OnEventoCardTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not EventoModel evento)
        {
            return;
        }

        if (_isProcessing)
            return;

        _isProcessing = true;
        try
        {
            _eventoActual = evento;

            var responseAbonos = await _supabaseClient
                .From<AbonoModel>()
                .Filter("evento_id", Supabase.Postgrest.Constants.Operator.Equals, evento.Id)
                .Get();

            decimal totalAbonado = (decimal)(responseAbonos.Models ?? new List<AbonoModel>()).Sum(a => a.Monto);
            await VerificarStaffAsignadoAsync(evento.Id);
            PintarDetallesEvento(evento, totalAbonado);
            ModoEdicionActivo(false);
            PanelLista.IsVisible = false;
            PanelDetalle.IsVisible = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al abrir evento: {ex.Message}");
            await DisplayAlert("Error", "No se pudo abrir el evento.", "Aceptar");
        }
        finally
        {
            _isProcessing = false;
        }
    }
}

public class UsuarioModelObj
{
    public long Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
}

public class StaffAsignadoViewItem
{
    public string Puesto { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
}