namespace RoslynMetadata.LocalConsole
{
    public class ValidResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public ValidResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

    }
}
