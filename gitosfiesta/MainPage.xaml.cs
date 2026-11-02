using gitosfiesta.Models;
using System.Collections.ObjectModel;

namespace gitosfiesta;

public partial class MainPage : ContentPage
{
    private readonly Supabase.Client _supabaseClient;

    public ObservableCollection<AlertaViewItem> AlertasList { get; set; } = new();
    public ObservableCollection<EventoDashboardItem> ProximosEventosList { get; set; } = new();

    public MainPage(Supabase.Client supabaseClient)
    {
        InitializeComponent();
        _supabaseClient = supabaseClient;

        ColeccionAlertas.ItemsSource = AlertasList;
        ColeccionProximos.ItemsSource = ProximosEventosList;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // Ejecutamos de forma controlada para que no bloquee el hilo de UI si ocurre un retraso de red
            await CargarDatosUsuarioAsync();
            await CargarDashboardDataAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al cargar el dashboard en OnAppearing: {ex.Message}");
        }
    }

    private Task CargarDatosUsuarioAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(SessionContext.NombreCompleto))
            {
                // Muestra el nombre completo de la persona que hizo login exitoso
                LblBienvenida.Text = $"¡Bienvenido, {SessionContext.NombreCompleto}!";
            }
            else
            {
                LblBienvenida.Text = "¡Bienvenido!";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al cargar el usuario de sesión: {ex.Message}");
            LblBienvenida.Text = "¡Bienvenido!";
        }

        return Task.CompletedTask;
    }

    // Detector automático de Pantalla (Laptop vs Celular)
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (GridDashboard != null)
        {
            if (width > 800)
            {
                GridDashboard.ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Star }
                };
            }
            else
            {
                GridDashboard.ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Star }
                };
            }
        }
    }

    private async Task CargarDashboardDataAsync()
    {
        try
        {
            var response = await _supabaseClient
                .From<EventoModel>()
                .Where(x => x.enable == true)
                .Get();

            var todosLosEventos = response.Models ?? new List<EventoModel>();

            DateTime hoy = DateTime.Today;

            // Mostramos la fecha actual formateada en la interfaz (ej. 03 de noviembre de 2026)
            LblFechaActual.Text = hoy.ToString("dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-ES"));

            int mesActual = hoy.Month;
            int anioActual = hoy.Year;

            var eventosDelMes = todosLosEventos
                .Where(e => e.fecha_evento.Month == mesActual && e.fecha_evento.Year == anioActual)
                .ToList();
            int totalMes = eventosDelMes.Count;

            int finalizados = eventosDelMes.Where(e => e.estatus?.Equals("Finalizado", StringComparison.OrdinalIgnoreCase) == true).Count();
            int liquidados = eventosDelMes.Where(e => e.estatus?.Equals("Liquidado", StringComparison.OrdinalIgnoreCase) == true).Count();

            int pendientes = eventosDelMes.Where(e =>
                !string.Equals(e.estatus, "Liquidado", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(e.estatus, "Finalizado", StringComparison.OrdinalIgnoreCase) &&
                e.fecha_evento.Date >= hoy
            ).Count();

            LblTotalMes.Text = totalMes.ToString();
            LblFinalizados.Text = finalizados.ToString();
            LblLiquidados.Text = liquidados.ToString();
            LblPendientes.Text = pendientes.ToString();

            var proximos = todosLosEventos
                .Where(e => e.fecha_evento.Date >= hoy)
                .OrderBy(e => e.fecha_evento)
                .Take(8)
                .ToList();

            ProximosEventosList.Clear();
            foreach (var ev in proximos)
            {
                ProximosEventosList.Add(new EventoDashboardItem
                {
                    EventoOriginal = ev,
                    nombre_cliente = ev.nombre_cliente,
                    tipo_evento = ev.tipo_evento,
                    FechaFormateada = ev.fecha_evento.ToString("dd/MM/yyyy"),
                    estatus = ev.estatus,
                    SaldoFormateado = $"Saldo: ${ev.Saldo:F2}"
                });
            }

            AlertasList.Clear();
            foreach (var ev in todosLosEventos.Where(e => e.fecha_evento.Date >= hoy && e.Saldo > 0))
            {
                int diasRestantes = (ev.fecha_evento.Date - hoy).Days;

                var abonosResp = await _supabaseClient
                    .From<AbonoModel>()
                    .Filter("evento_id", Supabase.Postgrest.Constants.Operator.Equals, ev.Id)
                    .Get();

                decimal totalAbonado = (decimal)(abonosResp.Models ?? new List<AbonoModel>()).Sum(a => a.Monto);
                decimal minimoRequeridoAnticipo = ev.precio * 0.5m;
                bool cumpleCincuentaPorciento = (ev.anticipo + totalAbonado) >= minimoRequeridoAnticipo;

                if (diasRestantes <= 7 && ev.Saldo > 0)
                {
                    AlertasList.Add(new AlertaViewItem
                    {
                        EventoOriginal = ev,
                        Folio = $"#{ev.Id}",
                        Cliente = ev.nombre_cliente,
                        MensajeAlerta = $"Faltan {diasRestantes} días y aún tiene saldo de ${ev.Saldo:F2} (Debe liquidarse)."
                    });
                }
                else if (diasRestantes <= 15 && !cumpleCincuentaPorciento)
                {
                    AlertasList.Add(new AlertaViewItem
                    {
                        EventoOriginal = ev,
                        Folio = $"#{ev.Id}",
                        Cliente = ev.nombre_cliente,
                        MensajeAlerta = $"Faltan {diasRestantes} días y no cubre el 50% requerido."
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cargando dashboard: {ex.Message}");
        }
    }

    private async void OnProximoEventoSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is EventoDashboardItem item && item.EventoOriginal != null)
        {
            ((CollectionView)sender).SelectedItem = null;
            await Navigation.PushAsync(new ListaEventoPage(_supabaseClient, item.EventoOriginal.Id));
        }
    }

    private async void OnAlertaSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is AlertaViewItem item && item.EventoOriginal != null)
        {
            ((CollectionView)sender).SelectedItem = null;
            await Navigation.PushAsync(new ListaEventoPage(_supabaseClient, item.EventoOriginal.Id));
        }
    }
}

public class EventoDashboardItem
{
    public EventoModel EventoOriginal { get; set; } = new();
    public string nombre_cliente { get; set; } = string.Empty;
    public string tipo_evento { get; set; } = string.Empty;
    public string FechaFormateada { get; set; } = string.Empty;
    public string estatus { get; set; } = string.Empty;
    public string SaldoFormateado { get; set; } = string.Empty;
}

public class AlertaViewItem
{
    public EventoModel EventoOriginal { get; set; } = new();
    public string Folio { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public string MensajeAlerta { get; set; } = string.Empty;
}