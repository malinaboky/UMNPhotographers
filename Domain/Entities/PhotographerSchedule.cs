using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Intrinsics.Arm;

namespace UMNPhotographers.Distribution.Domain.Entities;

[Table("photographer_schedule")]
public class PhotographerSchedule : BaseEntity
{
    [Column("event_id")]
    public long EventId { get; set; }
    
    [Column("photographer_id")]
    public long PhotographerId { get; set; }
    
    [Column("published")]
    public bool Published { get; set; }
    
    [ForeignKey("EventId")]
    public Event Event { get; set; }
    
    [ForeignKey("PhotographerId")]
    public Photographer Photographer { get; set; }
    public virtual ICollection<SchedulePart> SchedulePart { get; set; }
    public virtual ICollection<PhotographerZoneInfo> PhotographerZoneInfos { get; set; }
    public virtual ICollection<PhotographerFreetime> PhotographerFreetime { get; set; }
}