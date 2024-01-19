namespace UMNPhotographers.Distribution.Exception;

public class CustomException : System.Exception
{
    public string Code { get; }
    
    public CustomException()
        : base()
    {
    }

    public CustomException(
        string message
    )
        : base(message)
    {
    }

    public CustomException(
        string message,
        System.Exception innerException
    ) : base(message, innerException)
    {

    }

    public CustomException(string code, string message) : base(message)
    {
        Code = code;
    }
    
}