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
                LblBienvenida.Text = $"¡Bienvenido, {SessionContext.NombreCompleto}!";
            }
            else
            {
                LblBienvenida.Text = "¡Bienvenido!";
            }
        }
        catch (Exception ex)
        {
            LblBienvenida.Text = "¡Bienvenido!";
        }

        return Task.CompletedTask;
    }

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
            bool isStaff = SessionContext.Rol?.ToUpper() == "STAFF";

            if (isStaff)
            {
                PanelAlertasPagos.IsVisible = false;
                GridDashboard.ColumnDefinitions.Clear();
                GridDashboard.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                LblTituloProximos.Text = "Mis Próximos Eventos Asignados";

                var respAsignaciones = await _supabaseClient.From<EventoPersonalModel>()
                    .Filter("usuario_id", Supabase.Postgrest.Constants.Operator.Equals, SessionContext.UsuarioId)
                    .Get();

                var misAsignaciones = respAsignaciones.Models ?? new List<EventoPersonalModel>();
                var misEventosIds = misAsignaciones.Select(a => a.EventoId).ToList();

                todosLosEventos = todosLosEventos.Where(e => misEventosIds.Contains(e.Id)).ToList();
            }
            else
            {
                PanelAlertasPagos.IsVisible = true;
                LblTituloProximos.Text = "Próximos Eventos";
            }

            DateTime hoy = DateTime.Today;
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
                .Take(12)
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
            if (!isStaff)
            {
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

            if (SessionContext.Rol?.ToUpper() == "STAFF")
            {
                LblModalStaffCliente.Text = item.nombre_cliente;
                LblModalStaffFecha.Text = $"{item.FechaFormateada} a las {item.EventoOriginal.hora_evento:hh\\:mm}";
                LblModalStaffTipoEvento.Text = item.EventoOriginal.tipo_evento ?? "N/A";
                LblModalStaffPersonas.Text = item.EventoOriginal.numero_personas.ToString();
                LblModalStaffObservaciones.Text = string.IsNullOrWhiteSpace(item.EventoOriginal.observaciones) ? "Ninguna" : item.EventoOriginal.observaciones;

                var resp = await _supabaseClient.From<EventoPersonalModel>()
                    .Where(x => x.EventoId == item.EventoOriginal.Id && x.UsuarioId == SessionContext.UsuarioId)
                    .Get();
                var miAsignacion = resp.Models?.FirstOrDefault();

                string puesto = miAsignacion?.Puesto ?? "Staff General";
                string rol = miAsignacion?.Rol ?? "Sin rol específico";

                LblModalStaffPuesto.Text = puesto;
                LblModalStaffRol.Text = rol;
                LblModalStaffDescripcionRol.Text = ObtenerDescripcionActividades(rol, puesto);

                ModalStaffEvento.IsVisible = true;
            }
            else
            {
                await Navigation.PushAsync(new ListaEventoPage(_supabaseClient, item.EventoOriginal.Id));
            }
        }
    }

    private string ObtenerDescripcionActividades(string rol, string puesto)
    {
        string rolUpper = rol?.ToUpper() ?? "";
        string puestoUpper = puesto?.ToUpper() ?? "";

        if (puestoUpper.Contains("COCINERO"))
        {
            return "• Coordinar la preparación de platillos de adultos y niños.\n• Revisar tiempos de salida de alimentos según la pizarra de cocina.\n• Apoyar en la entrega y control de insumos de cocina.";
        }

        if (rolUpper.Contains("PRINCIPAL") || rolUpper.Contains("BARRA") || rolUpper.Contains("ROL 1"))
        {
            return "AL LLEGAR:\n" +
                   "• Trapear escaleras y terraza.\n" +
                   "• Preparar snacks: separar platos cuadrados, llenar 30 platos de nachos y colocar fruta/frituras en la mesa de snack.\n\n" +
                   "DURANTE EL EVENTO (Área Puerta y Snack, Mesas 1, 2 y 3):\n" +
                   "• Permanecer 1 hora en la puerta abriendo a invitados y guiando hacia los regalos.\n" +
                   "• Atender mesas 1, 2 y 3 (agua de sabor, palomitas, vasos, servilletas).\n" +
                   "• Estar al pendiente del queso de nachos y la mesa de pastel (llevar la mesa amarilla a cantar las mañanitas).\n" +
                   "• Servir comida a adultos por mesa en orden (del 1 al 3).\n\n" +
                   "AL TERMINAR:\n" +
                   "• Recoger vasos, platos y basura de mesas 1, 2 y 3 15 min antes de terminar, quitar manteles.\n" +
                   "• Recoger mesa de snack y lavar baños de hombres y mujeres.";
        }
        else if (rolUpper.Contains("ATENCIÓN") || rolUpper.Contains("MESAS") || rolUpper.Contains("ROL 2"))
        {
            return "AL LLEGAR (Área Mesas):\n" +
                   "• Colocar 4 vasos limpios en cada mesa y revisar que estén limpios.\n" +
                   "• Poner jarra de agua, bowl de palomitas y frituras al iniciar la hora del evento.\n" +
                   "• Recoger constantemente basura generada por los snacks.\n\n" +
                   "DURANTE EL EVENTO:\n" +
                   "• Sillas Infantiles: colocarlas frente al escenario al llegar el show.\n" +
                   "• Llevar el pastel en el show (cantar mañanitas) y retirarlo a la cocina.\n" +
                   "• Servir comida a adultos (mesas 4, 5 y 6), dar refill de refresco por 30 min.\n" +
                   "• Organizar mesas infantiles al terminar el show y servir refresco a los niños.\n\n" +
                   "AL TERMINAR:\n" +
                   "• Apoyar en la limpieza general y desalojo del salón.";
        }
        else
        {
            return "AL LLEGAR ROL 3:\n" +
                   "• Encender 3 climas (22°C) y luces del área de mesas.\n" +
                   "• Subir al área de juegos: encender clima, máquina de Xbox, ordenar juguetes, sacudir muebles y limpiar cristales.\n" +
                   "• Encender luces de la casita de cocina.\n" +
                   "• Revisar ambos baños (rollo, toallitas, botes de basura, espejos limpios).\n" +
                   "• Preparar jarras de agua y cortar pan en cocina.\n\n" +
                   "DURANTE EL EVENTO (Área Juegos y Baños):\n" +
                   "• Permanecer en área de juego supervisando el uso correcto de muebles, juguetes y Xbox (pedir IFE para controles).\n" +
                   "• Revisar baños constantemente durante el evento.\n" +
                   "• Ayudar en cocina a preparar platillos de adultos y niños.\n\n" +
                   "AL TERMINAR EL EVENTO:\n" +
                   "• Faltando 20 min apagar Xbox, entregar IFEs, apagar maquinitas y climas de juegos.\n" +
                   "• Barrer desde el área de juegos, bajar por escaleras hasta la puerta principal y terraza.\n" +
                   "• Sacar bolsas de basura.";
        }
    }

    private void OnCerrarModalStaffClicked(object sender, EventArgs e)
    {
        ModalStaffEvento.IsVisible = false;
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