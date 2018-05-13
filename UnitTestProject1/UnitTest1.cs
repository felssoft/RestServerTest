using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RestServerTest;
using Library;
using System.Net;
using System.Text;
using System.IO;
using Library.db;
using Library.MyControls;

namespace UnitTestProject1
{
    


    [TestClass]
    public class UnitTest1
    {
        private int statuscode;

        private string get(string path, string method="GET")
        {
            HttpWebRequest request = WebRequest.Create("http://localhost/resttest:8090/"+path) as HttpWebRequest;
            request.Method = method;
            request.KeepAlive = true;

            try
            {
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                statuscode = (int)response.StatusCode;
                Stream responseStream = response.GetResponseStream();
                StreamReader reader = new System.IO.StreamReader(responseStream, Encoding.UTF8);
                return reader.ReadToEnd();
            }
            catch (WebException wex)
            {
                statuscode = (int)(wex.Response as HttpWebResponse).StatusCode;
                if (wex.Response != null)
                {
                    using (var errorResponse = (HttpWebResponse)wex.Response)
                    {
                        using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
            }

            return "error";
        }

        private string post(string path, string method, string postdata)
        {
            byte[] postData = Encoding.ASCII.GetBytes(postdata);
            HttpWebRequest request = WebRequest.Create("http://localhost/resttest:8090/"+path) as HttpWebRequest;
            //request.Method = "POST";
            request.Method = method;
            request.KeepAlive = true;
            //request.Headers["Access-Control-Request-Method"] = method;
            //request.UserAgent = "Mozilla/5.0 (Windows NT 5.1) AppleWebKit/535.19 (KHTML, like Gecko) Chrome/18.0.1025.152 Safari/535.19";
            //request.ContentType = "application/x-www-form-urlencoded";

            
            //request.Referer = url;


            request.ContentLength = postData.Length;

            
            System.IO.Stream outputStream = request.GetRequestStream();
           
            outputStream.Write(postData, 0, postData.Length);
            outputStream.Close();


            try
            {
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                statuscode = (int)response.StatusCode;
                Stream responseStream = response.GetResponseStream();
                StreamReader reader = new System.IO.StreamReader(responseStream, Encoding.UTF8);
                return reader.ReadToEnd();
            }
            catch (WebException wex)
            {
                statuscode = (int)(wex.Response as HttpWebResponse).StatusCode;
                if (wex.Response != null)
                {
                    using (var errorResponse = (HttpWebResponse)wex.Response)
                    {
                        using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
            }

            return "error";
        }


        [TestInitialize]
        public void TestInitialize()
        {
            //DataBase.RunInMemory = true;
            //Program.Main(new string[] { "service" });
        }

        [TestMethod]
        public void TestMethod2()
        {
            

            string json = @"{ 
                ""id"": @@ID@@,
                ""FirstName"": ""Paul"",
                ""LastName"": ""Maxe"",
                ""tasks"": [
                {
                    ""id"": @@TID@@,
                    ""Title"": ""Task3"",
                    ""Description"": ""string"",
                    ""State"": true
                }
                ]
            }";

            string r = post("User", "POST", json.Replace("@@ID@@","-1").Replace("@@TID@@", "-1"));

            Assert.AreEqual(statuscode, 200);

            string id = r.getTextBetween(@"""id"":",",").Trim();

            string tid=r.getTextAfter("tasks").getTextBetween(@"""id"":", ",").Trim();

            string r2 = get("User/" + id);

            Assert.AreEqual(statuscode, 200);

            json = json.Replace(Environment.NewLine, "").Replace("\t", "").Replace(" ", "");
            r2 = r2.Replace(Environment.NewLine, "").Replace("\t", "").Replace(" ", "");

            Assert.AreEqual(json.Replace("@@ID@@",id).Replace("@@TID@@", tid), r2);

            r2 = r2.Replace("Maxe", "Klaxe");

            post("User", "PUT", r2);

            Assert.AreEqual(statuscode, 200);

            string r3 = get("User/" + id);

            r3 = r3.Replace(Environment.NewLine, "").Replace("\t", "").Replace(" ", "");

            Assert.AreEqual(r2, r3);

            string prototype = @"{ 
                ""id"": -1,
                ""FirstName"": """",
                ""LastName"": ""Klaxe"",
                ""tasks"": []
            }";


            string r4=post("User/SearchByPrototype", "POST", prototype);

            
            Assert.IsTrue(r4.Contains("Klaxe"));


            get("User/" + id, "DELETE");

            get("User/" + id);

            Assert.IsFalse(statuscode == 200);

        }


        
    }
}
