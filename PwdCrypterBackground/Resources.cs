using Windows.ApplicationModel.Resources.Core;

namespace PwdCrypterBackground
{
    public sealed class Resources
    {        
        private readonly ResourceContext ResourceContext;
        private readonly ResourceMap ResourceMap;

        public Resources()
        {
            ResourceContext = ResourceContext.GetForViewIndependentUse();
            ResourceMap = ResourceManager.Current.MainResourceMap.GetSubtree("Resources");
        }

        /// <summary>
        /// Restituisce il valore stringa della risorsa passata come argomento.
        /// </summary>
        /// <param name="stringName">Nome della risorsa</param>
        /// <returns>Valore della risorsa come stringa</returns>
        public string GetString(string stringName)
        {
            var resourceValue = ResourceMap.GetValue(stringName, ResourceContext);
            return resourceValue.ValueAsString;
        }
    }
}
