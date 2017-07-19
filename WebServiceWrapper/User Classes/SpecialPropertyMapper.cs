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
    public class SpecialPropertyMapper
    {
        Dictionary<string, Func<object, object>> FunctionMaps { get; set; }

        public SpecialPropertyMapper()
        {
            FunctionMaps = new Dictionary<string, Func<object, object>>();
        }

        public Func<object,object> TryGetFunction(string PropertyName)
        {
            PropertyName = PropertyName.ToLower();
            Func<object, object> retval = null;
            FunctionMaps.TryGetValue(PropertyName, out retval);
            return retval;
        }

        public void AddFunction(string PropertyName, Func<object,object> Function)
        {
            PropertyName = PropertyName.ToLower();
            if (FunctionMaps.ContainsKey(PropertyName))
                FunctionMaps[PropertyName] = Function;
            else
                FunctionMaps.Add(PropertyName, Function);
        }
    }
}
