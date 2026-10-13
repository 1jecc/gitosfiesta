using gitosfiesta.Models;

namespace gitosfiesta;

public partial class ContratoPage : ContentPage
{
    private readonly Supabase.Client? _supabaseClient;
    private readonly long _eventoId;
    private EventoModel? _eventoActual;
    private string? _firmaBase64;

    public ContratoPage(Supabase.Client supabaseClient, long eventoId)
    {
        InitializeComponent();
        _supabaseClient = supabaseClient;
        _eventoId = eventoId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarDatosContratoAsync();
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

            // 2. Cargar términos y condiciones sin duplicar
            var responseMisc = await _supabaseClient
                .From<MiscModel>()
                .Where(x => x.categoria == "TERMINO_CONTRATO" && x.enable == true)
                .Get();

            var terminos = responseMisc.Models?
                .OrderBy(x => int.TryParse(x.value1, out int num) ? num : 0)
                .ToList();

            if (terminos != null && terminos.Count > 0)
            {
                LblTerminos.Text = string.Join("\n\n", terminos.Select(t => $"{t.value1}.- {t.value2}"));
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

        await Navigation.PushAsync(pizarronPage);
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
                await Navigation.PopToRootAsync();
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