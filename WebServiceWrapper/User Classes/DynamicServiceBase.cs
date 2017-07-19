using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
/*
Author: Mike Uchman
Initial Publish Date: 8/26/2016
Disclaimer: This piece of software come with no warranty or guarantee. Use at your own risk!
*/
namespace RapidWrapper
{
    public class DynamicServiceBase : DynamicObject
    {
        public Dictionary<string, object> Values { get; set; }

        public DynamicServiceBase()
        {
            Values = new Dictionary<string, object>();
        }

        public void Add(string Name, object val)
        {
            Name = Name.ToLower();
            if (Values.ContainsKey(Name))
                Values[Name] = val;
            else
                Values.Add(Name, val);
        }

        // If you try to get a value of a property 
        // not defined in the class, this method is called.
        public override bool TryGetMember(
            GetMemberBinder binder, out object result)
        {
            // Converting the property name to lowercase
            // so that property names become case-insensitive.
            string name = binder.Name.ToLower();
            return Values.TryGetValue(name, out result);
        }

        // If you try to set a value of a property that is
        // not defined in the class, this method is called.
        public override bool TrySetMember(
            SetMemberBinder binder, object value)
        {

            Values[binder.Name.ToLower()] = value;
            return true;
        }
    }
}
