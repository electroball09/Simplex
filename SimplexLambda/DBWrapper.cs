using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using SimplexLambda.User;
using Simplex.User;
using Simplex;
using Amazon.DynamoDBv2.Model;

namespace SimplexLambda
{
    public class DBWrapper //: IUserDataLoader
    {
        private AmazonDynamoDBClient Client;
        private DynamoDBContext Context;
        private DynamoDBOperationConfig Cfg;
        private SimplexLambdaConfig LambdaConfig;

        public DBWrapper(SimplexLambdaConfig lambdaConfig)
        {
            LambdaConfig = lambdaConfig;
            Client = new AmazonDynamoDBClient(RegionEndpoint.USWest1);
            Context = new DynamoDBContext(Client);
            Cfg = new DynamoDBOperationConfig()
            {
                OverrideTableName = LambdaConfig.SimplexTable
            };
        }

        public SimplexError LoadItem<T>(T obj, out T Item, SimplexRequestContext context, out SimplexError err)
        {
            string diagName = $"DB_LOAD_[{obj.GetType().Name}]";
            var diagHandle = context.DiagInfo.BeginDiag(diagName);
            var task = Context.LoadAsync(obj, Cfg);
            task.Wait();
            Item = task.Result;
            err = task.Result == null ? SimplexError.GetError(SimplexErrorCode.DBItemNonexistent) : SimplexError.OK;
            context.DiagInfo.EndDiag(diagHandle);
            return err;
        }

        public SimplexError SaveItem<T>(T obj)
        {
            Context.SaveAsync(obj, Cfg).Wait();
            return SimplexError.OK;
        }
    }
}
