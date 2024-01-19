using UMNPhotographers.Distribution.Domain.Entities;

namespace UMNPhotographers.Distribution.Services;

public interface IDistributionService
{
    Task SaveDistributionToDB(long eventId, long zoneId, List<long> photographerId);
    bool CheckPhotographersNumber(long eventId, long zoneId, List<long> photographerId);
}