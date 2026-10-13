using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
namespace gitosfiesta.Models;

[Table("gitosfiesta_eventos")]
public class EventoModel : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("folio")]
    public string? folio { get; set; }

    [Column("fecha_contrato")]
    public DateTime fecha_contrato { get; set; }

    [Column("nombre_cliente")]
    public string? nombre_cliente { get; set; }

    [Column("telefono_cliente")]
    public string? telefono_cliente { get; set; }

    [Column("tipo_evento")]
    public string? tipo_evento { get; set; }

    [Column("fecha_evento")]
    public DateTime fecha_evento { get; set; }

    [Column("hora_evento")]
    public TimeSpan hora_evento { get; set; }

    [Column("numero_personas")]
    public int numero_personas { get; set; }

    [Column("menu")]
    public string? menu { get; set; }

    [Column("precio")]
    public decimal precio { get; set; }

    [Column("anticipo")]
    public decimal anticipo { get; set; }

    [Column("promocion")]
    public string? promocion { get; set; }

    [Column("observaciones")]
    public string? observaciones { get; set; }

    [Column("estatus")]
    public string? estatus { get; set; }

    [Column("usuario_creacion_id")]
    public long usuario_creacion_id { get; set; }

    [Column("enable")]
    public bool enable { get; set; }

    [Column("firma")]
    public string? firma { get; set; }

    [Column("paquete")]
    public string? paquete { get; set; }
}