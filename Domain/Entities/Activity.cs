using System.ComponentModel.DataAnnotations.Schema;
using UMNPhotographers.Distribution.Models;

namespace UMNPhotographers.Distribution.Domain.Entities;

[Table("activity")]
public class Activity : BaseEntity
{
    [Column("event_id")]
    public long EventId { get; set; }
    
    [Column("zone_id")]
    public long ZoneId { get; set; }
    
    [Column("start_time")]
    public DateTime StartTime { get; set; }
    
    [Column("end_time")]
    public DateTime EndTime { get; set; }
    
    [Column("photographers_count")]
    public int? PhotographersCount { get; set; }
    
    [Column("priority")]
    public int? Priority { get; set; }
    
    [Column("shooting_time")]
    public int? ShootingTime { get; set; }
    
    [Column("shooting_type")]
    public string? ShootingType { get; set; }
    
    [ForeignKey("EventId")]
    public Event Event { get; set; }
    
    [ForeignKey("ZoneId")]
    public Zone Zone { get; set; }
    
    public virtual ICollection<SchedulePart> SchedulePart { get; set; }
}