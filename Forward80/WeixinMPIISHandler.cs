using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Forward80
{
    /// <summary>
    /// 微信80端口数据转发
    /// </summary>
    public class WeixinMPIISHandler : IHttpHandler
    {
        /// <summary>
        /// 您将需要在您网站的 web.config 文件中配置此处理程序，
        /// 并向 IIS 注册此处理程序，然后才能进行使用。有关详细信息，
        /// 请参见下面的链接: http://go.microsoft.com/?linkid=8101007
        /// </summary>
        #region IHttpHandler Members

        public bool IsReusable
        {
            // 如果无法为其他请求重用托管处理程序，则返回 false。
            // 如果按请求保留某些状态信息，则通常这将为 false。
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            //在此写入您的处理程序实现。


            //获取请求的url
            var requestUrl = context.Request.Url.AbsoluteUri;
            //通过正则提取要转发的url
            Regex reg = new Regex(@"WeixinMP/(.*)/weixin.aspx?(.*)", RegexOptions.Compiled);
            Match match = reg.Match(requestUrl);
            if (match.Groups.Count > 0)
            {
                //获取url
                var url = match.Groups[1].Value;
                //获取参数
                var parmas = match.Groups[2].Value;
                //url解码
                url = HttpUtility.UrlDecode(url, Encoding.UTF8);
                //解密base64字符串
                url = Encoding.UTF8.GetString(Convert.FromBase64String(url));
                if (context.Request.HttpMethod == "GET")
                {
                    //执行GET
                    Log(context, string.Format("GET-\r\nrequestUrl:{2}\r\nurl:{0}\r\nparmas:{1}", url, parmas,requestUrl),url.Replace(":"," ").Replace("/"," "));
                    this.GET(context, string.Format("{0}{1}",url,parmas));
                }
                else
                {
                    //设置输出头
                    context.Response.ContentType = "text/xml";
                    Log(context, string.Format("POST\r\nrequestUrl:{2}\r\nurl:{0}\r\nparmas:{1}", url, parmas, requestUrl),url.Replace(":"," ").Replace("/"," "));
                    //执行POST
                    this.Post(context, string.Format("{0}{1}",url,parmas));
                }
            }
            

        }

        #endregion

        #region Method

        private void Post(HttpContext context,string url)
        {
            byte[] byts = new byte[context.Request.InputStream.Length];
            context.Request.InputStream.Read(byts, 0, byts.Length);
            byte[] postData = byts;
            WebClient webClient = new WebClient();
            webClient.Headers.Add("Content-Type",context.Request.ContentType);
            Log(context, string.Format("POST\r\npost-data:{0}", System.Text.Encoding.Default.GetString(postData)), url.Replace(":", " ").Replace("/", " ").Replace("?"," "));
            byte[] responseData = webClient.UploadData(url, "POST", postData); 
            string strResult = Encoding.UTF8.GetString(responseData);
            context.Response.Clear();
            context.Response.Write(strResult);
            context.Response.End();
        }

        private void GET(HttpContext context, string url)
        {
            string strResult = string.Empty;
            Stream dataStream = null;
            try
            {
                WebRequest request = WebRequest.Create(url);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                dataStream = response.GetResponseStream();
            }
            catch 
            {

            }
            if (dataStream != null)
            {
                try
                {
                    using (StreamReader reader = new StreamReader(dataStream, Encoding.UTF8))
                    {
                        strResult = reader.ReadToEnd();
                    }
                }
                catch
                {
                }
            }
            context.Response.Clear();
            context.Response.Write(strResult);
            context.Response.End();
        }

        private void Log(HttpContext context, string msg,string key="")
        {
            using (TextWriter tw = new StreamWriter(context.Server.MapPath("~/App_Data/WebxinMP_"+(string.IsNullOrEmpty(key)?"":key+"_")+"Log_" + DateTime.Now.Ticks + ".txt")))
            {
                tw.WriteLine(msg);
                tw.Flush();
                tw.Close();
            }
        }

        #endregion 
    }
}
