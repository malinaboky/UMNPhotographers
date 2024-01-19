using System.ComponentModel.DataAnnotations.Schema;

namespace UMNPhotographers.Distribution.Domain.Entities;

[Table("photographer_freetime")]
public class PhotographerFreetime : BaseEntity
{
    [Column("photographer_schedule_id")]
    public long PhotographerScheduleId { get; set; }
    
    [Column("start_time")]
    public DateTime StartTime { get; set; }
    
    [Column("end_time")]
    public DateTime EndTime { get; set; }
    
    [ForeignKey("PhotographerScheduleId")]
    public PhotographerSchedule PhotographerSchedule { get; set; }
}