using gitosfiesta.Models;

namespace gitosfiesta;

public partial class LoginPage : ContentPage
{
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

                // Llamamos a la carga dinámica pasando el rol/tipo de acceso del usuario encontrado
                CargarMenuDinamicoParaUsuario(userFound.AccessType);
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

    private async void CargarMenuDinamicoParaUsuario(string rolUsuario)
    {
        try
        {
            // Descargamos los registros de la tabla y filtramos en C# de forma segura 
            // para ignorar diferencias de mayúsculas/minúsculas o espacios accidentales.
            var response = await _supabaseClient!
                .From<MenuAccessModel>()
                .Get();

            var menuItems = response.Models?
                .Where(x => x.Enable == true &&
                            !string.IsNullOrEmpty(x.Access) &&
                            x.Access.Trim().Equals(rolUsuario?.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Verificación de seguridad si la lista sigue vacía
            if (menuItems == null || menuItems.Count == 0)
            {
                await DisplayAlert("Aviso", $"No hay pantallas configuradas para el rol: {rolUsuario}", "Aceptar");
                return;
            }

            var shell = new Shell();

            foreach (var item in menuItems)
            {
                var contentPage = ObtenerPaginaDinamica(item.ApiController);

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

            if (shell.Items.Count > 0)
            {
                Application.Current!.Windows[0].Page = shell;
            }
            else
            {
                await DisplayAlert("Error", "No se pudo cargar ninguna vista válida para este usuario.", "Aceptar");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo cargar el menú dinámico: {ex.Message}", "Aceptar");
        }
    }

    private Page? ObtenerPaginaDinamica(string nombreApi)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(nombreApi))
                return null;

            string nombreLimpio = nombreApi.Replace(" ", "").Trim();

            string nombreCompleto = nombreLimpio.ToLower() == "inicio"
                ? "gitosfiesta.MainPage"
                : $"gitosfiesta.{nombreLimpio}Page";

            var tipoPagina = Type.GetType(nombreCompleto);

            if (tipoPagina != null)
            {
                var constructorConParametro = tipoPagina.GetConstructor(new[] { typeof(Supabase.Client) });

                if (constructorConParametro != null)
                {
                    return constructorConParametro.Invoke(new object[] { _supabaseClient! }) as Page;
                }
                else
                {
                    return Activator.CreateInstance(tipoPagina) as Page;
                }
            }
        }
        catch (Exception ex)
        {
            // CORREGIDO: Cambiado a WriteLine correctamente
            System.Diagnostics.Debug.WriteLine($"Error al cargar página dinámica: {ex.Message}");
        }

        return null;
    }
}