namespace PwdCrypter
{
    /// <summary>
    /// Interfacce per l'autenticazione a 2 fattori
    /// </summary>
    public interface ITwoFactorAuth
    {
        string GetSecret();
        string GetBackupCode();
        string GetQRCodeURL(string secret, string account, string issuer);
        string GetTOTP(string secret);
    }
}
