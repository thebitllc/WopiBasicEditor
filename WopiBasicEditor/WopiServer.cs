// <copyright file="WopiServer.cs" company="Bit, LLC">
// Copyright (c) 2014 All Rights Reserved
// </copyright>
// <author>ock</author>
// <date></date>
// <summary></summary>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Json;
using Cobalt;

namespace WopiBasicEditor
{
    public class WopiServer
    {
        private HttpListener _listener;
        private string _docs;
        private int _port;

        public WopiServer(string docs, int port = 8080)
        {
            _docs = docs;
            _port = port;
        }

        public void Start()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(@"http://+:" + _port + @"/");
            _listener.Start();
            _listener.BeginGetContext(ProcessRequest, _listener);

            Console.WriteLine(@"WopiServer Started");
        }

        public void Stop()
        {
            _listener.Stop();
        }

        private void SendError(HttpListenerContext context)
        {
            byte[] buffer = Encoding.UTF8.GetBytes("Error occured");
            context.Response.ContentLength64 = buffer.Length;
            context.Response.ContentType = @"application/json";
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
            context.Response.StatusCode = 500;
            _listener.BeginGetContext(ProcessRequest, _listener);
        }

        private void ProcessCobaltRequest(HttpListenerContext context)
        {
        }
        
        private void ProcessWopiRequest(HttpListenerContext context)
        {
        }

        private void ProcessRequest(IAsyncResult result)
        {
            HttpListener listener = (HttpListener)result.AsyncState;
            HttpListenerContext context = listener.EndGetContext(result);

            var stringarr = context.Request.Url.AbsolutePath.Split('/');
            var access_token = context.Request.QueryString["access_token"];

            if (stringarr.Length < 3 || access_token == null)
            {
                Console.WriteLine(@"Invalid request");
                SendError(context);
                return;
            }

            var filename = stringarr[3];
            //filename = "test2.docx"; //overrride

            WopiSession cf = WopiSessionManager.Instance.GetSession(access_token);
            if (cf == null)
            {
                cf = new WopiSession(access_token, _docs + "/" + filename);
                WopiSessionManager.Instance.AddSession(cf);
            }

            if (stringarr.Length == 5 && context.Request.HttpMethod.Equals(@"GET"))
            {
                // get file's content
                var content = cf.GetFileContent();
                context.Response.ContentType = @"application/octet-stream";
                context.Response.ContentLength64 = content.Length;
                content.CopyTo(context.Response.OutputStream);
                context.Response.Close();
            }
            else if (context.Request.HttpMethod.Equals(@"POST") && context.Request.Headers["X-WOPI-Override"].Equals("COBALT"))
            {
                var ms = new MemoryStream();
                context.Request.InputStream.CopyTo(ms);
                AtomFromByteArray atomRequest = new AtomFromByteArray(ms.ToArray());
                RequestBatch requestBatch = new RequestBatch();

                Object ctx;
                ProtocolVersion protocolVersion;

                requestBatch.DeserializeInputFromProtocol(atomRequest, out ctx, out protocolVersion);
                cf.ExecuteRequestBatch(requestBatch);

                foreach (Request request in requestBatch.Requests)
                {
                    if (request.GetType() == typeof(PutChangesRequest) && request.PartitionId == FilePartitionId.Content)
                    {
                        cf.Save();
                    }
                }
                var response = requestBatch.SerializeOutputToProtocol(protocolVersion);
                
                context.Response.Headers.Add("X-WOPI-CorellationID", context.Request.Headers["X-WOPI-CorrelationID"]);
                context.Response.Headers.Add("request-id", context.Request.Headers["X-WOPI-CorrelationID"]);
                context.Response.ContentType = @"application/octet-stream";
                context.Response.ContentLength64 = response.Length;
                response.CopyTo(context.Response.OutputStream);
                context.Response.Close();
            }
            else if (stringarr.Length == 4 && context.Request.HttpMethod.Equals(@"GET"))
            {
                // encode json
                var memoryStream = new MemoryStream();
                var json = new DataContractJsonSerializer(typeof(WopiCheckFileInfo));
                json.WriteObject(memoryStream, cf.GetCheckFileInfo());
                memoryStream.Flush();
                memoryStream.Position = 0;
                StreamReader streamReader = new StreamReader(memoryStream);
                var jsonResponse = Encoding.UTF8.GetBytes(streamReader.ReadToEnd());

                context.Response.ContentType = @"application/json";
                context.Response.ContentLength64 = jsonResponse.Length;
                context.Response.OutputStream.Write(jsonResponse, 0, jsonResponse.Length);
                context.Response.Close();
            }
            else
            {
                Console.WriteLine(@"Invalid request parameters");
                SendError(context);
            }

            _listener.BeginGetContext(ProcessRequest, _listener);
        }
    }
}
