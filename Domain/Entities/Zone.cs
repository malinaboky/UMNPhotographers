using System.ComponentModel.DataAnnotations.Schema;

namespace UMNPhotographers.Distribution.Domain.Entities;

[Table("zone")]
public class Zone : BaseEntity
{
    [Column("event_id")]
    public long EventId { get; set; }
    
    [ForeignKey("EventId")]
    public Event Event { get; set; }
}