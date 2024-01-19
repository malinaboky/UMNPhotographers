using System.ComponentModel.DataAnnotations.Schema;

namespace UMNPhotographers.Distribution.Domain.Entities;

[Table("allocation_event")]
public class AllocationEvent : BaseEntity
{
    [Column("employee_id")]
    public long EmployeeId { get; set; }
    
    [Column("version")]
    public long Version { get; set; }
    
    [Column("event_time")]
    public DateTime EventTime { get; set; }
    
    [Column("code")]
    public string Code { get; set; }
    
    [Column("message")]
    public string Message { get; set; }
}