using Library.db;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Library;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization.NamingConventions;
using Newtonsoft.Json;

namespace RestServerTest
{
    //public class swaggerdef
    //{
    //    public string swagger
    //    {
    //        get;
    //        set;
    //    }
    //}


    public class RESTServer
    {
        private MyHttpServer httpserver;

        private MyList<Type> types;

        private Type swaggerType2CSharpType(string yaml_type)
        {
            if (yaml_type == "boolean") return typeof(bool);
            if (yaml_type == "integer") return typeof(int);
            if (yaml_type == "string") return typeof(string);

            throw new Exception("unknwon type " + yaml_type);
        }

        private MyList<MyTypeInfo> yamlTypes2CSharpTypes(string swagger_yaml)
        {
            var input = new StringReader(swagger_yaml);

            var yaml = new YamlStream();
            yaml.Load(input);

            var root = (YamlMappingNode)yaml.Documents[0].RootNode;

            var definitions = (YamlMappingNode)root.Children[new YamlScalarNode("definitions")];

            
            MyList<MyTypeInfo> classes = new MyList<MyTypeInfo>();



            foreach (var entry in definitions.Children)
            {
                MyTypeInfo classdef = new MyTypeInfo(entry.Key.ToString(), "RemoteTypes",new MyTypeInfo(typeof(DatabaseTable)));
                classdef.AddAttribute(new JsonObjectAttribute(MemberSerialization.Fields));
                classes.Add(classdef);
            }

            foreach (var entry in definitions.Children)
            {
                MyTypeInfo classdef = classes.Find(c => c.Name == entry.Key.ToString().Trim());
                    

                var props = (YamlMappingNode)((YamlMappingNode)entry.Value).Children[new YamlScalarNode("properties")];

                int counter = 0;

                foreach (var prop in props.Children)
                {
                    
                    MyTypeInfo fieldtype = null;
                    bool ismylist = false;

                    string fieldname = prop.Key.ToString().Trim();

                    Mapping mapping = null;

                    Details details = null;


                    if (((YamlMappingNode)prop.Value).Children.ContainsKey(new YamlScalarNode("$ref")))
                    {
                        string yamlref = ((YamlScalarNode)((YamlMappingNode)prop.Value).Children[new YamlScalarNode("$ref")]).Value;

                       
                        fieldtype = classes.Find(c => c.Name == yamlref.getTextAfterLast("/").Trim());

                        
                        mapping = new Mapping(fieldtype.GetFieldWithAttribute(new ID()).Name);

                       
                    }
                    else
                    {
                        var type = (YamlScalarNode)((YamlMappingNode)prop.Value).Children[new YamlScalarNode("type")];

                        if (type.Value == "array")
                        {
                            var items = (YamlMappingNode)((YamlMappingNode)prop.Value).Children[new YamlScalarNode("items")];


                            if (items.Children.ContainsKey(new YamlScalarNode("$ref")))
                            {
                                string yamlref = ((YamlScalarNode)items.Children[new YamlScalarNode("$ref")]).Value;


                                fieldtype = classes.Find(c => c.Name == yamlref.getTextAfterLast("/"));
                                ismylist = true;

                                var det_id = new MyFieldInfo(typeof(int), classdef.Name + "_id");
                                det_id.AddAttribute(new Details());
                                fieldtype.AddField(det_id);
                                det_id.AddAttribute(new JsonIgnoreAttribute());

                                details = new Details();

                               
                            }
                            else
                            {
                                var type2 = (YamlScalarNode)items.Children[new YamlScalarNode("type")];

                               
                                ismylist = true;

                                MyTypeInfo newtable = new MyTypeInfo(fieldname + "_table", "RemoteTypes", new MyTypeInfo(typeof(DatabaseTable)));
                                MyFieldInfo newtable_id = new MyFieldInfo(typeof(int), fieldname + "_id");
                                newtable_id.AddAttribute(new ID());
                                newtable.AddField(newtable_id);

                                MyFieldInfo newtable_ref = new MyFieldInfo(typeof(int), classdef.Name + "_id");
                                newtable.AddField(newtable_ref);
                                newtable_ref.AddAttribute(new Details());
                                newtable_ref.AddAttribute(new Mapping(classdef.GetFieldWithAttribute(new ID()).Name));


                                MyFieldInfo newtable_value = new MyFieldInfo(swaggerType2CSharpType(type2.Value), fieldname);
                                newtable.AddField(newtable_value);

                                classes.Add(newtable);

                                fieldtype = newtable;

                            }
                        }
                        else
                        {
                           
                            fieldtype = new MyTypeInfo(swaggerType2CSharpType(type.Value));
                        }
                    }

                    

                    MyFieldInfo fi = new MyFieldInfo(fieldtype, fieldname,ismylist);
                    counter++;
                   
                    if (counter==1)
                    {
                        fi.AddAttribute(new ID());
                        
                    }

                    if (fieldtype.internalType!=null && fieldtype.internalType.Equals(typeof(string)))
                    {
                        fi.AddAttribute(new NullableString());
                    }

                    if (mapping != null) fi.AddAttribute(mapping);

                    if (details != null) fi.AddAttribute(details);

       
        classdef.AddField(fi);
                }

              
            }

           

            return classes;
        }

        private string swagger_yaml;

        public RESTServer(string swagger_yaml)
        {
            this.swagger_yaml = swagger_yaml;
            var classes = yamlTypes2CSharpTypes(swagger_yaml);
            

            string result = "using System;using Library;using Library.db;using Newtonsoft.Json;" + Environment.NewLine.x2() + "namespace RemoteTypes {" + Environment.NewLine.x2();


            foreach (MyTypeInfo mti in classes)
            {
                result += mti.GenerateSource();
                result += Environment.NewLine.x2();
            }

            result += "}";

            //lib.ShowInNotepad(result);

            types = lib.BuildTypesFromString(result);

            foreach (Type t in types)
            {
                DataBase.EnsureTableExist(t);
            }

            //DataBase.DataBasePath

          
        }

        public void StartListening(string app="", int port = 80)
        {
            httpserver = new MyHttpServer(app, port);

            httpserver.HandleRequest += Httpserver_HandleRequest;

            httpserver.Start();
        }

        private void showRequest(HttpListenerContext arg, string payload)
        {
            string r = arg.Request.HttpMethod+Environment.NewLine;


            r += arg.Request.Url.ToString() + Environment.NewLine;

            foreach (string k in arg.Request.QueryString.AllKeys)
            {
                r += k + "\t:\t";

                foreach (string v in arg.Request.QueryString.GetValues(k))
                {
                    r += v + ", ";
                }


                r += Environment.NewLine;
            }

           

            foreach (string k in arg.Request.Headers.AllKeys)
            {
                r += k + "\t:\t";

                foreach (string v in arg.Request.Headers.GetValues(k))
                {
                    r += v + ", ";
                }


                r += Environment.NewLine;
            }



            

            r += payload;

            lib.ShowInNotepad(r);
        }


        private string Httpserver_HandleRequest(HttpListenerContext arg, string payload, string id)
        {

            //showRequest(arg, payload);

            arg.Response.ContentType = "application/json";


            string method = arg.Request.HttpMethod;

            
            var url = arg.Request.RawUrl.Parse("\\", "/");

            if (url.Find(u=>u.ToLower()=="swagger")!=null)
            {
                return swagger_yaml;
            }

            if (method == "GET" || method == "DELETE")
            {
                string classname = url.Last();
                if (classname.IsInteger())
                {
                    classname = url.ForeLast();

                    int cid = Convert.ToInt32(url.Last());

                    Type type = types.Find(t => t.Name.ToLower() == classname.ToLower());

                    DatabaseTable proto = Activator.CreateInstance(type) as DatabaseTable;
                    proto.SetID(cid);

                   
                    var data= DataBase.SearchObject(proto);

                    if (data==null)
                    {
                        arg.Response.StatusCode = 405;
                        return "id "+cid.ToString()+" in table "+classname+" not found";
                    }


                    if (method == "DELETE")
                    {
                        data.Deleted = true;
                        DataBase.SaveObject(data);
                    }
                    else
                    {
                        
                        string response = JsonConvert.SerializeObject(data, Formatting.Indented);
                        

                     
                        return response;
                    }

                   

                }


            }

            if (method == "POST")
            {
                bool search = false;

                string classname = url.Last();

                if (classname.ToLower()=="searchbyprototype")
                {
                    classname = url.ForeLast();
                    search = true;
                }

                Type type = types.Find(t => t.Name.ToLower() == classname.ToLower());


                object o = JsonConvert.DeserializeObject(payload, type);

                if (search)
                {
                    var data=DataBase.SearchObjects(o);

                    return JsonConvert.SerializeObject(data, Formatting.Indented);
                   

                }
                else
                {

                    (o as DatabaseTable).Added = true;

                    try
                    {
                        DataBase.SaveObject(o);
                    }
                    catch (MyException ex)
                    {
                        arg.Response.StatusCode = 405;
                        return ex.Message;

                    }
                    string response = JsonConvert.SerializeObject(o, Formatting.Indented);
                    return response;
                }
                

            }

            

            if (method == "PUT")
            {
                string classname = url.Last();

                Type type = types.Find(t => t.Name.ToLower() == classname.ToLower());

             
                DatabaseTable o = JsonConvert.DeserializeObject(payload, type) as DatabaseTable;

                int cid = Convert.ToInt32(o.GetID());


                DatabaseTable proto = Activator.CreateInstance(type) as DatabaseTable;
                proto.SetID(cid);


                var data = DataBase.SearchObject(proto);

                if (data == null)
                {
                    arg.Response.StatusCode = 405;
                    return "id " + cid.ToString() + " in table " + classname + " not found";
                }

                data.StartEditSteping();

                o.CopyTo(data);

                data.Changed = true;

                DataBase.SaveObject(data);

            }


            return "";
        }
    }
}
