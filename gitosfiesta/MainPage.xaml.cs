namespace gitosfiesta;

public partial class MainPage : ContentPage
{
    private readonly Supabase.Client _supabaseClient;

    public MainPage(Supabase.Client supabaseClient)
    {
        InitializeComponent();
        _supabaseClient = supabaseClient;

        // Probamos la conexión al iniciar la vista
        TestSupabaseConnection();
    }

    private void OnCounterClicked(object sender, EventArgs e)
    {
        // Puedes dejarlo vacío o ponerle lógica provisional
    }
    private async void TestSupabaseConnection()
    {
        try
        {
            // Intentamos inicializar el cliente de Supabase explícitamente
            await _supabaseClient.InitializeAsync();

            // Si llega aquí sin lanzar error, la conexión fue exitosa
            System.Diagnostics.Debug.WriteLine("¡Conexión exitosa con Supabase!");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al conectar con Supabase: {ex.Message}");
        }
    }
}
