/**
 * Author: 3DCrypter
 * Date: 18-07-2019
 * Script for popup window
 */

// Inietta una classe di utility
chrome.tabs.query({ active: true, currentWindow: true }, function(tabs) {
    // Trucco per vedere se una classe è già stata definita.
    // Se non c'è, includo lo script.

    if (typeof PwdCrypterUtility === 'function') {
        chrome.tabs.executeScript(null, 
            {
                file: 'utility.js'
            }
        );
    }
    if (typeof PwdCrypterAction === 'function') {
        chrome.tabs.executeScript(null, 
            {
                file: 'action.js'
            }
        );
    }
});

/**
 * Traduzioni
 */
function translate() {
    document.getElementById('popupTitle').innerHTML = chrome.i18n.getMessage("popupTitle");
    document.getElementById('mark-field-caption').innerHTML = chrome.i18n.getMessage("markFieldCaption");
    document.getElementById('paste-form-data-caption').innerHTML = chrome.i18n.getMessage("pasteFormDataCaption");
    document.getElementById('fill-username-caption').innerHTML = chrome.i18n.getMessage("fillUsernameCaption");
    document.getElementById('fill-email-caption').innerHTML = chrome.i18n.getMessage("fillEmailCaption");
    document.getElementById('fill-password-caption').innerHTML = chrome.i18n.getMessage("fillPasswordCaption");
}

/**
 * Abilita le funzioni dell'estensione in base alla versione dell'App
 */
function enableFeatures() {
    chrome.runtime.sendNativeMessage('com.3dcrypter.pwdcrypter', { Command: 'GetVersion' },
        function(response) {
            if (response === undefined) {
                console.error('No response from AppService');
                return;
            }

            // Abilita alcune funzioni in base alla versione dell'App
            let display = (response['major']) >= 2 ? 'block' : 'none';
            document.getElementById('fillUsername').style.display = display;
            document.getElementById('fillEmail').style.display = display;
            document.getElementById('fillPassword').style.display = display;

            // Visualizza la versione dell'App
            document.getElementById('app-version').innerHTML = 'PwdCrypter App version: ' + response['version'];
        }
    );
}

/**
 * Invia la richiesta all'App
 * @param {string} cmd Nome del comando
 * @param {string} param Nome del parametro del json che contiene la risposta da dare all'action script
 */
function onSendRequest(cmd, param) {
    chrome.runtime.sendNativeMessage('com.3dcrypter.pwdcrypter', { Command: cmd },
    function(response) {
        if (response === undefined) {
            console.error('No response from AppService');
            return;
        }
        chrome.tabs.query({ active: true, currentWindow: true }, function(tabs) {
            let data = {};
            data[param] = response;
            chrome.tabs.sendMessage(tabs[0].id, data, null, function(sendResponse) {
                console.log(sendResponse);
                if (sendResponse === undefined) {
                    console.error('Paste failed. Error: ' + chrome.runtime.lastError.message);  
                } else {
                    console.log('Paste executed. Result: ' + sendResponse.response);
                }
            });
        }); 
    });
}

/**
 * Compila la form di login con username e password
 */
function onPastePasswordInfo() {
    onSendRequest('GetPassword', 'getpassword');
}

/**
 * Permette di evidenziare i campi con l'icona della chiave
 */
function onMarkField() {
    let markFields = document.getElementById('markField').checked + '';
    localStorage.setItem('pwdcrypter_markFields', markFields); 
    
    // Imposta markField nel locale storage del tab corrente e ricarica la pagina
    chrome.tabs.query({ active: true, currentWindow: true }, function(tabs) {
        let code = 'localStorage.setItem(\'pwdcrypter_markFields\', \'' + markFields + '\');' +
            'window.location.reload();'
        chrome.tabs.executeScript(tabs[0].id, { code });
    });
}

/**
 * Compila la casella corrente con lo username
 */
function onPasteUsernameData() {
    onSendRequest('GetUsernameData', 'getusernamedata');
}

/**
 * Compila la casella corrente con l'email
 */
function onPasteEmailData() {
    onSendRequest('GetEmailData', 'getemaildata');
}

/**
 * Compila la casella corrente con la password
 */
function onPastePasswordData() {
    onSendRequest('GetPasswordData', 'getpassworddata');
}

// Collega gli eventi ai pulsanti del popup
document.getElementById('pasteFormData').addEventListener('click', onPastePasswordInfo);
document.getElementById('markField').addEventListener('click', onMarkField);
document.getElementById('btnFillUsername').addEventListener('click', onPasteUsernameData);
document.getElementById('btnFillEmail').addEventListener('click', onPasteEmailData);
document.getElementById('btnFillPassword').addEventListener('click', onPastePasswordData);

// Chiede al tab corrente lo stato della marcatura dei campi
chrome.tabs.query({ active: true, currentWindow: true }, function(tabs) {
    chrome.tabs.sendMessage(tabs[0].id, { data: "get_mark_fields" }, function(response) {
        if (response && response.markFields) {
            localStorage.setItem('pwdcrypter_markFields', response.markFields);
            document.getElementById('markField').checked = (response.markFields === 'true');
        }
    });
});

// Traduzioni
translate();

// Verifica le funzioni disponibili
enableFeatures();