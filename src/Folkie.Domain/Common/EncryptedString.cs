namespace Folkie.Domain.Common;

/// <summary>
/// IBAN gibi hassas veriler için sarmalayıcı.
/// Plaintext domain'de tutulmaz; sadece şifreli string saklanır.
/// Decrypt için Application/Infrastructure katmanında IIbanProtector kullanılır.
/// </summary>
public sealed record EncryptedString(string Cipher)
{
    public static EncryptedString Empty => new(string.Empty);
    public bool IsEmpty => string.IsNullOrEmpty(Cipher);
}
