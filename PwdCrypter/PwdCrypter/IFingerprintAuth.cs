namespace PwdCrypter
{
    public enum FingerprintSignOp
    {
        Crypt,
        Decrypt
    };

    public delegate void FingerprintAuthenticationSucceeded(byte[] result);
    public delegate void FingerprintAuthenticationFailed();
    public delegate void FingerprintAuthenticationError(int errorMsgId, string errorMsg);
    public delegate void FingerprintAuthenticationCanceled();
    public delegate void FingerprintAuthenticationHelp(int helpMsgId, string helpString);

    public interface IFingerprintAuth
    {
        event FingerprintAuthenticationSucceeded OnAuthenticationSucceeded;
        event FingerprintAuthenticationFailed OnAuthenticationFailed;
        event FingerprintAuthenticationError OnAuthenticationError;
        event FingerprintAuthenticationCanceled OnAuthenticationCanceled;
        event FingerprintAuthenticationHelp OnAuthenticationHelp;

        void Init();
        void Authenticate(byte[] secret, FingerprintSignOp operation);
        void Cancel();

        bool IsReady();
        bool IsAvailable();
    }
}
