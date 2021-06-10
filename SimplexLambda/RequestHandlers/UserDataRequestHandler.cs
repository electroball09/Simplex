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
        public override bool RequiresAccessToken => true;

        public override SimplexResult HandleRequest(SimplexRequestContext context)
        {
            var handle = context.DiagInfo.BeginDiag("USER_DATA_REQUEST_HANDLER");

            if (!context.Request.PayloadAs<UserDataRequest>(out var dataRq, out var err))
                return context.EndRequest(SimplexResult.Err(err), handle);

            if (!ValidateAccess(context.Token, dataRq, out err))
                return context.EndRequest(SimplexResult.Err(err), handle);

            List<UserDataResult> results = new List<UserDataResult>();

            foreach (var dataOp in dataRq.UserDataList)
            {
                UserDataResult result = null;
                if (dataRq.RequestType == UserDataRequestType.GetUserData)
                    context.DB.LoadUserData(dataRq.UserGUID, dataOp, context, out result, out _);
                if (dataRq.RequestType == UserDataRequestType.SetUserData)
                    context.DB.SaveUserData(dataRq.UserGUID, dataOp, context, out result, out _);
                if (dataRq.RequestType == UserDataRequestType.UpdateUserData)
                {
                    //TODO finish this lmao
                }
                results.Add(result);
            }

            UserDataResponse rsp = new UserDataResponse
            {
                Results = results
            };

            return context.EndRequest(SimplexResult.OK(rsp), handle);
        }

        public SimplexError ValidateAccess(SimplexAccessToken token, UserDataRequest dataRq, out SimplexError err)
        {
            var flags = dataRq.RequestType.ToAccessFlags();
            if (token.UserGUID == dataRq.UserGUID)
            {
                if ((token.Permissions.UserData_PrivateSelf & flags) > 0)
                    err = SimplexErrorCode.OK;
                else
                    err = SimplexErrorCode.PermissionDenied;
            }
            else
            {
                if ((token.Permissions.UserData_PrivateNonSelf & flags) > 0)
                    err = SimplexErrorCode.OK;
                else
                    err = SimplexErrorCode.PermissionDenied;
            }
            return err;
        }
    }
}
