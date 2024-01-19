using UMNPhotographers.Distribution.Domain.Entities;

namespace UMNPhotographers.Distribution.Models;

public class ActivityInfo : BaseEntity
{
    public long ZoneId;
    public int PhotographersCount;
    public int Priority;
    public ShootingType ShootingType;
    public double ShootingTime;
    public DateTime StartTime;
    public DateTime EndTime;
}