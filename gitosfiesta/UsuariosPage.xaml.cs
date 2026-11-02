using gitosfiesta.Models;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace gitosfiesta;

public partial class UsuariosPage : ContentPage
{
    private readonly Supabase.Client _supabaseClient;
    public ObservableCollection<Usuario> UsuariosRegistradosList { get; set; } = new();

    public UsuariosPage(Supabase.Client supabaseClient)
    {
        InitializeComponent();
        _supabaseClient = supabaseClient;
        ColeccionUsuariosRegistrados.ItemsSource = UsuariosRegistradosList;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        string rolUsuario = SessionContext.Rol?.ToUpper() ?? "";

        if (rolUsuario != "ADMINISTRADOR" && rolUsuario != "DEV")
        {
            PanelCrearUsuario.IsVisible = false;
            LblAccesoDenegado.IsVisible = true;
        }
        else
        {
            PanelCrearUsuario.IsVisible = true;
            LblAccesoDenegado.IsVisible = false;

            await CargarUsuariosAsync();
        }
    }

    private void OnNombreOApellidoChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is Entry entry)
        {
            string textoActual = e.NewTextValue ?? "";
            string textoFiltrado = Regex.Replace(textoActual, @"[^a-zA-ZÁÉÍÓÚáéíóúÑñÜü\s]", "");
            textoFiltrado = Regex.Replace(textoFiltrado, @"\s+", " ");

            if (textoFiltrado != textoActual)
            {
                entry.Text = textoFiltrado;
                return;
            }
        }

        string nombreCompleto = TxtNuevoNombre.Text?.Trim() ?? "";
        string apellido = TxtNuevoApellido.Text?.Trim() ?? "";

        // Generar usuario usando únicamente el primer nombre + 3 letras del apellido
        string primerNombre = nombreCompleto.Split(' ').FirstOrDefault() ?? "";
        string parteApellido = apellido.Length >= 3 ? apellido.Substring(0, 3) : apellido;

        TxtNuevoUsuario.Text = (primerNombre + parteApellido).ToUpper();
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
            await DisplayAlert("¡Éxito!", "Usuario creado correctamente.", "Aceptar");

            TxtNuevoNombre.Text = "";
            TxtNuevoApellido.Text = "";
            TxtNuevoUsuario.Text = "";
            TxtNuevaPassword.Text = "";
            PickerRol.SelectedIndex = -1;

            await CargarUsuariosAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo crear el usuario: {ex.Message}", "Aceptar");
        }
    }

    private async Task CargarUsuariosAsync()
    {
        try
        {
            var response = await _supabaseClient.From<Usuario>().Get();
            var todos = response.Models ?? new List<Usuario>();

            // Filtramos para excluir a los usuarios con AccessType igual a "DEV" (ignorando mayúsculas/minúsculas)
            var usuariosFiltrados = todos
                .Where(u => string.IsNullOrEmpty(u.AccessType) || !u.AccessType.Equals("DEV", StringComparison.OrdinalIgnoreCase))
                .ToList();

            UsuariosRegistradosList.Clear();
            foreach (var u in usuariosFiltrados)
            {
                UsuariosRegistradosList.Add(u);
            }
        }
        catch { }
    }

    private async void OnEliminarUsuarioDirectoClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Usuario usuarioSeleccionado)
        {
            if (usuarioSeleccionado.Id == SessionContext.UsuarioId)
            {
                await DisplayAlert("No permitido", "No puedes eliminar tu propia cuenta mientras estás conectado.", "Aceptar");
                return;
            }

            bool confirmar = await DisplayAlert("Confirmar", $"¿Estás seguro de eliminar permanentemente a {usuarioSeleccionado.NombreUsuario}?", "Sí", "No");
            if (!confirmar) return;

            try
            {
                await _supabaseClient.From<Usuario>().Where(x => x.Id == usuarioSeleccionado.Id).Delete();
                await DisplayAlert("Eliminado", "Usuario borrado correctamente.", "Aceptar");
                await CargarUsuariosAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "Aceptar");
            }
        }
    }

    private async void OnResetearPasswordClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Usuario usuarioSeleccionado)
        {
            string nuevaPassword = await DisplayPromptAsync("Resetear Contraseña", $"Ingresa la nueva contraseña para {usuarioSeleccionado.NombreUsuario}:", "Guardar", "Cancelar", keyboard: Keyboard.Plain);

            if (string.IsNullOrWhiteSpace(nuevaPassword)) return;

            try
            {
                await _supabaseClient
                    .From<Usuario>()
                    .Where(x => x.Id == usuarioSeleccionado.Id)
                    .Set(x => x.Password, nuevaPassword.Trim())
                    .Update();

                await DisplayAlert("¡Éxito!", $"La contraseña de {usuarioSeleccionado.NombreUsuario} ha sido actualizada.", "Aceptar");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo actualizar la contraseña: {ex.Message}", "Aceptar");
            }
        }
    }
}