using UMNPhotographers.Distribution.Domain.Entities;

namespace UMNPhotographers.Distribution.Models;

public class PhotographerInfo : BaseEntity
{
    public long Id;
    public int ZonePriority;
    public double Rating;
    public List<Time> FreeTime;
    public bool SelectedOnZone;
    public List<ScheduleInfo> ActivitySchedules = new List<ScheduleInfo>();
}