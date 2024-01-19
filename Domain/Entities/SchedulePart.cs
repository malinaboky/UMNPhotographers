using System.ComponentModel.DataAnnotations.Schema;

namespace UMNPhotographers.Distribution.Domain.Entities;

[Table("photographer_schedule_part")]
public class SchedulePart : BaseEntity
{
    [Column("photographer_schedule_id")]
    public long PhotographerScheduleId { get; set; }
    
    [Column("version")]
    public long Version { get; set; }
    
    [Column("activity_id")]
    public long ActivityId { get; set; }
    
    [Column("start_time")]
    public DateTime StartTime { get; set; }
    
    [Column("end_time")]
    public DateTime EndTime { get; set; }

    [ForeignKey("PhotographerScheduleId")]
    public PhotographerSchedule PhotographerSchedule { get; set; }
    
    [ForeignKey("ActivityId")]
    public Activity Activity { get; set; }
}