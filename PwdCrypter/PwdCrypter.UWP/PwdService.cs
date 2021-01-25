using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace PwdCrypter.UWP
{
    public class PwdService
    {
        /// <summary>
        /// Dati da utilizzare per l'elaborazione delle richieste
        /// </summary>
        public JObject Data { get; set; }

        /// <summary>
        /// Pulisce i dati
        /// </summary>
        public void ClearData()
        {
            Data = null;
        }

        /// <summary>
        /// Consuma la richiesta e genera una risposta
        /// </summary>
        /// <param name="request">Dictionary con le richieste da elaborare</param>
        /// <param name="response">Dictionary in cui restituire le risposte alle richieste</param>
        public void Consume(Dictionary<string, object> request, out Dictionary<string, object> response)
        {
            Dictionary<string, object> appResponse = new Dictionary<string, object>();

            foreach (object value in request.Values)
            {
                try
                {
                    JObject JSON = JObject.Parse(value as string);

                    string CommandString = "";
                    if (Utility.SafeTryGetJSONString(JSON, "Command", out string ret))
                        CommandString = ret;
                    if (CommandString == "GetPassword")
                    {
                        if (Data == null)
                            throw new Exception("Data not found");
                        JObject JSONResponse = new JObject
                        {
                            { "username", new JValue(Data["account_username"].ToString()) },
                            { "password", new JValue(Data["password"].ToString()) }
                        };
                        appResponse.Add("GetPasswordResponse", JSONResponse.ToString());
                    }
                    else if (CommandString == "GetUsernameData")
                    {
                        if (Data == null)
                            throw new Exception("Data not found");
                        JObject JSONResponse = new JObject
                        {
                            { "username", new JValue(Data["username"].ToString()) }
                        };
                        appResponse.Add("GetUsernameDataResponse", JSONResponse.ToString());
                    }
                    else if (CommandString == "GetEmailData")
                    {
                        if (Data == null)
                            throw new Exception("Data not found");
                        JObject JSONResponse = new JObject
                        {
                            { "email", new JValue(Data["email"].ToString()) }
                        };
                        appResponse.Add("GetEmailDataResponse", JSONResponse.ToString());
                    }
                    else if (CommandString == "GetPasswordData")
                    {
                        if (Data == null)
                            throw new Exception("Data not found");
                        JObject JSONResponse = new JObject
                        {
                            { "password", new JValue(Data["password"].ToString()) }
                        };
                        appResponse.Add("GetPasswordDataResponse", JSONResponse.ToString());
                    }
                    else if (CommandString == "GetVersion")
                    {
                        AppInformant info = new AppInformant();
                        string version = info.GetVersionNumber();
                        string[] vparts = version.Split(".");

                        JObject JSONResponse = new JObject
                        {
                            { "version", new JValue(version) }
                        };

                        string[] versionPartName = { "major", "minor", "build" };
                        for (int i = 0; i < Math.Min(vparts.Length, versionPartName.Length); i++)
                        {
                            JSONResponse.Add(versionPartName[i], vparts[i]);
                        }
                        appResponse.Add("GetVersionResponse", JSONResponse.ToString());
                    }
                }
                catch (Exception Ex)
                {
                    Debug.WriteLine("PwdServiceError: " + Ex.Message);
                    appResponse.Add("PwdServiceError", Ex.Message);
                }
            }

            response = appResponse;
        }
    }
}
