using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace gitosfiesta.Models;

[Table("gitosfiesta_misc")]
public class TipoAcceso : BaseModel
{
    [PrimaryKey("id", true)]
    public long Id { get; set; }

    [Column("categoria")]
    public string Categoria { get; set; } = string.Empty;

    [Column("value1")]
    public string Value1 { get; set; } = string.Empty;

    [Column("value2")]
    public string? Value2 { get; set; }

    [Column("enable")]
    public bool Enable { get; set; } = true;
}