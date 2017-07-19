using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;

namespace RapidWrapper
{
    [Serializable()]
    internal class SprocSchema
    {
        internal List<SprocParam> InputParamsList { get; set; }
        internal List<SprocParam> OutputParamsList { get; set; }
        internal SprocParam ReturnValue { get; set; }
        internal List<PropertyMap> PropertyMapList { get; set; }
        internal XmlSerializer Serializer { get; set; }
        internal SprocParam XMLOutParam { get; set; }

        internal SprocSchema()
        {
            InputParamsList = new List<SprocParam>();
            OutputParamsList = new List<SprocParam>();
            PropertyMapList = new List<PropertyMap>();
        }
    }

    [Serializable()]
    internal class SprocParam
    {
        internal string ParameterName { get; set; }
        internal string PropertyName { get; set; }
        internal int Size { get; set; }
        internal SqlDbType Type { get; set; }
        internal ParameterDirection Direction { get; set; }
    }

    [Serializable()]
    internal class PropertyMap
    {
        internal string PropertyName { get; set; }
        internal string ParameterName { get; set; }
        internal PropertyInfo PropInfo { get; set; }
        internal int ColumnIndex { get; set; }
    }
}
