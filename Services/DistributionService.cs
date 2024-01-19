using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using UMNPhotographers.Distribution.Domain;
using UMNPhotographers.Distribution.Domain.Entities;
using UMNPhotographers.Distribution.Exception;
using UMNPhotographers.Distribution.Models;

namespace UMNPhotographers.Distribution.Services;

public class DistributionService : IDistributionService
{
    private readonly DataContext _context;
    private readonly IParseService _parseService;
    private const double Coefficient = 2.5;
    private const int MinZonePriority = 1;
    private const int MinActivityPriority = 0;

    public DistributionService(DataContext context, IParseService parseService)
    {
        _context = context;
        _parseService = parseService;
    }
    
    public async Task SaveDistributionToDB(long eventId, long zoneId, List<long> photographerId)
    {
        var activities = this.GetListOfActivitiesFromDataBase(eventId, zoneId);

        if (activities.Count == 0)
            throw new CustomException("zone_empty","На зоне нет активностей");

        var free = this.GetListOfPhotographersFromDataBase(eventId, zoneId, DateOnly.FromDateTime(activities[0].StartTime),
            photographerId);
        
        if (free.Count == 0)
            throw new CustomException("photographers_empty","В списке нет свободных фотографов");
        
        var result = GetScheduleParts(activities, free);

        if (result.Count == 0)
            throw new CustomException("distribution_none","Не получилось сформировать распределение");

        try
        {
            var oldScheduleParts = _context.ScheduleParts
                .Include(x => x.Activity)
                .Where(x => x.Activity.ZoneId == zoneId);
            
            _context.ScheduleParts.RemoveRange(oldScheduleParts);
            await _context.ScheduleParts.AddRangeAsync(result);
            await _context.SaveChangesAsync();
        }
        catch (System.Exception e)
        {
            throw new CustomException("db_connection", "Ошибка подключения к бд");
        }
    }
    
    public bool CheckPhotographersNumber(long eventId, long zoneId, List<long> photographerId)
    {
        var activities = this.GetListOfActivitiesFromDataBase(eventId, zoneId);

        if (activities.Count == 0)
            return false;

        var free = this.GetListOfPhotographersFromDataBase(eventId, zoneId, DateOnly.FromDateTime(activities[0].StartTime),
            photographerId);

        if (free.Count == 0)
            return false;

        return CheckPhotographersNumber(activities, free.SelectMany(x => x.Value).ToList());
    }
    
    private List<ActivityInfo> GetListOfActivitiesFromDataBase(long eventId, long zoneId)
    {
        return _context.Activities
            .Where(x => x.EventId == eventId && x.ZoneId == zoneId)
            .Select(x => new ActivityInfo()
            {
                Id = x.Id, 
                ZoneId = x.ZoneId, 
                Priority = x.Priority ?? MinActivityPriority,
                PhotographersCount = x.PhotographersCount ?? 1,
                ShootingType = _parseService.TryParse(x.ShootingType),
                ShootingTime = _parseService.TryParse(x.ShootingType) == ShootingType.All || x.ShootingTime == null ? 0 : (double)x.ShootingTime,
                StartTime = x.StartTime,
                EndTime = x.EndTime
            })
            .OrderByDescending(x => x.Priority)
            .ToListAsync().Result;
    }

    private Dictionary<int, List<PhotographerInfo>> GetListOfPhotographersFromDataBase(long eventId, long zoneId, DateOnly day, List<long> photographerSchedulesId)
    {
        Random rnd = new Random();

        var other = _context.Activities
            .Include(x => x.SchedulePart)
            .Where(x => x.EventId == eventId && x.ZoneId != zoneId)
            .SelectMany(x => x.SchedulePart);

        return _context.PhotographerSchedules
            .Include(x => x.SchedulePart)
            .Include(x => x.PhotographerFreetime)
            .Include(x => x.PhotographerZoneInfos)
            .Where(x => photographerSchedulesId.Contains(x.Id) && x.EventId == eventId && x.Published)
            .Where(x => !other.Any() ||
                        !other.Any(y => y.PhotographerScheduleId == x.Id && DateOnly.FromDateTime(y.StartTime) == day))
            .Select(x => new PhotographerInfo()
            {
                Id = x.Id,
                ZonePriority = x.PhotographerZoneInfos.FirstOrDefault(y => y.ZoneId == zoneId) == null
                    ? MinZonePriority
                    : x.PhotographerZoneInfos.First(y => y.ZoneId == zoneId).Priority,
                Rating = rnd.NextDouble() * 10,
                FreeTime = x.PhotographerFreetime.Select(y => new Time()
                {
                    StartTime = y.StartTime,
                    EndTime = y.EndTime
                }).ToList()
            })
            .GroupBy(x => x.ZonePriority)
            .ToDictionaryAsync(x => x.Key, x
                => x.OrderByDescending(y => y.Rating).ToList()).Result;
    }

    private List<SchedulePart> GetScheduleParts(List<ActivityInfo> activities, Dictionary<int,List<PhotographerInfo>> photographers)
    {
        var maxPriority = photographers.Keys.Max();
        var minPriority = photographers.Keys.Min();
        var result = new List<SchedulePart>();
        foreach (var activity in activities)
        {
            while (activity.PhotographersCount > 0)
            {
                PhotographerInfo? photographer = null;
                var lowerRankPhotographer = new PhotographerInfo() {Rating = 0};
                for (var i = maxPriority; i >= minPriority; i--)
                {
                    foreach (var current in photographers[i])
                    {
                        var free = CheckAvailabilityOfFreeTime(current, activity);
                        var rating = CheckRating(current, activity.Priority);

                        if (free && rating)
                        {
                            photographer = current;
                            break;
                        }

                        if (free && lowerRankPhotographer.Rating < current.Rating)
                        {
                            lowerRankPhotographer = current;
                            break;
                        }
                    }
                    
                    if (photographer != null)
                        break;

                    if (i > minPriority) 
                        continue;

                    if (lowerRankPhotographer.Rating > 0)
                    {
                        photographer = lowerRankPhotographer;
                        break;
                    }
                }

                if (photographer == null)
                    break;
                
                var resultTime = SetTimeForPhotographer(photographer, activity);
                
                if (resultTime == null)
                    break;
                
                result.AddRange(resultTime.Select(time => new SchedulePart()
                {
                    Version = 0,
                    PhotographerScheduleId = photographer.Id, 
                    ActivityId = activity.Id, 
                    StartTime = time.StartTime, 
                    EndTime = time.EndTime
                }));

                photographer.ActivitySchedules.AddRange(
                    resultTime.Select(time => new ScheduleInfo()
                    {
                        ActivityInfo = activity,
                        TimeOfShooting = time
                    }));

                photographer.SelectedOnZone = true;
                activity.PhotographersCount--;
            }
        }

        return result;
    }

    private static bool CheckAvailabilityOfFreeTime(PhotographerInfo photographer, ActivityInfo activity)
    {
        if (activity.ShootingType == ShootingType.Start_End)
        {
            var startCheck = CheckPhotographerScheduleTime(photographer, activity, GetTimeOfShooting(activity, ShootingType.Start));
            var endCheck = CheckPhotographerScheduleTime(photographer, activity, GetTimeOfShooting(activity, ShootingType.End));
            return startCheck && endCheck;
        }

        return CheckPhotographerScheduleTime(photographer, activity, GetTimeOfShooting(activity, activity.ShootingType));
    }

    private static bool CheckRating(PhotographerInfo photographer, int priority)
    {
        return photographer.Rating >= Coefficient * priority;
    }
    
    private static Time GetTimeOfShooting(ActivityInfo activity, ShootingType shootingType)
    {
        return shootingType switch
        {
            ShootingType.Start => new Time()
            {
                StartTime = activity.StartTime, EndTime = activity.StartTime.AddMinutes(activity.ShootingTime)
            },
            ShootingType.End => new Time()
            {
                StartTime = activity.EndTime.AddMinutes(-activity.ShootingTime), EndTime = activity.EndTime
            },
            _ => new Time() { StartTime = activity.StartTime, EndTime = activity.EndTime }
        };
    }

    private static bool CheckPhotographerScheduleTime(PhotographerInfo photographer, ActivityInfo activityInfo, Time activityTime)
    {
        if (!photographer.SelectedOnZone) 
            return true;
        
        if (photographer.ActivitySchedules.Any(x => x.ActivityInfo.Id == activityInfo.Id))
            return false;
        
        if (activityInfo.ShootingType != ShootingType.Any)
            return photographer.ActivitySchedules.All(x =>
                x.TimeOfShooting.StartTime >= activityTime.EndTime || x.TimeOfShooting.EndTime <= activityTime.StartTime);
        
        var listOfCrossActivities = new List<Time>();
        
        foreach (var schedule in photographer.ActivitySchedules)
        {
            if (schedule.TimeOfShooting.StartTime <= activityTime.StartTime &&
                schedule.TimeOfShooting.EndTime >= activityTime.EndTime)
                return false;
            
            if (schedule.TimeOfShooting.StartTime >= activityTime.EndTime || 
                schedule.TimeOfShooting.EndTime <= activityTime.StartTime)
                continue;
            
            listOfCrossActivities.Add(schedule.TimeOfShooting);
        }

        if (listOfCrossActivities.Count == 0)
            return true;

        listOfCrossActivities = listOfCrossActivities.OrderBy(x => x.StartTime).ToList();

        for (var i = 0; i < listOfCrossActivities.Count; i++)
        {
            if (i == 0)
            {
                if ((listOfCrossActivities[i].StartTime - activityTime.StartTime).TotalMinutes >=
                    activityInfo.ShootingTime)
                    return true;
                
                continue;
            }

            if ((listOfCrossActivities[i].StartTime - listOfCrossActivities[i - 1].EndTime).TotalMinutes >=
                activityInfo.ShootingTime)
                return true;

            if (i == listOfCrossActivities.Count - 1)
            {
                if ((activityTime.EndTime - listOfCrossActivities[i].EndTime).TotalMinutes >=
                    activityInfo.ShootingTime)
                    return true; 
            }
        }

        return false;
    }
    
    private static Time[]? SetTimeForPhotographer(PhotographerInfo photographer, ActivityInfo activity)
    {
        switch (activity.ShootingType)
        {
            case ShootingType.Start: 
                return new[] {
                    new Time()
                    {
                        StartTime = activity.StartTime, EndTime = activity.StartTime.AddMinutes(activity.ShootingTime)
                    }
                };
            case ShootingType.End:
                return new[]
                {
                    new Time()
                    {
                        StartTime = activity.EndTime.AddMinutes(-activity.ShootingTime), EndTime = activity.EndTime
                    }
                };
            case ShootingType.Start_End:
                return new[]
                {
                    new Time()
                    { 
                        StartTime = activity.StartTime, EndTime = activity.StartTime.AddMinutes(activity.ShootingTime)
                    },
                    new Time()
                    {
                        StartTime = activity.EndTime.AddMinutes(-activity.ShootingTime), EndTime = activity.EndTime
                    }
                };
            case ShootingType.Any:
                var time = GetMostSuitableTime(photographer, activity);
                
                return time == null ? null : new[] { time };

            default:
                return new[]
                {
                    new Time()
                    {
                        StartTime = activity.StartTime, EndTime = activity.EndTime
                    }
                };
        }
    }

    private static Time? GetMostSuitableTime(PhotographerInfo photographer, ActivityInfo activity)
    {
        var listOfCrossActivities = new List<Time>();
        DateTime? time = null;
        var difference = double.MaxValue;
        
        foreach (var schedule in photographer.ActivitySchedules)
        {
            if (schedule.TimeOfShooting.StartTime >= activity.EndTime ||
                schedule.TimeOfShooting.EndTime <= activity.StartTime)
            {
                var x = Math.Max((schedule.TimeOfShooting.StartTime - activity.EndTime).TotalMinutes,
                    (activity.StartTime - schedule.TimeOfShooting.EndTime).TotalMinutes);
                if (difference >= activity.ShootingTime && x <= difference)
                {
                    time = schedule.TimeOfShooting.StartTime;
                    difference = x;
                }
                continue;
            }
            
            listOfCrossActivities.Add(schedule.TimeOfShooting);
        }

        if (time == null && listOfCrossActivities.Count == 0)
            return new Time()
            {
                StartTime = activity.StartTime,
                EndTime = activity.StartTime.AddMinutes(activity.ShootingTime)
            };
        
        if (time != null && listOfCrossActivities.Count == 0)
        {
            if ((time.Value - activity.EndTime).TotalMinutes >= 0)
                return new Time()
                {
                    StartTime = activity.EndTime.AddMinutes(-activity.ShootingTime),
                    EndTime = activity.EndTime
                };
            
            return new Time()
            {
                StartTime = activity.StartTime,
                EndTime = activity.StartTime.AddMinutes(activity.ShootingTime)
            };
        }

        listOfCrossActivities = listOfCrossActivities.OrderBy(x => x.StartTime).ToList();
        time = null;
        difference = 0;
        var flag = 1;

        for (var i = 0; i < listOfCrossActivities.Count; i++)
        {
            double x = 0;
            if (i == 0)
            {
                x = (listOfCrossActivities[i].StartTime - activity.StartTime).TotalMinutes;
                if (x >= activity.ShootingTime && x >= difference)
                {
                    time = listOfCrossActivities[i].StartTime;
                    difference = x;
                    flag = -1;
                }
                
                continue;
            }

            x = (listOfCrossActivities[i].StartTime - listOfCrossActivities[i - 1].EndTime).TotalMinutes;
            
            if (x >= activity.ShootingTime && x >= difference)
            {
                time = listOfCrossActivities[i-1].EndTime;
                difference = x;
                flag = 1;
            }

            if (i == listOfCrossActivities.Count - 1)
            {
                x = (activity.EndTime - listOfCrossActivities[i].EndTime).TotalMinutes;
                if (x >= activity.ShootingTime && x >= difference)
                {
                    time = listOfCrossActivities[i].EndTime;
                    difference = x;
                    flag = 1;
                }
            }
        }

        if (time == null)
            return null;

        if (time.Value.AddMinutes(activity.ShootingTime * flag).Ticks > time.Value.Ticks)
            return new Time()
            {
                StartTime = time.Value,
                EndTime = time.Value.AddMinutes(activity.ShootingTime * flag)
            };
        
        return new Time()
        {
            StartTime = time.Value.AddMinutes(activity.ShootingTime * flag),
            EndTime = time.Value
        };
    }

    private static ActivityInfo GetNewActivityInfo(ActivityInfo activity, ShootingType shootingType)
    {
        var time = GetTimeOfShooting(activity, shootingType);
        return new ActivityInfo()
        {
            StartTime = time.StartTime,
            EndTime = time.EndTime,
            PhotographersCount = activity.PhotographersCount
        };
    }

    private static List<int> GetHistogramOfRequiredResourcesForActivities(List<ActivityInfo> activities)
    {
        var listOfActivitiesTime = new List<ActivityInfo>();

        foreach (var activity in activities)
        {
            if (activity.ShootingType == ShootingType.Start_End)
            {
                listOfActivitiesTime.Add(GetNewActivityInfo(activity, ShootingType.Start));
                listOfActivitiesTime.Add(GetNewActivityInfo(activity, ShootingType.End));
                continue;
            }

            if (activity.ShootingType == ShootingType.Any)
            {
                activity.StartTime = activity.StartTime.AddMinutes(
                    ((activity.EndTime - activity.StartTime).TotalMinutes - activity.ShootingTime) * 0.5);
                listOfActivitiesTime.Add(GetNewActivityInfo(activity, ShootingType.Start));
                continue;
            }
            
            listOfActivitiesTime.Add(GetNewActivityInfo(activity, activity.ShootingType));
        }

        listOfActivitiesTime = listOfActivitiesTime.OrderBy(x => x.StartTime).ToList();

        var resultNumber = new List<int>();
        var startTime = activities.First().StartTime.Date;
        var endTime = startTime.AddDays(1);

        for (var time = startTime; time < endTime; time = time.AddMinutes(1))
        {
            var number = 0;
            foreach (var activity in listOfActivitiesTime)
            {
                if (activity.StartTime <= time && activity.EndTime > time)
                    number += activity.PhotographersCount;
                
                if (activity.StartTime > time)
                    break;
            }

            resultNumber.Add(number);
        }

        return resultNumber;
    }

    private static List<int> GetHistogramOfResourcesProvided(List<PhotographerInfo> photographers)
    {
        var listOfPhotographersFreeTime = photographers.SelectMany(x => x.FreeTime)
            .OrderBy(x => x.StartTime)
            .ToList();
        
        var resultNumber = new List<int>();
        var startTime = listOfPhotographersFreeTime.First().StartTime.Date;
        var endTime = startTime.AddDays(1);

        for (var time = startTime; time < endTime; time = time.AddMinutes(1))
        {
            var number = 0;
            foreach (var free in listOfPhotographersFreeTime)
            {
                if (free.StartTime <= time && free.EndTime >= time)
                    number++;
                
                if (free.StartTime > time)
                    break;
            }

            resultNumber.Add(number);
        }

        return resultNumber;
    }

    private static bool CheckPhotographersNumber(List<ActivityInfo> activities, List<PhotographerInfo> photographers)
    {
        var histogramOfActivities = GetHistogramOfRequiredResourcesForActivities(activities);
        var histogramOfPhotographers = GetHistogramOfResourcesProvided(photographers);

        for (var i = 0; i < histogramOfActivities.Count; i++)
            if (histogramOfActivities[i] > histogramOfPhotographers[i])
                return false;

        return true;
    }
}