using gitosfiesta.Models;

namespace gitosfiesta;

public partial class UsuariosPage : ContentPage
{
    private readonly Supabase.Client _supabaseClient;
    private List<Usuario> _listaUsuariosActivos = new();

    public UsuariosPage(Supabase.Client supabaseClient)
    {
        InitializeComponent();
        _supabaseClient = supabaseClient;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        string rolUsuario = SessionContext.Rol?.ToUpper() ?? "";

        // Si NO es Administrador ni Dev, ocultamos los paneles y mostramos mensaje
        if (rolUsuario != "ADMINISTRADOR" && rolUsuario != "DEV")
        {
            PanelCrearUsuario.IsVisible = false;
            PanelEliminarUsuario.IsVisible = false;
            LblAccesoDenegado.IsVisible = true;
        }
        else
        {
            PanelCrearUsuario.IsVisible = true;
            PanelEliminarUsuario.IsVisible = true;
            LblAccesoDenegado.IsVisible = false;

            _ = CargarUsuariosParaEliminarAsync();
        }
    }

    private void OnNombreOApellidoChanged(object sender, TextChangedEventArgs e)
    {
        string nombre = TxtNuevoNombre.Text?.Trim() ?? "";
        string apellido = TxtNuevoApellido.Text?.Trim() ?? "";

        string nombreCompleto = nombre.Replace(" ", "");
        string parteApellido = apellido.Length >= 3 ? apellido.Substring(0, 3) : apellido;

        TxtNuevoUsuario.Text = (nombreCompleto + parteApellido).ToUpper();
    }

    private async void OnCrearUsuarioClicked(object sender, EventArgs e)
    {
        string nombre = TxtNuevoNombre.Text?.Trim() ?? "";
        string apellido = TxtNuevoApellido.Text?.Trim() ?? "";
        string usuario = TxtNuevoUsuario.Text?.Trim() ?? "";
        string password = TxtNuevaPassword.Text?.Trim() ?? "";
        string rol = PickerRol.SelectedItem?.ToString() ?? "";

        if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(apellido) || string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(rol))
        {
            await DisplayAlert("Campos Vacíos", "Por favor, completa todos los campos.", "Aceptar");
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
            await DisplayAlert("¡Éxito!", $"Usuario creado correctamente.", "Aceptar");

            TxtNuevoNombre.Text = ""; TxtNuevoApellido.Text = ""; TxtNuevoUsuario.Text = ""; TxtNuevaPassword.Text = ""; PickerRol.SelectedIndex = -1;
            await CargarUsuariosParaEliminarAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo crear el usuario: {ex.Message}", "Aceptar");
        }
    }

    private async Task CargarUsuariosParaEliminarAsync()
    {
        try
        {
            var response = await _supabaseClient.From<Usuario>().Get();
            _listaUsuariosActivos = (response.Models ?? new List<Usuario>()).Where(u => u.Id != SessionContext.UsuarioId).ToList();
            PickerUsuariosAEliminar.ItemsSource = _listaUsuariosActivos;
        }
        catch { }
    }

    private async void OnEliminarUsuarioClicked(object sender, EventArgs e)
    {
        var usuarioSeleccionado = PickerUsuariosAEliminar.SelectedItem as Usuario;
        if (usuarioSeleccionado == null) return;

        bool confirmar = await DisplayAlert("Confirmar", $"¿Eliminar a {usuarioSeleccionado.NombreUsuario}?", "Sí", "No");
        if (!confirmar) return;

        try
        {
            await _supabaseClient.From<Usuario>().Where(x => x.Id == usuarioSeleccionado.Id).Delete();
            await DisplayAlert("Eliminado", "Usuario borrado.", "Aceptar");
            PickerUsuariosAEliminar.SelectedIndex = -1;
            await CargarUsuariosParaEliminarAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "Aceptar");
        }
    }
}