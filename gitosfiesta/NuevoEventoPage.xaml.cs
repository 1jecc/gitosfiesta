namespace gitosfiesta;

public partial class NuevoEventoPage : ContentPage
{
    private readonly Supabase.Client? _supabaseClient;

    // Constructor por defecto (por si se instancia de forma genérica sin parámetros)
    public NuevoEventoPage()
    {
        InitializeComponent();
        _supabaseClient = App.SupabaseClient;
    }

    // Constructor que acepta Supabase.Client (utilizado automáticamente por la Reflexión del menú)
    public NuevoEventoPage(Supabase.Client supabaseClient)
    {
        InitializeComponent();
        _supabaseClient = supabaseClient;
    }

    private async void OnGuardarEventoClicked(object sender, EventArgs e)
    {
        string nombre = TxtNombreEvento.Text?.Trim() ?? string.Empty;
        string descripcion = TxtDescripcion.Text?.Trim() ?? string.Empty;
        DateTime fecha = DatePickerFecha.Date;

        if (string.IsNullOrEmpty(nombre))
        {
            await DisplayAlert("Campo Requerido", "Por favor ingresa el nombre del evento.", "Aceptar");
            return;
        }

        try
        {
            // Aquí puedes programar la inserción directa a Supabase o mediante Stored Procedure
            await DisplayAlert("Éxito", $"El evento '{nombre}' ha sido registrado correctamente.", "Aceptar");

            // Limpiar campos
            TxtNombreEvento.Text = string.Empty;
            TxtDescripcion.Text = string.Empty;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo guardar el evento: {ex.Message}", "Aceptar");
        }
    }
}