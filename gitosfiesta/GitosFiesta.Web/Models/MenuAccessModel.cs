using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace gitosfiesta.Models
{
    [Table("gitosfiesta_menuaccess")]
    public class MenuAccessModel : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [Column("enable")]
        public bool Enable { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("apicontroller")]
        public string ApiController { get; set; } = string.Empty;

        [Column("access")]
        public string Access { get; set; } = string.Empty;
    }
}