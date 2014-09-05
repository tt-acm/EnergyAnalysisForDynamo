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
using Dynamo.Controls;
using RevitServices.Elements;
using Dynamo;
using DynamoUtilities;

//Revit Services
using RevitServices;

//AuthHelper
using GBSforDynamoAuthHelper;

//DataContract
using GBSforDynamo.DataContracts;
using Revit.Elements;
using System.Xml.Linq;
using System.Diagnostics;


namespace GBSforDynamo
{
    public static class GBSConnection
    {
        //RevitAuthProvider
        private static RevitAuthProvider revitAuthProvider;

        //// Check if currently logged-in
        ////https://github.com/DynamoDS/Dynamo/blob/a34344e4b06c9194b44afeb22d8bce76f66aef14/src/DynamoRevit/DynamoRevit.cs
        ///// <summary>
        ///// 
        ///// </summary>
        ///// <returns></returns>
        //public static bool IsLoggedin()
        //{
        //    RaaSClient client = new RaaSClient(DocumentManager.Instance.CurrentUIApplication);
        //    if (!client.IsLoggedIn())
        //    {
        //      client.ShowLoginDialog(); // This crashes in Vasari, good at Revit 201
        //    }
        //    return client.IsLoggedIn();
        //}

        /// GBS-Get Project List
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


        [MultiReturn("INPFile", "IDFFile")]
        public static Dictionary<string, object> GetEnergyModelFiles(int ProjectId, string ProjectTitle, bool Connect = false)
        {
            //local variables
            string INPFile = string.Empty;
            string IDFFile = string.Empty;

            //make Connect? inputs set to True mandatory
            if (Connect == false)
            {
                throw new Exception("Set 'Connect' to True!");
            }

            // defense


            // Initiate the Revit Auth
            InitRevitAuthProvider();

            /*
            // Request - I'm still not sure how to create the request - should know more after today meeting
            string requestUri = GBSUri.GBSAPIUri + string.Format(APIV1Uri.GetProjectList, "json");

            HttpWebResponse response = (HttpWebResponse)_CallGetApi(requestUri);
            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream);
            string result = reader.ReadToEnd();
            */

            return new Dictionary<string, object>
            {
                { "INPFile", INPFile},
                { "IDFFile", IDFFile}
            };

        }

        /// Create gbXML from Mass
        /// Exporting gbXML file and saving to a local location
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

        /// Create gbXML from Zones
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

        /// GBS_Base Run
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

        /// GBS_Get Run List
        /// 
        /// </summary>
        /// <param name="ProjectId"></param>
        /// <returns></returns>
        [MultiReturn("RunIds", "AltRunIds", "RunNames")]
        public static Dictionary<string, object> GetRunList(int ProjectId)
        {
            // Initiate the Revit Auth
            InitRevitAuthProvider();

            string requestUri = GBSUri.GBSAPIUri + string.Format(APIV1Uri.GetProjectRunListUri, ProjectId.ToString(), "json");
            HttpWebResponse response = (HttpWebResponse)_CallGetApi(requestUri);
            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream);
            string projectRunListJson = reader.ReadToEnd();

            //TextWriterTraceListener tr2 = new TextWriterTraceListener(System.IO.File.CreateText("C:\\00_demo\\Output.txt"));
            //Debug.Listeners.Add(tr2);
            Debug.WriteLine(projectRunListJson);
            Debug.Flush();

            List<ProjectRun> projectRuns = DataContractJsonDeserialize<List<ProjectRun>>(projectRunListJson);

            List<int> runIds = new List<int>();
            List<int> altRunIds = new List<int>();
            List<string> Names = new List<string>();

            foreach (var run in projectRuns)
            {
                runIds.Add(run.runId);
                altRunIds.Add(run.altRunId);
                Names.Add(run.name);
            }

            //Populate outputs
            return new Dictionary<string, object>
            {
                { "RunIds", runIds},
                { "AltRunIds", altRunIds},
                { "RunNames", Names}

            };
        
        }

        /// GBS_ Get Run Summary Results
        /// 
        /// gets the Run Summary results of given RunId
        /// </summary>
        /// <param name="RunId"></param>
        /// <returns></returns>
        [MultiReturn("RunTitle", "Location", "BuildingType","ProjectTemplate","FloorArea", "ElectricCost", "AnnualEnergyCost","LifecycleCost","AnnualCO2EmissionsElectric","AnnualCO2EmissionsOnsiteFuel","AnnualCO2EmissionsLargeSUVEquivalent")]
        public static Dictionary<string, object> GetRunSummaryResult(int RunId)
        {
            // Initiate the Revit Auth
            InitRevitAuthProvider();

            //Get results Summary of given RunID
            string requestGetRunSummaryResultsUri = GBSUri.GBSAPIUri +
                                     string.Format(APIV1Uri.GetRunSummaryResultsUri, RunId, 0, "json");
            HttpWebResponse response2 = (HttpWebResponse)_CallGetApi(requestGetRunSummaryResultsUri);
            Stream responseStream2 = response2.GetResponseStream();
            StreamReader reader2 = new StreamReader(responseStream2);
            string resultSummary = reader2.ReadToEnd();
            RunResultSummary runResultSummary = DataContractJsonDeserialize<RunResultSummary>(resultSummary);

            //Populate outputs
            return new Dictionary<string, object>
            {
                { "RunTitle", runResultSummary.Runtitle},
                { "Location", runResultSummary.Location},
                { "BuildingType", runResultSummary.BuildingType},
                { "ProjectTemplate", runResultSummary.ProjectTemplateApplied},
                { "FloorArea", runResultSummary.FloorArea.Value + runResultSummary.FloorArea.Units },
                { "ElectricCost", runResultSummary.ElectricCost.Value + runResultSummary.ElectricCost.Units },
                { "AnnualEnergyCost", runResultSummary.RunEnergyCarbonCostSummary.AnnualEnergyCost },
                { "LifecycleCost", runResultSummary.RunEnergyCarbonCostSummary.LifecycleCost},
                {"AnnualCO2EmissionsElectric", runResultSummary.RunEnergyCarbonCostSummary.AnnualCO2EmissionsElectric.Value + runResultSummary.RunEnergyCarbonCostSummary.AnnualCO2EmissionsElectric.Units},
                {"AnnualCO2EmissionsOnsiteFuel",runResultSummary.RunEnergyCarbonCostSummary.AnnualCO2EmissionsOnsiteFuel.Value + runResultSummary.RunEnergyCarbonCostSummary.AnnualCO2EmissionsOnsiteFuel.Units},
                {"AnnualCO2EmissionsLargeSUVEquivalent", runResultSummary.RunEnergyCarbonCostSummary.AnnualCO2EmissionsLargeSUVEquivalent.Value + runResultSummary.RunEnergyCarbonCostSummary.AnnualCO2EmissionsLargeSUVEquivalent.Units}

            };
        
        }

        /// <summary>
        /// Get Run Result 
        /// </summary>
        /// <param name="RunId"></param>
        public static string GetRunResult(int RunId, int AltRunId, string resulttype , string FilePath) // result type gbxml doe2 etc
        {
            // Initiate the Revit Auth
            InitRevitAuthProvider();

            // report
            string report = " The request is failed!";

            // Get result of given RunId
            string requestGetRunResultsUri = GBSUri.GBSAPIUri +
                                    string.Format(APIV1Uri.GetRunResultsUri, RunId, AltRunId, resulttype);

            using (HttpWebResponse response = (HttpWebResponse)_CallGetApi(requestGetRunResultsUri))
            using (Stream stream = response.GetResponseStream())
            {
                string zipFileName = Path.Combine(FilePath, string.Format("RunResults_{0}_{1}.zip", RunId, 0));

                using (var fs = File.Create(zipFileName))
                {
                    stream.CopyTo(fs);
                }
                
                if (File.Exists(zipFileName))
                { report = "The Analysis result file " + resulttype + " was successfully downloaded!"; }
  
            }

            return report ;
        }


        // PRIVATE METHODS

        // GBS Authentification private methods
        private static void InitRevitAuthProvider()
        {
            SingleSignOnManager.RegisterSingleSignOn();
            revitAuthProvider = revitAuthProvider ?? new RevitAuthProvider(SynchronizationContext.Current);
        }

        #region API Web Requests

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
