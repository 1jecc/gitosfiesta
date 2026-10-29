using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace GitosFiesta.Web.Models;

[Table("gitosfiesta_evento_personal")]
public class EventoPersonalModel : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("evento_id")]
    public long EventoId { get; set; }

    [Column("usuario_id")]
    public long UsuarioId { get; set; }

    [Column("puesto")]
    public string Puesto { get; set; } = string.Empty; // "Cocinero" o "Mesero"

    [Column("rol")]
    public string? Rol { get; set; } // "Principal / Barra", "Atención a Mesas", etc.

    [Column("enable")]
    public bool Enable { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
