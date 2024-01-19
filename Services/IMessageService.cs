namespace UMNPhotographers.Distribution.Services;

public interface IMessageService
{
    Task SendMessageToDB(long employeeId, string code, string message);
}