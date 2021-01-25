/**
 * Author: 3DCrypter
 * Date: 11-08-2019
 * Class for simulate the user typing
 */

 class PwCrypterTypewriteTimer {

    /**
     * Costruttore
     * @param {object} field Oggetto del campo da riempire 
     * @param {string} data Valore da inserire nel campo
     * @param {number} delay Intervallo in millisecondi tra un carattere e l'altro
     */
     constructor(field, data, delay) {
         this.iData = 0;
         this.field = field;
         this.data = data;
         this.delay = delay;
         this.timerData = null;
     }

     /**
      * Avvia la scrittura
      */
     startType() {
        this.iData = 0;
        this.field.value = '';
        this.timerData = setInterval(() => {
            this.field.value += this.data[this.iData++];
            if (this.iData === this.data.length) {
                clearInterval(this.timerData);
                this.timerData = null;
            }
        },
        this.delay);
     }

     /**
      * Ferma la scrittura
      */
     stopType() {
         if (this.timerData) {
            clearInterval(this.timerData);
         }
     }
 }