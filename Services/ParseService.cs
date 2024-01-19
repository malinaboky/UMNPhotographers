using UMNPhotographers.Distribution.Models;

namespace UMNPhotographers.Distribution.Services;

public class ParseService : IParseService
{
    public ShootingType TryParse(string type)
    {
        try
        {
            return Enum.Parse<ShootingType>(type, true);
        }
        catch
        {
            return ShootingType.All;
        }
    }
}