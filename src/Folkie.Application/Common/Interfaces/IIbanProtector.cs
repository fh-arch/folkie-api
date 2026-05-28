using Folkie.Domain.Common;

namespace Folkie.Application.Common.Interfaces;

/// <summary>
/// IBAN gibi hassas verileri şifreler/çözer.
/// Implementasyon Infrastructure katmanında ASP.NET Data Protection ile yapılır.
/// </summary>
public interface IIbanProtector
{
    EncryptedString Protect(string plaintextIban);
    string Unprotect(EncryptedString cipher);
}
