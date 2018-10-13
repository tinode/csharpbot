using Newtonsoft.Json;
using Pbx;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tinode.ChatBot
{
    /// <summary>
    /// Nullable property for json serialization message base class.
    /// </summary>
    public class MessageBase
    {
        /// <summary>
        /// override to json string
        /// </summary>
        /// <returns>json string </returns>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }
    }

    /// <summary>
    /// formatt control message
    /// </summary>
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

    /// <summary>
    /// entity message
    /// </summary>
    public class EntMessage:MessageBase
    {
        [JsonProperty("tp")]
        public string Tp { get; set; }
        [JsonProperty("data")]
        public EntData Data { get; set; }
        
    }

    /// <summary>
    /// entity data details
    /// </summary>
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

    /// <summary>
    /// Chat messaage
    /// </summary>
    public class ChatMessage:MessageBase
    {
        /// <summary>
        /// message text part
        /// </summary>
        [JsonProperty("txt")]
        public string Text { get; set; }
        /// <summary>
        /// message format information
        /// </summary>
        [JsonProperty("fmt")]
        public List<FmtMessage> Fmt { get; set; }
       /// <summary>
       /// message entity information
       /// </summary>
        [JsonProperty("ent")]
        public List<EntMessage> Ent { get; set; }
        /// <summary>
        /// if this is a plain text message
        /// </summary>
        [JsonIgnore]
        public bool IsPlainText { get;  set; }

        /// <summary>
        /// Get original text message, inlude original '\n' 
        /// </summary>
        /// <returns>original message</returns>
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

    /// <summary>
    /// Chat message builder
    /// </summary>
    public class MsgBuilder
    {
        /// <summary>
        /// parse a raw ServerData to friendly ChatMessage
        /// </summary>
        /// <param name="message">raw messsage</param>
        /// <returns>parsed chat message</returns>
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

        /// <summary>
        /// try to parse a raw ServerData to friendly ChatMessage
        /// </summary>
        /// <param name="message">raw message</param>
        /// <param name="chatMsg">parsed chat message</param>
        /// <returns>success:true, else:false</returns>
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

        /// <summary>
        /// build a image chat message
        /// </summary>
        /// <param name="imageName">image file name</param>
        /// <param name="mime">mime type.such as image/png</param>
        /// <param name="width">image width</param>
        /// <param name="height">image height</param>
        /// <param name="imageBase64">iamge with base64 encode</param>
        /// <param name="text">text message with the image</param>
        /// <returns>image chat message</returns>
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

        /// <summary>
        /// build a file chat message
        /// </summary>
        /// <param name="fileName">file name</param>
        /// <param name="mime">mime type, such as text/plain</param>
        /// <param name="contentBase64">file with base64 encode</param>
        /// <param name="text">text messaage with the file</param>
        /// <returns>file chat message</returns>
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
