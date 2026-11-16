namespace Shared.Exceptions
{
    public abstract class ApiException : Exception
    {
        protected ApiException(string message) : base(message)
        {
        }
    }

    public sealed class BadRequestException : ApiException
    {
        public BadRequestException(string message) : base(message)
        {
        }
    }

    public sealed class ForbiddenException : ApiException
    {
        public ForbiddenException(string message) : base(message)
        {
        }
    }

    public sealed class NotFoundException : ApiException
    {
        public NotFoundException(string message) : base(message)
        {
        }
    }

    public sealed class ConflictException : ApiException
    {
        public ConflictException(string message) : base(message)
        {
        }
    }

    public sealed class ServiceUnavailableException : ApiException
    {
        public ServiceUnavailableException(string message) : base(message)
        {
        }
    }
}
