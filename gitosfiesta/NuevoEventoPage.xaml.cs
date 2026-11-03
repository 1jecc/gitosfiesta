using gitosfiesta.Models;
using System.Text.RegularExpressions;

namespace gitosfiesta;

public partial class NuevoEventoPage : ContentPage
{
    private readonly Supabase.Client? _supabaseClient;
    private List<MiscModel> _catalogoMisc = new();
    private bool _esFinDeSemana = true;
    private bool _horaEntreSemanaSeleccionada = false;
    private bool _inicializado = false;
    private decimal _precioBasePaquete = 0;
    private decimal _descuentoPromocion = 0;

    private List<string> _extrasSeleccionados = new();
    private readonly long? _eventoId;

    public NuevoEventoPage(Supabase.Client supabaseClient) : this(supabaseClient, null)
    {
    }

    public NuevoEventoPage(Supabase.Client supabaseClient, long? eventoId)
    {
        // Ocultar la barra de navegación ANTES de inicializar para evitar el parpadeo o que persista la franja
        NavigationPage.SetHasNavigationBar(this, false);

        InitializeComponent();

        _supabaseClient = supabaseClient;
        _eventoId = eventoId;

        // RESTRICCIÓN: Definir MinimumDate antes de asignar la fecha inicial
        if (!_eventoId.HasValue)
        {
            DatePickerFecha.MinimumDate = DateTime.Today.AddDays(15);
            DatePickerFecha.Date = DateTime.Today.AddDays(15);
        }
        else
        {
            DatePickerFecha.MinimumDate = DateTime.Today;

            // Ocultar la sección de saldo pendiente en modo modificación
            if (ContainerSaldoPendiente != null)
            {
                ContainerSaldoPendiente.IsVisible = false;
            }
        }

        TxtNombreCliente.TextChanged += OnNombreClienteTextChanged;
        TxtTelefono.TextChanged += OnTelefonoTextChanged;

        DatePickerFecha.DateSelected += OnFechaSelected;

        if (_eventoId.HasValue)
        {
            Title = $"Actualizar / Folio #{_eventoId.Value}";
            BtnAccionEvento.Text = "Guardar Cambios del Evento";
        }
        else
        {
            Title = "Nuevo Evento";
            BtnAccionEvento.Text = "Guardar y Registrar Evento";
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_eventoId.HasValue && _inicializado)
        {
            LimpiarFormularioCompleto();
        }

        if (!_inicializado)
        {
            await CargarDatosInicialesAsync(); // <--- Cambia _asyncCargaInicial por esto

            if (_eventoId.HasValue)
            {
                await CargarDatosEventoPorIdAsync(_eventoId.Value);
            }
            else
            {
                LimpiarFormularioCompleto();
            }

            _inicializado = true;
        }
    }
    private async Task ActualizarControlHorarioSegunFechaAsync(DateTime fecha)
    {
        _esFinDeSemana = fecha.DayOfWeek == DayOfWeek.Saturday || fecha.DayOfWeek == DayOfWeek.Sunday;

        List<EventoModel> eventosEnFecha = new();
        if (_supabaseClient != null)
        {
            string fechaStr = fecha.ToString("yyyy-MM-dd");
            var query = await _supabaseClient
                .From<EventoModel>()
                .Filter("fecha_evento", Supabase.Postgrest.Constants.Operator.Equals, fechaStr)
                .Get();

            eventosEnFecha = query.Models ?? new List<EventoModel>();

            if (_eventoId.HasValue)
            {
                eventosEnFecha = eventosEnFecha.Where(x => x.Id != _eventoId.Value).ToList();
            }
        }

        if (!_esFinDeSemana)
        {
            PickerHorario.IsVisible = false;
            PickerHorario.IsEnabled = false;
            PickerHorarioEntreSemana.IsVisible = true;

            var horasDisponibles = GenerarHorarios30Minutos();
            PickerHorarioEntreSemana.ItemsSource = horasDisponibles;
            PickerHorarioEntreSemana.SelectedIndex = -1;

            if (eventosEnFecha.Count > 0)
            {
                PickerHorarioEntreSemana.IsEnabled = false;
                _horaEntreSemanaSeleccionada = false;
                await DisplayAlert("Día Ocupado", "Este día entre semana ya cuenta con un evento registrado. Selecciona otra fecha.", "Aceptar");
            }
            else
            {
                PickerHorarioEntreSemana.IsEnabled = true;
                _horaEntreSemanaSeleccionada = false;
            }
        }
        else
        {
            PickerHorario.IsVisible = true;
            PickerHorarioEntreSemana.IsVisible = false;
            PickerHorarioEntreSemana.IsEnabled = false;

            var horariosFinSemana = _catalogoMisc
                .Where(x => x.categoria == "HORARIO" && x.value2 == "FIN_SEMANA")
                .Select(x => x.value1)
                .ToList();

            var horasOcupadas = eventosEnFecha.Select(e => e.hora_evento.Hours).ToList();

            var horariosDisponibles = new List<string>();
            foreach (var h in horariosFinSemana)
            {
                bool esMatutino = h.Contains("2:00");
                int horaEnInt = esMatutino ? 14 : 18;

                if (!horasOcupadas.Contains(horaEnInt))
                {
                    horariosDisponibles.Add(h);
                }
            }

            PickerHorario.ItemsSource = horariosDisponibles;
            PickerHorario.SelectedIndex = -1;

            if (horariosDisponibles.Count == 0)
            {
                PickerHorario.IsEnabled = false;
                await DisplayAlert("Día Ocupado", "Los dos horarios de este fin de semana ya se encuentran ocupados.", "Aceptar");
            }
            else
            {
                PickerHorario.IsEnabled = true;
            }
        }

        EvaluarElegibilidadPromocion();
    }


    private List<string> GenerarHorarios30Minutos()
    {
        var lista = new List<string>();
        for (int hora = 8; hora < 22; hora++)
        {
            for (int min = 0; min < 60; min += 30)
            {
                DateTime dt = new DateTime(1, 1, 1, hora, min, 0);
                lista.Add(dt.ToString("hh:mm tt"));
            }
        }
        return lista;
    }

    private void OnHorarioEntreSemanaSelectedIndexChanged(object sender, EventArgs e)
    {
        if (PickerHorarioEntreSemana.SelectedIndex == -1)
        {
            _horaEntreSemanaSeleccionada = false;
            return;
        }

        _horaEntreSemanaSeleccionada = true;
        HabilitarPaquetes();
    }

    private void LimpiarFormularioCompleto()
    {
        if (_eventoId.HasValue) return;

        DatePickerFecha.DateSelected -= OnFechaSelected;

        TxtNombreCliente.Text = string.Empty;
        TxtTelefono.Text = string.Empty;
        TxtTipoEvento.Text = string.Empty;
        TxtObservaciones.Text = string.Empty;
        TxtAnticipo.Text = "1000";
        _precioBasePaquete = 0;
        _descuentoPromocion = 0;
        _extrasSeleccionados.Clear();

        DatePickerFecha.MinimumDate = DateTime.Today.AddDays(15);
        DatePickerFecha.Date = DateTime.Today.AddDays(15);

        if (ChkShowAnimador != null) ChkShowAnimador.IsChecked = false;
        if (ChkDecoracionDeluxe != null) ChkDecoracionDeluxe.IsChecked = false;
        if (ChkBotargaExtra != null) ChkBotargaExtra.IsChecked = false;
        if (ChkShowPayaso != null) ChkShowPayaso.IsChecked = false;
        if (ChkHoraExtra != null) ChkHoraExtra.IsChecked = false;
        if (ChkRentaHoraExtra != null) ChkRentaHoraExtra.IsChecked = false;

        _horaEntreSemanaSeleccionada = false;
        PickerHorario.SelectedIndex = -1;
        PickerHorario.ItemsSource = null;
        PickerHorario.IsEnabled = false;

        PickerHorarioEntreSemana.SelectedIndex = -1;
        PickerHorarioEntreSemana.ItemsSource = null;
        PickerHorarioEntreSemana.IsEnabled = false;

        PickerPaquete.ItemsSource = null;
        PickerPaquete.IsEnabled = false;
        PickerPaquete.SelectedIndex = -1;

        if (PickerPromocion != null)
        {
            PickerPromocion.ItemsSource = null;
            PickerPromocion.IsEnabled = false;
            PickerPromocion.SelectedIndex = -1;
        }

        BloquearPasosDesdePaquete();

        DatePickerFecha.DateSelected += OnFechaSelected;
    }

    private async Task CargarDatosInicialesAsync()
    {
        try
        {
            if (_supabaseClient == null) return;
            if (_catalogoMisc != null && _catalogoMisc.Count > 0) return;

            var response = await _supabaseClient
                .From<MiscModel>()
                .Where(x => x.enable == true)
                .Get();

            _catalogoMisc = response.Models ?? new List<MiscModel>();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudieron cargar los catálogos: {ex.Message}", "Aceptar");
        }
    }

    private void ConstruirControlesExtras(List<string> extrasPreSeleccionados)
    {
        _extrasSeleccionados = new List<string>(extrasPreSeleccionados ?? new List<string>());

        if (ChkShowAnimador != null) ChkShowAnimador.IsChecked = _extrasSeleccionados.Contains("Show (1 animador y 1 botarga)");
        if (ChkDecoracionDeluxe != null) ChkDecoracionDeluxe.IsChecked = _extrasSeleccionados.Contains("Decoración Deluxe");
        if (ChkBotargaExtra != null) ChkBotargaExtra.IsChecked = _extrasSeleccionados.Contains("Botarga extra");
        if (ChkShowPayaso != null) ChkShowPayaso.IsChecked = _extrasSeleccionados.Contains("Show de payaso");
        if (ChkHoraExtra != null) ChkHoraExtra.IsChecked = _extrasSeleccionados.Contains("Hora extra");
        if (ChkRentaHoraExtra != null) ChkRentaHoraExtra.IsChecked = _extrasSeleccionados.Contains("Renta de Hora Extra (Previa o Posterior)");
    }

    private void OnExtraCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.Parent is HorizontalStackLayout layout)
        {
            var label = layout.Children.FirstOrDefault(c => c is Label) as Label;
            if (label != null)
            {
                string nombreExtra = label.Text;
                if (e.Value)
                {
                    if (!_extrasSeleccionados.Contains(nombreExtra))
                        _extrasSeleccionados.Add(nombreExtra);
                }
                else
                {
                    if (_extrasSeleccionados.Contains(nombreExtra))
                        _extrasSeleccionados.Remove(nombreExtra);
                }
                CalcularPrecioTotalConExtras();
            }
        }
    }

    private async Task CargarDatosEventoPorIdAsync(long idEvento)
    {
        try
        {
            if (_supabaseClient == null) return;

            var response = await _supabaseClient
                .From<EventoModel>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, idEvento)
                .Get();

            var evento = response.Models?.FirstOrDefault();
            if (evento == null)
            {
                await DisplayAlert("Aviso", "No se encontró el evento con el folio especificado.", "Aceptar");
                return;
            }

            // 1. Datos Generales
            TxtNombreCliente.Text = evento.nombre_cliente;
            TxtTelefono.Text = evento.telefono_cliente;
            TxtTipoEvento.Text = evento.tipo_evento;
            DatePickerFecha.MinimumDate = DateTime.Today;
            DatePickerFecha.Date = evento.fecha_evento;
            TxtAnticipo.Text = evento.anticipo.ToString();
            TxtObservaciones.Text = evento.observaciones;

            // 2. Horarios y Fechas (Fin de semana o Entre semana)
            await ActualizarControlHorarioSegunFechaAsync(evento.fecha_evento);

            if (_esFinDeSemana)
            {
                PickerHorario.SelectedItem = evento.hora_evento.Hours == 14 ? "2:00 pm a 5:30 pm" : "6:30 pm a 10:00 pm";
            }
            else
            {
                DateTime dtTemp = new DateTime(1, 1, 1, evento.hora_evento.Hours, evento.hora_evento.Minutes, 0);
                string horaBuscada = dtTemp.ToString("hh:mm tt");
                PickerHorarioEntreSemana.SelectedItem = PickerHorarioEntreSemana.ItemsSource?
                    .Cast<string>()
                    .FirstOrDefault(h => h.Equals(horaBuscada, StringComparison.OrdinalIgnoreCase));

                _horaEntreSemanaSeleccionada = true;
            }

            HabilitarPaquetes();

            // 3. Seleccionar Paquete de forma exacta
            if (!string.IsNullOrEmpty(evento.paquete))
            {
                string paqueteBD = evento.paquete.Trim();
                PickerPaquete.SelectedItem = PickerPaquete.ItemsSource?
                    .Cast<string>()
                    .FirstOrDefault(p => p.Contains(paqueteBD, StringComparison.OrdinalIgnoreCase));
            }

            // 4. Seleccionar Platillo
            if (!string.IsNullOrEmpty(evento.menu))
            {
                var partesMenu = evento.menu.Split('|');
                string platilloBuscado = partesMenu.Length > 1
                    ? partesMenu[1].Replace("Platillo:", "").Trim()
                    : partesMenu[0].Trim();

                PickerPlatillo.SelectedItem = PickerPlatillo.ItemsSource?
                    .Cast<string>()
                    .FirstOrDefault(pl => pl.Equals(platilloBuscado, StringComparison.OrdinalIgnoreCase));
            }

            // 5. Promociones y Extras
            List<string> extrasPreSeleccionados = new();
            if (!string.IsNullOrEmpty(evento.promocion))
            {
                if (evento.promocion.Contains("Extra:"))
                {
                    var partesPromo = evento.promocion.Split('|');
                    foreach (var parte in partesPromo)
                    {
                        if (parte.Contains("Extra:"))
                        {
                            string listaExtras = parte.Replace("Extra:", "").Trim();
                            extrasPreSeleccionados = listaExtras.Split(',').Select(x => x.Trim()).ToList();
                        }
                        else if (PickerPromocion != null && PickerPromocion.IsEnabled)
                        {
                            string promoBuscada = parte.Trim();
                            PickerPromocion.SelectedItem = PickerPromocion.ItemsSource?
                                .Cast<string>()
                                .FirstOrDefault(pr => pr.Equals(promoBuscada, StringComparison.OrdinalIgnoreCase));
                        }
                    }
                }
                else if (PickerPromocion != null && PickerPromocion.IsEnabled)
                {
                    string promoBuscada = evento.promocion.Trim();
                    PickerPromocion.SelectedItem = PickerPromocion.ItemsSource?
                        .Cast<string>()
                        .FirstOrDefault(pr => pr.Equals(promoBuscada, StringComparison.OrdinalIgnoreCase));
                }
            }

            // Marcar los checkboxes de los extras que tenía guardados
            ConstruirControlesExtras(extrasPreSeleccionados);

            // 6. Refrescar el cálculo total para que la vista refleje los montos correctos inmediatamente
            CalcularPrecioTotalConExtras();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudieron cargar los datos del evento: {ex.Message}", "Aceptar");
        }
    }

    private async void OnFechaSelected(object sender, DateChangedEventArgs e)
    {
        await ActualizarControlHorarioSegunFechaAsync(e.NewDate);
        BloquearPasosDesdeHorario();
    }

    private void OnHorarioSelectedIndexChanged(object sender, EventArgs e)
    {
        if (PickerHorario.SelectedIndex == -1 && _esFinDeSemana) return;
        HabilitarPaquetes();
    }

    private void HabilitarPaquetes()
    {
        var paquetes = _catalogoMisc
            .Where(x => x.categoria == "PAQUETE")
            .Select(x => x.value1)
            .ToList();

        paquetes.Insert(0, "Selecciona un paquete...");

        PickerPaquete.ItemsSource = paquetes;
        PickerPaquete.IsEnabled = true;

        if (!_eventoId.HasValue)
            PickerPaquete.SelectedIndex = 0;

        BloquearPasosDesdePaquete();
    }

    private void OnPaqueteSelectedIndexChanged(object sender, EventArgs e)
    {
        string paqueteSeleccionado = PickerPaquete.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(paqueteSeleccionado) || paqueteSeleccionado.Contains("Selecciona")) return;

        Match matchPersona = Regex.Match(paqueteSeleccionado, @"\d+");
        if (matchPersona.Success)
        {
            TxtNumeroPersonas.Text = matchPersona.Value;
        }

        var infoPaquete = _catalogoMisc
            .FirstOrDefault(x => x.categoria == "PAQUETE" && x.value1 == paqueteSeleccionado);

        if (infoPaquete != null && decimal.TryParse(infoPaquete.value2, out decimal precioSugerido))
        {
            _precioBasePaquete = precioSugerido;
            CalcularPrecioTotalConExtras();
        }

        string tipoFiltroPaquete = paqueteSeleccionado.Contains("OREO", StringComparison.OrdinalIgnoreCase) ? "PAQUETE OREO" : "PAQUETE COMBINADO";

        var platillosDisponibles = _catalogoMisc
            .Where(x => x.categoria == "OPCION_PLATILLO" && x.value2 == tipoFiltroPaquete)
            .Select(x => x.value1)
            .ToList();

        PickerPlatillo.ItemsSource = platillosDisponibles;
        PickerPlatillo.IsEnabled = true;

        if (!_eventoId.HasValue)
            PickerPlatillo.SelectedIndex = -1;

        EvaluarElegibilidadPromocion();
    }

    private void EvaluarElegibilidadPromocion()
    {
        if (PickerPromocion == null) return;

        bool esEntreSemana = !_esFinDeSemana;
        int.TryParse(TxtNumeroPersonas.Text, out int numPersonas);

        bool aplicaPromocion = esEntreSemana && numPersonas > 40;

        if (LayoutPromocion != null)
        {
            LayoutPromocion.IsVisible = aplicaPromocion;
        }

        if (aplicaPromocion)
        {
            var promocionesDisponibles = _catalogoMisc
                .Where(x => x.categoria == "PROMOCIONES")
                .Select(x => x.value1)
                .ToList();

            PickerPromocion.ItemsSource = promocionesDisponibles;
            PickerPromocion.IsEnabled = true;
        }
        else
        {
            PickerPromocion.ItemsSource = null;
            PickerPromocion.SelectedIndex = -1;
            PickerPromocion.IsEnabled = false;
            _descuentoPromocion = 0;
            CalcularPrecioTotalConExtras();
        }
    }

    private void OnPromocionSelectedIndexChanged(object sender, EventArgs e)
    {
        _descuentoPromocion = 0;

        if (PickerPromocion?.SelectedItem != null)
        {
            string promoSeleccionada = PickerPromocion.SelectedItem.ToString() ?? "";
            var infoPromo = _catalogoMisc
                .FirstOrDefault(x => x.categoria == "PROMOCIONES" && x.value1 == promoSeleccionada);

            if (infoPromo != null && decimal.TryParse(infoPromo.value2, out decimal valorDescuento))
            {
                _descuentoPromocion = valorDescuento;
            }
        }

        CalcularPrecioTotalConExtras();
    }

    private void CalcularPrecioTotalConExtras()
    {
        decimal precioTotal = _precioBasePaquete;

        foreach (var extra in _extrasSeleccionados)
        {
            var infoExtra = _catalogoMisc
                .FirstOrDefault(x => x.categoria == "EXTRA" && x.value1 == extra);

            if (infoExtra != null && decimal.TryParse(infoExtra.value2, out decimal costoExtra))
            {
                precioTotal += costoExtra;
            }
        }

        if (PickerPromocion != null && PickerPromocion.IsEnabled && PickerPromocion.SelectedIndex != -1)
        {
            precioTotal -= _descuentoPromocion;
        }

        if (precioTotal < 0) precioTotal = 0;

        TxtPrecio.Text = precioTotal.ToString("F2");
        CalcularSaldo();
    }

    private async void OnVerTerminosClicked(object sender, EventArgs e)
    {
        try
        {
            // Consultamos directamente usando tu MiscModel y filtrando por categoria
            var response = await _supabaseClient!
                .From<MiscModel>()
                .Where(x => x.categoria == "TERMINO_CONTRATO" && x.enable == true)
                .Get();

            var terminos = response.Models?
                .OrderBy(x => int.TryParse(x.value1, out int num) ? num : 0)
                .ToList();

            if (terminos == null || terminos.Count == 0)
            {
                await DisplayAlert("Aviso", "No se encontraron términos registrados en el catálogo.", "Aceptar");
                return;
            }

            // Abrimos la nueva página de diseño elegante pasando la lista de MiscModel
            await Navigation.PushAsync(new TerminosPage(terminos));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudieron cargar los términos: {ex.Message}", "Aceptar");
        }
    }
    private void OnNombreClienteTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is Entry entry)
        {
            string textoActual = e.NewTextValue ?? "";
            string textoFiltrado = Regex.Replace(textoActual, @"[^a-zA-ZÁÉÍÓÚáéíóúÑñÜü\s]", "");
            textoFiltrado = Regex.Replace(textoFiltrado, @"\s+", " ");

            if (textoFiltrado != textoActual)
            {
                entry.Text = textoFiltrado;
                return;
            }
        }
    }

    private void OnTelefonoTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue)) return;
        string textoFiltrado = new string(e.NewTextValue.Where(char.IsDigit).ToArray());
        if (e.NewTextValue != textoFiltrado) TxtTelefono.Text = textoFiltrado;
    }

    private void BloquearPasosDesdeHorario()
    {
        if (!_eventoId.HasValue)
        {
            PickerPaquete.ItemsSource = null;
            PickerPaquete.IsEnabled = false;
        }
        BloquearPasosDesdePaquete();
    }

    private void BloquearPasosDesdePaquete()
    {
        if (!_eventoId.HasValue)
        {
            TxtNumeroPersonas.Text = string.Empty;
            _precioBasePaquete = 0;
            _descuentoPromocion = 0;
            TxtPrecio.Text = "$0.00";
            LblSaldo.Text = "$0.00";
            PickerPlatillo.ItemsSource = null;
            PickerPlatillo.IsEnabled = false;

            if (PickerPromocion != null)
            {
                PickerPromocion.ItemsSource = null;
                PickerPromocion.IsEnabled = false;
                PickerPromocion.SelectedIndex = -1;
            }
        }
    }

    private void OnPrecioAnticipoChanged(object sender, TextChangedEventArgs e)
    {
        CalcularSaldo();
    }

    private void CalcularSaldo()
    {
        if (decimal.TryParse(TxtPrecio.Text, out decimal precio) &&
            decimal.TryParse(TxtAnticipo.Text, out decimal anticipo))
        {
            decimal saldo = precio - anticipo;
            LblSaldo.Text = $"${saldo:F2}";
        }
        else
        {
            LblSaldo.Text = "$0.00";
        }
    }
    public void LimpiarFormularioDespuesDeExito()
    {
        // Permitimos limpiar incluso si _eventoId es nulo (modo nuevo evento)
        DatePickerFecha.DateSelected -= OnFechaSelected;

        TxtNombreCliente.Text = string.Empty;
        TxtTelefono.Text = string.Empty;
        TxtTipoEvento.Text = string.Empty;
        TxtObservaciones.Text = string.Empty;
        TxtAnticipo.Text = "1000";
        _precioBasePaquete = 0;
        _descuentoPromocion = 0;
        _extrasSeleccionados.Clear();

        DatePickerFecha.MinimumDate = DateTime.Today.AddDays(15);
        DatePickerFecha.Date = DateTime.Today.AddDays(15);

        if (ChkShowAnimador != null) ChkShowAnimador.IsChecked = false;
        if (ChkDecoracionDeluxe != null) ChkDecoracionDeluxe.IsChecked = false;
        if (ChkBotargaExtra != null) ChkBotargaExtra.IsChecked = false;
        if (ChkShowPayaso != null) ChkShowPayaso.IsChecked = false;
        if (ChkHoraExtra != null) ChkHoraExtra.IsChecked = false;
        if (ChkRentaHoraExtra != null) ChkRentaHoraExtra.IsChecked = false;

        _horaEntreSemanaSeleccionada = false;
        PickerHorario.SelectedIndex = -1;
        PickerHorario.ItemsSource = null;
        PickerHorario.IsEnabled = false;

        PickerHorarioEntreSemana.SelectedIndex = -1;
        PickerHorarioEntreSemana.ItemsSource = null;
        PickerHorarioEntreSemana.IsEnabled = false;

        PickerPaquete.ItemsSource = null;
        PickerPaquete.IsEnabled = false;
        PickerPaquete.SelectedIndex = -1;

        if (PickerPromocion != null)
        {
            PickerPromocion.ItemsSource = null;
            PickerPromocion.IsEnabled = false;
            PickerPromocion.SelectedIndex = -1;
        }

        BloquearPasosDesdePaquete();

        DatePickerFecha.DateSelected += OnFechaSelected;
    }
    private async void OnGuardarEventoClicked(object sender, EventArgs e)
    {
        if (!_eventoId.HasValue && DatePickerFecha.Date < DateTime.Today.AddDays(15))
        {
            await DisplayAlert("Plazo inválido", "Los eventos deben registrarse con un mínimo de 15 días de anticipación a partir de hoy.", "Aceptar");
            return;
        }

        bool horarioValido = _esFinDeSemana ? (PickerHorario.SelectedIndex != -1) : _horaEntreSemanaSeleccionada;

        if (string.IsNullOrWhiteSpace(TxtNombreCliente.Text) ||
            string.IsNullOrWhiteSpace(TxtTelefono.Text) ||
            string.IsNullOrWhiteSpace(TxtTipoEvento.Text) ||
            !horarioValido ||
            PickerPaquete.SelectedIndex == -1 ||
            PickerPlatillo.SelectedIndex == -1)
        {
            await DisplayAlert("Campos requeridos", "Por favor completa la selección en orden y asegúrate de elegir la hora del evento entre semana.", "Aceptar");
            return;
        }

        string telefonoLimpio = TxtTelefono.Text.Trim();
        if (telefonoLimpio.Length != 10 || !telefonoLimpio.All(char.IsDigit))
        {
            await DisplayAlert("Teléfono inválido", "El número de teléfono debe contener exactamente 10 dígitos numéricos.", "Aceptar");
            return;
        }

        try
        {
            decimal.TryParse(TxtPrecio.Text, out decimal precio);
            decimal.TryParse(TxtAnticipo.Text, out decimal anticipo);
            int.TryParse(TxtNumeroPersonas.Text, out int numPersonas);

            TimeSpan horaFinalEvento;
            if (_esFinDeSemana && PickerHorario.SelectedItem != null)
            {
                string bloque = PickerHorario.SelectedItem.ToString() ?? "";
                horaFinalEvento = bloque.Contains("2:00") ? TimeSpan.FromHours(14) : TimeSpan.FromHours(18.5);
            }
            else if (!_esFinDeSemana && PickerHorarioEntreSemana.SelectedItem != null)
            {
                string horaTexto = PickerHorarioEntreSemana.SelectedItem.ToString() ?? "";
                if (DateTime.TryParse(horaTexto, out DateTime dtParsed))
                {
                    horaFinalEvento = dtParsed.TimeOfDay;
                }
                else
                {
                    horaFinalEvento = new TimeSpan(12, 0, 0);
                }
            }
            else
            {
                horaFinalEvento = new TimeSpan(12, 0, 0);
            }

            string paqueteSeleccionado = PickerPaquete.SelectedItem?.ToString() ?? "";
            string nombrePaqueteSolo = paqueteSeleccionado.Split('-')[0].Trim();

            string platilloSeleccionado = PickerPlatillo.SelectedItem?.ToString() ?? "";
            string menuFormateado = $"{platilloSeleccionado}";

            string promoSeleccionada = PickerPromocion?.SelectedItem?.ToString() ?? "";
            string extrasCadena = string.Join(", ", _extrasSeleccionados);

            string promocionFinal = "";
            if (!string.IsNullOrWhiteSpace(promoSeleccionada) && !string.IsNullOrWhiteSpace(extrasCadena))
                promocionFinal = $"{promoSeleccionada} | Extra: {extrasCadena}";
            else if (!string.IsNullOrWhiteSpace(promoSeleccionada))
                promocionFinal = promoSeleccionada;
            else if (!string.IsNullOrWhiteSpace(extrasCadena))
                promocionFinal = $"Extra: {extrasCadena}";

            // =========================================================================
            // MODO ACTUALIZACIÓN (MODIFICAR EVENTO EXISTENTE SIN PASAR POR FIRMA)
            // =========================================================================
            if (_eventoId.HasValue)
            {
                // 1. Consultar abonos registrados previamente para este evento
                var responseAbonos = await _supabaseClient!
                    .From<AbonoModel>()
                    .Filter("evento_id", Supabase.Postgrest.Constants.Operator.Equals, _eventoId.Value)
                    .Get();

                var listaAbonos = responseAbonos.Models ?? new List<AbonoModel>();
                decimal totalAbonado = (decimal)listaAbonos.Sum(a => a.Monto);

                // 2. Recalcular el saldo real restando los abonos al nuevo precio actualizado
                decimal nuevoSaldo = precio - totalAbonado;
                if (nuevoSaldo < 0) nuevoSaldo = 0;

                // 3. Determinar el estatus automáticamente según el saldo resultante
                string nuevoEstatus = nuevoSaldo <= 0 ? "Liquidado" : "Abierto y por pagar";

                // 4. Actualizar en Supabase de forma explícita
                await _supabaseClient!
                    .From<EventoModel>()
                    .Where(x => x.Id == _eventoId.Value)
                    .Set(x => x.nombre_cliente, TxtNombreCliente.Text.Trim())
                    .Set(x => x.telefono_cliente, telefonoLimpio)
                    .Set(x => x.tipo_evento, TxtTipoEvento.Text.Trim())
                    .Set(x => x.fecha_evento, DatePickerFecha.Date)
                    .Set(x => x.hora_evento, horaFinalEvento)
                    .Set(x => x.numero_personas, numPersonas)
                    .Set(x => x.paquete, nombrePaqueteSolo)
                    .Set(x => x.menu, menuFormateado)
                    .Set(x => x.precio, precio)
                    .Set(x => x.anticipo, anticipo)
                    .Set(x => x.Saldo, nuevoSaldo)
                    .Set(x => x.estatus, nuevoEstatus)
                    .Set(x => x.promocion, string.IsNullOrWhiteSpace(promocionFinal) ? null : promocionFinal)
                    .Set(x => x.observaciones, TxtObservaciones.Text?.Trim())
                    .Update();

                await DisplayAlert("¡Éxito!", $"El evento ha sido actualizado correctamente.\nEstatus: {nuevoEstatus}", "Aceptar");

                // Regresamos a la pantalla anterior sin pedir firma de contrato
                await Navigation.PopAsync();
            }
            // =========================================================================
            // MODO NUEVO EVENTO (REGISTRO INICIAL)
            // =========================================================================
            else
            {
                decimal saldoInicial = precio - anticipo;

                var nuevoEvento = new EventoModel
                {
                    fecha_contrato = DateTime.Now,
                    nombre_cliente = TxtNombreCliente.Text.Trim(),
                    telefono_cliente = telefonoLimpio,
                    tipo_evento = TxtTipoEvento.Text.Trim(),
                    fecha_evento = DatePickerFecha.Date,
                    hora_evento = horaFinalEvento,
                    numero_personas = numPersonas,
                    paquete = nombrePaqueteSolo,
                    menu = menuFormateado,
                    precio = precio,
                    anticipo = anticipo,
                    Saldo = saldoInicial,
                    promocion = string.IsNullOrWhiteSpace(promocionFinal) ? null : promocionFinal,
                    observaciones = TxtObservaciones.Text?.Trim(),
                    estatus = "Abierto y por pagar",
                    usuario_creacion_id = SessionContext.UsuarioId,
                    enable = true
                };

                var response = await _supabaseClient!
                    .From<EventoModel>()
                    .Insert(nuevoEvento, new Supabase.Postgrest.QueryOptions { Returning = Supabase.Postgrest.QueryOptions.ReturnType.Representation });

                var eventoInsertado = response.Models?.FirstOrDefault();

                if (eventoInsertado != null)
                {
                    await DisplayAlert("¡Éxito!", "Evento registrado correctamente. Abriendo contrato...", "Aceptar");
                    await Navigation.PushAsync(new ContratoPage(_supabaseClient!, eventoInsertado.Id));
                }
                else
                {
                    await DisplayAlert("Aviso", "El evento se guardó, pero no se pudo recuperar el ID para la firma.", "Aceptar");
                    await Navigation.PopAsync();
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error al guardar", $"No se pudo procesar el evento: {ex.Message}", "Aceptar");
        }
    }
}