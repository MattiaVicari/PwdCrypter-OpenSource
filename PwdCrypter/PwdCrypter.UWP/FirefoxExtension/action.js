/**
 * Author: 3DCrypter
 * Date: 20-07-2019
 */

(function() {

    browser.runtime.onMessage.addListener(function(request, sender, sendResponse) {
        let action = new PwdCrypterAction();

        try {
            if (request.getpassword) {
                action.onPastePasswordInfo(request.getpassword["username"], request.getpassword["password"]);
            } else if (request.getusernamedata) {
                action.onPasteData(request.getusernamedata["username"]);
            } else if (request.getemaildata) {
                action.onPasteData(request.getemaildata["email"]);
            } else if (request.getpassworddata) {
                action.onPasteData(request.getpassworddata["password"]);
            } else {
                return false;
            }
            sendResponse( { response: "done"} );
        }
        finally {
            // Rimuove l'ascolto dopo l'esecuzione
            browser.runtime.onMessage.removeListener(event);
        }
        return true;    // Indica che voglio restituire una risposta
    });
})();


class PwdCrypterAction {
    
    constructor() {
        this.delayType = 50;
    }

    /**
     * Compila la form con username e password
     * @param {string} inUsername Username
     * @param {string} inPassword Password
     */
    onPastePasswordInfo(inUsername, inPassword) {
        console.log("Executing script...");
        
        let usernameField = document.activeElement,
            passwordField = null,
            findNext = false,
            util = new PwdCrypterUtility(),
            inputElems = util.getFields();

        if (usernameField === null) {
            console.log("Field not found");
            return;
        }
        if (usernameField.type !== "text" && usernameField.type !== "email") {
            console.log("Unexpected field type");
            return;
        }
        
        // Username
        console.log("Username field: " + (usernameField.name || usernameField.id));
        let typewriteUsername = new PwCrypterTypewriteTimer(usernameField, inUsername, this.delayType);
        typewriteUsername.startType();

        // Cerca il campo password tra gli elementi successivi
        for (let i=0; i < inputElems.length; i++) {
            let element = inputElems[i];
            if (element === usernameField) {
                findNext = true;
            } else if (findNext && element.type === "password") {
                passwordField = element;
                passwordField.focus();
                break;
            }
        }
        
        if (passwordField === null || (passwordField !== null && passwordField.type !== "password")) {
            console.log("Password field not found");
        } else {
            console.log("Password field: " + (passwordField.name || passwordField.id));
            let typewritePassword = new PwCrypterTypewriteTimer(passwordField, inPassword, this.delayType);
            typewritePassword.startType();
            console.log("Done!");
        }
    }

    onPasteData(data) {
        console.log("Executing script...");
        
        let currentField = document.activeElement;
        if (currentField === null) {
            console.log("Field not found");
            return;
        }

        console.log("Current field: " + (currentField.name || currentField.id));
        let typewrite = new PwCrypterTypewriteTimer(currentField, data, this.delayType);
        typewrite.startType();
    }
}