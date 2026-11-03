using gitosfiesta.Models;
using gitosfiesta.Services;

namespace gitosfiesta;

public partial class ContratoPage : ContentPage
{
    private readonly Supabase.Client? _supabaseClient;
    private readonly long _eventoId;
    private readonly ContratoApiService _contratoApiService;
    private EventoModel? _eventoActual;
    private string? _firmaBase64;
    private List<MiscModel> _listaTerminosCache = new(); // Para guardar los términos cargados
    private bool _datosCargados = false; // Bandera para evitar que se reseteen los datos

    public ContratoPage(Supabase.Client supabaseClient, long eventoId)
    {
        InitializeComponent();
        _supabaseClient = supabaseClient;
        _eventoId = eventoId;
        _contratoApiService = App.Current?.Handler?.MauiContext?.Services
            .GetService<ContratoApiService>()
            ?? new ContratoApiService(new HttpClient
            {
                BaseAddress = new Uri("https://gitosfiesta-api.onrender.com/")
            });
    }

    private async void OnGenerarPdfClicked(object sender, EventArgs e)
    {
        if (_eventoActual == null)
        {
            await DisplayAlert("Aviso", "El evento todavía no se ha cargado.", "Aceptar");
            return;
        }

        try
        {
            if (sender is Button button)
                button.IsEnabled = false;

            var pdfBytes = await _contratoApiService.GenerarPdfAsync(
                _eventoActual,
                _listaTerminosCache,
                _firmaBase64);

            var ruta = Path.Combine(
                FileSystem.CacheDirectory,
                $"contrato-{_eventoActual.Id}.pdf");

            await File.WriteAllBytesAsync(ruta, pdfBytes);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Compartir contrato",
                File = new ShareFile(ruta)
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Error al generar contrato",
                $"No se pudo obtener el PDF desde el servidor: {ex.Message}",
                "Aceptar");
        }
        finally
        {
            if (sender is Button button)
                button.IsEnabled = true;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Solo cargamos la información la primera vez que entra para evitar que se borre al regresar de otras vistas
        if (!_datosCargados)
        {
            await CargarDatosContratoAsync();
            _datosCargados = true;
        }
    }

    private async Task CargarDatosContratoAsync()
    {
        try
        {
            if (_supabaseClient == null) return;

            // 1. Cargar datos del evento
            var responseEvento = await _supabaseClient
                .From<EventoModel>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, _eventoId)
                .Get();

            _eventoActual = responseEvento.Models?.FirstOrDefault();
            if (_eventoActual != null)
            {
                LblFolio.Text = _eventoActual.folio ?? $"#{_eventoActual.Id}";
                LblFechaContrato.Text = _eventoActual.fecha_contrato.ToString("dd/MM/yyyy");
                LblNombre.Text = _eventoActual.nombre_cliente;
                LblTelefono.Text = _eventoActual.telefono_cliente;
                LblEvento.Text = _eventoActual.tipo_evento;
                LblFechaEvento.Text = _eventoActual.fecha_evento.ToString("dd/MM/yyyy");
                LblHora.Text = _eventoActual.hora_evento.ToString(@"hh\:mm");
                LblPersonas.Text = _eventoActual.numero_personas.ToString();
                LblMenu.Text = _eventoActual.menu;
                LblPrecio.Text = $"${_eventoActual.precio:F2}";
                LblAnticipo.Text = $"${_eventoActual.anticipo:F2}";
                LblSaldo.Text = $"${(_eventoActual.precio - _eventoActual.anticipo):F2}";
                LblPromocion.Text = string.IsNullOrWhiteSpace(_eventoActual.promocion) ? "Ninguna" : _eventoActual.promocion;
                LblObservaciones.Text = string.IsNullOrWhiteSpace(_eventoActual.observaciones) ? "Ninguna" : _eventoActual.observaciones;

                if (!string.IsNullOrEmpty(_eventoActual.firma))
                {
                    _firmaBase64 = _eventoActual.firma;
                    byte[] bytesFirma = Convert.FromBase64String(_firmaBase64);
                    ImgFirma.Source = ImageSource.FromStream(() => new MemoryStream(bytesFirma));
                }
            }

            // 2. Cargar términos y condiciones y guardarlos en caché
            var responseMisc = await _supabaseClient
                .From<MiscModel>()
                .Where(x => x.categoria == "TERMINO_CONTRATO" && x.enable == true)
                .Get();

            _listaTerminosCache = responseMisc.Models?
                .OrderBy(x => int.TryParse(x.value1, out int num) ? num : 0)
                .ToList() ?? new List<MiscModel>();

            if (_listaTerminosCache.Count > 0)
            {
                LblTerminos.Text = string.Join("\n\n", _listaTerminosCache.Select(t => $"{t.value1}.- {t.value2}"));
            }
            else
            {
                LblTerminos.Text = "No se encontraron términos cargados.";
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo cargar el contrato: {ex.Message}", "Aceptar");
        }
    }

    // Método para abrir la página independiente de Términos y Condiciones al presionar el botón
    private async void OnVerTerminosClicked(object sender, EventArgs e)
    {
        if (_listaTerminosCache.Count > 0)
        {
            await Navigation.PushAsync(new TerminosPage(_listaTerminosCache));
        }
        else
        {
            await DisplayAlert("Aviso", "Los términos aún no se han cargado o no están disponibles.", "Aceptar");
        }
    }

    private async void OnAbrirPizarronClicked(object sender, EventArgs e)
    {
        var pizarronPage = new PizarronFirmaPage();

        pizarronPage.Disappearing += (s, args) =>
        {
            if (!string.IsNullOrEmpty(pizarronPage.FirmaBase64))
            {
                _firmaBase64 = pizarronPage.FirmaBase64;
                byte[] bytesFirma = Convert.FromBase64String(_firmaBase64);
                ImgFirma.Source = ImageSource.FromStream(() => new MemoryStream(bytesFirma));
            }
        };

        // Abre la página directamente como modal limpio
        await Navigation.PushModalAsync(pizarronPage);
    }


    private async void OnFinalizarContratoClicked(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(_firmaBase64))
            {
                bool continuarSinFirma = await DisplayAlert("Sin firma", "¿Deseas finalizar sin capturar la firma del contrato?", "Sí", "No");
                if (!continuarSinFirma) return;
            }

            if (_eventoActual != null && _supabaseClient != null)
            {
                _eventoActual.firma = _firmaBase64;

                await _supabaseClient
                    .From<EventoModel>()
                    .Where(x => x.Id == _eventoId)
                    .Update(_eventoActual);

                await DisplayAlert("¡Listo!", "El contrato ha sido guardado y firmado exitosamente.", "Aceptar");

                // =========================================================================
                // SOLUCIÓN DEFINITIVA: Navegar directo a la lista y limpiar el flujo anterior
                // =========================================================================

                // 1. Abrimos la lista de eventos como la nueva página principal de la pila
                await Navigation.PushAsync(new ListaEventoPage(_supabaseClient));

                // 2. Removemos de la pila de navegación tanto la página de Contrato como la de Nuevo Evento
                var navStack = Navigation.NavigationStack.ToList();
                foreach (var page in navStack)
                {
                    if (page is ContratoPage || page is NuevoEventoPage)
                    {
                        Navigation.RemovePage(page);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo guardar la firma: {ex.Message}", "Aceptar");
        }
    }

    private async void OnCancelarClicked(object sender, EventArgs e)
    {
        bool confirmar = await DisplayAlert("Cancelar Evento",
            "¿Estás seguro de cancelar este registro? Los datos y el folio creado se eliminarán permanentemente de la base de datos.",
            "Sí, Cancelar", "No");

        if (!confirmar) return;

        try
        {
            if (_supabaseClient != null)
            {
                await _supabaseClient
                    .From<EventoModel>()
                    .Where(x => x.Id == _eventoId)
                    .Delete();

                await DisplayAlert("Registro Eliminado", "El evento fue cancelado y borrado de la base de datos correctamente.", "Aceptar");
                await Navigation.PopToRootAsync();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error al eliminar", $"No se pudo cancelar el registro: {ex.Message}", "Aceptar");
        }
    }
}