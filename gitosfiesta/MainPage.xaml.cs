namespace gitosfiesta;

public partial class MainPage : ContentPage
{
    private readonly Supabase.Client _supabaseClient;

    public MainPage(Supabase.Client supabaseClient)
    {
        InitializeComponent();
        _supabaseClient = supabaseClient;

        TestSupabaseConnection();
    }

    private void OnCounterClicked(object sender, EventArgs e)
    {
        // Acción rápida (puedes programarlo después)
    }

    private async void TestSupabaseConnection()
    {
        try
        {
            await _supabaseClient.InitializeAsync();
            System.Diagnostics.Debug.WriteLine("¡Conexión exitosa con Supabase!");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al conectar con Supabase: {ex.Message}");
        }
    }
}