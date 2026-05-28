using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using Microsoft.AspNetCore.DataProtection;

namespace Folkie.Infrastructure.Security;

/// <summary>
/// ASP.NET Core Data Protection API üzerinden IBAN şifreleme.
/// Anahtar rotation built-in; production'da KeyRingPath persistent volume olmalı.
/// </summary>
public sealed class IbanProtector : IIbanProtector
{
    private const string Purpose = "Folkie.Iban.v1";
    private readonly IDataProtector _protector;

    public IbanProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public EncryptedString Protect(string plaintextIban)
    {
        if (string.IsNullOrWhiteSpace(plaintextIban))
            return EncryptedString.Empty;
        return new EncryptedString(_protector.Protect(plaintextIban));
    }

    public string Unprotect(EncryptedString cipher)
    {
        if (cipher.IsEmpty) return string.Empty;
        return _protector.Unprotect(cipher.Cipher);
    }
}
