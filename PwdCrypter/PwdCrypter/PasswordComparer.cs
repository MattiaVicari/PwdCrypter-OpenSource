namespace PwdCrypter
{
    public class PasswordComparer
    {
        /// <summary>
        /// Confronta i nomi delle due password
        /// </summary>
        /// <param name="item1">Prima password da confrontare</param>
        /// <param name="item2">Seconda password da confrontare</param>
        /// <returns>Restituisce 0 se i nomi sono uguali, 1 se il nome della prima password è successivo alla seconda,
        /// -1 se precedente.</returns>
        public static int ComparePasswordByName(PwdListItem item1, PwdListItem item2)
        {
            return item1.Name.CompareTo(item2.Name);
        }

        /// <summary>
        /// Confronta la descrizione del tipo di account di due password.
        /// </summary>
        /// <param name="item1">Prima password da confrontare</param>
        /// <param name="item2">Seconda password da confrontare</param>
        /// <returns>Restituisce 0 se i tipi di account sono uguali, 1 se la descrizione del tipo di account della prima
        /// password è successiva a quella della seconda, -1 se precedente.</returns>
        public static int ComparePasswordByAccountType(PwdListItem item1, PwdListItem item2)
        {
            string accountType1 = Utility.EnumHelper.GetDescription(item1.AccountOption);
            string accountType2 = Utility.EnumHelper.GetDescription(item2.AccountOption);
            return accountType1.CompareTo(accountType2);
        }
    }
}
