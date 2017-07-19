using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Reflection;
/*
Author: Mike Uchman
Initial Publish Date: 8/26/2016
Disclaimer: This piece of software come with no warranty or guarantee. Use at your own risk!
*/
namespace RapidWrapper
{
    public class DynamicDBRequest : DynamicServiceBase
    {
        private Dictionary<string, PropertyInfo> CustomProperties;
        private object CustomObject { get; set; }

        public DynamicDBRequest()
        {
        }

        public DynamicDBRequest(object customObject)
        {
            if (CustomObject == null)
                throw new Exception("Object passed to DynamicDBRequest Constructor cannot be null!");

            Type t = CustomObject.GetType();
            RapidWrapperConfiguration.MetaFile.DynamicDBRequestObjectMaps.TryGetValue(t, out CustomProperties);

            if (CustomProperties == null)
            {
                CustomProperties = new Dictionary<string, PropertyInfo>();
                PropertyInfo[] proparray = t.GetProperties();
                foreach(PropertyInfo prop in proparray)
                {
                    CustomProperties.Add(prop.Name.ToLower(), prop);
                }
                RapidWrapperConfiguration.MetaFile.DynamicDBRequestObjectMaps.Add(t,CustomProperties);
            }

            CustomObject = customObject;
            //TODO: This is...ok...but maybe we just assign the object and get the values on the fly then from GetDBParameterValue when they are needed so we are not needlessly 
            //copying the entire object.
            //foreach( PropertyInfo prop in properties)
            //{
            //    AddDBParameter(prop.Name, prop.GetValue(CustomObject, null));
            //}
        }

        public void AddDBParameter(string key, object value)
        {
            Add(key, value);
        }

        public object GetDBParameterValue(string Name)
        {
            Name = Name.ToLower();
            object retval = null;

            if(CustomObject != null)
            {
                PropertyInfo pinfo;
                CustomProperties.TryGetValue(Name, out pinfo);
                if(pinfo != null)
                {
                    retval = pinfo.GetValue(CustomObject, null);
                    return retval;
                }
            }
            Values.TryGetValue(Name, out retval);
            return retval;
        }

        public T GetDBParamaterValue<T>(string Name)
        {
            Name = Name.ToLower();
            object retval = null;
            Values.TryGetValue(Name, out retval);
            return (T) Convert.ChangeType(retval, typeof(T));
        }
    }
}
