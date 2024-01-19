using System.ComponentModel.DataAnnotations.Schema;

namespace UMNPhotographers.Distribution.Domain.Entities;

[Table("photographer_zone_info")]
public class PhotographerZoneInfo : BaseEntity
{
    [Column("photographer_schedule_id")]
    public long PhotographerScheduleId { get; set; }
    
    [Column("zone_id")]
    public long ZoneId { get; set; }
    
    [Column("priority")]
    public int Priority { get; set; }
    
    [ForeignKey("PhotographerScheduleId")]
    public PhotographerSchedule PhotographerSchedule { get; set; }
    
    [ForeignKey("ZoneId")]
    public Zone Zone { get; set; }
}