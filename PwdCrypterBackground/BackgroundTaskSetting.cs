using System;
using Windows.Storage;
using System.Threading.Tasks;

namespace PwdCrypterBackground
{
    class BackgroundTaskSetting
    {
        public const string NextSuffix = "Next";        

        private readonly DateTime _executionMinutes;
        private readonly string _taskName;

        public Logger WorkLogger { get; set; }
        public uint Interval { get; set; }
        public uint TimeKind { get; set; }
        public uint TimeQty { get; set; }
        public DateTime NextDate { get; private set; }
        public bool Execute { get; private set; }

        /// <summary>
        /// Costruttore
        /// </summary>
        /// <param name="taskName">Nome del task</param>
        /// <param name="execution">Data e ora di esecuzione del work</param>
        public BackgroundTaskSetting(string taskName, DateTime execution)
        {
            Interval = 0;
            NextDate = DateTime.MaxValue;
            Execute = false;

            _executionMinutes = execution;
            _taskName = taskName;
        }

        /// <summary>
        /// Inizializzazione. Carica le impostazioni.
        /// </summary>            
        public async Task Init()
        {
            await LoadWorkSetting();
        }

        public async Task<bool> Next()
        {
            return await LoadWorkSetting(true);
        }

        private void GetInterval(bool next, out uint interval)
        {
            if (!next)
            {
                if (!uint.TryParse(ApplicationData.Current.LocalSettings.Values[_taskName + ".Interval"].ToString(), out interval))
                    throw new Exception("Impossibile rinnovare il trigger in quanto l'intervallo di esecuzione non è stato trovato");
            }
            else
            {
                DateTime nextDate = DateTime.ParseExact(ApplicationData.Current.LocalSettings.Values[_taskName + ".NextDate"].ToString(), "dd-MM-yyyy, HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                if (!uint.TryParse(ApplicationData.Current.LocalSettings.Values[_taskName + ".TimeKind"].ToString(), out uint tKind))
                    throw new Exception("Impossibile rinnovare il trigger in quanto la categoria del tempo di attesa non è stata trovata");
                if (!uint.TryParse(ApplicationData.Current.LocalSettings.Values[_taskName + ".TimeQty"].ToString(), out uint tQty))
                    throw new Exception("Impossibile rinnovare il trigger in quanto il tempo di attesa non è stato trovato");

                DateTime nextDateExec;
                switch(tKind)
                {
                    case 0:
                        nextDateExec = nextDate.AddMinutes(tQty);
                        break;
                    case 1:
                        nextDateExec = nextDate.AddHours(tQty);
                        break;
                    case 2:
                        nextDateExec = nextDate.AddDays(tQty);
                        break;
                    case 3:
                        nextDateExec = nextDate.AddMonths((int)tQty);
                        break;
                    case 4:
                        nextDateExec = nextDate.AddYears((int)tQty);
                        break;
                    default:
                        throw new Exception("Categoria del tempo di attesa non gestita");                        
                }

                interval = (uint)Math.Round((nextDateExec - nextDate).TotalMinutes, 0);
            }
        }

        private bool ExistsNextSetting(out uint interval)
        {
            interval = 0;

            if (!ExistsNextSetting())
                return false;

            GetInterval(true, out interval);
            return true;
        }

        private bool ExistsNextSetting()
        {            
            return ApplicationData.Current.LocalSettings.Values[_taskName + ".TimeKind"] != null
                && ApplicationData.Current.LocalSettings.Values[_taskName + ".TimeQty"] != null;
        }

        /// <summary>
        /// Restituisce le impostazioni del task periodico.
        /// </summary>
        /// <param name="next">Passare true per ottenere le impostazioni per il prossimo trigger</param>
        /// <returns>Restituisce sempre true. Se si passa next a true e non ci sono impostazioni successive, restituisce false.</returns>
        private async Task<bool> LoadWorkSetting(bool next = false)
        {
            if (!ExistsNextSetting())            
                return false;

            GetInterval(next, out uint interval);
            Interval = interval;

            Execute = false;
            NextDate = DateTime.ParseExact(ApplicationData.Current.LocalSettings.Values[_taskName + ".NextDate"].ToString(), "dd-MM-yyyy, HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);            
            if (_executionMinutes >= NextDate)
            {
                Execute = true;
                await WorkLogger?.Debug(string.Format("Trigger eseguito. NextTrigger: {0}, Execution date: {1}", NextDate, _executionMinutes));

                int maxCount = 100;
                do
                {
                    if (!next && ExistsNextSetting(out uint nextInterval))
                        NextDate = NextDate.AddMinutes(nextInterval);
                    else
                        NextDate = NextDate.AddMinutes(Interval);
                    ApplicationData.Current.LocalSettings.Values[_taskName + ".NextDate"] = NextDate.ToString("dd-MM-yyyy, HH:mm:ss");
                    maxCount--;
                }
                while (NextDate < _executionMinutes && maxCount >= 0);

                Interval = (uint)Math.Round((NextDate - _executionMinutes).TotalMinutes, 0);
                await WorkLogger?.Debug(string.Format("Nuovo trigger: {0}, intervallo: {1} minuti", NextDate, Interval));
            }            

            return true;
        }
    }
}
