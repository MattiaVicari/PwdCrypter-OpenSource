/**
 * Author: 3DCrypter
 * Date: 20-07-2019
 */

console.log("PwdCrypter Chrome Extension v.2.0.1.0");

let markFields = localStorage.getItem('pwdcrypter_markFields');
if (markFields === 'true') {
    try {
        let util = new PwdCrypterUtility(),
            fieldsArray = util.getFields();
        fieldsArray.forEach(field => {
            if (field.className === null || (field.className !== undefined && field.className.indexOf("pwdcrypterField") === -1)) {
                if (field.className !== null && field.className !== undefined && field.className !== "") {
                    field.className += " pwdcrypterField";
                } else {
                    field.className = "pwdcrypterField";
                }
            } 
        });
    } catch(err) {
        console.error("PwdCrypter Chrome Extension error: " + err);
    }
}

chrome.runtime.onMessage.addListener( function(request, sender, sendResponse) {
    let data = request.data || {};
    if (data === 'get_mark_fields') {
        let markFields = localStorage.getItem('pwdcrypter_markFields');
        sendResponse( { markFields } );
        return true;
    }
    return false;
});