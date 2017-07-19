using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Data;
using System.Data.SqlTypes;
/*
Author: Mike Uchman
Initial Publish Date: 8/26/2016
Disclaimer: This piece of software come with no warranty or guarantee. Use at your own risk!
*/
namespace RapidWrapper
{
    public static class Extensions
    {
        //A passthrough incase we have a globally defined connection string
        public static WebServiceResult SQLExecuteNonQuery(this DynamicDBRequest Request, string SQLSprocName)
        {
            if (RapidWrapperConfiguration.ConnectionString == null)
            {
                WebServiceResult result = new WebServiceResult();
                result.Success = false;
                result.Exceptions.Add(new Exception("No connection string specified"));
                return result;
            }
            else
                return SQLExecuteNonQuery(Request, SQLSprocName, RapidWrapperConfiguration.ConnectionString);
        }

        public static WebServiceResult SQLExecuteNonQuery(this DynamicDBRequest Request, string SQLSprocName, string DBConnectionString)
        {
            WebServiceResult Result = new WebServiceResult();

            SprocSchema Schema = null;
            RapidWrapperConfiguration.MetaFile.SprocSchemaMetaData.TryGetValue(SQLSprocName, out Schema);

            using (SqlCommand cmd = new SqlCommand(SQLSprocName, Helpers.ConnectToDatabase(DBConnectionString)))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CompleteSQLRequest(Request, ref Schema);

                try
                {
                    cmd.ExecuteNonQuery();

                    try
                    {
                        foreach (SprocParam outparam in Schema.OutputParamsList)
                        {
                            Result.Add(outparam.PropertyName, cmd.Parameters[outparam.ParameterName].Value is DBNull ? null : cmd.Parameters[outparam.ParameterName].Value);                         
                        }
                        if (Schema.ReturnValue != null)
                        {
                            Result.Add(Schema.ReturnValue.PropertyName, cmd.Parameters[Schema.ReturnValue.ParameterName].Value is DBNull ? null : cmd.Parameters[Schema.ReturnValue.ParameterName].Value);
                        }
                    }
                    catch (Exception ex)
                    {
                        Result.Exceptions.Add(ex);
                        Result.Success = false;
                        Console.WriteLine("Error in SQLExecuteQuery Outputs: " + ex.Message);
                        return Result;
                    }
                }
                catch (Exception ex)
                {
                    Result.Exceptions.Add(ex);
                    Result.Success = false;
                    Console.WriteLine("Error in SQLExecuteQuery: " + ex.Message);
                    return Result;
                }
            }

            RapidWrapperConfiguration.MetaFile.CacheMetaData(SQLSprocName, Schema);
            Result.Success = true;
            return Result;
        }

        public static WebServiceResult SQLExecuteMapObjectFromOutputs<T>(this DynamicDBRequest Request, T ResultObject, string SQLSprocName, SpecialPropertyMapper FunctionMap = null)
        {
            if (RapidWrapperConfiguration.ConnectionString == null)
            {
                WebServiceResult result = new WebServiceResult();
                result.Success = false;
                result.Exceptions.Add(new Exception("No connection string specified"));
                return result;
            }
            else
                return SQLExecuteMapObjectFromOutputs(Request, ResultObject, SQLSprocName, RapidWrapperConfiguration.ConnectionString, FunctionMap);
        }

        public static WebServiceResult SQLExecuteMapObjectFromOutputs<T>(this DynamicDBRequest Request, T ResultObject, string SQLSprocName, string DBConnectionString, SpecialPropertyMapper FunctionMap = null)
        {
            WebServiceResult Result = new WebServiceResult();

            ResultObject = Activator.CreateInstance<T>();

            SprocSchema Schema = null;
            RapidWrapperConfiguration.MetaFile.SprocSchemaMetaData.TryGetValue(SQLSprocName, out Schema);

            using (SqlCommand cmd = new SqlCommand(SQLSprocName, Helpers.ConnectToDatabase(DBConnectionString)))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CompleteSQLRequest(Request, ref Schema);

                try
                {
                    cmd.ExecuteNonQuery();

                    try
                    {
                        if (Schema.PropertyMapList.Count > 0)
                        {
                            foreach (PropertyMap pmap in Schema.PropertyMapList)
                            {
                                if (pmap.PropInfo != null)
                                {
                                    Helpers.MapValue(ResultObject, cmd.Parameters[pmap.ParameterName].Value, pmap, FunctionMap, Result);
                                }
                                else
                                {
                                    Result.Add(pmap.PropertyName, cmd.Parameters[pmap.ParameterName].Value is DBNull ? null : cmd.Parameters[Schema.ReturnValue.ParameterName].Value);
                                }
                            }
                        }
                        else
                        {
                            foreach (SprocParam outparam in Schema.OutputParamsList)
                            {
                                PropertyInfo pinfo = typeof(T).GetProperty(outparam.PropertyName, Helpers.BindFlags);
                                PropertyMap pmap = new PropertyMap();
                                pmap.ParameterName = outparam.ParameterName;
                                if (pinfo != null && pinfo.CanWrite)
                                {
                                    pmap.PropInfo = pinfo;
                                    Helpers.MapValue(ResultObject, cmd.Parameters[outparam.ParameterName].Value, pmap, FunctionMap, Result);
                                }
                                else
                                {
                                    pmap.PropertyName = outparam.PropertyName;
                                    Result.Add(outparam.PropertyName, cmd.Parameters[outparam.ParameterName].Value is DBNull ? null : cmd.Parameters[outparam.ParameterName].Value);
                                }
                                Schema.PropertyMapList.Add(pmap);
                            }
                            if (Schema.ReturnValue != null)
                            {
                                Result.Add(Schema.ReturnValue.PropertyName, cmd.Parameters[Schema.ReturnValue.ParameterName].Value is DBNull ? null : cmd.Parameters[Schema.ReturnValue.ParameterName].Value);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Result.Exceptions.Add(ex);
                        Result.Success = false;
                        Console.WriteLine("Error in SQLExecuteQuery Outputs: " + ex.Message);
                        return Result;
                    }
                }
                catch (Exception ex)
                {
                    Result.Exceptions.Add(ex);
                    Result.Success = false;
                    Console.WriteLine("Error in SQLExecuteQuery: " + ex.Message);
                    return Result;
                }
            }

            RapidWrapperConfiguration.MetaFile.CacheMetaData(SQLSprocName, Schema);
            Result.Success = true;
            return Result;
        }

        public static WebServiceResult SQLExecuteMapObjectFromXML<T>(this DynamicDBRequest Request, T ResultObject, string SQLSprocName, SpecialPropertyMapper FunctionMap = null)
        {
            if (RapidWrapperConfiguration.ConnectionString == null)
            {
                WebServiceResult result = new WebServiceResult();
                result.Success = false;
                result.Exceptions.Add(new Exception("No connection string specified"));
                return result;
            }
            else
                return SQLExecuteMapObjectFromXML(Request, ResultObject, SQLSprocName, RapidWrapperConfiguration.ConnectionString, FunctionMap);
        }

        public static WebServiceResult SQLExecuteMapObjectFromXML<T>(this DynamicDBRequest Request, T ResultObject, string SQLSprocName, string DBConnectionString, SpecialPropertyMapper FunctionMap = null)
        {
            WebServiceResult Result = new WebServiceResult();

            ResultObject = Activator.CreateInstance<T>();

            SprocSchema Schema = null;
            RapidWrapperConfiguration.MetaFile.SprocSchemaMetaData.TryGetValue(SQLSprocName, out Schema);

            using (SqlCommand cmd = new SqlCommand(SQLSprocName, Helpers.ConnectToDatabase(DBConnectionString)))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CompleteSQLRequest(Request, ref Schema);

                try
                {
                    cmd.ExecuteNonQuery();

                    try
                    {
                        if (Schema.Serializer != null && Schema.XMLOutParam != null)
                        {
                            ResultObject = (T)Helpers.DeserializeDatabaseXML((SqlXml)cmd.Parameters[Schema.XMLOutParam.ParameterName].Value, Schema.Serializer);
                        }
                        else
                        {
                            foreach (SprocParam outparam in Schema.OutputParamsList)
                            {
                                if (outparam.Type == SqlDbType.Xml)
                                {
                                    Schema.Serializer = new XmlSerializer(typeof(T));
                                    Schema.XMLOutParam = outparam;
                                    ResultObject = (T)Helpers.DeserializeDatabaseXML((SqlXml)cmd.Parameters[Schema.OutputParamsList[0].ParameterName].SqlValue, Schema.Serializer);
                                }
                                else
                                {
                                    PropertyMap pmap = new PropertyMap();
                                    pmap.ParameterName = outparam.ParameterName;
                                    pmap.PropertyName = outparam.PropertyName;
                                    Result.Add(outparam.PropertyName, cmd.Parameters[outparam.ParameterName].Value is DBNull ? null : cmd.Parameters[outparam.ParameterName].Value);
                                    Schema.PropertyMapList.Add(pmap);
                                }
                            }
                            if (Schema.ReturnValue != null)
                            {
                                Result.Add(Schema.ReturnValue.PropertyName, cmd.Parameters[Schema.ReturnValue.ParameterName].Value is DBNull ? null : cmd.Parameters[Schema.ReturnValue.ParameterName].Value);
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        Result.Exceptions.Add(ex);
                        Result.Success = false;
                        Console.WriteLine("Error in SQLExecuteQuery Outputs: " + ex.Message);
                        return Result;
                    }
                }
                catch (Exception ex)
                {
                    Result.Exceptions.Add(ex);
                    Result.Success = false;
                    Console.WriteLine("Error in SQLExecuteQuery: " + ex.Message);
                    return Result;
                }
            }

            RapidWrapperConfiguration.MetaFile.CacheMetaData(SQLSprocName, Schema);
            Result.Success = true;
            return Result;
        }

        public static WebServiceResult SQLExecuteQuery<T>(this DynamicDBRequest Request, IList<T> ObjectResultList, string SQLSprocName, SpecialPropertyMapper FunctionMap = null)
        {
            ObjectResultList = ObjectResultList ?? (IList<T>)Activator.CreateInstance(typeof(IList<T>));
            if (RapidWrapperConfiguration.ConnectionString == null)
            {
                WebServiceResult result = new WebServiceResult();
                result.Success = false;
                result.Exceptions.Add(new Exception("No connection string specified"));
                return result;
            }
            else
                return SQLExecuteQuery(Request, ObjectResultList, SQLSprocName, RapidWrapperConfiguration.ConnectionString, FunctionMap);
        }

        public static WebServiceResult SQLExecuteQuery<T>(this DynamicDBRequest Request, IList<T> ObjectResultList, string SQLSprocName, string DBConnectionString, SpecialPropertyMapper FunctionMap = null)
        {
            //TODO: error handeling if all this info is fubar
            //get the types of the item in the result list and the request
            Type type = ObjectResultList.GetType().GetGenericArguments()[0];
            WebServiceResult Result = new WebServiceResult();

            //load in a schema if we have one
            SprocSchema Schema = null;
            RapidWrapperConfiguration.MetaFile.SprocSchemaMetaData.TryGetValue(SQLSprocName, out Schema);

            using (SqlCommand cmd = new SqlCommand(SQLSprocName, Helpers.ConnectToDatabase(DBConnectionString)))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                //this call completes the SqlCommand by either filling in the info from the cached schema for the params,
                //or calling the DB to pull down meta data, create the params, and then create them in the cache for use
                //after the first call
                cmd.CompleteSQLRequest(Request, ref Schema);


                try
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        //do while reader.nextresult for multi record sets.  We can rebuild this 
                        while (reader.HasRows && reader.Read())
                        {
                            try
                            {
                                //create a new type in the list and map the data to it.
                                var ListObject = Activator.CreateInstance(type);
                                Helpers.MapDataReaderToObject(ListObject, reader, ref Schema, FunctionMap, Result);
                                ObjectResultList.Add((T)ListObject);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Error in SQLExecuteQuery: " + ex.Message);
                                Result.Success = false;
                                Result.Exceptions.Add(ex);
                                return Result;
                            }
                        }
                    }
                    try
                    {
                        //map the output params
                        foreach (SprocParam outparam in Schema.OutputParamsList)
                        {
                            Result.Add(outparam.PropertyName, cmd.Parameters[outparam.ParameterName].Value is DBNull ? null : cmd.Parameters[outparam.ParameterName].Value);

                        }
                        if (Schema.ReturnValue != null)
                        {
                            Result.Add(Schema.ReturnValue.PropertyName, cmd.Parameters[Schema.ReturnValue.ParameterName].Value is DBNull ? null : cmd.Parameters[Schema.ReturnValue.ParameterName].Value);
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error in SQLExecuteQuery Outputs: " + ex.Message);
                        Result.Success = false;
                        Result.Exceptions.Add(ex);
                        return Result;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error in SQLExecuteQuery: " + ex.Message);
                    Result.Success = false;
                    Result.Exceptions.Add(ex);
                    return Result;
                }
            }
            //save the cache to the metafile cache.
            RapidWrapperConfiguration.MetaFile.CacheMetaData(SQLSprocName, Schema);
            Result.Success = true;
            return Result;
        }
    }

}
