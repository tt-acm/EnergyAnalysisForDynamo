using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Text;
using System.Net;
using System.Windows.Threading;

// Serialization
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

//Autodesk
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.DesignScript.Runtime;

//Dynamo
using DSCore;
using DSCoreNodesUI;
using Dynamo.Models;
using Dynamo.Nodes;
using Dynamo.Utilities;
using ProtoCore.AST.AssociativeAST;
using RevitServices.Persistence;
using RevitServices.Transactions;
using ProtoCore;
using ProtoCore.Utils;

// 
using RevitRaaS;
using RevitServices;

using Dynamo.Controls;
using RevitServices.Elements;
using Dynamo.Applications;
using Dynamo;
using DynamoUtilities;


//Greg Graph registry
using Greg;
using RestSharp;


//AuthHelper
using GBSforDynamoAuthHelper;

//DataContract
using GBSforDynamo.DataContracts;
using Revit.Elements;
using System.Xml.Linq;


namespace GBSforDynamo
{
    public static class GBSConnection
    {
        //RevitAuthProvider
        private static RevitAuthProvider revitAuthProvider;

        // Check if currently logged-in
        //https://github.com/DynamoDS/Dynamo/blob/a34344e4b06c9194b44afeb22d8bce76f66aef14/src/DynamoRevit/DynamoRevit.cs
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static bool IsLoggedin()
        {
            RaaSClient client = new RaaSClient(DocumentManager.Instance.CurrentUIApplication);
            if (!client.IsLoggedIn())
            {
              client.ShowLoginDialog(); // This crashes in Vasari, good at Revit 201
            }
            return client.IsLoggedIn();
        }

        /// <summary>
        /// Returns Project Lists from GBS web service
        /// </summary>
        /// <returns></returns>
        [MultiReturn("ProjectIds", "ProjectTitles","ProjectDateAdded")]
        public static Dictionary<string, object> GetProjectLists(bool Connect = false )
        {
            //Local Output variables 
            List<int> ProjectIds = new List<int>();
            List<string> ProjectTitles = new List<string>();
            List<DateTime?> DateAdded = new List<DateTime?>();

            //make Connect? inputs set to True mandatory
            if (Connect == false)
            {
               throw new Exception("Set 'Connect' to True!");
            }

            // Initiate the Revit Auth
            InitRevitAuthProvider();

            // Request 
            string requestUri = GBSUri.GBSAPIUri + string.Format(APIV1Uri.GetProjectList, "json");

            HttpWebResponse response = (HttpWebResponse)_CallGetApi(requestUri);
            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream);
            string result = reader.ReadToEnd();
            List<Project> projectList = DataContractJsonDeserialize<List<Project>>(result);
            int count = projectList.Count;

            for (int i = 0; i < count; i++)
            {
                ProjectIds.Add(projectList[i].Id);
                ProjectTitles.Add(projectList[i].Title);
                DateAdded.Add(projectList[i].DateAdded); // output date object 
            }

            return new Dictionary<string, object>
            {
                { "ProjectIds", ProjectIds},
                { "ProjectTitles", ProjectTitles}, 
                { "ProjectDateAdded",  DateAdded}
            };

        }

        /// <summary>
        /// Exporting gbXML file and saving to local location
        /// </summary>
        /// <returns></returns>
        [MultiReturn("message", "gbXMLPath")]
        public static Dictionary<string, object> gbXMLCompiler_fromMass(string FilePath, AbstractFamilyInstance MassFamilyInstance = null, Boolean Run = false)
        {
            Boolean IsSuccess = false;

            string FileName = Path.GetFileNameWithoutExtension(FilePath);
            string Folder = Path.GetDirectoryName(FilePath);

            //make RUN? inputs set to True mandatory
            if (Run == false)
            {
               throw new Exception("Set 'Connect' to True!");
            }

             //make mass instance and levels mandatory inputs
            if (MassFamilyInstance == null)
            {
                throw new Exception("MassFamily Instance are mandatory inputs");
            }

            //local varaibles
            Document RvtDoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument.Document;
            //get the id of the analytical model associated with that mass
            ElementId myEnergyModelId = MassEnergyAnalyticalModel.GetMassEnergyAnalyticalModelIdForMassInstance(RvtDoc, MassFamilyInstance.InternalElement.Id);
            MassEnergyAnalyticalModel mea = (MassEnergyAnalyticalModel)RvtDoc.GetElement(myEnergyModelId);
            ICollection<ElementId> ZoneIds = mea.GetMassZoneIds();

            MassGBXMLExportOptions gbXmlExportOptions = new MassGBXMLExportOptions(ZoneIds.ToList()); // two constructors 

            RvtDoc.Export(Folder, FileName, gbXmlExportOptions);
            
            // if the file exists return success message if not return failed message
            string path = Path.Combine(Folder, FileName + ".xml");

            if (System.IO.File.Exists(path))
            {
                IsSuccess = true;
            }
            string message = "Failed to create gbXML file!";

            if (IsSuccess)
            {
                message = "Success! The gbXML file was created";
            }
            else
            {
                path = string.Empty;
            }

            // Populate Output Values
            return new Dictionary<string, object>
            {
                { "message", message},
                { "gbXMLPath", path} 
            };
        }

        /// <summary>
        /// Exporting gbXML file from giving
        /// </summary>
        /// <param name="FilePath"></param>
        /// <param name="FileName"></param>
        /// <param name="ZoneIds"></param>
        /// <param name="Run"></param>
        /// <returns></returns>
        [MultiReturn("message", "gbXMLPath")]
        public static Dictionary<string, object> gbXMLCompiler_fromZones(string FilePath, List<ElementId> ZoneIds = null, Boolean Run = false)
        {
            Boolean IsSuccess = false;

            string FileName = Path.GetFileNameWithoutExtension(FilePath);
            string Folder = Path.GetDirectoryName(FilePath);

            //make RUN? inputs set to True mandatory
            if (Run == false)
            {
                throw new Exception("Set 'Connect' to True!");
            }

            //make mass instance and levels mandatory inputs
            if (ZoneIds == null)
            {
                throw new Exception("MassFamily Instance are mandatory inputs");
            }

            //local varaibles
            Document RvtDoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument.Document;

            // Create gbXML
            MassGBXMLExportOptions gbXmlExportOptions = new MassGBXMLExportOptions(ZoneIds);  

            RvtDoc.Export(Folder, FileName, gbXmlExportOptions);


            // if the file exists return success message if not return failed message
            string path = Path.Combine(Folder, FileName + ".xml");

            if (System.IO.File.Exists(path))
            {
                IsSuccess = true;
            }
            string message = "Failed to create gbXML file!";

            if (IsSuccess)
            {
                message = "Success! The gbXML file was created";
            }
            else 
            {
                path = string.Empty;
            }


            return new Dictionary<string, object>
            {
                { "message", message},
                { "gbXMLPath", path} 
            };

        
        }

        /// <summary>
        /// Creates Project and Post gbXML to Run 
        /// </summary>
        /// <param name="ProjectTitle"></param>
        /// <param name="IsDemo"></param>
        /// <param name="BuildingTypeId"></param>
        /// <param name="ScheduleId"></param>
        /// <param name="gbXMLPath"></param>
        /// <returns></returns>
        [MultiReturn("ProjectId", "RunId")]
        public static Dictionary<string, object> GBS_BaseRun(string ProjectTitle, Boolean IsDemo, string gbXMLPath)
        {
            int newProjectId = 0;
            int newRunId = 0;

            //local variable to get SiteLocation and Lat & Lon information
            Document RvtDoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument.Document;

            //Load the default energy setting from the active Revit instance
            EnergyDataSettings myEnergySettings = Autodesk.Revit.DB.Analysis.EnergyDataSettings.GetFromDocument(RvtDoc);

            // get BuildingType and ScheduleId from document
            int BuildingTypeId = (int) myEnergySettings.BuildingType;
            int ScheduleId = (int) myEnergySettings.BuildingOperatingSchedule;
            
            // Angles are in Rdaians when coming from revit API
            //Convert to display
            const double angleRatio = Math.PI / 180; // angle conversion factor

            double lat = RvtDoc.SiteLocation.Latitude / angleRatio;
            double lon = RvtDoc.SiteLocation.Longitude / angleRatio;
            
            // Initiate the Revit Auth
            InitRevitAuthProvider();

            // Try to get Default Utility Costs from API
            string requestGetDefaultUtilityCost = GBSUri.GBSAPIUri + APIV1Uri.GetDefaultUtilityCost;
            string requestUriforUtilityCost = string.Format(requestGetDefaultUtilityCost, BuildingTypeId, lat, lon, "xml");
            HttpWebResponse responseUtility = (HttpWebResponse)_CallGetApi(requestUriforUtilityCost);

            string theresponse = "";
            using (Stream responseStream = responseUtility.GetResponseStream())
            {
                using (StreamReader streamReader = new StreamReader(responseStream))
                {
                    theresponse = streamReader.ReadToEnd();
                }
            }
            DefaultUtilityItem utilityCost = DataContractDeserialize<DefaultUtilityItem>(theresponse);

            // Create A Project
            string requestUri = GBSUri.GBSAPIUri + string.Format(APIV1Uri.CreateProjectUri, "xml");

            var response =
                (HttpWebResponse)
                _CallPostApi(requestUri, typeof(NewProjectItem), _CreateProjectItem(ProjectTitle, IsDemo, BuildingTypeId, ScheduleId, lat, lon, utilityCost.ElecCost, utilityCost.FuelCost));
                
            newProjectId = DeserializeHttpWebResponse(response);

            //Create A Base Run

            string requestCreateBaseRunUri = GBSUri.GBSAPIUri + string.Format(APIV1Uri.CreateBaseRunUri,"xml");

            var response2 =
                (HttpWebResponse)
                 _CallPostApi(requestCreateBaseRunUri, typeof(NewRunItem), _GetNewRunItem(newProjectId, gbXMLPath));
                newRunId = DeserializeHttpWebResponse(response2);


            return new Dictionary<string, object>
            {
                { "ProjectId", newProjectId},
                { "RunId", newRunId} 
            };
        }



        // GBS Auth private methods
        private static void InitRevitAuthProvider()
        {
            SingleSignOnManager.RegisterSingleSignOn();
            revitAuthProvider = revitAuthProvider ?? new RevitAuthProvider(SynchronizationContext.Current);
        }

        #region API Requests

        private static WebResponse _CallGetApi(string requestUri)
        {
            // Sign URL using Revit auth
            var signedRequestUri = revitAuthProvider.SignRequest(requestUri, HttpMethod.Get, null);

            // Send request to GBS
            System.Net.WebRequest request = System.Net.WebRequest.Create(signedRequestUri);
            WebResponse response = request.GetResponse();

            return response;
        }

        private static WebResponse _CallPostApi(string requestUri, System.Type type, object o)
        {
            var s = new DataContractSerializer(type);
            string postString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                s.WriteObject(stream, o);
                postString = Encoding.UTF8.GetString(stream.ToArray());
            }

            // Sign URL using Revit auth
            var signedRequestUri = revitAuthProvider.SignRequest(requestUri, HttpMethod.Post, null);

            // Send request to GBS
            var request = (HttpWebRequest)System.Net.WebRequest.Create(signedRequestUri);
            request.Method = "POST";
            request.ContentType = "application/xml";
            using (Stream requestStream = request.GetRequestStream())
            using (StreamWriter requestWriter = new StreamWriter(requestStream))
            {
                requestWriter.Write(postString);
            }

            // get response
            WebResponse response = request.GetResponse();
            return response;
        }

        #endregion

        #region Serialize/ Deserialize 

        static T DataContractJsonDeserialize<T>(string response)
        {
            using (MemoryStream stream = new MemoryStream(System.Text.Encoding.Default.GetBytes(response)))
            {
                DataContractJsonSerializer serialize = new DataContractJsonSerializer(typeof(T));
                return (T)serialize.ReadObject(stream);
            }
        }

        private static int DeserializeHttpWebResponse(HttpWebResponse response)
        {
            var theresponse = "";
            using (Stream responseStream = response.GetResponseStream())
            {
                using (var streamReader = new StreamReader(responseStream))
                {
                    theresponse = streamReader.ReadToEnd();
                }
            }
            var newId = DataContractDeserialize<int>(theresponse);
            return newId;
        }

        private static T DataContractDeserialize<T>(string response)
        {
            using (var stream = new MemoryStream(Encoding.Default.GetBytes(response)))
            {
                var serialize = new DataContractSerializer(typeof(T));
                return (T)serialize.ReadObject(stream);
            }
        }

        #endregion

        private static NewProjectItem _CreateProjectItem(string title, Boolean demo, int bldgTypeId, int scheduleId, double lat, double lon, float electCost, float fuelcost)
        {

            var newProject = new NewProjectItem
            {
                Title = title,
                Demo = demo,
                BuildingTypeId = bldgTypeId,
                ScheduleId = scheduleId,
                Latitude = lat,
                Longitude = lon,
                CultureInfo = "en-US",
                ElecCost = electCost,
                FuelCost = fuelcost
                // ElectCost, FuelCost, CultureInfo not required if no value should se the default values ! Ask GBS Team!
            };

            return newProject;
        }

        private static NewRunItem _GetNewRunItem(int projectId, string gbxmlFile)
        {
            string FileName = Path.GetFileName(gbxmlFile);
            string Folder = Path.GetDirectoryName(gbxmlFile);
            string ZipFileName = Path.GetFileNameWithoutExtension(gbxmlFile);

            string ZipFullPath = Path.Combine(Folder, ZipFileName + ".zip");
            //ZipUtil.ZipFile(path, gbxmlFile, gbxmlFile + ".zip", "95401", "Office");
            //ZipUtil.ZipFile(path, gbxmlFile, gbxmlFile + ".zip", "95401", "BuildingType");

            ZipUtil.ZipFile(gbxmlFile, ZipFullPath);
            byte[] fileBuffer = System.IO.File.ReadAllBytes(ZipFullPath);
            string gbXml64 = Convert.ToBase64String(fileBuffer);
            
            var newRun = new NewRunItem
            {
                Title = Path.GetFileName(gbxmlFile),
                ProjectId = projectId,
                Base64EncodedGbxml = gbXml64,
                UtilityId = string.Empty
            };

            return newRun;
        
        }
    }
}
