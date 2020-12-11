using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Windows;

namespace PodcastMusicSwitcher
{
    public class Requester<T>
    {
        public T Request(string request)
        {
            WebRequest webRequest = CreateRequest(request);
            return GetResponse(webRequest);
        }

        private WebRequest CreateRequest(string request)
        {
            WebRequest webRequest = WebRequest.Create(request);
            SetAuthentication(webRequest);
            return webRequest;
        }

        private void SetAuthentication(WebRequest webRequest)
        {
            var cache = new CredentialCache { { webRequest.RequestUri, "Ntlm", CredentialCache.DefaultNetworkCredentials } };
            webRequest.Credentials = cache;
        }

        private T GetResponse(WebRequest request)
        {
            try
            {
                using (var response = request.GetResponse() as HttpWebResponse)
                {
                    CheckIfResponseIsOk(response);

                    return SerializeJsonObject(response);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
                return default(T);
            }
        }

        private void CheckIfResponseIsOk(HttpWebResponse response)
        {
            if (response == null)
            {
                throw new Exception("Not HTTP Response received");
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"Server error (HTTP {response.StatusCode}: {response.StatusDescription}).");
            }
        }

        private T SerializeJsonObject(WebResponse response)
        {
            Stream stream = response.GetResponseStream();
            if (stream == null)
            {
                throw new Exception("No response stream");
            }

            var jsonSerializer = new DataContractJsonSerializer(typeof(T));
            return (T)jsonSerializer.ReadObject(stream);
        }
    }
}
