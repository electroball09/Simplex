using System;
using System.Collections.Generic;
using System.Text;
using RestSharp;

namespace Simplex.Util
{
    public static class RestClientUtil
    {
        public static void AddQueryParameters(this RestRequest rq, Dictionary<string, string> parameters)
        {
            foreach (var kvp in parameters)
                rq.AddQueryParameter(kvp.Key, kvp.Value);
        }
    }

    public class RestClientWrapper
    {
        RestClient restClient = new RestClient();

        public SimplexError EZSendRequest(RestRequest rq, SimplexDiagnostics diag, out IRestResponse response, out SimplexError err)
        {
            var diagHandle = diag.BeginDiag($"EXTERNAL_API_REQUEST [{rq.Resource}]");

            Console.WriteLine($"QUERY PARAMETERS {string.Join(' ', rq.Parameters)}");

            try
            {
                response = restClient.Execute(rq);
                err = SimplexErrorCode.OK;
            }
            catch (Exception ex)
            {
                err = SimplexError.Custom(SimplexErrorCode.Unknown, ex.ToString());
                response = null;
            }

            diag.EndDiag(diagHandle);

            return err;
        }

        public SimplexError EZPost(string url, Dictionary<string, string> queryParameters, SimplexDiagnostics diag, out IRestResponse response, out SimplexError err)
        {
            RestRequest rq = new RestRequest(url);
            rq.Method = Method.POST;
            if (queryParameters != null)
                rq.AddQueryParameters(queryParameters);
            return EZSendRequest(rq, diag, out response, out err);
        }

        public SimplexError EZGet(string url, Dictionary<string, string> queryParameters, SimplexDiagnostics diag, out IRestResponse response, out SimplexError err)
        {
            RestRequest rq = new RestRequest(url);
            rq.Method = Method.GET;
            if (queryParameters != null)
                rq.AddQueryParameters(queryParameters);
            return EZSendRequest(rq, diag, out response, out err);
        }
    }
}
