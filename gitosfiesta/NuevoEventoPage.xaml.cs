using gitosfiesta.Models;
using System.Text.RegularExpressions;

namespace gitosfiesta;

public partial class NuevoEventoPage : ContentPage
{
    private readonly Supabase.Client? _supabaseClient;
    private List<MiscModel> _catalogoMisc = new();
    private bool _esFinDeSemana = true;
    private bool _horaEntreSemanaSeleccionada = false;
    private decimal _precioBasePaquete = 0;
    private decimal _descuentoPromocion = 0;

    private List<string> _extrasSeleccionados = new();
    private readonly long? _eventoId;

    public NuevoEventoPage(Supabase.Client supabaseClient) : this(supabaseClient, null)
    {
    }

    public NuevoEventoPage(Supabase.Client supabaseClient, long? eventoId)
    {
        InitializeComponent();
        _supabaseClient = supabaseClient;
        _eventoId = eventoId;

        // RESTRICCIÓN: Solo se pueden registrar eventos a partir de 15 días a partir de hoy (si es nuevo)
        if (!_eventoId.HasValue)
        {
            DatePickerFecha.MinimumDate = DateTime.Today.AddDays(15);
            DatePickerFecha.Date = DateTime.Today.AddDays(15);
        }
        else
        {
            DatePickerFecha.MinimumDate = DateTime.Today; // Si se edita, respetamos el histórico si fuera necesario
        }

        TxtNombreCliente.TextChanged += OnNombreClienteTextChanged;
        TxtTelefono.TextChanged += OnTelefonoTextChanged;

        if (_eventoId.HasValue)
        {
            Title = $"Contrato / Folio #{_eventoId.Value}";
            BtnAccionEvento.Text = "Guardar Cambios y Continuar a Firma";
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
        await CargarDatosInicialesAsync();

        if (_eventoId.HasValue)
        {
            await CargarDatosEventoPorIdAsync(_eventoId.Value);
        }
        else
        {
            LimpiarFormularioCompleto();
            await ValidarFechaSilenciosaAsync(DatePickerFecha.Date);
        }
    }

    private async Task ValidarFechaSilenciosaAsync(DateTime fecha)
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
            TimePickerEntreSemana.IsVisible = true;

            if (eventosEnFecha.Count > 0)
            {
                TimePickerEntreSemana.IsEnabled = false;
                _horaEntreSemanaSeleccionada = false;
            }
            else
            {
                TimePickerEntreSemana.IsEnabled = true;
                _horaEntreSemanaSeleccionada = false;
            }
        }
        else
        {
            PickerHorario.IsVisible = true;
            TimePickerEntreSemana.IsVisible = false;
            TimePickerEntreSemana.IsEnabled = false;

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
            PickerHorario.IsEnabled = horariosDisponibles.Count > 0;
        }

        EvaluarElegibilidadPromocion();
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
            TimePickerEntreSemana.IsVisible = true;

            if (eventosEnFecha.Count > 0)
            {
                TimePickerEntreSemana.IsEnabled = false;
                _horaEntreSemanaSeleccionada = false;
                await DisplayAlert("Día Ocupado", "Este día entre semana ya cuenta con un evento registrado. Selecciona otra fecha.", "Aceptar");
            }
            else
            {
                TimePickerEntreSemana.IsEnabled = true;
                _horaEntreSemanaSeleccionada = false;
            }
        }
        else
        {
            PickerHorario.IsVisible = true;
            TimePickerEntreSemana.IsVisible = false;
            TimePickerEntreSemana.IsEnabled = false;

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

            TxtNombreCliente.Text = evento.nombre_cliente;
            TxtTelefono.Text = evento.telefono_cliente;
            TxtTipoEvento.Text = evento.tipo_evento;
            DatePickerFecha.Date = evento.fecha_evento;
            TxtAnticipo.Text = evento.anticipo.ToString();
            TxtObservaciones.Text = evento.observaciones;

            await ActualizarControlHorarioSegunFechaAsync(evento.fecha_evento);

            if (_esFinDeSemana)
            {
                PickerHorario.SelectedItem = evento.hora_evento.Hours == 14 ? "2:00 pm a 5:30 pm" : "6:30 pm a 10:00 pm";
            }
            else
            {
                TimePickerEntreSemana.Time = evento.hora_evento;
                _horaEntreSemanaSeleccionada = true;
            }

            HabilitarPaquetes();

            if (!string.IsNullOrEmpty(evento.paquete))
            {
                PickerPaquete.SelectedItem = evento.paquete.Trim();
            }
            else if (!string.IsNullOrEmpty(evento.menu))
            {
                var partesMenu = evento.menu.Split('|');
                string nombrePaqueteCompleto = partesMenu[0].Trim();
                string nombreLimpio = nombrePaqueteCompleto.Split('-')[0].Trim();
                PickerPaquete.SelectedItem = nombreLimpio;
            }

            if (!string.IsNullOrEmpty(evento.menu))
            {
                var partesMenu = evento.menu.Split('|');
                if (partesMenu.Length > 1)
                {
                    string platillo = partesMenu[1].Replace("Platillo:", "").Trim();
                    PickerPlatillo.SelectedItem = platillo;
                }
            }

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
                            PickerPromocion.SelectedItem = parte.Trim();
                        }
                    }
                }
                else if (PickerPromocion != null && PickerPromocion.IsEnabled)
                {
                    PickerPromocion.SelectedItem = evento.promocion.Trim();
                }
            }

            ConstruirControlesExtras(extrasPreSeleccionados);
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

    private void OnTimePickerTimeSelected(object sender, TimeChangedEventArgs e)
    {
        if (!_esFinDeSemana)
        {
            TimeSpan horaSeleccionada = e.NewTime;

            // RESTRICCIÓN: Prohibido entre las 22:00 (10:00 PM) y las 08:00 (8:00 AM)
            if (horaSeleccionada >= TimeSpan.FromHours(22) || horaSeleccionada < TimeSpan.FromHours(8))
            {
                // Usamos MainThread para evitar bloqueos de UI al mostrar la alerta y resetear
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlert("Horario no permitido", "No se pueden registrar eventos entre las 10:00 PM y las 8:00 AM.", "Aceptar");

                    // Desvinculamos temporalmente para evitar bucle
                    TimePickerEntreSemana.PropertyChanged -= TimePicker_PropertyChanged;
                    TimePickerEntreSemana.Time = new TimeSpan(12, 0, 0); // Resetea a las 12:00 PM
                    TimePickerEntreSemana.PropertyChanged += TimePicker_PropertyChanged;
                });

                _horaEntreSemanaSeleccionada = false;
                return;
            }

            _horaEntreSemanaSeleccionada = true;
            HabilitarPaquetes();
        }
    }

    private void TimePicker_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Control auxiliar para prevenir ciclos de actualización en MAUI
    }
    private void HabilitarPaquetes()
    {
        var paquetes = _catalogoMisc
            .Where(x => x.categoria == "PAQUETE")
            .Select(x => x.value1)
            .ToList();

        PickerPaquete.ItemsSource = paquetes;
        PickerPaquete.IsEnabled = true;

        if (!_eventoId.HasValue)
            PickerPaquete.SelectedIndex = -1;

        BloquearPasosDesdePaquete();
    }

    private void OnPaqueteSelectedIndexChanged(object sender, EventArgs e)
    {
        string paqueteSeleccionado = PickerPaquete.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(paqueteSeleccionado)) return;

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

        if (esEntreSemana && numPersonas >= 40)
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
            var terminos = _catalogoMisc
                .Where(x => x.categoria == "TERMINO_CONTRATO")
                .OrderBy(x => int.TryParse(x.value1, out int num) ? num : 0)
                .ToList();

            if (terminos.Count == 0)
            {
                await DisplayAlert("Aviso", "No se encontraron términos registrados en el catálogo.", "Aceptar");
                return;
            }

            string mensajeTerminos = string.Join("\n\n", terminos.Select(t => $"{t.value1}.- {t.value2}"));
            await DisplayAlert("Términos y Condiciones - Gitos Fiesta", mensajeTerminos, "Cerrar");
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

            // Permitir solo letras y espacios, evitando múltiples espacios seguidos
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

    private async void OnGuardarEventoClicked(object sender, EventArgs e)
    {
        // Validación de plazo de 15 días para nuevos eventos
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

            decimal saldo = precio - anticipo;

            TimeSpan horaFinalEvento;
            if (_esFinDeSemana && PickerHorario.SelectedItem != null)
            {
                string bloque = PickerHorario.SelectedItem.ToString() ?? "";
                horaFinalEvento = bloque.Contains("2:00") ? TimeSpan.FromHours(14) : TimeSpan.FromHours(18.5);
            }
            else
            {
                horaFinalEvento = TimePickerEntreSemana.Time;

                // Validación extra al guardar por si acaso
                if (horaFinalEvento >= TimeSpan.FromHours(22) || horaFinalEvento < TimeSpan.FromHours(8))
                {
                    await DisplayAlert("Horario no permitido", "No se pueden registrar eventos entre las 10:00 PM y las 8:00 AM.", "Aceptar");
                    return;
                }
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

            if (_eventoId.HasValue)
            {
                var eventoActualizado = new EventoModel
                {
                    Id = _eventoId.Value,
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
                    Saldo = saldo,
                    promocion = string.IsNullOrWhiteSpace(promocionFinal) ? null : promocionFinal,
                    observaciones = TxtObservaciones.Text?.Trim()
                };

                await _supabaseClient!
                    .From<EventoModel>()
                    .Where(x => x.Id == _eventoId.Value)
                    .Update(eventoActualizado);

                await DisplayAlert("Actualizado", "Datos guardados correctamente. Pasando al contrato para firma...", "Aceptar");
                await Navigation.PushAsync(new ContratoPage(_supabaseClient!, _eventoId.Value));
            }
            else
            {
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
                    Saldo = saldo,
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