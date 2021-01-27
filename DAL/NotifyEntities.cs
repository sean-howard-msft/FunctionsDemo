using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.EntityClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.DAL
{
    public partial class NotifyEntities : DbContext
    {
        public NotifyEntities(string ConnectionString) : base(ConnectionString)
        {

        }

        public static string GetConnectionString(string ProviderConnectionString)
        {
            var builder = new EntityConnectionStringBuilder()
            {
                ProviderConnectionString = ProviderConnectionString,
                Metadata = "res://*/DemoModel.csdl|res://*/DemoModel.ssdl|res://*/DemoModel.msl",
                Provider = "System.Data.SqlClient"
            };
            return builder.ConnectionString;
        }
    }
}
