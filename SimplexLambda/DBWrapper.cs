using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using SimplexLambda.User;
using Simplex.UserData;
using Simplex;
using Amazon.DynamoDBv2.DocumentModel;
using Simplex.Protocol;

namespace SimplexLambda
{
    public class DBWrapper //: IUserDataLoader
    {
        private AmazonDynamoDBClient dbClient;
        private DynamoDBContext dbContext;
        private DynamoDBOperationConfig dbCfg;
        private Table dbTable;
        private GetItemOperationConfig getOpCfg = new GetItemOperationConfig();
        private PutItemOperationConfig putOpCfg = new PutItemOperationConfig();

        public DBWrapper(SimplexLambdaConfig lambdaConfig)
        {
            dbClient = new AmazonDynamoDBClient(RegionEndpoint.USWest1);
            dbContext = new DynamoDBContext(dbClient);
            dbCfg = new DynamoDBOperationConfig()
            {
                OverrideTableName = lambdaConfig.SimplexTable
            };
            dbTable = Table.LoadTable(dbClient, lambdaConfig.SimplexTable);
            getOpCfg.ConsistentRead = true;
        }

        public SimplexError LoadUserData(Guid userGUID, UserDataOperation dataOp, SimplexRequestContext context, out UserDataResult result, out SimplexError err)
        {
            var diag = context.DiagInfo.BeginDiag($"DB_LOAD_[{dataOp.Json.DataType}]");
            string range = dataOp.GetDBRange();
            var t = dbTable.GetItemAsync(userGUID, range, getOpCfg);
            t.Wait();
            result = new UserDataResult(dataOp);
            result.Error = t.Result == null ? SimplexErrorCode.DBItemNonexistent : SimplexErrorCode.OK;
            if (t.Result != null)
            {
                result.Json.DataJSON = t.Result.ToJson();
            }
            context.DiagInfo.EndDiag(diag);
            err = result.Error;
            return err;
        }

        public SimplexError SaveUserData(Guid userGUID, UserDataOperation dataOp, SimplexRequestContext context, out UserDataResult result, out SimplexError err)
        {
            var diag = context.DiagInfo.BeginDiag($"DB_SAVE_[{dataOp.Json.DataType}]");
            Document doc = Document.FromJson(dataOp.Json.DataJSON);
            doc["Hash"] = userGUID.ToString();
            doc["Range"] = dataOp.GetDBRange();
            var task = dbTable.PutItemAsync(doc);
            task.Wait();
            result = new UserDataResult(dataOp);
            context.DiagInfo.EndDiag(diag);
            err = SimplexErrorCode.OK;
            return err;
        }

        public SimplexError LoadItem<T>(T obj, out T Item, SimplexRequestContext context, out SimplexError err)
        {
            var diagHandle = context.DiagInfo.BeginDiag($"DB_LOAD_[{obj.GetType().Name}]");
            var task = dbContext.LoadAsync(obj, dbCfg);
            task.Wait();
            Item = task.Result;
            err = task.Result == null ? SimplexErrorCode.DBItemNonexistent : SimplexErrorCode.OK;
            context.DiagInfo.EndDiag(diagHandle);
            return err;
        }

        public SimplexError SaveItem<T>(T obj, out SimplexError err)
        {
            dbContext.SaveAsync(obj, dbCfg).Wait();
            err = SimplexErrorCode.OK;
            return err;
        }
    }
}
