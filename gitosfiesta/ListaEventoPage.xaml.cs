using gitosfiesta.Models;
using System.Collections.ObjectModel;

namespace gitosfiesta;

public partial class ListaEventoPage : ContentPage
{
    private readonly Supabase.Client _supabaseClient;
    private List<EventoModel> _todosLosEventos = new();
    private EventoModel? _eventoActual; // Variable para rastrear el evento abierto actualmente
    private bool _isInitialized = false;

    public ObservableCollection<EventoModel> EventosFiltrados { get; set; } = new();
    public ObservableCollection<AbonoModel> HistorialModalAbonos { get; set; } = new();

    private decimal _saldoPendienteModalActual = 0;

    public ListaEventoPage(Supabase.Client supabaseClient)
    {
        InitializeComponent();
        _supabaseClient = supabaseClient;
        ColeccionEventos.ItemsSource = EventosFiltrados;
        ColeccionModalHistorial.ItemsSource = HistorialModalAbonos;

        DateTime hoy = DateTime.Today;
        DatePickerDesde.Date = hoy.AddDays(-15);
        DatePickerHasta.Date = hoy.AddDays(15);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (PickerFiltro != null) PickerFiltro.SelectedIndex = 2; // Todos
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

    // Al hacer clic en un evento: Carga la info, consulta sus abonos, oculta los filtros y muestra el detalle
    private async void OnEventoSelected(object sender, SelectionChangedEventArgs e)
    {
        var evento = e.CurrentSelection.FirstOrDefault() as EventoModel;
        if (evento == null) return;

        // Limpiar la selección para permitir clics repetidos
        ((CollectionView)sender).SelectedItem = null;

        // Guardamos la referencia del evento actual
        _eventoActual = evento;

        // Consultar los abonos de este evento para calcular la suma total acumulada
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

        PintarDetallesEvento(evento, totalAbonado);

        // Ocultar la lista/filtros y mostrar el panel de detalles
        PanelLista.IsVisible = false;
        PanelDetalle.IsVisible = true;
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

        // Mostrar la suma total de abonos en la tarjeta
        LblDetalleAbonos.Text = $"${totalAbonado:F2}";

        LblDetalleSaldo.Text = $"${evento.Saldo:F2}";

        LblDetallePromocion.Text = string.IsNullOrEmpty(evento.promocion) ? "Ninguna" : evento.promocion;
        LblDetalleObservaciones.Text = string.IsNullOrEmpty(evento.observaciones) ? "Ninguna" : evento.observaciones;
    }

    // Botón para regresar a los filtros y la lista
    private void OnRegresarClicked(object sender, EventArgs e)
    {
        PanelDetalle.IsVisible = false;
        PanelLista.IsVisible = true;
        _eventoActual = null;
        CargarEventosAsync(); // Refrescar lista por si hubo cambios
    }

    private async void OnAsignarPersonalClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Asignar Personal", "Aquí se abrirá la vista para asignar personal al evento.", "Aceptar");
    }

    // ==========================================
    // LÓGICA DE COMENTARIOS / OBSERVACIONES
    // ==========================================
    private void OnAgregarComentariosClicked(object sender, EventArgs e)
    {
        if (_eventoActual == null) return;

        // Cargar el texto actual en el Editor multilínea
        TxtModalComentarios.Text = _eventoActual.observaciones ?? string.Empty;

        // Mostrar el modal
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

            // Recalcular el total de abonos para mantener la vista actualizada
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

        try
        {
            // 1. Registrar el nuevo abono en la tabla de abonos
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
            decimal nuevoSaldoRestante = _saldoPendienteModalActual - montoNuevo;

            // 3. Actualizar estatus y saldo en Supabase
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

                OnRealizarPagoClicked(sender, e); // Recargar datos del modal

                // Recalcular la suma total de abonos actualizada y reflejarla en la tarjeta principal
                decimal nuevoTotalAbonado = HistorialModalAbonos.Sum(a => a.Monto);
                PintarDetallesEvento(_eventoActual, nuevoTotalAbonado);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error al guardar", $"Hubo un problema registrando el pago: {ex.Message}", "Aceptar");
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