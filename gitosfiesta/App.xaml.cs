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
        // Conservamos tu inicio en LoginPage tal como lo tenías
        var window = new Window(new LoginPage());

        window.Created += (s, e) =>
        {
#if WINDOWS
            // Este código maximiza la ventana automáticamente en Windows
            var handle = WinRT.Interop.WindowNative.GetWindowHandle(window.Handler.PlatformView);
            var id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);
            
            if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
            {
                presenter.Maximize();
            }
#endif
        };

        return window;
    }
}