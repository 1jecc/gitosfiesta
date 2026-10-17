using gitosfiesta.Models;
using System.Collections.ObjectModel;

namespace gitosfiesta;

public partial class ListaEventoPage : ContentPage
{
    private readonly Supabase.Client _supabaseClient;
    private List<EventoModel> _todosLosEventos = new();

    // Esta lista se actualiza automáticamente en la vista
    public ObservableCollection<EventoModel> EventosFiltrados { get; set; } = new();

    public ListaEventoPage(Supabase.Client supabaseClient)
    {
        InitializeComponent();
        _supabaseClient = supabaseClient;
        ColeccionEventos.ItemsSource = EventosFiltrados;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Cargar datos por primera vez
        await CargarEventosAsync();

        // Al abrir la pantalla, preseleccionar "Vigentes"
        if (PickerFiltro.SelectedIndex == -1)
        {
            PickerFiltro.SelectedIndex = 0;
        }
    }

    private async Task CargarEventosAsync()
    {
        try
        {
            // 1. Preparamos la consulta base (solo eventos habilitados)
            var query = _supabaseClient
                .From<EventoModel>()
                .Where(x => x.enable == true);

            // ==========================================================
            // REGLA DE NEGOCIO (PUNTO 3): 
            // Si es STAFF, solo ve los que él creó o se le asignaron
            // ==========================================================
            if (SessionContext.Rol?.ToUpper() == "STAFF")
            {
                query = query.Filter("usuario_creacion_id", Supabase.Postgrest.Constants.Operator.Equals, SessionContext.UsuarioId);
            }

            // Ejecutamos la consulta a Supabase
            var response = await query.Get();

            // Los ordenamos por fecha, para que los más próximos salgan primero
            _todosLosEventos = response.Models?.OrderBy(e => e.fecha_evento).ToList() ?? new List<EventoModel>();

            // Aplicamos el filtro visual de Vigentes/Caducados
            AplicarFiltro();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudieron cargar los eventos: {ex.Message}", "Aceptar");
        }
    }

    private void OnFiltroChanged(object sender, EventArgs e)
    {
        AplicarFiltro();
    }

    private void AplicarFiltro()
    {
        if (_todosLosEventos == null) return;

        EventosFiltrados.Clear();

        string seleccion = PickerFiltro.SelectedItem?.ToString() ?? "Vigentes (Próximos)";
        DateTime hoy = DateTime.Today;

        IEnumerable<EventoModel> filtrados;

        if (seleccion.Contains("Vigentes"))
        {
            // Eventos cuya fecha sea HOY o en el FUTURO
            filtrados = _todosLosEventos.Where(e => e.fecha_evento.Date >= hoy);
        }
        else if (seleccion.Contains("Caducados"))
        {
            // Eventos cuya fecha ya pasó
            filtrados = _todosLosEventos.Where(e => e.fecha_evento.Date < hoy);
        }
        else
        {
            // Todos los eventos
            filtrados = _todosLosEventos;
        }

        // Agregar a la lista que la pantalla observa (CollectionView)
        foreach (var ev in filtrados)
        {
            EventosFiltrados.Add(ev);
        }
    }
}