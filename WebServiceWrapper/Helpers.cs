using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
/*
Author: Mike Uchman
Initial Publish Date: 8/26/2016
Disclaimer: This piece of software come with no warranty or guarantee. Use at your own risk!
*/
namespace RapidWrapper
{
    internal static class Helpers
    {

        internal static readonly BindingFlags BindFlags = BindingFlags.SetProperty |
                                         BindingFlags.IgnoreCase |
                                         BindingFlags.Public |
                                         BindingFlags.Instance;

        internal static SqlConnection ConnectToDatabase(string connString)
        {
            int retryCount = 0;
            SqlConnection _dbConn = null;

            do
            {
                try
                {
                    _dbConn = new SqlConnection(connString);
                    _dbConn.Open();
                }
                catch (Exception ex)
                {
                    System.Threading.Thread.Sleep(1000);
                }

                retryCount++;

            } while (_dbConn == null && retryCount < 5);

            return _dbConn;
        }

        internal static void MapDataReaderToObject(object MapTo, SqlDataReader reader, ref SprocSchema Schema, SpecialPropertyMapper functionmap, WebServiceResult wsresult)
        {
            if (Schema.PropertyMapList.Count > 0)
            {
                foreach (PropertyMap pmap in Schema.PropertyMapList)
                {
                    var value = reader[pmap.ColumnIndex];
                    MapValue(MapTo, value, pmap, functionmap, wsresult);
                }
            }
            else
            {
                Type type = MapTo.GetType();

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string columnname = reader.GetName(i);
                    PropertyInfo pinfo = type.GetProperty(columnname, BindFlags);

                     if (pinfo != null && pinfo.CanWrite)
                    {
                        PropertyMap pmap = new PropertyMap();
                        pmap.PropInfo = pinfo;
                        pmap.ColumnIndex = i;
                        var value = reader[pinfo.Name];
                        MapValue(MapTo, value, pmap, functionmap, wsresult);
                        Schema.PropertyMapList.Add(pmap);
                    }

                }
            }
        }

        internal static void MapValue(Object MapTo, Object value, PropertyMap Pmap, SpecialPropertyMapper functionmap, WebServiceResult wsresult)
        {
            try
            {
                Type t = Nullable.GetUnderlyingType(Pmap.PropInfo.PropertyType) ?? Pmap.PropInfo.PropertyType;

                if (value is DBNull)
                    return;

                Func<object, object> function = functionmap?.TryGetFunction(Pmap.PropInfo.Name);
                if (function != null)
                {
                    value = function(value);
                }

                Object safe = Convert.ChangeType(value, t);
                Pmap.PropInfo.SetValue(MapTo, safe, null);
            }
            catch (Exception ex)
            {
                wsresult.Success = false;
                wsresult.Exceptions.Add(ex);
            }
        }

        internal static SqlString SerializeToXMLString(Object objToSerialize, XmlSerializer serializer)
        {
            SqlString databaseXML = null;
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = false;
            settings.NewLineHandling = NewLineHandling.None;
            settings.OmitXmlDeclaration = true;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (XmlWriter writer = XmlWriter.Create(memoryStream, settings))
                {
                    lock (serializer)
                    {
                        serializer.Serialize(writer, objToSerialize);
                        writer.Flush();
                        writer.Close();
                    }
                }
                memoryStream.Seek(0, SeekOrigin.Begin);
                using (StreamReader sr = new StreamReader(memoryStream))
                {
                    databaseXML = sr.ReadToEnd();
                }
            }
            return databaseXML;
        }

        internal static Object DeserializeDatabaseXML(SqlXml sqlXml, XmlSerializer serializer)
        {
            if (sqlXml == null || sqlXml.IsNull)
                return null;

            using (XmlReader xmlReader = sqlXml.CreateReader())
            {
                lock (serializer)
                {
                    return serializer.Deserialize(xmlReader);
                }
            }
        }

        //This is the core bread and butter for creating a sql request dynamically
        internal static void CompleteSQLRequest(this SqlCommand Command, DynamicDBRequest Request, ref SprocSchema Schema)
        {
            //We have a cached schema to work with
            if (Schema != null && (Schema.InputParamsList.Count != 0 || Schema.OutputParamsList.Count != 0))
            {
                foreach (SprocParam param in Schema.InputParamsList)
                {
                    SqlParameter sqlparam = new SqlParameter() { ParameterName = param.ParameterName, Value = Request.GetDBParameterValue(param.ParameterName), Direction = param.Direction, SqlDbType = param.Type };
                    if (param.Size != 0)
                        sqlparam.Size = param.Size;
                    Command.Parameters.Add(sqlparam);
                }

                if (Schema.ReturnValue != null)
                {
                    SqlParameter sqlparam = new SqlParameter() { ParameterName = Schema.ReturnValue.ParameterName, Direction = Schema.ReturnValue.Direction, SqlDbType = Schema.ReturnValue.Type };
                    if (Schema.ReturnValue.Size != 0)
                        sqlparam.Size = Schema.ReturnValue.Size;
                    Command.Parameters.Add(sqlparam);
                }

                foreach (SprocParam param in Schema.OutputParamsList)
                {
                    SqlParameter sqlparam = new SqlParameter() { ParameterName = param.ParameterName, Size = param.Size, Direction = param.Direction, SqlDbType = param.Type };
                    if (param.Size != 0)
                        sqlparam.Size = param.Size;
                    Command.Parameters.Add(sqlparam);
                }
            }
            else //Get the definition and build the cache from the DB
            {
                Schema = new SprocSchema();

                SqlCommandBuilder.DeriveParameters(Command);

                for (int i = 0; i < Command.Parameters.Count; i++)
                {
                    SqlParameter p = Command.Parameters[i];
                    if (p.Direction == System.Data.ParameterDirection.Input)
                    {
                        SprocParam inparam = new SprocParam { ParameterName = p.ParameterName, PropertyName = p.ParameterName.Replace("@", ""), Size = p.Size, Type = p.SqlDbType, Direction = p.Direction };

                        object ParamValue = Request.GetDBParameterValue(p.ParameterName);
                        if (ParamValue != null)
                        {
                            p.Value = ParamValue;
                        }
                        Schema.InputParamsList.Add(inparam);
                    }
                    else
                    {
                        if (p.Direction == System.Data.ParameterDirection.InputOutput)
                            p.Direction = System.Data.ParameterDirection.Output;
                        if (p.Direction == ParameterDirection.ReturnValue)
                            Schema.ReturnValue = new SprocParam { ParameterName = p.ParameterName, PropertyName = p.ParameterName.Replace("@", ""), Size = p.Size, Type = p.SqlDbType, Direction = p.Direction };
                        else
                            Schema.OutputParamsList.Add(new SprocParam { ParameterName = p.ParameterName, PropertyName = p.ParameterName.Replace("@", ""), Size = p.Size, Type = p.SqlDbType, Direction = p.Direction });
                    }

                }
            }
        }
    }
}
