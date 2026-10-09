using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace gitosfiesta.Models;

[Table("gitosfiesta_menu")]
public class MenuItemModel : BaseModel
{
    [PrimaryKey("id", true)]
    public long Id { get; set; }

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("value1")]
    public string Value1 { get; set; } = string.Empty; // Nombre de la página/controlador

    [Column("value2")]
    public string? Value2 { get; set; } // Categoría (ej. General, Administración, Contratos)

    [Column("enable")]
    public bool Enable { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}