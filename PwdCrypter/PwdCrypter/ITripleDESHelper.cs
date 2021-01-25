namespace PwdCrypter
{
    public interface ITripleDESHelper
    {
        string Password { get; set; }

        string Encrypt(string content);
        string Decrypt(string content);
    }
}
