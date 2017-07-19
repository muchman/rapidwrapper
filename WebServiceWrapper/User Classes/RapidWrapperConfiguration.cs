using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
/*
Author: Mike Uchman
Initial Publish Date: 8/26/2016
Disclaimer: This piece of software come with no warranty or guarantee. Use at your own risk!
*/
namespace RapidWrapper
{
    //test
    public static class RapidWrapperConfiguration
    {
        public static string ConnectionString { get; set; }

        internal static DBMetaFile MetaFile = new DBMetaFile();

        public static void SaveDBMetaFile()
        {
            MetaFile.SaveMetaData();
        }
    }
}
