using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace gitosfiesta.Models;

[Table("gitosfiesta_abonos")]
public class AbonoModel : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("evento_id")]
    public long EventoId { get; set; }

    [Column("monto")]
    public decimal Monto { get; set; }

    [Column("fecha_pago")]
    public DateTime FechaPago { get; set; }

    [Column("metodo_pago")]
    public string? MetodoPago { get; set; }

    [Column("enable")]
    public bool Enable { get; set; } = true;
}