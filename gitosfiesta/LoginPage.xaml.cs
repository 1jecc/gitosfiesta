using gitosfiesta.Models;
using Microsoft.Maui.Controls.Shapes;

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
                // Guardamos los datos de sesión de manera global e interna
                SessionContext.UsuarioId = (int)userFound.Id;
                SessionContext.NombreCompleto = $"{userFound.Nombre} {userFound.ApellidoPaterno}";
                SessionContext.Rol = userFound.AccessType;

                // Transición directa al menú dinámico
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
            var response = await _supabaseClient!
                .From<MenuAccessModel>()
                .Get();

            var menuItems = response.Models?
                .Where(x => x.Enable == true &&
                            !string.IsNullOrEmpty(x.Access) &&
                            x.Access.Trim().Equals(rolUsuario?.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (menuItems == null || menuItems.Count == 0)
            {
                await DisplayAlert("Aviso", $"No hay pantallas configuradas para el rol: {rolUsuario}", "Aceptar");
                return;
            }

            // 1. Instanciamos el Shell eliminando los colores oscuros de la NavigationBar
            var shell = new Shell
            {
                FlyoutBackgroundColor = Color.FromArgb("#F8F9FE")
            };

            // Eliminamos la barra negra superior y ponemos el turquesa de Gitos Fiesta
            Shell.SetBackgroundColor(shell, Color.FromArgb("#00D2C4"));
            Shell.SetForegroundColor(shell, Colors.White);
            Shell.SetTitleColor(shell, Colors.White);

            // 2. Encabezado superior con la marca Gitos Fiesta (al estilo de la imagen)
            var headerLayout = new VerticalStackLayout
            {
                Padding = new Thickness(20, 35, 20, 15),
                Spacing = 10,
                Children =
                {
                    new Image
                    {
                        Source = "alargado.png",
                        Aspect = Aspect.AspectFit,
                        HorizontalOptions = LayoutOptions.Start
                    },
                }
            };

            shell.FlyoutHeader = headerLayout;

            // 3. Plantilla de botones del menú (Estilo tarjeta suave con bordes redondeados)
            shell.ItemTemplate = new DataTemplate(() =>
            {
                var border = new Border
                {
                    BackgroundColor = Color.FromArgb("#EAE8F8"),
                    StrokeShape = new RoundRectangle { CornerRadius = 14 },
                    Stroke = Colors.Transparent,
                    Padding = new Thickness(16, 12),
                    Margin = new Thickness(16, 4)
                };

                var label = new Label
                {
                    FontSize = 15,
                    TextColor = Color.FromArgb("#3F3B78"),
                    FontAttributes = FontAttributes.Bold,
                    VerticalOptions = LayoutOptions.Center
                };
                label.SetBinding(Label.TextProperty, "Title");

                border.Content = label;
                return border;
            });

            // 4. Pie de página del menú (Perfil del Usuario abajo, como en la imagen de referencia)
            var footerLayout = new Grid
            {
                Padding = new Thickness(20, 15),
                BackgroundColor = Color.FromArgb("#F0EEF9"),
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            var userDetails = new VerticalStackLayout
            {
                Spacing = 2,
                Children =
                {
                    new Label
                    {
                        Text = SessionContext.NombreCompleto,
                        TextColor = Color.FromArgb("#2D2B55"),
                        FontSize = 14,
                        FontAttributes = FontAttributes.Bold
                    },
                    new Label
                    {
                        Text = SessionContext.Rol,
                        TextColor = Color.FromArgb("#8A88AD"),
                        FontSize = 12
                    }
                }
            };

            // Boton cerrar sesion 
            var btnCerrarSesion = new Button
            {
                Text = "Salir",
                BackgroundColor = Color.FromArgb("#E57373"),
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 8,
                HeightRequest = 35,
                Padding = new Thickness(15, 0),
                VerticalOptions = LayoutOptions.Center
            };

            btnCerrarSesion.Clicked += async (s, e) =>
            {
                bool confirmar = await Application.Current!.MainPage!.DisplayAlert(
                    "Cerrar Sesi[on",
                    "Estas seguro de que deseas cerrar sesion?",
                    "Si, salir",
                    "Cancelar");
                if (confirmar)
                {
                    SessionContext.UsuarioId = 0;
                    SessionContext.NombreCompleto = string.Empty;
                    SessionContext.Rol = string.Empty;

                    Application.Current.Windows[0].Page = new LoginPage();
                }
            };

            Grid.SetColumn(userDetails, 0);
            Grid.SetColumn(btnCerrarSesion, 1); 
            footerLayout.Children.Add(userDetails);
            footerLayout.Children.Add(btnCerrarSesion);
            shell.FlyoutFooter = footerLayout;

            // 5. Cargar los elementos del menú
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
            System.Diagnostics.Debug.WriteLine($"Error al cargar página dinámica: {ex.Message}");
        }

        return null;
    }
}