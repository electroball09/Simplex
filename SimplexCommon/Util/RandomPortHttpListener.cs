using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Net;

namespace Simplex.Util
{
    public class LocalHttpListenerGenerator
    {
        public static (string url, HttpListener listener) GenerateListener(int minPort = 65500, int maxPort = 65600)
        {
            Exception ex = null;
            string url = "";

            for (int i = minPort; i <= maxPort; i++)
            {
                HttpListener l = new HttpListener();
                url = $"http://localhost:{i}/";
                try
                {
                    l.Prefixes.Add(url);
                    l.Start();
                    l.Stop();
                    HttpListener l2 = new HttpListener();
                    l2.Prefixes.Add(url);
                    return (url, l);
                }
                catch (Exception ex2)
                {
                    ex = ex2;
                    continue; 
                }
            }

            throw ex;
        }
    }
}
