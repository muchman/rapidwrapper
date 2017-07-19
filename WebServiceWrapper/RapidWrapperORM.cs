using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace RapidWrapper
{
    public class RapidWrapperORM<T>
    {
        private string TableName { get; }
        private string DBConnectionString { get; }

        public RapidWrapperORM(string table, string connectionString = null)
        {
            TableName = table;
            DBConnectionString = connectionString ?? RapidWrapperConfiguration.ConnectionString;
            if (DBConnectionString == null)
            {
                throw new Exception(
                    "There is no connection string associated with RapidWrapperORM Constructor.  Please pass in a connection string or assign it globally.");
            }
        }

        public IList<T> Select()
        {
            Type type = typeof(T);
            string query = String.Format("SELECT * FROM {0}", TableName);

            using (SqlCommand cmd = new SqlCommand(query, Helpers.ConnectToDatabase(DBConnectionString)))
            {
                try
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        return MapORMReaderToObject(reader);
                    }
                }
                catch (Exception ex)
                {
                    //Console.WriteLine("Error in SQLExecuteQuery: " + ex.Message);
                    //Result.Success = false;
                    //Result.Exceptions.Add(ex);
                    //return Result;
                }
            }
            //save the cache to the metafile cache.
            //RapidWrapperConfiguration.MetaFile.CacheMetaData(SQLSprocName, Schema);
            //Result.Success = true;
            //return Result;
            return null;

        }

        private IList<T> MapORMReaderToObject(SqlDataReader reader)
        {
            var ListContainer = Activator.CreateInstance(typeof(List<T>));
            
            //TODO: REVISE NEED FOR THIS
            SprocSchema Schema = new SprocSchema();
            WebServiceResult Result = new WebServiceResult();

            //do while reader.nextresult for multi record sets.  We can rebuild this 
            while (reader.HasRows && reader.Read())
            {
                try
                {
                    //create a new type in the list and map the data to it.
                    var ListObject = Activator.CreateInstance(typeof(T));
                    Helpers.MapDataReaderToObject(ListObject, reader, ref Schema, null, Result);
                    ((IList<T>)ListContainer).Add((T)ListObject);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error in SQLExecuteQuery: " + ex.Message);
                    Result.Success = false;
                    Result.Exceptions.Add(ex);
                    return (IList<T>)ListContainer;
                }
            }

            return (IList<T>)ListContainer;
        }
    }
}
