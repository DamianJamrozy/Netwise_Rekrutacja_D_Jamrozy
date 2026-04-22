namespace Netwise_Rekrutacja_D_Jamrozy.Infrastructure.Exceptions;

public sealed class StorageOperationException : Exception
{
    public StorageOperationException(string message)
        : base(message)
    {
    }

    public StorageOperationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}