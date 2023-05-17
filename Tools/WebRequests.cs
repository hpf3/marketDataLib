using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace marketDataLib.Tools
{
    public static class WebRequests
    {
        //run a web request and return the response:GET
        public static string GetResponse(string url)
        {
            //start a httpclient
            using (var client = new System.Net.Http.HttpClient())
            {
                //send a get request to the url
                var response = client.GetAsync(url).Result;
                //if the response is successful
                if (response.IsSuccessStatusCode)
                {
                    //return the response as a string
                    return response.Content.ReadAsStringAsync().Result;
                }
                //if the response is not successful
                else
                {
                    //throw an exception
                    throw new Exception("Request failed");
                }
            }
        }
        //run a web request and return the response:POST
        public static string PostResponse(string url, string data)
        {
            //start a httpclient
            using (var client = new System.Net.Http.HttpClient())
            {
                //send a post request to the url
                var response = client.PostAsync(url, new System.Net.Http.StringContent(data)).Result;
                //if the response is successful
                if (response.IsSuccessStatusCode)
                {
                    //return the response as a string
                    return response.Content.ReadAsStringAsync().Result;
                }
                //if the response is not successful
                else
                {
                    //throw an exception
                    throw new Exception("Request failed");
                }
            }
        }
    }
}