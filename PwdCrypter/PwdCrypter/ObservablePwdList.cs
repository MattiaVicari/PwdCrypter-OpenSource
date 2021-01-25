using System;
using System.Collections.Generic;

namespace PwdCrypter
{
    class ObservablePwdList: List<PwdListItem>
    {
        public delegate void EventOnPassword(object sender, EventArgs e, PwdListItem item);

        public event EventOnPassword OnAdd;
        public event EventOnPassword OnRemove;
        public event EventHandler OnClear;

        public new void Add(PwdListItem item)
        {
            OnAdd?.Invoke(this, new EventArgs(), item);
            base.Add(item);
        }

        public new bool Remove(PwdListItem item)
        {
            OnRemove?.Invoke(this, new EventArgs(), item);
            return base.Remove(item);
        }

        public new void Clear()
        {
            OnClear?.Invoke(this, new EventArgs());
            base.Clear();
        }

        /// <summary>
        /// Restituisce la prima password con id pari a quello passato come parametro.
        /// </summary>
        /// <param name="id">ID della password</param>
        /// <returns>Oggetto PwdListItem della password o null se non viene trovata</returns>
        public PwdListItem FindById(string id)
        {
            PwdListItem item = base.Find((PwdListItem pwd) =>
            {
                return pwd.Id == id;
            });
            return item;
        }
    }
}
