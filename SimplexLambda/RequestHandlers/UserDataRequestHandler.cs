using Simplex.Protocol;
using System;
using System.Collections.Generic;
using System.Text;
using Simplex;
using SimplexLambda.User;

namespace SimplexLambda.RequestHandlers
{
    public class UserDataRequestHandler : RequestHandler
    {
        public override SimplexResponse HandleRequest(SimplexRequestContext context)
        {
            var handle = context.DiagInfo.BeginDiag("USER_DATA_REQUEST_HANDLER");

            SimplexAccessToken accessToken = new SimplexAccessToken();
            SimplexAccessToken.FromString(context.Request.AccessToken, context, out accessToken, out var tokenErr);
            if (!tokenErr)
                return new SimplexResponse(context.Request, tokenErr);

            var dataRq = context.Request.PayloadAs<UserDataRequest>();
            if (dataRq == null)
                return context.EndRequest(SimplexError.GetError(SimplexErrorCode.InvalidPayloadType), null, handle);

            SimplexAccessFlags requiredFlags = GetRequiredFlags(accessToken, dataRq);
            if (!LambdaUtil.ValidateAccessToken(accessToken, requiredFlags, context, out var validateErr))
                return context.EndRequest(validateErr, null, handle);

            List<UserDataResult> results = new List<UserDataResult>();
            foreach (var rq in dataRq.UserDataList)
                results.Add(new UserDataResult(rq) { Error = SimplexError.OK });
            UserDataResponse rsp = new UserDataResponse();
            rsp.Results = results;
            return context.EndRequest(SimplexError.OK, rsp, handle);
        }

        private SimplexAccessFlags GetRequiredFlags(SimplexAccessToken token, UserDataRequest dataRq)
        {
            SimplexAccessFlags reqFlags = SimplexAccessFlags.None;
            reqFlags = (SimplexAccessFlags)((ulong)SimplexAccessFlags.GetUserData << (int)dataRq.RequestType - 1);
            if (token.UserGUID != dataRq.UserGUID)
                reqFlags |= SimplexAccessFlags.Admin;
            return reqFlags;
        }
    }
}
