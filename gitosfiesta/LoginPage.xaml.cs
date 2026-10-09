using gitosfiesta.Models;

namespace gitosfiesta;

public partial class LoginPage : ContentPage
{
    // Agregamos el signo de interrogación (?) para indicar que puede ser nulo de forma segura
    private readonly Supabase.Client? _supabaseClient;

    public LoginPage()
    {
        InitializeComponent();

        // Obtenemos la instancia global de Supabase
        _supabaseClient = App.SupabaseClient;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string usuario = TxtUsuario.Text?.Trim() ?? string.Empty;
        string password = TxtPassword.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(password))
        {
            await DisplayAlert("Campos requeridos", "Por favor, completa el usuario y la contraseña.", "Aceptar");
            return;
        }

        if (_supabaseClient == null)
        {
            await DisplayAlert("Error", "El cliente de Supabase no está inicializado.", "Aceptar");
            return;
        }

        try
        {
            var response = await _supabaseClient
                .From<Usuario>()
                .Where(x => x.NombreUsuario == usuario && x.Password == password && x.Enable == true)
                .Get();

            var userFound = response.Models.FirstOrDefault();

            if (userFound != null)
            {
                await DisplayAlert("¡Éxito!", $"Bienvenido/a, {userFound.Nombre} {userFound.ApellidoPaterno}\nRol: {userFound.AccessType}", "Continuar");

                // Llamamos a la carga dinámica del menú y las vistas
                CargarMenuDinamico();
            }
            else
            {
                await DisplayAlert("Acceso denegado", "Usuario o contraseña incorrectos.", "Aceptar");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error de conexión", $"No se pudo comunicar con el servidor: {ex.Message}", "Aceptar");
        }
    }

    private async void CargarMenuDinamico()
    {
        try
        {
            // Consultamos todos los elementos habilitados de la tabla de menú ordenados por ID o categoría
            var response = await _supabaseClient!
                .From<MenuItemModel>()
                .Where(x => x.Enable == true)
                .Get();

            var menuItems = response.Models;

            // Aquí creamos una estructura de Flyout / Shell de forma dinámica
            var shell = new Shell();

            foreach (var item in menuItems)
            {
                // item.Description -> El texto que se muestra en el menú (ej. "Inicio")
                // item.Value1 -> La página o ruta asociada (ej. "inicio")
                // item.Value2 -> La categoría (ej. "General")

                var contentPage = ObtenerPaginaPorNombre(item.Value1);
                if (contentPage != null)
                {
                    var flyoutItem = new FlyoutItem
                    {
                        Title = item.Description,
                        Items = { new ShellContent { Content = contentPage } }
                    };

                    shell.Items.Add(flyoutItem);
                }
            }

            // Reemplazamos la pantalla principal con el Shell dinámico ya construido
            Application.Current!.Windows[0].Page = shell;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo cargar el menú dinámico: {ex.Message}", "Aceptar");
        }
    }

    // Método auxiliar para asociar el texto de la base de datos con tus páginas de C# pasando el cliente de Supabase
    private Page? ObtenerPaginaPorNombre(string nombreApi)
    {
        return nombreApi.ToLower() switch
        {
            "inicio" => new MainPage(_supabaseClient!), // Pasamos el cliente requerido por MainPage
            // "usuarios" => new UsuariosPage(_supabaseClient!),
            // "contratos" => new ContratosPage(_supabaseClient!),
            _ => null
        };
    }
}