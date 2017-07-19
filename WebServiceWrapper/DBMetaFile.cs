using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Data;
using System.Reflection;
using System.Threading;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
/*
Author: Mike Uchman
Initial Publish Date: 8/26/2016
Disclaimer: This piece of software come with no warranty or guarantee. Use at your own risk!
*/
namespace RapidWrapper
{
    internal class DBMetaFile
    {
        internal volatile bool CacheUpToDate = false;
        internal Dictionary<string, SprocSchema> SprocSchemaMetaData { get; set; }

        internal Dictionary<Type,Dictionary<string,PropertyInfo>> DynamicDBRequestObjectMaps { get; set; }

        internal DBMetaFile()
        {
            SprocSchemaMetaData = LoadMetaData();
            DynamicDBRequestObjectMaps = new Dictionary<Type, Dictionary<string,PropertyInfo>>();
        }

        internal void CacheMetaData(string Name, SprocSchema Schema)
        {
            new Thread(() =>
            {
                lock (SprocSchemaMetaData)
                {
                    if (!SprocSchemaMetaData.ContainsKey(Name))
                    {
                        SprocSchemaMetaData.Add(Name, Schema);
                        CacheUpToDate = false;
                    }
                }
            }).Start();
        }

        internal void SaveMetaData()
        {
            new Thread(() =>
            {
                lock (SprocSchemaMetaData)
                {
                    if (!CacheUpToDate)
                    {
                        using (Stream stream = File.Create("DBMetaFile.bin"))
                        {
                            BinaryFormatter serializer = new BinaryFormatter();
                            serializer.Serialize(stream, SprocSchemaMetaData);
                        }
                    }
                }
            }).Start();
        }

        private Dictionary<string, SprocSchema> LoadMetaData()
        {
            Dictionary<string, SprocSchema> result = new Dictionary<string, SprocSchema>();

            if (File.Exists("DBMetaFile.bin"))
            {
                using (Stream stream = File.OpenRead("DBMetaFile.bin"))
                {
                    try
                    {
                        BinaryFormatter deserializer = new BinaryFormatter();
                        result = (Dictionary<string, SprocSchema>) deserializer.Deserialize(stream);
                        CacheUpToDate = true;
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
            return result;
        }
    }
}
