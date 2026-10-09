using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace gitosfiesta.Models;

[Table("gitosfiesta_usuarios")]
public class Usuario : BaseModel
{
    [PrimaryKey("id", true)]
    public long Id { get; set; }

    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Column("apellido_paterno")]
    public string ApellidoPaterno { get; set; } = string.Empty;

    [Column("apellido_materno")]
    public string? ApellidoMaterno { get; set; }

    [Column("usuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Column("password")]
    public string Password { get; set; } = string.Empty;

    [Column("access_type")]
    public string AccessType { get; set; } = string.Empty;

    [Column("fecha_ingreso")]
    public DateTime FechaIngreso { get; set; }

    [Column("fecha_fin")]
    public DateTime? FechaFin { get; set; }

    [Column("enable")]
    public bool Enable { get; set; } = true;
}