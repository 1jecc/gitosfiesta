using gitosfiesta.Models;

namespace gitosfiesta;

public partial class MainPage : ContentPage
{
    private readonly Supabase.Client _supabaseClient;
    private List<Usuario> _listaUsuariosActivos = new();

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

    protected override void OnAppearing()
    {
        base.OnAppearing();

        string rolUsuario = SessionContext.Rol?.ToUpper() ?? "";

        // Si NO es Administrador ni Dev, ocultamos ambos paneles
        if (rolUsuario != "ADMINISTRADOR" && rolUsuario != "DEV")
        {
            PanelCrearUsuario.IsVisible = false;
            PanelEliminarUsuario.IsVisible = false;
        }
        else
        {
            PanelCrearUsuario.IsVisible = true;
            PanelEliminarUsuario.IsVisible = true;

            // Cargamos la lista de usuarios al Picker
            _ = CargarUsuariosParaEliminarAsync();
        }
    }

    // ==========================================
    // LÓGICA DE CREAR USUARIO Y AUTO-GENERAR
    // ==========================================

    private void OnNombreOApellidoChanged(object sender, TextChangedEventArgs e)
    {
        string nombre = TxtNuevoNombre.Text?.Trim() ?? "";
        string apellido = TxtNuevoApellido.Text?.Trim() ?? "";

        string parteNombre = nombre.Length >= 2 ? nombre.Substring(0, 2) : nombre;
        string parteApellido = apellido.Length >= 2 ? apellido.Substring(0, 2) : apellido;

        TxtNuevoUsuario.Text = (parteNombre + parteApellido).ToUpper();
    }

    private async void OnCrearUsuarioClicked(object sender, EventArgs e)
    {
        string nombre = TxtNuevoNombre.Text?.Trim() ?? "";
        string apellido = TxtNuevoApellido.Text?.Trim() ?? "";
        string usuario = TxtNuevoUsuario.Text?.Trim() ?? "";
        string password = TxtNuevaPassword.Text?.Trim() ?? "";
        string rol = PickerRol.SelectedItem?.ToString() ?? "";

        if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(apellido) ||
            string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(rol))
        {
            await DisplayAlert("Campos Vacíos", "Por favor, completa todos los campos y selecciona un rol.", "Aceptar");
            return;
        }

        try
        {
            var nuevoUsuario = new Usuario
            {
                Nombre = nombre,
                ApellidoPaterno = apellido,
                NombreUsuario = usuario,
                Password = password,
                AccessType = rol,
                FechaIngreso = DateTime.Now,
                Enable = true
            };

            await _supabaseClient.From<Usuario>().Insert(nuevoUsuario);

            await DisplayAlert("¡Éxito!", $"El usuario {usuario} ha sido creado como {rol} correctamente.", "Aceptar");

            // Limpiamos los campos
            TxtNuevoNombre.Text = string.Empty;
            TxtNuevoApellido.Text = string.Empty;
            TxtNuevoUsuario.Text = string.Empty;
            TxtNuevaPassword.Text = string.Empty;
            PickerRol.SelectedIndex = -1;

            // Recargamos la lista del panel de eliminar para que incluya al nuevo usuario
            await CargarUsuariosParaEliminarAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo crear el usuario: {ex.Message}", "Aceptar");
        }
    }

    // ==========================================
    // LÓGICA DE ELIMINAR USUARIO
    // ==========================================

    private async Task CargarUsuariosParaEliminarAsync()
    {
        try
        {
            var response = await _supabaseClient.From<Usuario>().Get();
            var todosLosUsuarios = response.Models ?? new List<Usuario>();

            // El administrador actual NO puede borrarse a sí mismo
            _listaUsuariosActivos = todosLosUsuarios
                .Where(u => u.Id != SessionContext.UsuarioId)
                .ToList();

            PickerUsuariosAEliminar.ItemsSource = _listaUsuariosActivos;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al cargar la lista de usuarios: {ex.Message}");
        }
    }

    private async void OnEliminarUsuarioClicked(object sender, EventArgs e)
    {
        var usuarioSeleccionado = PickerUsuariosAEliminar.SelectedItem as Usuario;

        if (usuarioSeleccionado == null)
        {
            await DisplayAlert("Aviso", "Por favor, selecciona un usuario de la lista antes de presionar eliminar.", "Aceptar");
            return;
        }

        bool confirmar = await DisplayAlert("Confirmar Eliminación",
            $"¿Estás seguro que deseas eliminar permanentemente al usuario '{usuarioSeleccionado.NombreUsuario}' ({usuarioSeleccionado.AccessType})?",
            "Sí, Eliminar", "Cancelar");

        if (!confirmar) return;

        try
        {
            await _supabaseClient.From<Usuario>()
                .Where(x => x.Id == usuarioSeleccionado.Id)
                .Delete();

            await DisplayAlert("Eliminado", $"El usuario {usuarioSeleccionado.NombreUsuario} fue borrado del sistema correctamente.", "Aceptar");

            PickerUsuariosAEliminar.SelectedIndex = -1;
            await CargarUsuariosParaEliminarAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo eliminar el usuario: {ex.Message}", "Aceptar");
        }
    }
}