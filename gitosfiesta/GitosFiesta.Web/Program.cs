using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using GitosFiesta.Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<Routes>("#app"); // <--- Aquí cambiamos App por Routes
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// --- CONFIGURACIÓN GLOBAL DE SUPABASE ---
var supabaseUrl = "https://qokxfhxkbdoqgvlnzruh.supabase.co"; //

var supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InFva3hmaHhrYmRvcWd2bG56cnVoIiwicm9sZSI6ImFub24iLCJpYXQiOjE3OTEyNDY4MjEsImV4cCI6MjEwNjgyMjgyMX0.HiRxtNy_Q4MlVZP59Yj8vhX4oPzHs3R58TSd4P1Qbpg";        

var options = new Supabase.SupabaseOptions
{
    AutoConnectRealtime = true
};

var supabaseClient = new Supabase.Client(supabaseUrl, supabaseKey, options);
builder.Services.AddSingleton(supabaseClient);
// ----------------------------------------

await builder.Build().RunAsync();



