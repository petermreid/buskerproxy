using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Services.Client;
using System.Data.Services.Common;
using System.Linq;
using System.Runtime.Caching;
using System.Net;

namespace RoutingConfig
{
    public class Config
    {
        const string _storageAccount = "ConfigStorageAccount";
        static string _tableName = "RoutingConfig";
        static string _partitionKey = "RoutingInfo";

        static CloudTableClient _tableClient;
        private static ObjectCache _configCache = MemoryCache.Default;
        private const int _expireMinutes = 1; 

        [DataServiceKey("PartitionKey", "RowKey")]
        public class RouteEntity
            : TableServiceEntity
        {
            public RouteEntity(string routeName, RoutingInfo routingInfo)
                : base(_partitionKey, routeName)
            {
                persist = GenericDataContractSerializer<RoutingInfo>.WriteObject(routingInfo);
            }

            public RouteEntity()
            {          
            }

            public string persist { get; set; }
            public RoutingInfo GetRoutingInfo()
            {
                return GenericDataContractSerializer<RoutingInfo>.ReadObject(persist);
            }
        }

        static Config()
        {
            string storageConnection = ConfigurationManager.AppSettings[_storageAccount];
            _tableClient = CloudStorageAccount.Parse(storageConnection).CreateCloudTableClient();
            _tableClient.CreateTableIfNotExist(_tableName);
        }

        private static TableServiceContext GetTableContext()
        {
            TableServiceContext tableContext;
            tableContext = _tableClient.GetDataServiceContext();
            tableContext.RetryPolicy = RetryPolicies.RetryExponential(RetryPolicies.DefaultClientRetryCount, RetryPolicies.DefaultMaxBackoff);
            tableContext.IgnoreMissingProperties = true;
            tableContext.SaveChangesDefaultOptions = SaveChangesOptions.ReplaceOnUpdate;
            tableContext.IgnoreResourceNotFoundException = true;
            return tableContext;
        }

        public static void RegisterRoute(string routeName, RoutingInfo routingInfo)
        {
            TableServiceContext tableContext = GetTableContext();
            RouteEntity entity = new RouteEntity(routeName, routingInfo);
            //this performs an Upsert (InsertOrRepalce)
            tableContext.AttachTo(_tableName, entity, null);
            tableContext.UpdateObject(entity);
            tableContext.SaveChanges();
            _configCache.Remove(_partitionKey);
        }

        public static void UnregisterRoute(string routeName)
        {
            TableServiceContext tableContext = GetTableContext();
            RouteEntity entity = new RouteEntity(routeName, null);
            tableContext.AttachTo(_tableName, entity, "*");
            tableContext.DeleteObject(entity);
            tableContext.SaveChanges();
            _configCache.Remove(_partitionKey);
        }

        public static RoutingInfo GetRoute(string routeName)
        {
            var d = GetConfigDictionary();
            if (d.ContainsKey(routeName))
                return d[routeName];
            else
                return null;
        }

        public static string GetConnection(string host)
        {
            var d = GetConfigDictionary();
            var connection = (from route in d.Values
                              where route.routeTo == host
                              select route.connectionString).FirstOrDefault();
            return connection;
        }

        public static Dictionary<string, RoutingInfo> GetConfigDictionary()
        {
            Dictionary<string, RoutingInfo> config = (Dictionary<string, RoutingInfo>)_configCache.Get(_partitionKey);
            if (config == null)
            {
                new Dictionary<string, RoutingInfo>();
                TableServiceContext tableContext = GetTableContext();
                CloudTableQuery<RouteEntity> routeQuery =
                        (from e in tableContext.CreateQuery<RouteEntity>(_tableName)
                         where e.PartitionKey == _partitionKey
                         select e)
                            .AsTableServiceQuery<RouteEntity>();
                IEnumerable<RouteEntity> re = routeQuery.Execute();
                config = re.ToDictionary(r => r.RowKey, v => v.GetRoutingInfo());
                _configCache.Remove(_partitionKey);
                _configCache.Add(_partitionKey, config, DateTimeOffset.Now.AddMinutes(_expireMinutes));
            }
            return config;
        }
    }
}
