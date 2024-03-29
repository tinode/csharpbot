﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pbx;
using Grpc.Core;
using Grpc;
using static Pbx.Plugin;
using static Pbx.Node;
using Google.Protobuf;
using System.Threading;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using Google.Protobuf.Collections;
using System.Runtime.InteropServices;
using System.IO;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Net;

namespace Tinode.ChatBot
{
    /// <summary>
    /// ChatBot Plugin implement
    /// </summary>
    public class ChatBotPlugin : PluginBase
    {
        public override Task<Unused> Account(AccountEvent request, ServerCallContext context)
        {
            string action = string.Empty;
            if (request.Action==Crud.Create)
            {
                action = "created";
            }
            else if (request.Action==Crud.Update)
            {
                action = "updated";
            }
            else if (request.Action==Crud.Delete)
            {
                action = "deleted";
            }
            else
            {
                action = "unknown";
            }
            return Task.FromResult(new Unused());
        }
    }

    /// <summary>
    /// CSharp ChatBot implement, same as the python version.
    /// </summary>
    public class ChatBot
    {
        /// <summary>
        /// Uploaded attachment file info
        /// </summary>
        public class UploadedAttachmentInfo
        {
            /// <summary>
            /// File fullname from local before upload
            /// </summary>
            public string FullFileName { get; set; }
            /// <summary>
            /// file short name
            /// </summary>
            public string FileName { get; set; }
            /// <summary>
            /// mime type, now is "file/[file extention]"
            /// </summary>
            public string Mime { get; set; }
            /// <summary>
            /// file size
            /// </summary>
            public long Size { get; set; }
            /// <summary>
            /// when uploaded to the server, the relative url to visit the file
            /// </summary>
            public string RelativeUrl { get; set; }
        }

        /// <summary>
        /// Server pres event 
        /// </summary>
        public class ServerPresEventArgs : EventArgs
        {
            public ServerPres Pres { get; private set; }

            public ServerPresEventArgs(ServerPres pres)
            {
                Pres = pres;
            }
        }

        /// <summary>
        /// Server meta event
        /// </summary>
        public class ServerMetaEventArgs : EventArgs
        {
            public ServerMeta Meta { get; private set; }
            public ServerMetaEventArgs(ServerMeta meta)
            {
                Meta = meta;
            }
        }

        /// <summary>
        /// Ctrl message event
        /// </summary>
        public class CtrlMessageEventArgs : EventArgs
        {
            /// <summary>
            /// Ctrl message type
            /// </summary>
            public FutureTypes Type { get;private set; }
            /// <summary>
            /// tid
            /// </summary>
            public string Id { get;private set; }
            /// <summary>
            /// ctrl code
            /// </summary>
            public int Code { get; private set; }
            /// <summary>
            /// ctrl text
            /// </summary>
            public string Text { get; private set; }
            /// <summary>
            /// ctrl topic
            /// </summary>
            public string Topic { get; private set; }
            /// <summary>
            /// rpc callback status, if error or failed ,this will be true
            /// </summary>
            public bool HasError => !(Code >= 200 && Code < 400);
            /// <summary>
            /// paramaters
            /// </summary>
            public MapField<string,ByteString> Params { get; private set; }

            public CtrlMessageEventArgs(FutureTypes type,string id,int code,string text,string topic, MapField<string, ByteString> paramaters)
            {
                Type = type;
                Id = id;
                Code = code;
                Text = text;
                Topic = topic;
                Params = paramaters;
            }


        }

        /// <summary>
        /// Server Data event, when there is a message this event will fired
        /// </summary>
        public class ServerDataEventArgs : EventArgs
        {
            public ServerData Data { get; private set; }
            public ServerDataEventArgs(ServerData data)
            {
                Data = data;
            }
        }

        /// <summary>
        /// Chatbot subscribed user/group information
        /// </summary>
        public class Subscriber:MessageBase
        {
            /// <summary>
            /// User Id
            /// </summary>
            public string UserId { get; set; }
            /// <summary>
            /// topic
            /// </summary>
            public string Topic { get; set; }
            /// <summary>
            /// user name/nick
            /// </summary>
            public string UserName { get; set; }
            /// <summary>
            /// user photo with base64 encode
            /// </summary>
            public string PhotoData { get; set; }
            /// <summary>
            /// subscribed user/group type, if user,this will be "user",else will be "group"
            /// </summary>
            public string Type { get; set; }
            /// <summary>
            /// online status
            /// </summary>
            public bool Online { get; set; }
            /// <summary>
            /// user photo image type
            /// </summary>
            public string PhotoType { get; set; }
            public Subscriber()
            {

            }
            /// <summary>
            /// constructor
            /// </summary>
            /// <param name="userId">user id</param>
            /// <param name="topic">topic</param>
            /// <param name="username"> user name/nick</param>
            /// <param name="type">subscribed type, user or group?</param>
            /// <param name="photo"> user photo with base64 encode</param>
            /// <param name="photoType">user photo image type</param>
            /// <param name="online">if the subscriber is online now</param>
            public Subscriber(string userId,string topic,string username,string type,string photo,string photoType,bool online)
            {
                UserId = userId;
                Topic = topic;
                UserName = username;
                Type = type;
                PhotoData = photo;
                PhotoType = photoType;
                Online = online;
            }

        }

        /// <summary>
        /// chatbot received pres event
        /// </summary>
        public event EventHandler<ServerPresEventArgs> ServerPresEvent;
        /// <summary>
        /// chatbot receive meta data event
        /// </summary>
        public event EventHandler<ServerMetaEventArgs> ServerMetaEvent;
        /// <summary>
        /// chatbot receive ctrl message event
        /// </summary>
        public event EventHandler<CtrlMessageEventArgs> CtrlMessageEvent;
        /// <summary>
        /// chatbot receive message data event
        /// </summary>
        public event EventHandler<ServerDataEventArgs> ServerDataEvent;
        /// <summary>
        /// Login success event
        /// </summary>
        public event EventHandler LoginSuccessEvent;
        /// <summary>
        /// Login failed event
        /// </summary>
        public event EventHandler LoginFailedEvent;
        /// <summary>
        /// When chatbot disconnected with the server event
        /// </summary>
        public event EventHandler DisconnectedEvent;

        void OnServerPresEvent(ServerPresEventArgs e)
        {
            var handler = ServerPresEvent;
            if (handler!=null)
            {
                handler(this, e);
            }
        }

        void OnServerMetaEvent(ServerMetaEventArgs e)
        {
            var handler = ServerMetaEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        void OnServerDataEvent(ServerDataEventArgs e)
        {
            var handler = ServerDataEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        void OnCtrlMessageEvent(FutureTypes type, string id, int code, string text, string topic, MapField<string, ByteString> paramaters)
        {
            var e = new CtrlMessageEventArgs(type, id, code, text, topic, paramaters);
            var handler = CtrlMessageEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        void OnLoginEvent(bool isSuccess)
        {
            if (isSuccess)
            {
                var handler = LoginSuccessEvent;
                if (handler!=null)
                {
                    handler(this, new EventArgs());
                }
            }
            else
            {
                var handler = LoginFailedEvent;
                if (handler != null)
                {
                    handler(this, new EventArgs());
                }
            }
        }

        void OnDisconnected()
        {
            var handler = DisconnectedEvent;
            if (handler!=null)
            {
                handler(this, new EventArgs());
            }
        }

        /// <summary>
        /// future callback types 
        /// </summary>
        public enum FutureTypes
        {
            /// <summary>
            /// defatul, unknown callback operation
            /// </summary>
            Unknown,
            /// <summary>
            /// Hi rpc call
            /// </summary>
            Hi,
            /// <summary>
            /// login rpc call
            /// </summary>
            Login,
            /// <summary>
            /// sub rpc call
            /// </summary>
            Sub,
            /// <summary>
            /// get rpc call
            /// </summary>
            Get,
            /// <summary>
            /// pub rpc call
            /// </summary>
            Pub,
            /// <summary>
            /// note rpc call
            /// </summary>
            Note, 
            /// <summary>
            /// leave rpc call
            /// </summary>
            Leave,
        }

        /// <summary>
        /// Help define functionale which will be called in future.
        /// </summary>
        public class Future
        {
            /// <summary>
            /// Each rpc call message id
            /// </summary>
            public string Tid { get; private set; }
            /// <summary>
            /// Argument needs by action.
            /// </summary>
            public string Arg { get; private set; }
            /// <summary>
            /// Future action type
            /// </summary>
            public FutureTypes Type { get; private set; }
            /// <summary>
            /// callback function
            /// </summary>
            public Action<string, MapField<string, ByteString>> Action { get; private set; }
            /// <summary>
            /// construction
            /// </summary>
            /// <param name="tid"> Each rpc call message id</param>
            /// <param name="action">Argument needs by action.</param>
            /// <param name="arg">callback function</param>
            public Future(string tid,FutureTypes type,Action<string, MapField<string, ByteString>> action,string arg="")
            {
                Tid = tid;
                Type = type;
                Action = action;
                Arg = arg;
            }
        }

        /// <summary>
        /// Chatbot application name
        /// </summary>
        public string AppName => "ChatBot";
        /// <summary>
        /// Chatbot version
        /// </summary>
        public string AppVersion => "0.18.1";
        /// <summary>
        /// Chatbot library version
        /// </summary>
        public string LibVersion => "0.18.1";
        /// <summary>
        /// Chatbot current platfrom information
        /// </summary>
        public string Platform => $"({RuntimeInformation.OSDescription} {RuntimeInformation.OSArchitecture})";
        /// <summary>
        /// upload large file endpoint
        /// </summary>
        public const string UploadEndpoint = "/v0/file/u";
        /// <summary>
        /// download large file endpoint
        /// </summary>
        public const string DownloadEndpoint = "/v0/file/s";
        /// <summary>
        /// Chatbot instance id, this will be used in chat
        /// </summary>
        public string BotUID { get; private set; }
        /// <summary>
        /// Next tid
        /// </summary>
        public long NextTid { get; private set; }
        /// <summary>
        /// gRPC server
        /// </summary>
        public string ServerHost { get; set; }
        /// <summary>
        /// Plugin API calls listen addr
        /// </summary>
        public string Listen { get; set; }
        /// <summary>
        /// Cookie file
        /// </summary>
        public string CookieFile { get; set; }
        /// <summary>
        /// Login in schema
        /// </summary>
        public string Schema { get; set; }
        /// <summary>
        /// Login in credentials
        /// </summary>
        public ByteString Secret { get; set; }
        /// <summary>
        /// Chatbot auto reply implement interface,you can use this to make you own chat logic
        /// </summary>
        public IBotResponse BotResponse { get; set; }

        public Dictionary<string, Subscriber> Subscribers { get; private set; }

        Server server;
        AsyncDuplexStreamingCall<ClientMsg, ServerMsg> client;
        Channel channel;
        CancellationTokenSource cancellationTokenSource;
        Queue<ClientMsg> sendMsgQueue;
        Dictionary<string, bool> subscriptions;
        Dictionary<string, Future> onCompletion;
        string token;
        string apiKey;
        string apiBaseUrl;
        /// <summary>
        /// Contruction
        /// </summary>
        /// <param name="serverHost">gRPC server</param>
        /// <param name="listen">Plugin API calls listen addr</param>
        /// <param name="cookie">Cookie file</param>
        /// <param name="schema">Login in schema</param>
        /// <param name="secret">Login in credentials</param>
        /// <param name="botResponse">Chatbot auto reply implement interface,you can use this to make you own chat logic</param>
        public ChatBot(string serverHost="localhost:6061",string listen="0.0.0.0:40052",string cookie=".tn-cookie",string schema="basic",string secret="",IBotResponse botResponse=null)
        {
            //Initial a tid with a random value btw 1~1000
            NextTid =new Random().Next(1,1000);
            ServerHost = serverHost;
            Listen = listen;
            CookieFile = cookie;
            Schema = schema;
            Secret = ByteString.CopyFromUtf8(secret);
            BotResponse = botResponse;
            cancellationTokenSource = new CancellationTokenSource();
            sendMsgQueue = new Queue<ClientMsg>();
            subscriptions = new Dictionary<string, bool>();
            onCompletion = new Dictionary<string, Future>();
            Subscribers = new Dictionary<string, Subscriber>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="apiBaseUrl"></param>
        /// <param name="apikey">http api operation api key</param>
        public void SetHttpApi(string apiBaseUrl,string apikey)
        {
            this.apiBaseUrl = apiBaseUrl;
            this.apiKey = apikey;
        }

        /// <summary>
        /// Show log to console
        /// </summary>
        /// <param name="title">message title</param>
        /// <param name="message">message content</param>
        void Log(string title,string message)
        {
            Console.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] - [{title}] : {message}");
        }

        /// <summary>
        /// generate the next tid
        /// </summary>
        /// <returns>new tid</returns>
        public string GetNextTid()
        {
            NextTid += 1;
            return NextTid.ToString();
        }
        
        /// <summary>
        /// Initialize plugin api calls listen server
        /// </summary>
        /// <returns>Plugin api calls server</returns>
        public Server InitServer()
        {
            var server = new Server();
            server.Services.Add(Plugin.BindService(new ChatBotPlugin()));
            var listenHost = Listen.Split(':')[0];
            var listenPort = int.Parse(Listen.Split(':')[1]);
            server.Ports.Add(new ServerPort(listenHost,listenPort, ServerCredentials.Insecure));
            server.Start();
            return server;
        }

        /// <summary>
        /// Initialize chatbot client
        /// </summary>
        /// <returns>chatbot client instance</returns>
        public AsyncDuplexStreamingCall<ClientMsg, ServerMsg> InitClient()
        {
            //ping / 2s and timeout 2s
            var options = new List<ChannelOption>
            {
                new ChannelOption("grpc.keepalive_time_ms", 2000),
                new ChannelOption("grpc.keepalive_timeout_ms",2000)
            };

            channel = new Channel(ServerHost, ChannelCredentials.Insecure,options);
            
            var stub = new NodeClient(channel);
            
            var stream = stub.MessageLoop(cancellationToken:cancellationTokenSource.Token);
            ClientPost(Hello());
            ClientPost(Login(CookieFile, Schema, Secret));
            ClientPost(Subscribe("me"));
            return stream;
        }

        /// <summary>
        /// Add future callback
        /// </summary>
        /// <param name="tid">tid</param>
        /// <param name="bundle">callback instance</param>
        public void AddFuture(string tid,Future bundle)
        {
            onCompletion.Add(tid, bundle);
        }

        /// <summary>
        /// Execute callbacks in future callback collection
        /// </summary>
        /// <param name="tid">tid</param>
        /// <param name="code">rpc status code</param>
        /// <param name="text">text</param>
        /// <param name="topic">topic name</param>
        /// <param name="paramaters">paramaters</param>
        public void ExecFuture(string tid,int code,string text, string topic, MapField<string,ByteString> paramaters)
        {
            
            if (onCompletion.ContainsKey(tid))
            {
                var bundle = onCompletion[tid];
                var type = onCompletion[tid].Type;
                onCompletion.Remove(tid);
                if (code>=200 && code<400)
                {
                    var arg = bundle.Arg;
                    bundle.Action(arg, paramaters);
                    if (type==FutureTypes.Sub)
                    {
                        ClientPost(GetSubs("me"));
                    }
                    else if (type==FutureTypes.Login)
                    {
                        OnLoginEvent(true);
                    }
                   
                    Log("Exec Future", $"Tid={tid} Code={code} Topic={topic} Text={text} Params={paramaters}  ...OK");
                }
                else
                {
                    if (type == FutureTypes.Login)
                    {
                        OnLoginEvent(false);
                    }
                    Log("Exec Future", $"Tid={tid} Code={code} Topic={topic} Text={text} Params={paramaters}   ...Failed");
                }

                OnCtrlMessageEvent(type, tid, code, text, topic, paramaters);
            }
        }

        /// <summary>
        /// add a chat topic to subscription
        /// </summary>
        /// <param name="topic">topic name </param>
        public void AddSubscription(string topic)
        {
            if (!subscriptions.ContainsKey(topic))
            {
                subscriptions.Add(topic, true);
            }
        }

        public void AddSubscriber(Subscriber sub)
        {
            Log("Update Subscriber", sub.ToString());
            //Log("Update Subscriber", $"UserId={sub.UserId} Name={sub.UserName} Online={sub.Online} Type={sub.Type}");
            if (Subscribers.ContainsKey(sub.Topic))
            {
                Subscribers[sub.Topic] = sub;
            }
            else
            {
                Subscribers.Add(sub.Topic, sub);
                //if the first, sub it ahead
                ClientPost(Subscribe(sub.Topic));
            }
        }

        /// <summary>
        /// delete a chat topic from subscription
        /// </summary>
        /// <param name="topic">topic name </param>
        public void DelSubscription(string topic)
        {
            if (subscriptions.ContainsKey(topic))
            {
                subscriptions.Remove(topic);
            }
        }

        /// <summary>
        /// Server version callback implement
        /// </summary>
        /// <param name="paramaters">paramaters</param>
        public void ServerVersion(MapField<string, ByteString> paramaters)
        {
            if (paramaters==null)
            {
                return;
            }
            Log("Server Version",$"Server:{paramaters["build"].ToString(Encoding.ASCII)},{paramaters["ver"].ToString(Encoding.ASCII)}");
        }

        /// <summary>
        /// login callback implement
        /// </summary>
        /// <param name="cookieFile">cookie file</param>
        /// <param name="paramaters">paramaters</param>
        public void OnLogin(string cookieFile, MapField<string, ByteString> paramaters)
        {
            if (paramaters == null || string.IsNullOrEmpty(cookieFile))
            {
                return;
            }
            if (paramaters.ContainsKey("user"))
            {
                BotUID = paramaters["user"].ToString(Encoding.ASCII);
            }
            Dictionary<string, string> cookieDics = new Dictionary<string, string>();
            cookieDics["schema"] = "token";
            if (paramaters.ContainsKey("token"))
            {
                cookieDics["secret"] = JsonConvert.DeserializeObject<string>(paramaters["token"].ToString(Encoding.UTF8));
                cookieDics["expires"] = JsonConvert.DeserializeObject<string>(paramaters["expires"].ToString(Encoding.UTF8));
            }
            else
            {
                cookieDics["schema"] = "basic";
                cookieDics["secret"] = JsonConvert.DeserializeObject<string>(paramaters["token"].ToString(Encoding.UTF8));
            }
            //save token for upload operation
            token = cookieDics["secret"];
            try
            {
                using (FileStream stream = new FileStream(cookieFile, FileMode.Create,FileAccess.Write))
                using (StreamWriter w = new StreamWriter(stream))
                {
                    w.Write(JsonConvert.SerializeObject(cookieDics));
                }

            }
            catch (Exception e)
            {
                Log("On Login",$"Failed to save authentication cookie:{e}");
            }

        }

        public void OnGetMeta(ServerMeta meta)
        {
            if (meta.Sub!=null)
            {
                foreach (var sub in meta.Sub)
                {
                    var userId = sub.UserId;
                    var online = sub.Online;
                    var topic = sub.Topic;
                    var publicInfo = sub.Public.ToStringUtf8();
                    var subObj = JsonConvert.DeserializeObject<JObject>(publicInfo);
                    string userName = topic;
                    string type = subObj == null ? "group" : "user";
                    string photoData = string.Empty;
                    string photoType = string.Empty;
                    if (subObj != null)
                    {
                        userName = subObj["fn"].ToString();
                        if (subObj.ContainsKey("photo"))
                        {
                            photoData = subObj["photo"]["data"].ToString();
                            photoType = subObj["photo"]["type"].ToString();
                        }
                    }
                    AddSubscriber(new Subscriber(userId,topic, userName, type, photoData, photoType,online)); 
                }
            }
        }

        /// <summary>
        /// read auth information from cookie
        /// </summary>
        /// <param name="schema">schema type</param>
        /// <param name="secret">secret</param>
        /// <returns>success-true, faild-false</returns>
        public bool ReadAuthCookie(out string schema,out ByteString secret)
        {
            schema = null;
            secret =null;
            if (!File.Exists(CookieFile))
            {
                return false;
            }

            try
            {
                using (FileStream stream = new FileStream(CookieFile, FileMode.Open,FileAccess.Read))
                using (StreamReader r = new StreamReader(stream))
                {
                    var cookies = JsonConvert.DeserializeObject<Dictionary<string, string>>(r.ReadToEnd());
                    schema = cookies["schema"];
                    secret = null;
                    if (schema == "token")
                    {
                        var defautl = Encoding.Default.GetBytes(cookies["secret"]);
                        var utf8Str = Encoding.UTF8.GetString(defautl);
                        secret = ByteString.FromBase64(utf8Str);
                    }
                    else
                    {
                        secret = ByteString.CopyFromUtf8(cookies["secret"]);
                    }
                    return true;

                }
            }
            catch (Exception)
            {
                return false;
            }
            
        }

        /// <summary>
        /// Post message to message queue
        /// </summary>
        /// <param name="msg">message</param>
        public void ClientPost(ClientMsg msg)
        {
            sendMsgQueue.Enqueue(msg);
        }

        /// <summary>
        /// reset client status
        /// </summary>
        public void ClientReset()
        {
            try
            {
                token = null;
                subscriptions.Clear();
                onCompletion.Clear();
                Subscribers.Clear();
                while (sendMsgQueue.Count > 0)
                {
                    sendMsgQueue.Dequeue();
                }
            }
            catch (Exception e)
            {
            }
        }

        /// <summary>
        /// Say Hi to server
        /// </summary>
        /// <returns>Hi message</returns>
        public ClientMsg Hello()
        {
            var tid = GetNextTid();
            AddFuture(tid, new Future(tid, FutureTypes.Hi,new Action<string, MapField<string, ByteString>>((unused, paramaters) =>
            {
                ServerVersion(paramaters);
            })));

            return new ClientMsg() { Hi = new ClientHi() { Id = tid, UserAgent = $"{AppName}/{AppVersion} {Platform}; gRPC-csharp/{AppVersion}", Ver = LibVersion, Lang = "EN" } };
        }

        public ClientMsg GetSubs(string topic="me",bool getAll=false)
        {
            var tid = GetNextTid();
            if (getAll)
            {
                return new ClientMsg() { Get = new ClientGet() { Id = tid, Topic = topic, Query = new GetQuery() { What = "sub" } } };
            }
            else
            {
                return new ClientMsg() { Get = new ClientGet() { Id = tid, Topic = topic} };
            }
            
        }



        /// <summary>
        /// login in
        /// </summary>
        /// <param name="cookieFile">cookie file</param>
        /// <param name="scheme">schema type</param>
        /// <param name="secret">secret</param>
        /// <returns>login in message</returns>
        public ClientMsg Login(string cookieFile,string scheme,ByteString secret)
        {
            var tid = GetNextTid();
            AddFuture(tid, new Future(tid, FutureTypes.Login,new Action<string, MapField<string, ByteString>>((fname, paramaters) =>
            {
                OnLogin(fname, paramaters);
            }),cookieFile));

            return new ClientMsg() { Login = new ClientLogin() { Id = tid, Scheme = scheme, Secret = secret } };
        }

        /// <summary>
        /// Subscribe topic
        /// </summary>
        /// <param name="topic">topic name</param>
        /// <returns>subscribe message</returns>
        public ClientMsg Subscribe(string topic)
        {
            var tid = GetNextTid();
            AddFuture(tid, new Future(tid, FutureTypes.Sub,new Action<string, MapField<string, ByteString>>((topicName, unused) =>
            {
                AddSubscription(topicName);
            }),topic));

            return new ClientMsg() { Sub = new ClientSub() { Id = tid, Topic = topic } };
        }

        /// <summary>
        /// leave topic
        /// </summary>
        /// <param name="topic">topic name</param>
        /// <returns>leave message</returns>
        public ClientMsg Leave(string topic)
        {
            var tid = GetNextTid();
            AddFuture(tid, new Future(tid, FutureTypes.Leave,new Action<string, MapField<string, ByteString>>((topicName, unused) =>
            {
                DelSubscription(topicName);
            }), topic));

            return new ClientMsg() { Leave = new ClientLeave() { Id = tid, Topic = topic } };
        }

        /// <summary>
        /// publish message to a topic 
        /// </summary>
        /// <param name="topic">topic name</param>
        /// <param name="text">message</param>
        /// <returns>publish message</returns>
        public ClientMsg Publish(string topic,string text)
        {
            var tid = GetNextTid();
            var message = ByteString.CopyFromUtf8(JsonConvert.SerializeObject(text));

            return new ClientMsg() { Pub = new ClientPub() { Id = tid, Topic = topic, NoEcho = true, Content = message } };
        }

        public ClientMsg Publish(string topic,ChatMessage msg)
        {
            var tid = GetNextTid();
            var pub = new ClientPub() { Id = tid, Topic = topic, NoEcho = true,  Content = ByteString.CopyFromUtf8(msg.ToString()) };
            pub.Head.Add("mime",ByteString.CopyFromUtf8("\"text/x-drafty\""));
            return new ClientMsg() { Pub = pub };
        }

        public ClientMsg PublishWithAttachments(string topic, List<string> attachments,ChatMessage msg)
        {
            var tid = GetNextTid();
            
            StringBuilder builder = new StringBuilder();
            builder.Append("[");
            foreach (var attach in attachments)
            {
                builder.Append($"\"{attach}\",");
            }
            builder.Remove(builder.Length - 1,1);
            builder.Append("]");

            var pub = new ClientPub() { Id = tid, Topic = topic, NoEcho = true, Content = ByteString.CopyFromUtf8(msg.ToString()) };
            pub.Head.Add("attachments", ByteString.CopyFromUtf8(builder.ToString()));
            pub.Head.Add("mime", ByteString.CopyFromUtf8("\"text/x-drafty\""));

            return new ClientMsg() { Pub = pub };
        }


        /// <summary>
        /// note read
        /// </summary>
        /// <param name="topic">topic name </param>
        /// <param name="seq">chat sequence id</param>
        /// <returns>note message</returns>
        public ClientMsg NoteRead(string topic, int seq)
        {
            return new ClientMsg() { Note = new ClientNote() { SeqId = seq, Topic = topic, What = InfoNote.Read } };
        }

        /// <summary>
        /// upload large file as attachment
        /// </summary>
        /// <param name="fileName">the file will be uploaded</param>
        /// <param name="redirectUrl">if upload operation should temp changed</param>
        /// <returns>uploaded information for send message to user or group</returns>
        public async Task<UploadedAttachmentInfo> Upload(string fileName, string redirectUrl = "")
        {
            if (string.IsNullOrEmpty(token))
            {
                //not login or login failed, disable upload
                Log("Upload Attachment Error", $"Not login, upload operation disabled...");
                return null;
            }
            if (string.IsNullOrEmpty(fileName)||!File.Exists(fileName))
            {
                Log("Upload Attachment Error", $"can not find file:{fileName}");
                return null;
            }
            try
            {
                var fullFileName = Path.GetFullPath(fileName);
                var fileInfo = new FileInfo(fullFileName);

                UploadedAttachmentInfo attachmentInfo = new UploadedAttachmentInfo();
                attachmentInfo.FullFileName = fullFileName;
                attachmentInfo.FileName = fileInfo.Name;
                attachmentInfo.Size = fileInfo.Length;
                attachmentInfo.Mime = $"file/{fileInfo.Extension}";

                var restClient = new RestClient(apiBaseUrl) ;
                RestRequest request;
                if (string.IsNullOrEmpty(redirectUrl))
                {
                    request = new RestRequest(UploadEndpoint, Method.Put);
                }
                else
                {
                    request = new RestRequest(redirectUrl, Method.Put);
                }
                
                request.AddHeader("X-Tinode-APIKey", apiKey);
                request.AddHeader("X-Tinode-Auth", $"Token {token}");
                request.AddBody("id", GetNextTid());

                request.AddFile("file", fullFileName);
                var cancellationTokenSource = new CancellationTokenSource();
                var response = await restClient.ExecuteAsync(request,cancellationTokenSource.Token);
                if (response.StatusCode >= HttpStatusCode.OK && response.StatusCode < HttpStatusCode.BadRequest)
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var obj = JsonConvert.DeserializeObject<JToken>(response.Content);
                        var code = obj["ctrl"]["code"].ToString();
                        var url = obj["ctrl"]["params"]["url"].ToString();
                        attachmentInfo.RelativeUrl = url;

                        Log("Upload Attachment Success", $"Upload file success, File={fullFileName}, RefUrl={url}");
                        return attachmentInfo;
                    }
                    else if (response.StatusCode == HttpStatusCode.TemporaryRedirect)
                    {
                        var tmpRedirectUrl = string.Empty;
                        //307,should reupload to the redirect url
                        foreach (var h in response.Headers)
                        {
                            //查找重定向地址
                            if (h.Name.ToLower() == "location")
                            {
                                tmpRedirectUrl = h.Value.ToString();
                                break;
                            }
                        }
                        Log("Upload Attachment Redirect", $"redirect upload...");
                        return await Upload(fileName, redirectUrl);
                    }
                    else
                    {
                        Log("Upload Attachment Error", $"Upload failed, Http Code={response.StatusCode}");
                        return null;
                    }


                }
                else
                {
                    //Failed
                    Log("Upload Attachment Error", $"Upload failed, Http Code={response.StatusCode}");
                    return null;
                }


            }
            catch (Exception e)
            {
                //exception occurs
                Log("Upload Attachment Exception", $"Upload failed, Exception={e.Message}");
                return null;
            }
        }

        /// <summary>
        /// sending message queue loop
        /// </summary>
        public void SendMessageLoop()
        {
            Task sendBackendTask = new Task(async () =>
            {
                Log("Start Message Queue","Message send queue started...");
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    if (sendMsgQueue.Count > 0)
                    {
                        var msg = sendMsgQueue.Dequeue();
                        try
                        {
                            await client.RequestStream.WriteAsync(msg);
                        }
                        catch (Exception e)
                        {
                            Log("Send Message Error",$"{e.Message} ,Failed message will be put back to queue...");
                            sendMsgQueue.Enqueue(msg);
                            Thread.Sleep(1000);
                        }


                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }
                Log("User Cancel","Detect cancel message,stop sending message...");
            }, cancellationTokenSource.Token);
            sendBackendTask.Start();

        }

        /// <summary>
        /// client receive message loop
        /// </summary>
        /// <returns></returns>
        public async Task ClientMessageLoop()
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                if (!await client.ResponseStream.MoveNext())
                {
                    break;
                }
                var response = client.ResponseStream.Current;
                if (response.Ctrl != null)
                {
                    Log("Ctrl Message",$"ID={response.Ctrl.Id}  Code={response.Ctrl.Code}  Text={response.Ctrl.Text}  Params={response.Ctrl.Params}");
                    ExecFuture(response.Ctrl.Id, response.Ctrl.Code, response.Ctrl.Text, response.Ctrl.Topic, response.Ctrl.Params);
                }
                else if (response.Data != null)
                {
                    OnServerDataEvent(new ServerDataEventArgs(response.Data.Clone()));
                    if (response.Data.FromUserId != BotUID)
                    {
                        ClientPost(NoteRead(response.Data.Topic, response.Data.SeqId));
                        Thread.Sleep(50);
                        if (BotResponse != null)
                        {
                            var reply =await BotResponse.ThinkAndReply(response.Data.Clone());
                            //if the response is null, means no need to reply
                            if (reply!=null)
                            {
                                ClientPost(Publish(response.Data.Topic, reply));
                            }
                            
                        }
                        else
                        {
                            ClientPost(Publish(response.Data.Topic, "I don't know how to talk with you, maybe my father didn't put my brain in my head..."));
                        }

                    }
                }
                else if (response.Pres != null)
                {
                    if (response.Pres.Topic == "me")
                    {
                        if ((response.Pres.What == ServerPres.Types.What.On || response.Pres.What == ServerPres.Types.What.Msg) && !subscriptions.ContainsKey(response.Pres.Src))
                        {
                            ClientPost(Subscribe(response.Pres.Src));

                        }
                        else if (response.Pres.What == ServerPres.Types.What.Off && subscriptions.ContainsKey(response.Pres.Src))
                        {
                            ClientPost(Leave(response.Pres.Src));
                        }
                    }

                    OnServerPresEvent(new ServerPresEventArgs(response.Pres.Clone()));
                }
                else if (response.Meta!=null)
                {
                    OnGetMeta(response.Meta);
                    OnServerMetaEvent(new ServerMetaEventArgs(response.Meta.Clone()));
                }
            }
        }

        /// <summary>
        /// start chatbot
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {

            server= InitServer();
            client = InitClient();
            SendMessageLoop();
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    await ClientMessageLoop();
                }
                catch (Exception e)
                {
                    Log("Connection Broken",$"Connection Closed:{e}");
                    OnDisconnected();
                    Thread.Sleep(2000);
                    ClientReset();
                    client = InitClient();
                }
            }

        } 
        
        /// <summary>
        /// stop chatbot
        /// </summary>
        public void Stop()
        {
            Log("Stopping","ChatBot is exiting...Wait a second...");
            cancellationTokenSource.Cancel();
            server.ShutdownAsync().Wait();
            channel.ShutdownAsync().Wait();

        }

    }
}