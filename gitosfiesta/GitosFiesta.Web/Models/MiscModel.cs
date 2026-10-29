using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace GitosFiesta.Web.Models;

[Table("gitosfiesta_misc")]
public class MiscModel : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("categoria")]
    public string? categoria { get; set; }

    [Column("value1")]
    public string? value1 { get; set; }

    [Column("value2")]
    public string? value2 { get; set; }

    [Column("enable")]
    public bool enable { get; set; }
}
