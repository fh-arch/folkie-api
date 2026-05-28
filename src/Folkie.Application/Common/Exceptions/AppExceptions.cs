namespace Folkie.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string what) : base($"{what} bulunamadı.") { }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string reason = "Bu işlem için yetkin yok.") : base(reason) { }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
