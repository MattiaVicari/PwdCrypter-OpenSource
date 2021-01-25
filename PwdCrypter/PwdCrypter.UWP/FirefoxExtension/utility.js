/**
 * Author: 3DCrypter
 * Date: 17-07-2019
 * Utility script
 */

class PwdCrypterUtility {
    /**
     * Restituisce la lista dei campi accessibili all'estensione
     */
    getFields() {
        let filterInput = function(item) {
            return item.type === "text" || item.type === "email" || item.type === "password";
        };

        let tags = document.getElementsByTagName("input"),
            iframes = document.getElementsByTagName("iframe"),
            fieldsArray = [];
        
        for (let i=0; i < iframes.length; i++) {
            try {
                let iframe = iframes[i],
                    innerDoc = iframe.contentDocument || iframe.contentWindow.document;
                if (innerDoc) {
                    let inputs = innerDoc.getElementsByTagName("input");
                    for (let j=0; j < inputs.length; j++) {
                        if (filterInput(inputs[j])) {
                            fieldsArray.push(inputs[j]);
                        }
                    }
                }
            } catch(err) {
                console.error("PwdCrypter Chrome Extension iframe error: " + err);
            }
        }

        for (let i=0; i < tags.length; i++) {
            if (filterInput(tags[i])) {
                fieldsArray.push(tags[i]);
            }
        }
        
        return fieldsArray;
    }
}