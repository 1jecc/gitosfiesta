using Microsoft.Maui.Graphics;

namespace gitosfiesta;

public partial class PizarronFirmaPage : ContentPage, IDrawable
{
    private readonly List<List<PointF>> _trazos = new();
    private List<PointF> _trazoActual = new();
    public string? FirmaBase64 { get; private set; }

    public PizarronFirmaPage()
    {
        InitializeComponent();
        CanvasFirma.Drawable = this;

        // Suscribirse a los eventos táctiles nativos de GraphicsView
        CanvasFirma.StartInteraction += OnStartInteraction;
        CanvasFirma.DragInteraction += OnDragInteraction;
        CanvasFirma.EndInteraction += OnEndInteraction;
    }

    private void OnStartInteraction(object? sender, TouchEventArgs e)
    {
        var punto = e.Touches.FirstOrDefault();
        _trazoActual = new List<PointF> { punto };
        _trazos.Add(_trazoActual);
        CanvasFirma.Invalidate();
    }

    private void OnDragInteraction(object? sender, TouchEventArgs e)
    {
        var punto = e.Touches.FirstOrDefault();
        if (_trazoActual != null)
        {
            _trazoActual.Add(punto);
            CanvasFirma.Invalidate();
        }
    }

    private void OnEndInteraction(object? sender, TouchEventArgs e)
    {
        _trazoActual = new List<PointF>();
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        // 1. Rellenar todo el fondo de blanco para evitar canales transparentes que se vuelvan negros
        canvas.FillColor = Colors.White;
        canvas.FillRectangle(dirtyRect);

        // 2. Configurar los estilos de dibujo para la firma
        canvas.StrokeColor = Colors.Black;
        canvas.StrokeSize = 3;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;

        // 3. Dibujar cada uno de los trazos registrados
        foreach (var trazo in _trazos)
        {
            for (int i = 0; i < trazo.Count - 1; i++)
            {
                canvas.DrawLine(trazo[i], trazo[i + 1]);
            }
        }
    }
    private void OnBorrarClicked(object sender, EventArgs e)
    {
        _trazos.Clear();
        _trazoActual.Clear();
        CanvasFirma.Invalidate();
    }

    private async void OnGuardarFirmaClicked(object sender, EventArgs e)
    {
        if (_trazos.Count == 0)
        {
            await DisplayAlert("Aviso", "Por favor dibuja tu firma antes de guardar.", "Aceptar");
            return;
        }

        var snapshot = await CanvasFirma.CaptureAsync();
        if (snapshot != null)
        {
            using var memoryStream = new MemoryStream();
            await snapshot.CopyToAsync(memoryStream);
            FirmaBase64 = Convert.ToBase64String(memoryStream.ToArray());
        }

        await Navigation.PopAsync();
    }
}