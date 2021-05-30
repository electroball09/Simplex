using System;
using System.Collections.Generic;
using System.Text;
using RestSharp;

namespace Simplex.Util
{
    public class RestClientWrapper
    {
        RestClient restClient = new RestClient();

        public SimplexError EZSendRequest(RestRequest rq, SimplexDiagnostics diag, out IRestResponse response, out SimplexError err)
        {
            var diagHandle = diag.BeginDiag($"EXTERNAL_API_REQUEST [{rq.Resource}]");

            try
            {
                response = restClient.Execute(rq);
                err = SimplexError.OK;
            }
            catch (Exception ex)
            {
                err = SimplexError.GetError(SimplexErrorCode.Unknown, ex.ToString());
                response = null;
            }

            diag.EndDiag(diagHandle);

            return err;
        }
    }
}
