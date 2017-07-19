using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
/*
Author: Mike Uchman
Initial Publish Date: 8/26/2016
Disclaimer: This piece of software come with no warranty or guarantee. Use at your own risk!
*/
namespace RapidWrapper
{
    public class WebServiceResult : DynamicServiceBase
    {
        public List<Exception> Exceptions { get; set; }
        public bool Success { get; set; }

        public dynamic Outputs { get; set; }

        public WebServiceResult()
        {
            Exceptions = new List<Exception>();
            Outputs = this;
        }

    }
}
