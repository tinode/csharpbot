using CommandLine;
using Google.Protobuf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pbx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tinode.ChatBot;

namespace Tinode.ChatBot.DemoNet46
{
    class Program
    {
        public class CmdOptions
        {
            [Option('C', "login-cookie", Required = false, Default = ".tn-cookie", HelpText = "read credentials from the provided cookie file")]
            public string CookieFile { get; set; }
            [Option('T', "login-token", Required = false, HelpText = "login using token authentication")]
            public string Token { get; set; }
            [Option('B', "login-basic", Required = false, HelpText = "login using basic authentication username:password")]
            public string Basic { get; set; }
            [Option('L', "listen", Required = false, Default = "0.0.0.0:40051", HelpText = "address to listen on for incoming Plugin API calls")]
            public string Listen { get; set; }
            [Option('S', "server", Required = false, Default = "localhost:16060", HelpText = "address of Tinode server gRPC endpoint")]
            public string Host { get; set; }
        }

        /// <summary>
        /// ChatBot auto reply implement
        /// </summary>
        public class BotReponse : IBotResponse
        {
            ChatBot bot;
            public BotReponse(ChatBot bot)
            {
                this.bot = bot;
            }
            public async Task<ChatMessage> ThinkAndReply(ServerData message)
            {
                foreach (var sub in bot.Subscribers)
                {
                    //Current account friends info
                }
                ChatMessage responseMsg;
                var msgText = message.Content.ToStringUtf8();
                var msg = MsgBuilder.Parse(message);
                if (msg.IsPlainText)
                {
                    if (msg.Text == "image")
                    {
                        //Send Image File
                        responseMsg = MsgBuilder.BuildImageMessage("a.png", "image/png", 47, 48, "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ACV0RVh0ZGF0ZTptb2RpZnkAMjAxMS0wNS0xOFQyMzo1NDowMCswODowMDef1rEAAABDdEVYdHNvZnR3YXJlAC91c3IvbG9jYWwvaW1hZ2VtYWdpY2svc2hhcmUvZG9jL0ltYWdlTWFnaWNrLTcvL2luZGV4Lmh0bWy9tXkKAAAAGHRFWHRUaHVtYjo6RG9jdW1lbnQ6OlBhZ2VzADGn/7svAAAAF3RFWHRUaHVtYjo6SW1hZ2U6OkhlaWdodAA2MLuNbZ0AAAAWdEVYdFRodW1iOjpJbWFnZTo6V2lkdGgANTkR00Z3AAAAGXRFWHRUaHVtYjo6TWltZXR5cGUAaW1hZ2UvcG5nP7JWTgAAABd0RVh0VGh1bWI6Ok1UaW1lADEzMDU3MzQwNDCrCw2wAAAAEXRFWHRUaHVtYjo6U2l6ZQA1OTQzQjpo3pUAAABgdEVYdFRodW1iOjpVUkkAZmlsZTovLy9ob21lL3d3d3Jvb3QvbmV3c2l0ZS93d3cuZWFzeWljb24ubmV0L2Nkbi1pbWcuZWFzeWljb24uY24vc3JjLzUwOTIvNTA5Mjg0LnBuZ91LspIAAAAASUVORK5CYII=", "this is a image by chatbot");
                    }
                    else if (msg.Text == "file")
                    {
                        //Send file
                        responseMsg = MsgBuilder.BuildFileMessage("a.txt", "text/plain", "ZHNmZHMKZmRzZmRzZnNkZgpm5Y+N5YCS5piv56a75byA5oi/6Ze055qE5LiK6K++5LqGCg==", "this is a file by chatbot");
                    }
                    else if (msg.Text=="more")
                    {
                        MsgBuilder builder = new MsgBuilder();
                        //add text with bold
                        builder.AppendText("Hi,this is bold\n", isBold: true);
                        //add text with italic
                        builder.AppendText("Hi,this is italic\n", isItalic: true);
                        //add text with deleted
                        builder.AppendText("Hi,this is deleted\n", isDeleted: true);

                        //add text with code style
                        builder.AppendText("int a=100;\nint b=a*100-90;\n", isCode: true);
                        //add link
                        builder.AppendText("https://google.com\n", isLink: true);
                        //add domain
                        builder.AppendText("baidu.com\n", isLink: true);
                        //add mention message
                        builder.AppendText("@tinode\n", isMention: true);
                        //add hashtags
                        builder.AppendText("#Tinode\n", isHashTag: true);
                        builder.AppendText("\n\nnext is image\n");
                        //add image message
                        builder.AppendImage("a.png", "image/png", 0, 0, "iVBORw0KGgoAAAANSUhEUgAAAC8AAAAwCAYAAACBpyPiAAAABGdBTUEAALGPC/xhBQAAACBjSFJNAAB6JgAAgIQAAPoAAACA6AAAdTAAAOpgAAA6mAAAF3CculE8AAAABmJLR0QA/wD/AP+gvaeTAAAAB3RJTUUH2wUSFzYAFqhyqAAAENNJREFUaN7FmlusncdVx39rZr69zz7HPuc4tpM4iR3bca4UkaZqi1p6L7QIVaBKwAM3lSJFBcQDT/CMhIAnHhCoEi9cXnigqqCINq2aXigladOE1tSO4zgX2/H9+Fz3d5mZtXiY2fuchPIK+2j8zf72d/nPmv9a819rLABPP/UxFpdWWFrZj9PRcQn+E843v+bFPy7ONd43OB9Kcx7npBy9w7kGJx5xDhFfmnOoes6/+iCnjr+EcxlTxSyXpopaRjWiWVHNqFo55oTmRM4RU43Z8gua499Zyv+sbnh1Z2OL6c4GH/qZLyHUz5O/Db/75G990HL+g2z5Q6p94wRx3uGDL805nHO4IKXvS/POIU5w4hBxiAgpC1//5n184H2XCN4wM8wUNcXUyKoFeNbST4Zq6eeUySmX3w1zbhy9+KfF+z/5i8/+9dc++5cFs/+dJx/ji1/4ZU6dePijKbZ/3nXb7xn6Ha+qAgYYgoEphoLVhoIlqKDMEqYR04TlREyZl1+ecOzoGmJDtfJAzqmCTmga0JzJKZVjTuisnxI5Z2KMMvRTn9JwyjS/452PP/HSH//RBy7cvrWOPPutX6Rt80PjMX819O2H1RLeeVzwhODxwc2t7L3DecF7KTPgBKlH50BEEBFASNnx1a/dw4c/+AbBKzCzvqEKqlboo9Xi2dBs5D2zkZOSUkZTJmvGSWA0mnyl6/JnFifhvP+vZ34YPvrxU7+pqf9033fiyruLxesRMxB7i9XrbMzOaUZVMc2YJnLKnH95iRPH1hCLlc8Zy3ne39sKTXKlUCbPzuf63JxJQwLsfhF36dO/8vln/J/96ftOjBv9/Rz7E6YZV0ELVh3Car+Crt/NFDGF6oCmu5Qyy+SkvHR+kZPH1xFJ82tmoEwzlst3q9ZWrYPL+qaBWc7YzA9Scjlp866fvO+boYv5KJJ/LHYD4gURwzDMXAXnEHVgAubAlaNzgJMyUAdOBHOU2RIhR0dKkRxjGbjZbLJQszJOo9KGCr7SpjqvZp3TaNZPOdEsuLe1MR8N67fbw8fu9qs5Jpx51BTMgxla3ybeAWUwVsEjgjnDeUG0AMYVxokImgt4zQlFC98pk6d1IJoNU0GtgDYtvNc54D39lNEMmjLjcV5dv90eDlub3ZLY2FtOhdcikA3DYXhMBEMxE5IWSqkTnCuOa+ZwImVGtE6MCJocOSY0RbRaXkvwKtY2Q1N1VC3NDFDB1ErLhuUMWUG14MoZsei3NrulEIcUrPKu3Osqxyt9RMgqZPOMRgcYj5bxwQOQdYc0rOF9BOcQV7wCEXJ2pBzJOeJmtKHQJquSc8BxkJFbAgc5Zfphk2G4DZLLTGWDrNUnDEulbzkThxSCmYlpPYlhYmiliJrDVBBZYN++46wun2RhvIJzAQNimrLTvkI3nIfQ4UQq5ymWTwmNsVKxctyMlBZYaE6xNDlBExYRQDXR9Rusb15ge/tVzLpqfUWVPcCtrtYmwWqYI+caIwvP1UtdmAL79x1haXyUN6623NrYZkiO8Ug4uNJw16GjqEv0/VlCiDgnIGC5gLecUNE5XVIKjJvjNO4oFy9vcWtjjX4wRkE5uOI5fOAoqe/Z2r6AkNBsMG+FOmjGTAnUGGopF4tjSHE7NAtutIL3h3nlcs/gjnLX8VNMFpcZ+ik3r73GzqVL3H3HISyvkO1qcW63x/IpYaKgVKc8SOYQF671ZH+MIyfuZzRepJ1ucv3KebYvX+Tw6mGEa+iwVtiQZ448o04GVQKUaSAbJloiuggOQfF4W+bWesAtPsJPvO2nOHjoCD40aE6s3/cg588+y8310+ybrGByHUdGVOoLq2GkrKw5etRW2NgKjJcf49Qj72L1wCGcD+QUuXXvA7x4+t+4tX6ahmVyXkMoIbQ4b5kB07LeOFQRzZU6ClmR+tKcjGEYM3Anx0+9k4OH7il8VxAJrBy4k2Mn3072R5hOF8gDWKqLStUqtneRGSjX+SMcO/l2Vg7ciUh5nnOBg4fu4fipdzJwJ8MwJqcaXSquEnVyxVtpQyqWL6to9ThPARAWWFq9n+WVQ8SYafuOlDIheBbGDUv7V1laPcb06hmyKeYz4sCSKytmyoCWKDMoKS+ydOgYS/tXGYZI18f58ybjhuWVQyyt3k979QwWM3iFTME3s34qoTOUGF41ibgS1x2ISlV4wurkDtTgey9e4W++/DLnLm1w4sh+fv2jD/D2Bw4yHq+wmR2ZiGlGTdBkdTnP6Iw2OZKyYzxeQZPyzIuX+duvvMwrV7Z46L4VfuOnH+DRYyuMJ3ewmQWXEkhd4JQa/xUzj6EEMUNUKSvIzPJgQbCsxK7D+RH9EPmHr5zlH88dZlh4hP+8cBP31Dke+tQ7EB+IcYrZAKmsF5Zc0SUpFykNWDJimiI+sNMN/P1T5/j8q0dI48f4wbmrLMhZ/vBXn8D5EbHrGOWEeSDZ3PpowStmuJmQmgmnmWCylHFkUlwnxW3iMHDpym2GtITGEUn3cenKOkPfk+IUS+uIFS0+U5ozsWZWhJZYwtI6KU4Z+p5LV9ZJug+NI4a0xKUrt4nDQIrbpLiOow4+z9SqzjMyTHFlSiqX1KraLSuKF2XETfqt14lDz6mjqyy1l3AbbzDZvsiD962QU6TfvsTYXSW4DHVhsaoMZ33UCC4zdlfpty+RU+TB+1aYbF/EbbzBUnuJU0dXiUNPv/U6I27iZSboCi5mUUfLuYDtfinawxAtC41zMGaduP5duu2H+YX3P4D3r/DaxR9y7z2rfOK9J0n9LdLmd5i46zgpz0KoCYdWjV8WKScwcddJm98hHTjJL334JJPxBS6/cYX7j97BJ977AN32FeL6dxmzXldemytRNZgZG7MS58W08MgJYlUluqLTGhmw7efYuX4Hh4/+HJ/5+YfxfoTmyNDdYOvKl/Dtt2lci6jDpMriKrjIBs7m+UzjWmi/TXdzlXuPfIzf++RjON+Q80C/8wa3L/4rsv1ceW8VcqIgZqVpwQtGMGajsrm/ijNEQQW8CE43iDeeYpsbcPBRRot3ELsNuo1z5NvP4fV2tbbO9fw8MujuzM7AeLtJXvsCrV3BVh6iWVhhmK6xfesM8cbzON3EsLfo/j1qtIq8IBhObJ45zeWBzJUOYhkbbtNd/yZ5/Tv4EGp21OMslhmrqhgBEcOFIt5dUJzTOXC1YkXLN4m3vkq6/S3MAjkl4tBhMRZ5VbHMcOzFN8MbSgyrkg9mt8xTVqmyGIOcBtCBG9twa1Pwvibd8ySk3I+DlOHW+sCLr0LwUrVyzaaqNDYbyHng4LKxuq8soGZSdX2x+mwgsCefrliLw87+9v5mVjI/CnCp572HZ8+O+Nw3lhg1brfw8yN6RuTM+ZXZa/d8bP7vEJVPvn+Hn333QMrspotvwiTzysPsDzOCYmS1MmqpgKVwviY2iBolZRWyGk0z4uDBQyyMm13A8uYB/O8f251koOsjTRPJ2pWVuea0JQOd9XfLJaqQ1VCMEHWRnbiPjT7jQ6m/OF+p4AuPxe2GztFgmFtmZWWZ8SjMyPk/YcueM3soWWzK/L7xkDHXsr7jGLJQBeM8LazrUanpKGgyfPRE3SYcPvkkj37k4/T9gADNaIT3rpTTfEn3mlGDc+WcE+EJdXzKQonDFZhI4b/V72aGq87wpnP1OTrLh9UYN+Ck1mxqwyBrKXfknOdUjikxHo+4MHyRcP3GJmfOvc7mxgaaEpOlJUIILC0u4kPx58lkgdA0jEbjeXVApFTNxAk5ZURmPuERgRgT3jtUjRA8ZpBSwnmHmeG9r3UcnQ9azej7jhQjwxBLJS0ltnd2yDnTty04x/LKCtdvbOIA+q6j6ztG4xFt22JmdH1HzhnvPX0/oGr0fV/LeUZKCVUlDgPOuVrZKho+xoj3nhgTZkaMkZwTzjniEIsFh2HOppRKntv3fX2WklIipUTXdTRNoO86sipd39F3XYk2ZobzHifFSqPRCOc93vlaHYPQBJxItZrinJtbuFhN59XhXZpbrWe6SpvSyn2uzlRZ1LwvM+O8Q3N5fhMCMUWcd6SshKZQV81qCV1x9Zm0bVu4BvR9h2H0XYdJyT1jSqgZwzCUEnZK84JQignnhBgjYLUSnCt9hhI0Y5yHu5Rm1o8IQt5Tk4wx4pyjHwZMjWGIxDjgnaOdTjEzhn4os4SUaWuahhRL9r8wHpNiJDQj4hAJIRC8J+dMCIGUEk0TijNiiAg5K03TVIlQHdegCc2cfjnlWqwKZM344Ek5EULAOSnWdMUw4/EYNaMGOrIqi0uLpJTwzrGwsICzGrebpsEql7uuL/RxMrdayolRsxvXC58hhDCvKsc4zClVtIkyxAHvfYkyQrVyqvfJ3JFVbff51XFFYDQaVSWpTHd2aJqGZjTC1Irl27ala1tCaJi2Ld47ptOWnDOIkFJEgLbtMCs0moW0rjpxoYwQY2IYIiKuUswxDJGU0vx3ELq+nwu2lEuFrG278qwUqyNnptOWEALTtsU5P8cqIoRhiHRdz42bt9jZmeKbQNt2TBYmbGxuMVlcxDlhtLVNCA3rss5o1JBSro5WFpNZxAnBV767GscVN6OMd/OYX6hWrp/F8ZQzwzBgpnRtV4zTdcRUBt9Pu7KyhhE705Zw8fzzlqeX7cLFyywuTopHq7J//366tmP/8v7C2eAZj8fEOLC4uEjfdWX6aoo3Go3pupaFhQlDdbohjnGyw2SyQNu2jEajus+kNM2Irm1ZmEzmIdIM2q5j3IzY2tqiaRr6vidWnm9ubjJZXOLA0sQuX7pmod94Jts+P6x4JtoViwSBfq04y7Qv9XcR6CgSoVuzuoE2q81DTznubM+sKrxwdpnHH91EtxVMmFYnNqCt+qVdq+K3FoIB+tkxl2tD1Tz7RJDOGAUGFzWH8aJrH36o2XTCJA67YMRRdU5VuVXbzM7LbB9qz/VSr0EgJuHGxhLve3dLE6zqlfluUEnpKuC5AKvf56XwzO75en0zAjU2r6wNXTh9Lt96/BF7/eQxd9e0K9aeg/GCOKPKkfnGmZMyqPlAKmgnNYlxEAchBNg3EZoRc22utqsa58CylNy/KseifcBq2XAGXg2WFoSXL+rrp1/KN90rl/Pl759Jz/eddU1Td2/qJiZW96UqoLL5UHJSqbmquD1NDOcNcQZ+nvMVA3hDxN5yfRWfzubPFrdHpc5q+hVT00DXWf+DM+mFVy7nyw64/vXnhmefP53OkTDva5WhDmA2XbMkZddqRVPPkwSdJQ+1v4ci8xyWvflsuX8mfXXPO+Y0qTiUkgSRsOdPpxef/u7wDHDde8fQ9ujFa7p/MuKue1fcfj/C4d6SFtU9NSnbU1DTv7kcdrvXISWl+/7ZBX780Rbv96R+Mttk2DWO1gQl700RZ2lAKZtiO6T/+EF67Z++Hr+4tmFf8o7XvRkqMN1prT/7qvprazq6e9ktLgaaRkRGDeKBIBDqsQG8wGjveQpTPKVJFk6/uMgTD7U0znAKTss1TgtTvBa2hNn3PS0I2ICl1vTaVdv63NPDi19+Jn1tY9v+ReD7arSyx74HgMeB9x++w73j8Qf9iSMHZHl5QSYy32+w+dZN3fzbTZpkzywIpCz8+5l9vOfR7fp/D/YkVHv6yh5Kms0dzgTb6mmv3NbN58/lV26s6feAbwDPA7erK+wmbsAicH8dxKPA3cD+asz/608GtoCrwBngBeA1YEqNJz8qY/bAKnAXcLAO6P8L/BS4BVwD1uu5+ee/Aa0AqnTFGSHgAAAAJXRFWHRkYXRlOmNyZWF0ZQAyMDE4LTA2LTI4VDIyOjM5OjU1KzA4OjAwwmBIVQAAACV0RVh0ZGF0ZTptb2RpZnkAMjAxMS0wNS0xOFQyMzo1NDowMCswODowMDef1rEAAABDdEVYdHNvZnR3YXJlAC91c3IvbG9jYWwvaW1hZ2VtYWdpY2svc2hhcmUvZG9jL0ltYWdlTWFnaWNrLTcvL2luZGV4Lmh0bWy9tXkKAAAAGHRFWHRUaHVtYjo6RG9jdW1lbnQ6OlBhZ2VzADGn/7svAAAAF3RFWHRUaHVtYjo6SW1hZ2U6OkhlaWdodAA2MLuNbZ0AAAAWdEVYdFRodW1iOjpJbWFnZTo6V2lkdGgANTkR00Z3AAAAGXRFWHRUaHVtYjo6TWltZXR5cGUAaW1hZ2UvcG5nP7JWTgAAABd0RVh0VGh1bWI6Ok1UaW1lADEzMDU3MzQwNDCrCw2wAAAAEXRFWHRUaHVtYjo6U2l6ZQA1OTQzQjpo3pUAAABgdEVYdFRodW1iOjpVUkkAZmlsZTovLy9ob21lL3d3d3Jvb3QvbmV3c2l0ZS93d3cuZWFzeWljb24ubmV0L2Nkbi1pbWcuZWFzeWljb24uY24vc3JjLzUwOTIvNTA5Mjg0LnBuZ91LspIAAAAASUVORK5CYII=");
                        builder.AppendText("\n\nnext is file\n");
                        //add file message
                        builder.AppendFile("a.txt", "text/plain", "ZHNmZHMKZmRzZmRzZnNkZgpm5Y+N5YCS5piv56a75byA5oi/6Ze055qE5LiK6K++5LqGCg==");
                        responseMsg = builder.Message;
                    }
                    else if (msg.Text=="attach")
                    {
                        //upload a test file as attachment
                        var uploadInfo=await bot.Upload("./libgrpc_csharp_ext.x64.so");
                        if (uploadInfo!=null)
                        {
                            responseMsg = MsgBuilder.BuildAttachmentMessage(uploadInfo, "This is a larget attachment file");
                        }
                        else
                        {
                            responseMsg = MsgBuilder.BuildTextMessage("I try to send you a larget attach file, but I am sorry I failed...");
                        }
                        
                    }
                    else if (msg.Text=="form")
                    {
                        //send a form with button
                        MsgBuilder builder = new MsgBuilder();
                        builder.AppendText("What's your gender?", isBold: true, isForm : true);
                        builder.AppendText("Male", isButton: true, buttonDataName: "male", buttonDataVal: "user click male");
                        builder.AppendText("Female", isButton: true, buttonDataName: "female", buttonDataVal: "user click female");
                        builder.AppendText("Not Sure", isButton: true, buttonDataName: "NA", buttonDataVal: "user click NA");
                        responseMsg = builder.Message;
                    }
                    else
                    {
                        responseMsg = msg;
                    }

                }
                else
                {
                    responseMsg = msg;
                    //Extract more information
                    var mentions = msg.GetMentions();
                    foreach (var m in mentions)
                    {
                        Console.WriteLine($"Mentions:{m.Val}");
                    }

                    var images = msg.GetImages();
                    foreach (var image in images)
                    {
                        Console.WriteLine($"Image:Name={image.Name} Mime={image.Mime}");
                    }

                    var hashTags = msg.GetHashTags();
                    foreach (var hash in hashTags)
                    {
                        Console.WriteLine($"HashTags:{hash.Val}");
                    }

                    var links = msg.GetLinks();
                    foreach (var link in links)
                    {
                        Console.WriteLine($"Links:{link.Url}");
                    }

                    var files = msg.GetGenericAttachment();
                    foreach (var f in files)
                    {
                        Console.WriteLine($"Image:Name={f.Name} Mime={f.Mime}");
                    }

                }
                return responseMsg;
            }

        }

        static ChatBot bot;
        static void Main(string[] args)
        {
            Console.CancelKeyPress += Console_CancelKeyPress;
            string schemaArg = string.Empty;
            string secretArg = string.Empty;
            string cookieFile = string.Empty;
            string host = string.Empty;
            string listen = string.Empty;
            Parser.Default.ParseArguments<CmdOptions>(args)
                   .WithParsed<CmdOptions>(o =>
                   {
                       if (!string.IsNullOrEmpty(o.Host))
                       {
                           host = o.Host;
                           Console.WriteLine($"gRPC server:{host}");
                       }
                       if (!string.IsNullOrEmpty(o.Listen))
                       {
                           listen = o.Listen;
                           Console.WriteLine($"Plugin API calls Listen server:{listen}");
                       }
                       if (!string.IsNullOrEmpty(o.Token))
                       {
                           schemaArg = "token";
                           secretArg = Encoding.ASCII.GetString(Encoding.Default.GetBytes(o.Token));
                           Console.WriteLine($"Login in with token {o.Token}");
                           bot = new ChatBot(serverHost: host, listen: listen, schema: schemaArg, secret: secretArg);
                       }
                       else if (!string.IsNullOrEmpty(o.Basic))
                       {
                           schemaArg = "basic";
                           secretArg = Encoding.UTF8.GetString(Encoding.Default.GetBytes(o.Basic));
                           Console.WriteLine($"Login in with login:password {o.Basic}");
                           bot = new ChatBot(serverHost: host, listen: listen, schema: schemaArg, secret: secretArg);
                       }
                       else
                       {
                           cookieFile = o.CookieFile;
                           Console.WriteLine($"Login in with cookie file {o.CookieFile}");
                           bot = new ChatBot(serverHost: host, listen: listen, cookie: cookieFile, schema: string.Empty, secret: string.Empty);
                           if (bot.ReadAuthCookie(out var schem, out var secret))
                           {
                               bot.Schema = schem;
                               bot.Secret = secret;
                           }
                           else
                           {
                               Console.WriteLine("Login in with cookie file failed, please check your credentials and try again... Press any key to exit.");
                               Console.ReadKey();
                               return;
                           }
                       }
                       bot.ServerDataEvent += Bot_ServerDataEvent;
                       bot.ServerMetaEvent += Bot_ServerMetaEvent;
                       bot.ServerPresEvent += Bot_ServerPresEvent;
                       bot.CtrlMessageEvent += Bot_CtrlMessageEvent;
                       bot.LoginSuccessEvent += Bot_LoginSuccessEvent;
                       bot.LoginFailedEvent += Bot_LoginFailedEvent;
                       bot.DisconnectedEvent += Bot_DisconnectedEvent;
                       //your should set your chatserver's http base url and api access key to handle larget file upload
                       bot.SetHttpApi("http://localhost:6660", "AQAAAAABAABtfBKva9nJN3ykjBi0feyL");
                       bot.BotResponse = new BotReponse(bot);
                       bot.Start().Wait();

                       Console.WriteLine("[Bye Bye] ChatBot Stopped");
                   });

        }

        private static void Bot_DisconnectedEvent(object sender, EventArgs e)
        {
            Console.WriteLine("[!!!Disconnected!!!]");
        }

        private static void Bot_LoginFailedEvent(object sender, EventArgs e)
        {
            Console.WriteLine("[!!!Login Failed!!!]");
        }

        private static void Bot_LoginSuccessEvent(object sender, EventArgs e)
        {
            Console.WriteLine("[!!!Login Success!!!]");
        }

        private static void Bot_CtrlMessageEvent(object sender, ChatBot.CtrlMessageEventArgs e)
        {
            //Console.WriteLine($"[Ctrl Message] {e.Code} {e.Id}  {e.Topic}  {e.Text}  {e.Params}  {e.Type}  {e.HasError}");
        }

        private static void Bot_ServerPresEvent(object sender, ChatBot.ServerPresEventArgs e)
        {
            //Console.WriteLine($"[Pres Message] {e.Pres.ToString()}");
        }

        private static void Bot_ServerMetaEvent(object sender, ChatBot.ServerMetaEventArgs e)
        {
            //Console.WriteLine($"[Meta Message] {e.Meta.ToString()}");
        }

        private static void Bot_ServerDataEvent(object sender, ChatBot.ServerDataEventArgs e)
        {
            //Console.WriteLine($"[Data Message] {e.Data.ToString()}");
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            bot.Stop();
        }
    }

}
