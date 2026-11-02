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

    // Evento al presionar Enter en Usuario -> Salta al Password
    private void OnUsuarioCompleted(object sender, EventArgs e)
    {
        TxtPassword.Focus();
    }

    // Evento al presionar Enter en Password -> Inicia sesión automáticamente
    private void OnPasswordCompleted(object sender, EventArgs e)
    {
        OnLoginClicked(sender, e);
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

            // 1. Instanciamos el Shell configurando el fondo general del menú lateral
            var shell = new Shell
            {
                FlyoutBackgroundColor = Color.FromArgb("#F8FAFC") // Fondo gris claro muy moderno y limpio
            };

            // Color de la barra superior principal
            Shell.SetBackgroundColor(shell, Color.FromArgb("#1B1464"));
            Shell.SetForegroundColor(shell, Colors.White);
            Shell.SetTitleColor(shell, Colors.White);

            // 2. ENCABEZADO FLOTANTE CON DISEÑO ELEGANTE Y FONDO #DCE8F0
            var headerLayout = new Grid
            {
                HeightRequest = 210,
                BackgroundColor = Color.FromArgb("#F8FAFC"), // Fondo del menú lateral
                Padding = new Thickness(16, 20, 16, 10)
            };

            // Tarjeta flotante con el fondo celeste claro y esquinas muy redondeadas
            var tarjetaHeader = new Border
            {
                BackgroundColor = Color.FromArgb("#DCE8F0"),
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Stroke = Color.FromArgb("#CBD5E1"),
                StrokeThickness = 1,
                Padding = new Thickness(15),
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };

            // Contenido centrado dentro de la tarjeta
            var contentLayout = new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                Spacing = 8,
                Children =
            {
                new Border
                {
                    WidthRequest = 56,
                    HeightRequest = 56,
                    BackgroundColor = Colors.White,
                    StrokeShape = new RoundRectangle { CornerRadius = 28 },
                    Stroke = Color.FromArgb("#00D2C4"),
                    StrokeThickness = 2,
                    HorizontalOptions = LayoutOptions.Center,
                    Content = new Image
                    {
                        Source = "imagen1.png",
                        WidthRequest = 34,
                        HeightRequest = 34,
                        Aspect = Aspect.AspectFit,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    }
                },
                new Label
                {
                    Text = "Gitos Fiesta",
                    TextColor = Color.FromArgb("#1E293B"),
                    FontSize = 17,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalTextAlignment = TextAlignment.Center
                },
                new Label
                {
                    Text = "Panel de Control",
                    TextColor = Color.FromArgb("#64748B"),
                    FontSize = 11,
                    HorizontalTextAlignment = TextAlignment.Center
                }
            }
            };

            tarjetaHeader.Content = contentLayout;
            headerLayout.Children.Add(tarjetaHeader);
            shell.FlyoutHeader = headerLayout;
            // 3. Plantilla de botones del menú optimizada (Estilo tarjeta moderna con icono y texto)
            shell.ItemTemplate = new DataTemplate(() =>
            {
                var border = new Border
                {
                    BackgroundColor = Color.FromArgb("#FFFFFF"),
                    StrokeShape = new RoundRectangle { CornerRadius = 12 },
                    Stroke = Color.FromArgb("#E2E8F0"),
                    StrokeThickness = 1,
                    Padding = new Thickness(14, 10),
                    Margin = new Thickness(12, 6),
                    HorizontalOptions = LayoutOptions.Fill
                };

                var layoutInterno = new HorizontalStackLayout
                {
                    Spacing = 12,
                    VerticalOptions = LayoutOptions.Center
                };

                var imgIcono = new Image
                {
                    WidthRequest = 22,
                    HeightRequest = 22,
                    VerticalOptions = LayoutOptions.Center,
                    Aspect = Aspect.AspectFit
                };
                imgIcono.SetBinding(Image.SourceProperty, "Icon");

                var label = new Label
                {
                    FontSize = 14,
                    TextColor = Color.FromArgb("#1E293B"),
                    FontAttributes = FontAttributes.Bold,
                    VerticalOptions = LayoutOptions.Center
                };
                label.SetBinding(Label.TextProperty, "Title");

                layoutInterno.Children.Add(imgIcono);
                layoutInterno.Children.Add(label);

                border.Content = layoutInterno;
                return border;
            });

            // 4. Pie de página del menú rediseñado (Perfil de Usuario y Botón de Salir Estilizado)
            var footerLayout = new Grid
            {
                Padding = new Thickness(16, 14),
                BackgroundColor = Color.FromArgb("#F1F5F9"),
                ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
            };

            var userDetails = new VerticalStackLayout
            {
                Spacing = 2,
                VerticalOptions = LayoutOptions.Center,
                Children =
            {
                new Label
                {
                    Text = SessionContext.NombreCompleto,
                    TextColor = Color.FromArgb("#0F172A"),
                    FontSize = 13,
                    FontAttributes = FontAttributes.Bold
                },
                new Label
                {
                    Text = SessionContext.Rol,
                    TextColor = Color.FromArgb("#64748B"),
                    FontSize = 11
                }
            }
            };

            var btnCerrarSesion = new Button
            {
                Text = "Salir",
                BackgroundColor = Color.FromArgb("#EF4444"), // Tono coral/rojo moderno y limpio
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                FontSize = 13,
                CornerRadius = 8,
                HeightRequest = 36,
                Padding = new Thickness(14, 0),
                VerticalOptions = LayoutOptions.Center
            };

            btnCerrarSesion.Clicked += async (s, e) =>
            {
                bool confirmar = await Application.Current!.MainPage!.DisplayAlert(
                    "Cerrar Sesión",
                    "¿Estás seguro de que deseas cerrar sesión?",
                    "Sí, salir",
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

            // 5. Cargar los elementos del menú dinámicamente
            foreach (var item in menuItems)
            {
                var contentPage = ObtenerPaginaDinamica(item.ApiController);

                if (contentPage != null)
                {
                    string iconoAsignado = "dotnet_bot.png";
                    string desc = item.Description?.ToLower() ?? "";
                    string controller = item.ApiController?.ToLower() ?? "";

                    if (desc.Contains("inicio") || controller.Contains("home"))
                        iconoAsignado = "dotnet_bot.png";
                    else if (desc.Contains("evento") || controller.Contains("evento"))
                        iconoAsignado = "dotnet_bot.png";
                    else if (desc.Contains("usuario") || controller.Contains("usuario"))
                        iconoAsignado = "dotnet_bot.png";

                    var flyoutItem = new FlyoutItem
                    {
                        Title = item.Description,
                        Icon = iconoAsignado,
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