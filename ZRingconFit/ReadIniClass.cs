using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace BaseServer
{
    public class ReadIniClass
    {
        #region 用户类
        public struct user
        {
            private string username;
            private string password;
            private string rank;

            public user(string username, string password, string rank)
            {
                this.username = username;
                this.password = password;
                this.rank = rank;
            }

            public string Username { get => username; }
            public string Password { get => password; }
            public string Rank { get => rank; }
        };
        #endregion

        #region 属性
        private static string info;//配置信息
        private static string userInfo;//用户信息
        private static Dictionary<string, user> userDic;//用户列表

        public static Dictionary<string, user> UserDic { get => userDic; }//获取用户列表
        #endregion

        #region 读取配置信息
        /// <summary>
        /// 读取配置信息
        /// </summary>
        public static void Read()
        {
            try
            {
                StreamReader sr = File.OpenText(AppDomain.CurrentDomain.BaseDirectory + "\\Configure.ini");
                info = sr.ReadToEnd();
                info.Replace(" ", "");
                sr.Close();
            }
            catch (Exception)
            {
                Console.WriteLine("读取失败");
            }
        }
        #endregion

        #region 写入配置信息
        /// <summary>
        /// 读取配置信息
        /// </summary>
        public static void Write()
        {
            try
            {
                FileStream fs = new FileStream(AppDomain.CurrentDomain.BaseDirectory + "\\Configure.ini", FileMode.Truncate);
                StreamWriter sr = new StreamWriter(fs, Encoding.UTF8);
                sr.Write(info);
                sr.Close();
                fs.Close();
            }
            catch (Exception)
            {
                Console.WriteLine("写入失败");
            }
        }
        #endregion

        #region 读取用户配置信息
        /// <summary>
        /// 读取用户配置信息
        /// </summary>
        public static void ReadUser()
        {
            try
            {
                userDic = new Dictionary<string, user>();
                StreamReader sr = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "\\User.ini", Encoding.UTF8);
                userInfo = sr.ReadToEnd();
                userInfo.Replace(" ", "");
                sr.Close();
                SubXML();
            }
            catch (Exception)
            {
                Console.WriteLine("读取失败");
            }
        }
        #endregion

        #region 分割XML信息
        /// <summary>
        /// 分割XML信息
        /// </summary>
        private static void SubXML()
        {
            string temp = userInfo;
            while (temp.Length > 0)
            {
                int start = userInfo.IndexOf("<user>");
                int end = userInfo.IndexOf("</user>") + "</user>".Length;
                userDic.Add(getUserWithName("username"), new user(getUserWithName("username"), getUserWithName("password"), getUserWithName("rank")));
                temp = temp.Substring(end);
            }
        }
        #endregion

        #region 通过用户名获取密码
        /// <summary>
        /// 通过用户名获取密码
        /// </summary>
        /// <param name="name">用户名</param>
        /// <returns></returns>
        private static string getUserWithName(string name)
        {
            try
            {
                if (userInfo == null || userInfo == "")
                    ReadUser();
                //string temp = info.Substring(info.IndexOf(name) + name.Length + 1, info.Substring(info.IndexOf(name) + name.Length + 1).IndexOf("\n") - (info.IndexOf(name) + name.Length + 1));
                int start = userInfo.IndexOf(name) + name.Length + 1;
                int length = userInfo.Substring(start).IndexOf("\r\n");
                if (length > 0)
                    return userInfo.Substring(start, length);
                else
                    return userInfo.Substring(start);
            }
            catch (Exception)
            {
                Console.WriteLine("解析失败");
            }
            return "";
        }
        #endregion

        #region 通过关键字获取信息
        /// <summary>
        /// 通过关键字获取信息
        /// </summary>
        /// <param name="name">关键字</param>
        /// <returns></returns>
        public static string getWithName(string name)
        {
            try
            {
                //if (info == null || info == "")
                Read();
                var configs = Regex.Split(info, "\r\n", RegexOptions.IgnoreCase);
                foreach (var config in configs)
                {
                    int t_start = config.IndexOf(name + "=");
                    if (t_start == -1)
                    {
                        continue;
                    }

                    if (t_start != 0)
                        continue;

                    return config.Substring(t_start + name.Length + 1);
                    //int t_length = info.Substring(t_start).IndexOf("\r\n");
                    //if (t_length >= 0)
                    //    return info.Substring(t_start, t_length);
                    //else
                    //    return info.Substring(t_start);
                }

                return "";

                //string temp = info.Substring(info.IndexOf(name) + name.Length + 1, info.Substring(info.IndexOf(name) + name.Length + 1).IndexOf("\n") - (info.IndexOf(name) + name.Length + 1));
                //int start = info.IndexOf(name + "=") + name.Length + 1;
                //if (start <= name.Length)
                //{
                //    return "";
                //}

                //int length = info.Substring(start).IndexOf("\r\n");
                //if (length >= 0)
                //    return info.Substring(start, length);
                //else
                //    return info.Substring(start);
            }
            catch (Exception)
            {
                Console.WriteLine("解析失败");
            }
            return "";
        }
        #endregion

        #region 通过关键字设置信息
        /// <summary>
        /// 通过关键字获取信息
        /// </summary>
        /// <param name="name">关键字</param>
        /// <returns></returns>
        public static void setWithName(string name, string value)
        {
            try
            {
                if (info == null || info == "")
                    Read();
                //string temp = info.Substring(info.IndexOf(name) + name.Length + 1, info.Substring(info.IndexOf(name) + name.Length + 1).IndexOf("\n") - (info.IndexOf(name) + name.Length + 1));
                if (info.IndexOf(name + "=") == -1)
                    info = info + "\r\n" + name + "=";
                int start = info.IndexOf(name + "=") + name.Length + 1;
                int length = info.Substring(start).IndexOf("\r\n");
                if (length >= 0)
                    info = info.Substring(0, start) + value + info.Substring(start + length);
                else
                    info = info.Substring(0, start) + value;
                Write();
            }
            catch (Exception)
            {
                Console.WriteLine("设置失败");
            }
        }
        #endregion
    }
}
