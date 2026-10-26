using gitosfiesta.Models;
using System.Collections.ObjectModel;

namespace gitosfiesta;

public partial class AbonosPage : ContentPage
{
    private readonly Supabase.Client _supabaseClient;
    private List<EventoModel> _eventosPendientes = new();
    private EventoModel? _eventoSeleccionado;
    private decimal _saldoPendienteActual = 0;

    public ObservableCollection<AbonoModel> HistorialAbonos { get; set; } = new();

    public AbonosPage(Supabase.Client supabaseClient)
    {
        InitializeComponent();
        _supabaseClient = supabaseClient;

        ColeccionHistorial.ItemsSource = HistorialAbonos;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarEventosPendientesAsync();
    }

    private async Task CargarEventosPendientesAsync()
    {
        try
        {
            var response = await _supabaseClient
                .From<EventoModel>()
                .Where(x => x.enable == true && x.estatus != "Liquidado")
                .Get();

            _eventosPendientes = response.Models?.OrderBy(x => x.fecha_evento).ToList() ?? new List<EventoModel>();
            PickerEventos.ItemsSource = _eventosPendientes;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudieron cargar los eventos: {ex.Message}", "Aceptar");
        }
    }

    private async void OnEventoSeleccionado(object sender, EventArgs e)
    {
        _eventoSeleccionado = PickerEventos.SelectedItem as EventoModel;

        if (_eventoSeleccionado == null)
        {
            PanelEstadoCuenta.IsVisible = false;
            PanelRegistrarPago.IsVisible = false;
            PanelHistorialPagos.IsVisible = false;
            return;
        }

        try
        {
            var responseAbonos = await _supabaseClient
                .From<AbonoModel>()
                .Filter("evento_id", Supabase.Postgrest.Constants.Operator.Equals, _eventoSeleccionado.Id)
                .Get();

            var listaAbonos = responseAbonos.Models?.OrderBy(a => a.FechaPago).ToList() ?? new List<AbonoModel>();

            HistorialAbonos.Clear();
            foreach (var abono in listaAbonos)
            {
                HistorialAbonos.Add(abono);
            }

            decimal totalAbonado = listaAbonos.Sum(a => a.Monto);
            decimal costoTotal = _eventoSeleccionado.precio;
            decimal anticipo = _eventoSeleccionado.anticipo;
            _saldoPendienteActual = costoTotal - anticipo - totalAbonado;

            if (_saldoPendienteActual <= 0)
            {
                _eventoSeleccionado.estatus = "Liquidado";

                await _supabaseClient.From<EventoModel>()
                    .Where(x => x.Id == _eventoSeleccionado.Id)
                    .Update(_eventoSeleccionado);

                await DisplayAlert("Cuenta Cubierta", "Este evento ya tiene el saldo totalmente cubierto (o a favor). Se marcará como Liquidado y se quitará de la lista de deudores.", "Aceptar");

                PickerEventos.SelectedIndex = -1;
                PanelEstadoCuenta.IsVisible = false;
                PanelRegistrarPago.IsVisible = false;
                PanelHistorialPagos.IsVisible = false;

                await CargarEventosPendientesAsync();
                return;
            }

            LblCostoTotal.Text = $"${costoTotal:F2}";
            LblAnticipo.Text = $"${anticipo:F2}";
            LblAbonosAcumulados.Text = $"${totalAbonado:F2}";
            LblSaldoPendiente.Text = $"${_saldoPendienteActual:F2}";

            PanelEstadoCuenta.IsVisible = true;
            PanelRegistrarPago.IsVisible = true;
            PanelHistorialPagos.IsVisible = true;
            TxtMontoAbono.Text = string.Empty;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo calcular el estado de cuenta: {ex.Message}", "Aceptar");
        }
    }

    private async void OnRegistrarPagoClicked(object sender, EventArgs e)
    {
        if (_eventoSeleccionado == null) return;

        if (!decimal.TryParse(TxtMontoAbono.Text, out decimal montoNuevo) || montoNuevo <= 0)
        {
            await DisplayAlert("Dato inválido", "Ingresa una cantidad válida mayor a cero.", "Aceptar");
            return;
        }

        if (PickerMetodo.SelectedIndex == -1)
        {
            await DisplayAlert("Falta Método", "Por favor selecciona un método de pago.", "Aceptar");
            return;
        }

        if (montoNuevo > _saldoPendienteActual)
        {
            await DisplayAlert("Monto excedido", $"No puedes abonar más del saldo pendiente (${_saldoPendienteActual:F2}).", "Aceptar");
            return;
        }

        try
        {
            // 1. Guardar el nuevo abono en la tabla de abonos
            var nuevoAbono = new AbonoModel
            {
                EventoId = _eventoSeleccionado.Id,
                Monto = montoNuevo,
                FechaPago = DateTime.Now,
                MetodoPago = PickerMetodo.SelectedItem.ToString(),
                Enable = true
            };

            await _supabaseClient.From<AbonoModel>().Insert(nuevoAbono);

            // 2. Calcular el nuevo saldo que va a quedar pendiente
            decimal nuevoSaldoRestante = _saldoPendienteActual - montoNuevo;

            // 3. Actualizar el evento en Supabase (Estatus y Saldo)
            if (nuevoSaldoRestante <= 0)
            {
                // Si cubre todo o queda a favor, se liquida
                _eventoSeleccionado.estatus = "Liquidado";
                _eventoSeleccionado.Saldo = 0;

                await _supabaseClient.From<EventoModel>()
                    .Where(x => x.Id == _eventoSeleccionado.Id)
                    .Set(x => x.estatus, "Liquidado")
                    .Set(x => x.Saldo, 0)
                    .Update();

                await DisplayAlert("¡Cuenta Liquidada!", "El pago se ha registrado y el evento está totalmente pagado.", "Aceptar");

                PickerEventos.SelectedIndex = -1;
                PanelEstadoCuenta.IsVisible = false;
                PanelRegistrarPago.IsVisible = false;
                PanelHistorialPagos.IsVisible = false;

                await CargarEventosPendientesAsync();
            }
            else
            {
                // Si es un abono parcial, actualizamos el saldo restado en la base de datos
                _eventoSeleccionado.Saldo = nuevoSaldoRestante;

                await _supabaseClient.From<EventoModel>()
                    .Where(x => x.Id == _eventoSeleccionado.Id)
                    .Set(x => x.Saldo, nuevoSaldoRestante)
                    .Update();

                await DisplayAlert("Abono Exitoso", $"Se ha registrado el pago de ${montoNuevo:F2} correctamente.", "Aceptar");

                // Recargar la información del evento seleccionado para refrescar los montos en pantalla
                OnEventoSeleccionado(null, EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error al guardar", $"Hubo un problema registrando el pago: {ex.Message}", "Aceptar");
        }
    }
    
}