namespace gitosfiesta;

public partial class App : Application
{
    public static Supabase.Client? SupabaseClient { get; private set; }

    public App(Supabase.Client supabaseClient)
    {
        InitializeComponent();
        SupabaseClient = supabaseClient;

        Task.Run(async () =>
        {
            try
            {
                if (SupabaseClient != null)
                {
                    await SupabaseClient.InitializeAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al inicializar Supabase: {ex.Message}");
            }
        });
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new LoginPage());
    }
}