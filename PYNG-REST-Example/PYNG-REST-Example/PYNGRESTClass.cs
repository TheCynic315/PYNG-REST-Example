using System;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp; // For Basic SIMPL# Classes
using Crestron.SimplSharp.Net.Https;



namespace REST_Client
{
    /*This Library can be used with the PYNG REST API. It requires SSL to be enabled on the PYNG.
     You must locate the unique Authorization Token in the PYNG advanced setup.
     This Library does not handle login time outs and automatically logging back in.*/
    public class RestClient 
    {
        public string URL; //https://(hostname or ip}/cws/api/
        public string Path; // this is for anything that would come after the cws/api/ as shown in the REST API doc.
        public string AuthToken;
        private const string LogInAuthHeader = "Crestron-RestAPI-AuthToken"; 
        private const string AuthHeader = "Crestron-RestAPI-AuthKey";
        static string sAuthKey;
        static string _Response;
        static string _Rooms;
        static string _Devices;
        public static Rooms RoomsObj; //JSON object for ROOMS can be parsed for all information
        public static Devices DevicesObj; //JSON object for Devices can also be parsed.

        MyHttpsClient MyClient = new MyHttpsClient(); //This is the HTTPS client you will be using throughout.
        

        // Added two exta settings to the client constructor that are needed for HTTPS to function
        private class MyHttpsClient : HttpsClient
       {
           public MyHttpsClient()
           {
               this.PeerVerification = false;
               this.HostVerification = false;
           }
       }


        /*All Requests rquire the URL with the /cws/api apended. Path is anything that comes after /cws/api/. 
            _AuthHeader is either the constant LogInAuthHeader for logging in or the AuthHeader for all other commands
         _AuthString is the unique Authroization KEY sent back after log in*/
        private class MyHttpsClientRequest : HttpsClientRequest
       {
          public MyHttpsClientRequest(string _URL, string _Path, string _AuthHeader, string _AuthString)
           {
               string _uri = _URL + _Path;
               CrestronConsole.PrintLine(_uri);
               this.Url.Parse(_uri);
               this.Header.SetHeaderValue(_AuthHeader, _AuthString);
               this.Header.SetHeaderValue("Content-Transfer-Encoding", "chunked");
               this.Header.AddHeader(new HttpsHeader("Content-Type", "application/json"));
               this.Header.AddHeader(new HttpsHeader("Expect", ""));
           }
       }
       
        //Once we have logged in we need to find the KEY for the rest of our commands
        private string ParseForKey()
        {
            string _sAuthKey;
            try
           {
               JObject jResponse = JObject.Parse(_Response.TrimStart(' '));
               //For debuging
               CrestronConsole.PrintLine("Jobject is: {0}\n", jResponse);
               var _key = jResponse["authkey"];
               _sAuthKey = _key.ToString();
               return _sAuthKey;
           }
           catch (JsonReaderException e)
           {
               CrestronConsole.PrintLine("Json Reader exception: {0}\n", e.Message);
           }
           catch (JsonSerializationException e)
           {
               CrestronConsole.PrintLine("Json Serial exception: {0}\n", e.Message);
           }
           catch (Exception e)
           {
               CrestronConsole.PrintLine("General Exception in the Json part: {0}\n", e.Message);
           }
           return "";
        }


        /*This will take the returned ROOMS and DEVICES JOSN lists and put them into objects 
         for use later*/
        private void SerilizeFunction()
        {
            try
            {
                RoomsObj = JsonConvert.DeserializeObject<Rooms>(_Rooms);
            }
            catch (JsonSerializationException e)
            {
                CrestronConsole.PrintLine("Rooms Obj exception: {0}\n", e.Message);
            }
            try
            {
                DevicesObj = JsonConvert.DeserializeObject<Devices>(_Devices);
            }
            catch (JsonSerializationException e)
            {
                CrestronConsole.PrintLine("Device Obj exception: {0}\n", e.Message);
            }
            //For Debugging REMOVE BEFORE FLIGHT!
            CrestronConsole.PrintLine(_Rooms);
            CrestronConsole.PrintLine(_Devices);

        }

        //First thing you need to do before anything else will work.
        public void LogIn()
        {
           try
           {
               MyHttpsClientRequest LogIn = new MyHttpsClientRequest(URL, "login", LogInAuthHeader, AuthToken);
               LogIn.RequestType = RequestType.Get;
               HttpsClientResponse LogInResponse;
               LogInResponse = MyClient.Dispatch(LogIn);
               _Response = LogInResponse.ContentString;
           }
           catch (HttpsException e)
           {
               CrestronConsole.PrintLine("HTTPS Exception: {0}\n", e.Message);
           }
           catch (HttpsHeaderException e)
           {
               CrestronConsole.PrintLine("Header Execption: {0}\n", e.Message);
           }
           catch (HttpsRequestInvalidException e)
           {
               CrestronConsole.PrintLine("HTTPS Request Exception: {0}\n", e.Message);
           }
           sAuthKey = ParseForKey();


            /*The Document says to make these two requests before any other calls
             so here they are and they are then parsed into classes for easy access*/
           try
           {
               MyHttpsClientRequest RequestRooms = new MyHttpsClientRequest(URL, "rooms", AuthHeader, sAuthKey);
               RequestRooms.RequestType = RequestType.Get;
               HttpsClientResponse RoomsResponse;
               RoomsResponse = MyClient.Dispatch(RequestRooms);
               _Rooms = RoomsResponse.ContentString;

               MyHttpsClientRequest RequestDevices = new MyHttpsClientRequest(URL, "devices", AuthHeader, sAuthKey);
               RequestDevices.RequestType = RequestType.Get;
               HttpsClientResponse DevicesResponse;
               DevicesResponse = MyClient.Dispatch(RequestDevices);
               _Devices = DevicesResponse.ContentString;
           }
           catch (HttpsException e)
           {
               CrestronConsole.PrintLine("HTTPS Exception: {0}\n", e.Message);
           }
           catch (HttpsHeaderException e)
           {
               CrestronConsole.PrintLine("Header Execption: {0}\n", e.Message);
           }
           catch (HttpsRequestInvalidException e)
           {
               CrestronConsole.PrintLine("HTTPS Request Exception: {0}\n", e.Message);
           }

           SerilizeFunction();
       }
        

        /*This Method will set the LIGHT at _id to any level between 0-65535 in 100 miliseconds
         _id can be found in the _Devices print out. These IDs can be parsed out with this Library but 
         an example is not provided*/
        public void LightsExample(ushort _id, ushort level)
        {
           string _Post = "{\"lights\": [ { \"" + _id + "\": 01 , \"level\": " + level + 
               ", \"time\": 100}]}";
           CrestronConsole.PrintLine(_Post);
           MyHttpsClientRequest LightRequest = new MyHttpsClientRequest(URL, Path, AuthHeader, sAuthKey);
           LightRequest.RequestType=RequestType.Post;
           LightRequest.ContentString = _Post;
           try
           {
               HttpsClientResponse PostResponse = MyClient.Dispatch(LightRequest);
           }
           catch (HttpsException e)
           {
               CrestronConsole.PrintLine("Post error: {0}\n", e.Message);
           }
        }
        
        /*Classes for the JSON Serialization. Use these to find the ID of the item you want to control.*/
        public class Room
        {
            public ushort id { get; set; }
            public string name { get; set; }
        }

        public class Rooms
        {
            public IList<Room> rooms { get; set; }
            public string version { get; set; }
        }

        public class Device
        {
            public ushort id { get; set; }
            public string name { get; set; }
            public string type { get; set; }
            public string subType { get; set; }
            public ushort roomId { get; set; }
        }

        public class Devices
        {
            public IList<Device> devices { get; set; }
            public string version { get; set; }
        }
      
    }
}