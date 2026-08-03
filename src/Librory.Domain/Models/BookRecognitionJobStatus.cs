namespace Librory.Domain.Models;

public enum BookRecognitionJobStatus
{
    Queued = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3,
}
