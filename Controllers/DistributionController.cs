using Microsoft.AspNetCore.Mvc;
using UMNPhotographers.Distribution.Domain;
using UMNPhotographers.Distribution.Exception;
using UMNPhotographers.Distribution.Models;
using UMNPhotographers.Distribution.Services;

namespace UMNPhotographers.Distribution.Controllers;

[Route("api/")]
[ApiController]
public class DistributionController : Controller
{
    private readonly IDistributionService _distributionService;
    private readonly IMessageService _messageService;

    public DistributionController(IDistributionService distributionService, IMessageService messageService)
    {
        _distributionService = distributionService;
        _messageService = messageService;
    }
    
    [HttpPost("distribute/{eventId:long}/{zoneId:long}")]
    public async Task MakeDistributionOnZone(long eventId, long zoneId, 
        [FromBody]ListPhotographers list)
    {
        try
        {
            await _distributionService.SaveDistributionToDB(eventId, zoneId, list.List);
            await _messageService.SendMessageToDB(list.Employee, "ok", "Распределение успешно заверешено");
        }
        catch (CustomException e)
        {
            await _messageService.SendMessageToDB(list.Employee, e.Code, e.Message);
        }
        catch
        {
            await _messageService.SendMessageToDB(list.Employee, "error", "Внутрисерверная ошибка");
        }
    }
    
    [HttpPost("check/{eventId:long}/{zoneId:long}")]
    public IActionResult CheckPhotographersNumber(long eventId, long zoneId, 
        [FromBody]ListPhotographers list)
    {
        try
        {
            var result = _distributionService.CheckPhotographersNumber(eventId, zoneId, list.List);
            return Ok(new { result });
        }
        catch
        {
            return BadRequest();
        }
    }
}