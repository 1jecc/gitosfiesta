using gitosfiesta.Models;
using System.Collections.ObjectModel;

namespace gitosfiesta;

public partial class ListaEventoPage : ContentPage
{
    private readonly Supabase.Client _supabaseClient;
    private List<EventoModel> _todosLosEventos = new();
    private EventoModel? _eventoActual;
    private bool _isInitialized = false;
    private bool _isProcessing = false;

    // Variables para el filtrado dinámico en los Pickers
    private List<UsuarioModelObj> _listaStaffObjetos = new(); // Almacena objetos completos con su ID
    private List<string> _nombresStaffGlobal = new();
    private readonly List<string> _rolesMeserosBase = new() { "Principal / Barra", "Atención a Mesas", "Apoyo General" };
    private bool _isUpdatingPickers = false;
    private bool _eventoTieneStaffAsignado = false; // Bandera para cambiar el comportamiento del botón

    public ObservableCollection<EventoModel> EventosFiltrados { get; set; } = new();
    public ObservableCollection<AbonoModel> HistorialModalAbonos { get; set; } = new();
    public ObservableCollection<StaffAsignadoViewItem> StaffAsignadoList { get; set; } = new();

    private decimal _saldoPendienteModalActual = 0;

    public ListaEventoPage(Supabase.Client supabaseClient)
    {
        InitializeComponent();
        _supabaseClient = supabaseClient;
        ColeccionEventos.ItemsSource = EventosFiltrados;
        ColeccionModalHistorial.ItemsSource = HistorialModalAbonos;
        ColeccionStaffAsignado.ItemsSource = StaffAsignadoList;

        DateTime hoy = DateTime.Today;
        DatePickerDesde.Date = hoy.AddDays(-15);
        DatePickerHasta.Date = hoy.AddDays(15);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        PanelDetalle.IsVisible = false;
        PanelLista.IsVisible = true;
        _eventoActual = null;

        if (PickerFiltro != null) PickerFiltro.SelectedIndex = 2;
        if (PickerEstatus != null && PickerEstatus.SelectedIndex == -1) PickerEstatus.SelectedIndex = 0;

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
                query = query.Filter("usuario_creacion_id", Supabase.Postgrest.Constants.Operator.Equals, SessionContext.UsuarioId);
            }

            var response = await query.Get();
            _todosLosEventos = response.Models?.OrderBy(e => e.fecha_evento).ToList() ?? new List<EventoModel>();

            AplicarFiltrosCombinados();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudieron cargar los eventos: {ex.Message}", "Aceptar");
        }
    }

    private void OnFiltroCombinadoChanged(object sender, EventArgs e)
    {
        if (!_isInitialized) return;
        AplicarFiltrosCombinados();
    }

    private async void OnEventoSelected(object sender, SelectionChangedEventArgs e)
    {
        var evento = e.CurrentSelection.FirstOrDefault() as EventoModel;
        if (evento == null) return;

        ((CollectionView)sender).SelectedItem = null;
        _eventoActual = evento;

        decimal totalAbonado = 0;
        try
        {
            var responseAbonos = await _supabaseClient
                .From<AbonoModel>()
                .Filter("evento_id", Supabase.Postgrest.Constants.Operator.Equals, evento.Id)
                .Get();

            var listaAbonos = responseAbonos.Models ?? new List<AbonoModel>();
            totalAbonado = listaAbonos.Sum(a => a.Monto);
        }
        catch (Exception)
        {
            totalAbonado = 0;
        }

        // ¡AQUÍ ESTABA EL DETALLE! Llamamos a la verificación para actualizar el estado del botón
        await VerificarStaffAsignadoAsync(evento.Id);

        PintarDetallesEvento(evento, totalAbonado);

        PanelLista.IsVisible = false;
        PanelDetalle.IsVisible = true;
    }


    private async Task VerificarStaffAsignadoAsync(long eventoId)
    {
        try
        {
            // Consultamos directamente por evento_id sin restricciones extra que puedan fallar
            var response = await _supabaseClient
                .From<EventoPersonalModel>()
                .Filter("evento_id", Supabase.Postgrest.Constants.Operator.Equals, eventoId)
                .Get();

            var asignaciones = response.Models ?? new List<EventoPersonalModel>();

            // Verificamos si realmente existen registros guardados para este evento
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
                BtnAsignarPersonal.Text = "👥 Staff Asignado";
                BtnAsignarPersonal.BackgroundColor = Color.FromArgb("#2B6CB0"); // Azul distintivo
            }
            else
            {
                BtnAsignarPersonal.Text = "👥 Asignar Personal";
                BtnAsignarPersonal.BackgroundColor = Color.FromArgb("#319795"); // Turquesa original
            }
        }
    }

    private async void OnVerContratoDetalleClicked(object sender, EventArgs e)
    {
        if (_eventoActual != null && _supabaseClient != null)
        {
            try
            {
                var responseMisc = await _supabaseClient
                    .From<MiscModel>()
                    .Where(x => x.categoria == "TERMINO_CONTRATO" && x.enable == true)
                    .Get();

                var listaTerminos = responseMisc.Models?
                    .OrderBy(m => int.TryParse(m.value1, out int num) ? num : 0)
                    .Select(m => $"{m.value1}.- {m.value2}")
                    .ToList() ?? new List<string>();

                var pdfService = new ContratoPdfService(_eventoActual, listaTerminos);
                byte[] pdfBytes = pdfService.GenerarPdf();

                string fileName = $"Contrato_{_eventoActual.folio ?? "Evento"}.pdf";
                string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
                File.WriteAllBytes(filePath, pdfBytes);

                await Launcher.OpenAsync(new OpenFileRequest
                {
                    Title = "Contrato Gitos Fiesta",
                    File = new ReadOnlyFile(filePath)
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo generar el contrato: {ex.Message}", "Aceptar");
            }
        }
        else
        {
            await DisplayAlert("Aviso", "No se pudo identificar el evento seleccionado.", "Aceptar");
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

    private void OnRegresarClicked(object sender, EventArgs e)
    {
        PanelDetalle.IsVisible = false;
        PanelLista.IsVisible = true;
        _eventoActual = null;
        CargarEventosAsync();
    }

    // ==========================================
    // MÓDULO DE ASIGNACIÓN DE PERSONAL (STAFF)
    // ==========================================
    private async void OnAsignarPersonalClicked(object sender, EventArgs e)
    {
        if (_eventoActual == null) return;

        // Si ya tiene staff asignado, en lugar de abrir el formulario de asignación, mostramos el modal con el staff actual
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
                // Consultamos el nombre del usuario
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

    // --- FILTRADO DE NOMBRES DE STAFF ---
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

    // --- FILTRADO DINÁMICO DE ROLES DE MESEROS ---
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

        // Calculamos cuántos espacios libres nos quedan por asignar en los otros pickers
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
            // 1. Guardar en Base de Datos (Insertar en gitosfiesta_evento_personal)
            // Obtenemos los IDs reales de los usuarios seleccionados
            long cocineroId = ObtenerIdStaffPorNombre(PickerCocaine.SelectedItem?.ToString());
            long mesero1Id = ObtenerIdStaffPorNombre(PickerMesero1.SelectedItem?.ToString());
            long mesero2Id = ObtenerIdStaffPorNombre(PickerMesero2.SelectedItem?.ToString());
            long mesero3Id = ObtenerIdStaffPorNombre(PickerMesero3.SelectedItem?.ToString());
            long? mesero4Id = esGrupoGrande ? ObtenerIdStaffPorNombre(PickerMesero4.SelectedItem?.ToString()) : null;

            // Limpiamos asignaciones previas si existieran para este evento
            var existingRows = await _supabaseClient
                .From<EventoPersonalModel>()
                .Filter("evento_id", Supabase.Postgrest.Constants.Operator.Equals, _eventoActual.Id)
                .Get();

            foreach (var row in existingRows.Models ?? new List<EventoPersonalModel>())
            {
                await _supabaseClient.From<EventoPersonalModel>().Where(x => x.Id == row.Id).Delete();
            }

            // Insertar Cocinero
            await _supabaseClient.From<EventoPersonalModel>().Insert(new EventoPersonalModel
            {
                EventoId = _eventoActual.Id,
                UsuarioId = cocineroId,
                Puesto = "Cocinero",
                Rol = null,
                Enable = true
            });

            // Insertar Mesero 1
            await _supabaseClient.From<EventoPersonalModel>().Insert(new EventoPersonalModel
            {
                EventoId = _eventoActual.Id,
                UsuarioId = mesero1Id,
                Puesto = "Mesero 1",
                Rol = PickerRol1.SelectedItem?.ToString(),
                Enable = true
            });

            // Insertar Mesero 2
            await _supabaseClient.From<EventoPersonalModel>().Insert(new EventoPersonalModel
            {
                EventoId = _eventoActual.Id,
                UsuarioId = mesero2Id,
                Puesto = "Mesero 2",
                Rol = PickerRol2.SelectedItem?.ToString(),
                Enable = true
            });

            // Insertar Mesero 3
            await _supabaseClient.From<EventoPersonalModel>().Insert(new EventoPersonalModel
            {
                EventoId = _eventoActual.Id,
                UsuarioId = mesero3Id,
                Puesto = "Mesero 3",
                Rol = PickerRol3.SelectedItem?.ToString(),
                Enable = true
            });

            // Insertar Mesero 4 (Si aplica)
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

            // Actualizamos el estado del botón
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

    // ==========================================
    // LÓGICA DE COMENTARIOS / OBSERVACIONES
    // ==========================================
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

            decimal totalAbonado = HistorialModalAbonos.Sum(a => a.Monto);
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

    // ==========================================
    // LÓGICA DE REALIZAR PAGO / ABONOS
    // ==========================================
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

            decimal totalAbonado = listaAbonos.Sum(a => a.Monto);
            decimal costoTotal = _eventoActual.precio;
            decimal anticipo = _eventoActual.anticipo;

            _saldoPendienteModalActual = _eventoActual.Saldo;

            LblModalCostoTotal.Text = $"${costoTotal:F2}";
            LblModalAnticipo.Text = $"${anticipo:F2}";
            LblModalAbonosAcumulados.Text = $"${totalAbonado:F2}";
            LblModalSaldoPendiente.Text = $"${_saldoPendienteModalActual:F2}";

            TxtModalMontoAbono.Text = string.Empty;
            PickerModalMetodo.SelectedIndex = -1;

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

        if (montoNuevo > _saldoPendienteModalActual)
        {
            await DisplayAlert("Monto excedido", $"No puedes abonar más del saldo pendiente (${_saldoPendienteModalActual:F2}).", "Aceptar");
            return;
        }

        _isProcessing = true;

        try
        {
            var nuevoAbono = new AbonoModel
            {
                EventoId = _eventoActual.Id,
                Monto = montoNuevo,
                FechaPago = DateTime.Now,
                MetodoPago = PickerModalMetodo.SelectedItem.ToString(),
                Enable = true
            };

            await _supabaseClient.From<AbonoModel>().Insert(nuevoAbono);

            decimal nuevoSaldoRestante = _saldoPendienteModalActual - montoNuevo;
            if (nuevoSaldoRestante < 0) nuevoSaldoRestante = 0;

            if (nuevoSaldoRestante <= 0)
            {
                _eventoActual.estatus = "Liquidado";
                _eventoActual.Saldo = 0;

                await _supabaseClient.From<EventoModel>()
                    .Where(x => x.Id == _eventoActual.Id)
                    .Set(x => x.estatus, "Liquidado")
                    .Set(x => x.Saldo, 0)
                    .Update();

                await DisplayAlert("¡Cuenta Liquidada!", "El pago se ha registrado y el evento está totalmente pagado.", "Aceptar");

                ModalAbonos.IsVisible = false;
                PanelDetalle.IsVisible = false;
                PanelLista.IsVisible = true;
                await CargarEventosAsync();
            }
            else
            {
                _eventoActual.Saldo = nuevoSaldoRestante;

                await _supabaseClient.From<EventoModel>()
                    .Where(x => x.Id == _eventoActual.Id)
                    .Set(x => x.Saldo, nuevoSaldoRestante)
                    .Update();

                await DisplayAlert("Abono Exitoso", $"Se ha registrado el pago de ${montoNuevo:F2} correctamente.", "Aceptar");

                OnRealizarPagoClicked(sender, e);

                decimal nuevoTotalAbonado = HistorialModalAbonos.Sum(a => a.Monto);
                PintarDetallesEvento(_eventoActual, nuevoTotalAbonado);
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

    // ==========================================
    // FILTROS Y BÚSQUEDA GENERAL
    // ==========================================
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
}

// Clases auxiliares locales para mapeo rápido en vistas
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