using Newtonsoft.Json;
using Pbx;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tinode.ChatBot
{
    public class MessageBase
    {
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }
    }

    public class FmtMessage:MessageBase
    {
        [JsonProperty("at")]
        public int? At { get; set; }

        [JsonProperty("len")]
        public int? Len { get; set; }

        [JsonProperty("tp")]
        public string Tp { get; set; }
        

        [JsonProperty("key")]
        public int? Key { get; set; }
    }

    public class EntMessage:MessageBase
    {
        [JsonProperty("tp")]
        public string Tp { get; set; }
        [JsonProperty("data")]
        public EntData Data { get; set; }
        
    }

    public class EntData:MessageBase
    {
        [JsonProperty("mime")]
        public string Mime { get; set; }
        [JsonProperty("val")]
        public string Val { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("ref")]
        public string Ref { get; set; }
        [JsonProperty("width")]
        public int? Width { get; set; }
        [JsonProperty("height")]
        public int? Height { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("size")]
        public int? Size { get; set; }

      
    }
    public class ChatMessage:MessageBase
    {
        [JsonProperty("txt")]
        public string Text { get; set; }

        [JsonProperty("fmt")]
        public List<FmtMessage> Fmt { get; set; }
       
        [JsonProperty("ent")]
        public List<EntMessage> Ent { get; set; }

        [JsonIgnore]
        public bool IsPlainText { get;  set; }

        /// <summary>
        /// Get original text message, inlude original '\n' 
        /// </summary>
        /// <returns></returns>
        public string GetFormattedText()
        {
            if (Text==null)
            {
                return null;
            }
            var textArray = Text.ToCharArray();
            if (Fmt!=null)
            {
                foreach (var fmt in Fmt)
                {
                    if (!string.IsNullOrEmpty(fmt.Tp))
                    {
                        if (fmt.Tp=="BR")
                        {
                            textArray[fmt.At.Value] = '\n';
                        }
                    }
                }
            }
            return new String(textArray);
        }
       
    }

    public class MsgBuilder
    {
        public static ChatMessage Parse(ServerData message)
        {
            ChatMessage chatMsg;
            if (message.Head.ContainsKey("mime"))
            {
                chatMsg = JsonConvert.DeserializeObject<ChatMessage>(message.Content.ToStringUtf8()
                        , new JsonSerializerSettings()
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        });
                chatMsg.IsPlainText = false;
            }
            else
            {
                chatMsg = new ChatMessage() { Text = JsonConvert.DeserializeObject<string>(message.Content.ToStringUtf8()) };
                chatMsg.IsPlainText = true;
            }

            return chatMsg;
        }

        public static bool TryParse(ServerData message,out ChatMessage chatMsg)
        {
            try
            {
                chatMsg = MsgBuilder.Parse(message);
                return true;
            }
            catch (Exception e)
            {
                chatMsg = null;
                return false;
            }
        }

        public static ChatMessage BuildImageMessage(string imageName,string mime,int width,int height,string imageBase64,string text=" ")
        {
            var msg = new ChatMessage();
            msg.Text = text;
            msg.Ent = new List<EntMessage>();
            msg.Fmt = new List<FmtMessage>();
            msg.Ent.Add(new EntMessage()
            {
                Tp = "IM",
                Data = new EntData()
                {
                    Mime = mime,
                    Width = width,
                    Height = height,
                    Name = imageName,
                    Val = imageBase64,

                }
            });
            msg.Fmt.Add(new FmtMessage()
            {
                Len = 1
            });
            return msg;
        }

        public static ChatMessage BuildImageMessage(string imageFile, string text = " ")
        {
          
            string mime=string.Empty;
            int width = 0;
            int height=0;
            string imageName = string.Empty;
            string imageBase64 = string.Empty;
            //TODO:read image information from file
            return BuildImageMessage(imageName, mime, width, height, imageBase64, text);
        }

        public static ChatMessage BuildFileMessage(string fileName, string mime, string contentBase64, string text = " ")
        {
            var msg = new ChatMessage();
            msg.Text = text;
            msg.Ent = new List<EntMessage>();
            msg.Fmt = new List<FmtMessage>();
            msg.Ent.Add(new EntMessage()
            {
                Tp = "EX",
                Data = new EntData()
                {
                    Mime = mime,
                   
                    Name = fileName,
                    Val = contentBase64,

                }
            });
            msg.Fmt.Add(new FmtMessage()
            {
                Len = 1
            });
            return msg;
        }

        
    }
}
