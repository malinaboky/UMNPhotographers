using UMNPhotographers.Distribution.Domain;
using UMNPhotographers.Distribution.Domain.Entities;

namespace UMNPhotographers.Distribution.Services;

public class MessageService : IMessageService
{
    private readonly DataContext _context;

    public MessageService(DataContext context)
    {
        _context = context;
    }
    
    public async Task SendMessageToDB(long employeeId, string code, string message)
    {
        try
        {
            _context.AllocationEvents.Add(new AllocationEvent()
            {
                Version = 0,
                EmployeeId = employeeId,
                EventTime = DateTime.Now,
                Code = code,
                Message = message
            });

            await _context.SaveChangesAsync();
        }
        catch (System.Exception e)
        {
            return;
        }
    }
}