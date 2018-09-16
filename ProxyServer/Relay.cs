using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace ProxyServer
{
    public class Relay
    {
        private readonly HttpListenerContext _originalContext;

        public Relay(HttpListenerContext originalContext)
        {
            this._originalContext = originalContext;
        }

        public void ProcessRequest()
        {
            var rawUrl = "https://habr.com" + _originalContext.Request.RawUrl;
            Console.WriteLine("Proxy receive a request for: " + rawUrl);

            var relayRequest = (HttpWebRequest)WebRequest.Create(rawUrl);
            relayRequest.KeepAlive = false;
            relayRequest.Proxy.Credentials = CredentialCache.DefaultCredentials;
            relayRequest.UserAgent = _originalContext.Request.UserAgent;

            var requestData = new RequestState(relayRequest, _originalContext);
            relayRequest.BeginGetResponse(ResponseCallBack, requestData);
        }

        private static void ResponseCallBack(IAsyncResult asynchronousResult)
        {
            var requestData = (RequestState)asynchronousResult.AsyncState;
            Console.WriteLine("Proxy receive a response from " + requestData.Context.Request.RawUrl);

            using (var response = (HttpWebResponse)requestData.WebRequest.EndGetResponse(asynchronousResult))
            {
                using (var responseStream = response.GetResponseStream())
                {
                    var originalResponse = requestData.Context.Response;

                    if (response.ContentType.Contains("text/html"))
                    {
                        var reader = new StreamReader(responseStream);
                        var html = reader.ReadToEnd();
                        html = html.Replace("https://habr.com", "http://localhost:8080");
                        var document = new HtmlDocument();
                        document.LoadHtml(html);

                        var regex = new Regex(@"\b(\w|[а-яА-ЯёЁ-]){6}\b");

                        foreach (var node in document.DocumentNode.SelectNodes("//*[not(script) and not(style)]"))
                        {
                            foreach (var child in node.ChildNodes)
                            {
                                if (child.NodeType == HtmlNodeType.Text && regex.IsMatch(child.InnerHtml))
                                {
                                    var newValue = regex.Replace(child.InnerHtml, m => m.Value + '\u2122');
                                    child.InnerHtml = newValue;
                                }
                            }
                        }

                        byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(document.DocumentNode.InnerHtml);
                        var stream = new MemoryStream(byteArray);
                        stream.CopyTo(originalResponse.OutputStream);
                    }
                    else
                    {
                        responseStream.CopyTo(originalResponse.OutputStream);
                    }
                    originalResponse.OutputStream.Close();
                }
            }
        }

        private class RequestState
        {
            public readonly HttpWebRequest WebRequest;
            public readonly HttpListenerContext Context;

            public RequestState(HttpWebRequest request, HttpListenerContext context)
            {
                this.WebRequest = request;
                this.Context = context;
            }
        }
    }
}