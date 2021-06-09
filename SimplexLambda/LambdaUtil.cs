using System;
using System.Collections.Generic;
using System.Text;
using SimplexLambda.User;
using Simplex;
using System.Security.Cryptography;
using Simplex.Protocol;
using SimplexLambda.DBSchema;
using Simplex.UserData;

namespace SimplexLambda
{
    public static class LambdaUtil
    {
        public static SimplexAccessPermissions.AccessFlags ToAccessFlags(this UserDataRequestType rqType)
        {
            ulong val = (ulong)1 << (int)rqType;
            return (SimplexAccessPermissions.AccessFlags)val;
        }

        public static string HashInput(HashAlgorithm hash, string input, string salt)
        {
            string tmp = $"{input}{salt}";
            byte[] inData = Encoding.UTF8.GetBytes(tmp);
            byte[] data = hash.ComputeHash(inData);
            return Convert.ToBase64String(data);
        }

        public static SimplexError CreateNewAccount(ref AuthAccount acc, SimplexRequestContext context, out SimplexError err)
        {
            context.Log.Info($"Creating new account [hash: {acc.Hash}]");

            context.DB.SaveItem(acc, out err);

            return err;
        }

        public static SimplexError CreateNewUser(AuthAccount acc, SimplexRequestContext context, out Guid newGuid, out SimplexError err)
        {
            var diag = context.DiagInfo.BeginDiag("CREATE_NEW_USER");
            SimplexError End(SimplexError err, out SimplexError outErr)
            {
                outErr = err;
                context.DiagInfo.EndDiag(diag);
                return err;
            }

            newGuid = GetNewUniqueID(context);
            if (newGuid == Guid.Empty)
                return End(SimplexError.Custom(SimplexErrorCode.Unknown, "Reached max interations for creating a unique ID!"), out err);

            UserBasicDataPrivate privData = new UserBasicDataPrivate()
            {

            };
            UserBasicDataPublic pubData = new UserBasicDataPublic()
            {
                CreationDate = DateTime.UtcNow,
            };

            UserDataOperation privDataOp = new UserDataOperation();
            privDataOp.SetDataJSON(privData);
            UserDataOperation pubDataOp = new UserDataOperation();
            pubDataOp.SetDataJSON(pubData);

            if (!context.DB.SaveUserData(newGuid, privDataOp, context, out _, out err))
                return End(err, out err);

            if (!context.DB.SaveUserData(newGuid, pubDataOp, context, out _, out err))
                return End(err, out err);

            return End(SimplexErrorCode.OK, out err);
        }

        public static SimplexError LinkAccountToUser(AuthAccount acc, Guid userGUID, SimplexRequestContext context, out SimplexError err)
        {
            var diag = context.DiagInfo.BeginDiag("LINK_ACCOUNT_TO_USER");
            SimplexError End(SimplexError err, out SimplexError outErr)
            {
                outErr = err;
                context.DiagInfo.EndDiag(diag);
                return err;
            }

            UserBasicDataPrivate privData = new UserBasicDataPrivate();
            UserDataOperation privDataOp = new UserDataOperation();
            privDataOp.SetDataJSON(privData);

            if (!context.DB.LoadUserData(userGUID, privDataOp, context, out _, out var loadErr))
                return End(loadErr, out err);

            privData.ConnectedAccounts.Add(acc.ToAccountDetails(context.LambdaConfig));
            privDataOp.SetDataJSON(privData);

            if (!context.DB.SaveUserData(userGUID, privDataOp, context, out _, out var saveErr))
                return End(saveErr, out err);

            acc.ConnectedUserGUID = userGUID;

            if (!context.DB.SaveItem(acc, out var accErr))
                return End(accErr, out err);

            return End(SimplexErrorCode.OK, out err);
        }

        private static Guid GetNewUniqueID(SimplexRequestContext context)
        {
            var diag = context.DiagInfo.BeginDiag("GET_NEW_UNIQUE_ID");

            UserBasicDataPrivate privData = new UserBasicDataPrivate();
            UserDataOperation op = new UserDataOperation();
            op.SetDataJSON(privData);

            Guid newGuid = Guid.NewGuid();

            for (int i = 0; i < context.LambdaConfig.MaxGetNewGUIDRetries; i++)
            {
                if (!context.DB.LoadUserData(newGuid, op, context, out var result, out var err))
                {
                    context.DiagInfo.EndDiag(diag);
                    return newGuid;
                }

                newGuid = Guid.NewGuid();
            }

            context.DiagInfo.EndDiag(diag);
            return Guid.Empty;
        }
    }
}
