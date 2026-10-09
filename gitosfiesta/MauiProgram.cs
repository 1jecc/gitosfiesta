using Microsoft.Extensions.Logging;

namespace gitosfiesta;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // ==========================================
        // CONFIGURACIÓN DE SUPABASE
        // ==========================================
        var supabaseUrl = "https://qokxfhxkbdoqgvlnzruh.supabase.co"; //
        var supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InFva3hmaHhrYmRvcWd2bG56cnVoIiwicm9sZSI6ImFub24iLCJpYXQiOjE3OTEyNDY4MjEsImV4cCI6MjEwNjgyMjgyMX0.HiRxtNy_Q4MlVZP59Yj8vhX4oPzHs3R58TSd4P1Qbpg";        

        var options = new Supabase.SupabaseOptions
        {
            AutoConnectRealtime = true
        };

        // Registramos el cliente de Supabase como un servicio Singleton en el contenedor de dependencias
        builder.Services.AddSingleton(provider =>
            new Supabase.Client(supabaseUrl, supabaseKey, options));

        return builder.Build();
    }
}