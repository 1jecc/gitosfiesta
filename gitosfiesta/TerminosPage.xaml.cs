using gitosfiesta.Models;

namespace gitosfiesta;

public partial class TerminosPage : ContentPage
{
    public TerminosPage(List<MiscModel> terminos)
    {
        InitializeComponent();
        CargarTerminosEnInterfaz(terminos);
    }

    private void CargarTerminosEnInterfaz(List<MiscModel> terminos)
    {
        ContenedorTerminos.Children.Clear();

        foreach (var t in terminos)
        {
            // Alternamos colores sutiles entre filas para mayor legibilidad
            bool esPar = int.TryParse(t.value1, out int n) && n % 2 == 0;
            string colorFondo = esPar ? "#FFFFFF" : "#F8F9FA";

            var gridFila = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = 40 },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                ColumnSpacing = 10,
                BackgroundColor = Color.FromArgb(colorFondo),
                Padding = new Thickness(12)
            };

            var lblNumero = new Label
            {
                Text = t.value1,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#1B1464"),
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalOptions = LayoutOptions.Center
            };

            var lblTexto = new Label
            {
                Text = t.value2,
                FontSize = 13,
                TextColor = Color.FromArgb("#1E293B"),
                VerticalOptions = LayoutOptions.Center
            };

            Grid.SetColumn(lblNumero, 0);
            Grid.SetColumn(lblTexto, 1);

            gridFila.Children.Add(lblNumero);
            gridFila.Children.Add(lblTexto);

            ContenedorTerminos.Children.Add(gridFila);
        }
    }

    private async void OnEntendidoClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}