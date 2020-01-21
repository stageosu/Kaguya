﻿using KaguyaProjectV2.KaguyaBot.Core.Global;
using LinqToDB.Common;
using LinqToDB.Configuration;
using LinqToDB.Data;
using System.Collections.Generic;
using System.Linq;

namespace KaguyaProjectV2.KaguyaBot.DataStorage.DbData.Context
{
    public class KaguyaContext : IConnectionStringSettings
    {
        public string ConnectionString { get; set; }
        public string Name { get; set; }
        public string ProviderName { get; set; }
        public bool IsGlobal => false;
    }

    public class KaguyaSettings : ILinqToDBSettings
    {
        public IEnumerable<IDataProviderSettings> DataProviders => Enumerable.Empty<IDataProviderSettings>();
        public string DefaultConfiguration => "KaguyaContext";
        public string DefaultDataProvider => "MySQL";

        public IEnumerable<IConnectionStringSettings> ConnectionStrings
        {
            get
            {
                yield return new KaguyaContext
                {
                    Name = "KaguyaContext",
                    ProviderName = "MySql.Data.MySqlClient",
                    ConnectionString = $"Server={ConfigProperties.BotConfig.MySQL_Server.Split(':').First()};Port={ConfigProperties.BotConfig.MySQL_Server.Split(':').Last()};" +
                    $"Database={ConfigProperties.BotConfig.MySQL_Database};Uid={ConfigProperties.BotConfig.MySQL_Username};Pwd={ConfigProperties.BotConfig.MySQL_Password};charset=utf8mb4;"
                };
            }
        }
    }

    public class Init
    {
        public Init()
        {
            DataConnection.DefaultSettings = new KaguyaSettings();
            Configuration.Linq.AllowMultipleQuery = true;
        }
    }
}
