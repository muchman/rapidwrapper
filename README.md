# RapidWrapper

RapidWrapper is a C# SQL stored procedure wrapper to help eliminate tedious stored procedure wrapping by hand.

```  
var ReturnList = new List<MyObject>();

using (var conn = new SqlConnection(connectionString))
using (var sqlCommand = new SqlCommand("sp_create_attachment_records", conn))
{

    sqlCommand.CommandType = CommandType.StoredProcedure;
    sqlCommand.Parameters.AddWithValue("@Param1", "");
    sqlCommand.Parameters.AddWithValue("@Param2", claim.Payer_txtName);

    //probably have some retry logic or other stuff you do other than just open
    conn.Open();

    using (var rdr = cmd.ExecuteReader())
    {
        while (rdr.Read())
        {
            var MyNewObject = new MyObject();
            MyObject.Field1 = StringOrNull(rdr[0]);
            MyObject.Field2 = StringOrNull(rdr[1]);

            ReturnList.Add(MyNewObject);
        }
}

return ReturnList;
```

becomes

```
var ReturnList = new List<MyObject>();
DynamicDBRequest req = new DynamicDBRequest();
req.AddDBParameter("@Param1", "SomeValue");
req.AddDBParameter("@Param2", 4);
WebServiceResult result = req.SQLExecuteQuery(ReturnList, "NameOfSomeStoredProc");
```

## Getting Started

Simply import the RapidWrapper.dll into your project and start using it!

## Definitions

### RapidWrapperConfiguration

RapidWrapperConfiguration is a global static object that is used by every call in the framework.  It can store a global connection string for all calls to use.  It also houses the cache object of sproc info used to speed up performance.

* Properties
    * String ConnectionString
        * A global connection string that every call will use if one is not specified to the call directly.  
        * If you specify this connection string and pass one in to the call separately the one passed into the call will be used.  
        * If you attempt to make a stored procedure call without specifying this property or passing in the connection to the call an exception will be returned in the WebServiceResult.


* Functions
    * Void SaveDBMetaFile()
        * This will save a binary version of the sproc cache to disk.
        * The location is the location of the executable + “DBMetaFile.bin”
        * If this file exists it will automatically be loaded by the framework at runtime.
        * The framework currently has no way to detect changes in the sproc signature so it is best to avoid using this function if changes can occur.

### DymamicDBRequest

DynamicDBRequest is a dynamic type object that lets you add input parameters and values.

* Functions
    * void AddDBParameter(string key, object value)
        * This is used to add an input parameter to the collection.
        * Naming is case insensitive but spelling must be exactly what is expected in SQL

    * object GetDBParamaterValue(string Name)
        * This can be used to retrieve the value from the collection
        * Naming is case insensitive but spelling must be correct

    * T GetDBParamaterValue<T>(string Name)
        * This can be used to cast the value stored at the specified key to whatever type you want to store it in.
        * This does not handle cast exceptions! That is on you.

### WebServiceResult

The WebServiceResult is returned from all the SQL calls.  It contains information about what happened during execution along with any output parameters that could not be mapped.

* Properties
    * List<Exception> Exceptions
        * A collection of exceptions that happened during execution

    * bool Success
        * A flag that shows if the call completed successfully
        * Currently this only reflects if an exception occurred during execution

    * dynamic Outputs
        * This is a collection of output parameters that did not map to an existing object
        * This is dynamic so it will not validate if the property you are trying to access actually exists.
        * This is case and spelling sensitive
        * The name is the name of the output parameter sans any “@” symbol.


### SpecialPropertyMapper

The SpecialPropertyMapper is an optional parameter on SQL calls that allow you to specify how the sql value will be mapped to your chosen property.  It is a collection of property name keys with functions that take an Object type and return an Object type as the value.  This function is what will be run to map the SQL value to the property.

* Functions
    * Func<object,object> TryGetFunction(string PropertyName)
        * This will return a Func<object,object> if it exists for a specified property name
        * Will return null if no function exists

    * void AddFunction(string PropertyName, Func<object,object> Function)
        * This is used to add a function to the PropertyMapper for a given property name
        * If there is already a function for a given property it will override it.


Example
```
List<MyObject> ReturnList = new List< MyObject >();
DynamicDBRequest req = new DynamicDBRequest();
req.AddDBParameter("@Param1", "someinfo");

SpecialPropertyMapper functionMap = new SpecialPropertyMapper();

functionMap.AddFunction("Property1", CSVToStringHash);

WebServiceResult result = req.SQLExecuteQuery(ReturnList, " nameOfSomeStoredProc", functionmap);

//Function used in the SpecialPropertyMap, note this is re-usable
static object CSVToStringHash(object x)
{
    HashSet<string> hash = new HashSet<string>();
    string[] items = ((string)x).Split(',');
    foreach (string str in items)
    {
        if (!String.IsNullOrWhiteSpace(str))
            hash.Add(str);
    }
    return hash;
}
```

### SQLExecuteNonQuery

This is an extension method on DynamicDBRequest.  It will try to call the stored procedure passed in and map the input parameters from the extended class.

Example
```
DynamicDBRequest req = new DynamicDBRequest();
req.AddDBParameter("@ADACodeLine", "someinfo");
req.AddDBParameter("@Username", "TESTUSER");
WebServiceResult result = req.SQLExecuteNonQuery("nameOfSomeStoredProc");
```

### SQLExecuteMapObjectFromOutputs

This is an extension method on DynamicDBRequest.  It is used to map output parameters to an object.  It takes an out parameter of whatever object you want to map from output parameters.

### SQLExecuteQuery

This is an extension method on type of IList<T>.  It is used to map a recordset to a list of objects.  You must new up an instance of your list before making the call.  It will infer the type from the contained type in the list and try to map each row in the recordset to a new instance of the contained type and insert it into the list.
    
NOTE: Currently this does not support multiple record sets


Example
```
List<MyObject> s = new List< MyObject >();

DynamicDBRequest req = new DynamicDBRequest();

req.AddDBParameter("@SomeParam", "SomeValue");

WebServiceResult result = req.SQLExecuteQuery(s, " nameOfSomeStoredProc");

//Get output parameters from dynamic Outputs property in WebServiceResult
bool shouldValidate = result.Outputs.MyOutputParam;
```

## Authors

* **Mike Uchman** - *Initial work* - [muchman](https://github.com/muchman)


