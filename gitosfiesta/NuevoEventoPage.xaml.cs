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
        DatePickerFecha.MinimumDate = DateTime.Today;

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
        }
    }

    private void LimpiarFormularioCompleto()
    {
        if (_eventoId.HasValue) return;

        TxtNombreCliente.Text = string.Empty;
        TxtTelefono.Text = string.Empty;
        TxtTipoEvento.Text = string.Empty;
        TxtObservaciones.Text = string.Empty;
        TxtAnticipo.Text = "1000";
        _precioBasePaquete = 0;
        _descuentoPromocion = 0;
        _extrasSeleccionados.Clear();

        DatePickerFecha.Date = DateTime.Today;

        _horaEntreSemanaSeleccionada = false;
        PickerHorario.SelectedIndex = -1;
        PickerHorario.ItemsSource = null;

        PickerPaquete.ItemsSource = null;
        PickerPaquete.IsEnabled = false;
        PickerPaquete.SelectedIndex = -1;

        ConstruirControlesExtras(new List<string>(), new List<string>());

        if (PickerPromocion != null)
        {
            PickerPromocion.ItemsSource = null;
            PickerPromocion.IsEnabled = false;
            PickerPromocion.SelectedIndex = -1;
        }

        BloquearPasosDesdePaquete();
    }

    private async Task CargarDatosInicialesAsync()
    {
        try
        {
            if (_supabaseClient == null) return;

            var response = await _supabaseClient
                .From<MiscModel>()
                .Where(x => x.enable == true)
                .Get();

            _catalogoMisc = response.Models ?? new List<MiscModel>();

            var extrasDisponibles = _catalogoMisc
                .Where(x => x.categoria == "EXTRA")
                .Select(x => x.value1)
                .ToList();

            ConstruirControlesExtras(extrasDisponibles, new List<string>());

            if (!_eventoId.HasValue)
            {
                await ActualizarControlHorarioSegunFechaAsync(DatePickerFecha.Date);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudieron cargar los catálogos: {ex.Message}", "Aceptar");
        }
    }

    private void ConstruirControlesExtras(List<string> extrasDisponibles, List<string> extrasPreSeleccionados)
    {
        if (ContainerExtras == null) return;
        ContainerExtras.Children.Clear();
        _extrasSeleccionados = new List<string>(extrasPreSeleccionados);

        foreach (var extra in extrasDisponibles)
        {
            var horizontalLayout = new HorizontalStackLayout
            {
                Spacing = 10,
                VerticalOptions = LayoutOptions.Center
            };

            var checkBox = new CheckBox
            {
                IsChecked = _extrasSeleccionados.Contains(extra),
                VerticalOptions = LayoutOptions.Center
            };

            checkBox.CheckedChanged += (s, e) =>
            {
                if (e.Value)
                {
                    if (!_extrasSeleccionados.Contains(extra))
                        _extrasSeleccionados.Add(extra);
                }
                else
                {
                    if (_extrasSeleccionados.Contains(extra))
                        _extrasSeleccionados.Remove(extra);
                }
                CalcularPrecioTotalConExtras();
            };

            var label = new Label
            {
                Text = extra,
                TextColor = Color.FromArgb("#333333"),
                FontSize = 13,
                VerticalOptions = LayoutOptions.Center
            };

            horizontalLayout.Children.Add(checkBox);
            horizontalLayout.Children.Add(label);
            ContainerExtras.Children.Add(horizontalLayout);
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
            if (!string.IsNullOrEmpty(evento.promocion) && evento.promocion.Contains("Extra:"))
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

            var extrasDisponibles = _catalogoMisc
                .Where(x => x.categoria == "EXTRA")
                .Select(x => x.value1)
                .ToList();

            ConstruirControlesExtras(extrasDisponibles, extrasPreSeleccionados);
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

    private void OnHorarioSelectedIndexChanged(object sender, EventArgs e)
    {
        if (PickerHorario.SelectedIndex == -1 && _esFinDeSemana) return;
        HabilitarPaquetes();
    }

    private void OnTimePickerTimeSelected(object sender, TimeChangedEventArgs e)
    {
        if (!_esFinDeSemana)
        {
            _horaEntreSemanaSeleccionada = true;
            HabilitarPaquetes();
        }
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
        if (string.IsNullOrWhiteSpace(e.NewTextValue)) return;
        string textoFiltrado = new string(e.NewTextValue.Where(c => char.IsLetter(c) || c == ' ').ToArray());
        if (e.NewTextValue != textoFiltrado) TxtNombreCliente.Text = textoFiltrado;
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
            else
            {
                horaFinalEvento = TimePickerEntreSemana.Time;
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