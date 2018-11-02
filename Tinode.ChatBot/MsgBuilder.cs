using Newtonsoft.Json;
using Pbx;
using System;
using System.Collections.Generic;
using System.Text;
using static Tinode.ChatBot.ChatBot;

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
        /// message from type, user or group, if not set it is unknown
        /// </summary>
        [JsonIgnore]
        public string MessageType { get; set; }

        public ChatMessage()
        {
            Text = "";
            MessageType = "unknown";
        }

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
       
        /// <summary>
        /// get entity from chat message by entity type
        /// </summary>
        /// <param name="tp">entity type</param>
        /// <returns>entity datas</returns>
        public List<EntData> GetEntDatas(string tp)
        {
            var ret = new List<EntData>();
            if (Ent != null)
            {
                foreach (var ent in Ent)
                {
                    if (ent.Tp == tp)
                    {
                        ret.Add(ent.Data);
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// get mentioned users
        /// </summary>
        /// <returns>mentioned user datas</returns>
        public List<EntData> GetMentions()
        {
            return GetEntDatas("MN");
        }

        /// <summary>
        /// get images
        /// </summary>
        /// <returns>image data</returns>
        public List<EntData> GetImages()
        {
            return GetEntDatas("IM");
        }

        /// <summary>
        /// get hashtags
        /// </summary>
        /// <returns>hashtag data</returns>
        public List<EntData> GetHashTags()
        {
            return GetEntDatas("HT");
        }

        /// <summary>
        /// get links
        /// </summary>
        /// <returns>link data</returns>
        public List<EntData> GetLinks()
        {
            return GetEntDatas("LN");
        }

        /// <summary>
        /// get files
        /// </summary>
        /// <returns>file data</returns>
        public List<EntData> GetFiles()
        {
            return GetEntDatas("EX");
        }
    }

    /// <summary>
    /// Chat message builder
    /// </summary>
    public class MsgBuilder
    {
        /// <summary>
        /// Build message data
        /// </summary>
        public ChatMessage Message { get; private set; }

        public MsgBuilder()
        {
            ReSet();
        }

        /// <summary>
        /// Reset all build message to init
        /// </summary>
        public void ReSet()
        {
            Message = new ChatMessage();
            Message.Ent = new List<EntMessage>();
            Message.Fmt = new List<FmtMessage>();
        }

        /// <summary>
        /// Append text message to build message
        /// </summary>
        /// <param name="text">text message</param>
        /// <param name="isBold">if text should bold style</param>
        /// <param name="isItalic">if text should italic style</param>
        /// <param name="isDeleted">if text should deleted style</param>
        /// <param name="isCode">if text is code like data</param>
        /// <param name="isLink">if text is link</param>
        /// <param name="isMention">if text is mention user, should contains '@', for example: @tinode</param>
        /// <param name="isHashTag">if text is hashtags, should contains '#',for example: #tinode</param>
        public void AppendText(string text,bool isBold=false,bool isItalic=false,bool isDeleted=false
            ,bool isCode=false,bool isLink=false,bool isMention=false,bool isHashTag=false)
        {
            int baseLen = Message.Text.Length;
            Message.Text += text;
            if (text.Contains("\n"))
            {
                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] == '\n')
                    {
                        FmtMessage fmt = new FmtMessage() { At = baseLen + i, Tp = "BR", Len = 1 };
                        Message.Fmt.Add(fmt);
                    }
                }
            }

            var leftLen =baseLen+(text.Length- text.TrimStart().Length);
            var subLen = text.Length - text.TrimEnd().Length;
            var validLen = Message.Text.Length - leftLen - subLen;
            
            if (isBold)
            {
                FmtMessage fmt = new FmtMessage() { Tp = "ST",At= leftLen, Len= validLen };
                Message.Fmt.Add(fmt);
            }

            if (isItalic)
            {
                FmtMessage fmt = new FmtMessage() { Tp = "EM" ,At = leftLen, Len = validLen };
                Message.Fmt.Add(fmt);
            }

            if (isDeleted)
            {
                FmtMessage fmt = new FmtMessage() { Tp = "DL" , At = leftLen, Len = validLen };
                Message.Fmt.Add(fmt);
            }

            if (isCode)
            {
                FmtMessage fmt = new FmtMessage() { Tp = "CO" , At = leftLen, Len = validLen };
                Message.Fmt.Add(fmt);
            }

            if (isLink)
            {
                FmtMessage fmt = new FmtMessage() {  At = leftLen, Len = validLen ,Key=Message.Ent.Count};
                Message.Fmt.Add(fmt);
                var url = text.Trim().ToLower();
                if (!url.StartsWith("http://")&&!url.StartsWith("https://"))
                {
                    url = $"http://{text.Trim()}";
                }
                EntMessage ent = new EntMessage() { Tp="LN",Data=new EntData() { Url=url} };
                Message.Ent.Add(ent);
            }

            if (isMention)
            {
                FmtMessage fmt = new FmtMessage() { At = leftLen, Len = validLen, Key = Message.Ent.Count };
                Message.Fmt.Add(fmt);
                var mentionName = text.Trim().Substring(1);
                EntMessage ent = new EntMessage() { Tp = "MN", Data = new EntData() { Val = mentionName} };
                Message.Ent.Add(ent);
            }

            if (isHashTag)
            {
                FmtMessage fmt = new FmtMessage() { At = leftLen, Len = validLen, Key = Message.Ent.Count };
                Message.Fmt.Add(fmt);
                var hashTag = text.Trim();
                EntMessage ent = new EntMessage() { Tp = "HT", Data = new EntData() { Val = hashTag } };
                Message.Ent.Add(ent);
            }
        }

        /// <summary>
        /// Append text message and line break to build message
        /// </summary>
        /// <param name="text">text message</param>
        /// <param name="isBold">if text should bold style</param>
        /// <param name="isItalic">if text should italic style</param>
        /// <param name="isDeleted">if text should deleted style</param>
        /// <param name="isCode">if text is code like data</param>
        /// <param name="isLink">if text is link</param>
        /// <param name="isMention">if text is mention user, should contains '@', for example: @tinode</param>
        /// <param name="isHashTag">if text is hashtags, should contains '#',for example: #tinode</param>
        public void AppendTextLine(string text, bool isBold = false, bool isItalic = false, bool isDeleted = false
            , bool isCode = false, bool isLink = false, bool isMention = false, bool isHashTag = false)
        {
            AppendText($"{text}\n", isBold, isItalic, isDeleted, isCode, isLink, isMention, isHashTag);
        }


        /// <summary>
        /// Append image to build message
        /// </summary>
        /// <param name="imageName">image name</param>
        /// <param name="mime">image mime type</param>
        /// <param name="width">display width</param>
        /// <param name="height">display height</param>
        /// <param name="imageBase64">image base64 value</param>
        public void AppendImage(string imageName, string mime, int width, int height, string imageBase64)
        {
            Message.Fmt.Add(new FmtMessage()
            {
                At = Message.Text.Length,
                Len = 1,
                Key = Message.Ent.Count,
            });

            Message.Ent.Add(new EntMessage()
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
            
        }

        /// <summary>
        /// Append file to build message
        /// </summary>
        /// <param name="fileName">file name</param>
        /// <param name="mime">file mime type</param>
        /// <param name="contentBase64">file base64 value</param>
        public void AppendFile(string fileName, string mime, string contentBase64)
        {
            Message.Fmt.Add(new FmtMessage()
            {
                At=Message.Text.Length,
                Len = 0,
                Key=Message.Ent.Count,
            });

            Message.Ent.Add(new EntMessage()
            {
                Tp = "EX",
                Data = new EntData()
                {
                    Mime = mime,
                    Name = fileName,
                    Val = contentBase64,
                }
            });
        }

        /// <summary>
        /// append a attachment file to chat message
        /// </summary>
        /// <param name="attachmentInfo">attachment file information, you can get it with ChatBot.Upload(...)</param>
        public void AppendAttachment(UploadedAttachmentInfo attachmentInfo)
        {
            Message.Fmt.Add(new FmtMessage()
            {
                At=Message.Text.Length,
                Key=Message.Ent.Count,
                Len = 1,
            });

            Message.Ent.Add(new EntMessage()
            {
                Tp="EX",
                Data=new EntData()
                {
                    Mime=attachmentInfo.Mime,
                    Name=attachmentInfo.FileName,
                    Ref=attachmentInfo.RelativeUrl,
                    Size=int.Parse(attachmentInfo.Size.ToString()),
                },
            });
        }
        

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
            if (message.Topic.StartsWith("usr"))
            {
                chatMsg.MessageType = "user";
            }
            else if (message.Topic.StartsWith("grp"))
            {
                chatMsg.MessageType = "group";
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
        /// build text chat message with formatted
        /// </summary>
        /// <param name="text">text message</param>
        /// <returns>text chat message</returns>
        public static ChatMessage BuildTextMessage(string text=" ")
        {
            var msg = new ChatMessage();
            msg.Text = text;
            msg.Ent = new List<EntMessage>();
            msg.Fmt = new List<FmtMessage>();
            if (text.Contains("\n"))
            {
                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] == '\n')
                    {
                        FmtMessage fmt = new FmtMessage() { At = i, Tp = "BR",Len=1 };
                        msg.Fmt.Add(fmt);
                    }
                }
            }
            return msg;
        }

        /// <summary>
        /// build a image chat message
        /// </summary>
        /// <param name="imageName">image file name</param>
        /// <param name="mime">mime type.such as image/png</param>
        /// <param name="width">image display width</param>
        /// <param name="height">image display height</param>
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
                At=text.Length,
                Len = 1,
                Key=0,
            });

            if (text.Contains("\n"))
            {
                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] == '\n')
                    {
                        FmtMessage fmt = new FmtMessage() { At = i, Tp = "BR",Len=1 };
                        msg.Fmt.Add(fmt);
                    }
                }
            }
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
                At=text.Length,
                Len = 0,
                Key=0,
            });

            if (text.Contains("\n"))
            {
                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] == '\n')
                    {
                        FmtMessage fmt = new FmtMessage() { At = i, Tp = "BR",Len=1 };
                        msg.Fmt.Add(fmt);
                    }
                }
            }
            return msg;
        }

        /// <summary>
        /// build a attachment message
        /// </summary>
        /// <param name="attachmentInfo">attachment file information, you can get it with ChatBot.Upload(...)</param>
        /// <param name="text">text message with the attachment</param>
        /// <returns>attachment chat message</returns>
        public static ChatMessage BuildAttachmentMessage(UploadedAttachmentInfo attachmentInfo, string text=" ")
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
                    Mime = attachmentInfo.Mime,
                    Name = attachmentInfo.FileName,
                    Ref = attachmentInfo.RelativeUrl,
                    Size= int.Parse(attachmentInfo.Size.ToString()),

                }
            });
            msg.Fmt.Add(new FmtMessage()
            {
                At = text.Length,
                Len = 1,
                Key = 0,
            });

            if (text.Contains("\n"))
            {
                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] == '\n')
                    {
                        FmtMessage fmt = new FmtMessage() { At = i, Tp = "BR", Len = 1 };
                        msg.Fmt.Add(fmt);
                    }
                }
            }
            return msg;
        }
    }
}
