using UMNPhotographers.Distribution.Models;

namespace UMNPhotographers.Distribution.Services;

public interface IParseService
{
    ShootingType TryParse(string type);
}