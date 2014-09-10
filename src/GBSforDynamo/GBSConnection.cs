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


        // NODE: GBS-Get Project List
        /// <summary> 
        /// Returns Project Lists from GBS web service
        /// </summary> 
        /// <param name="Connect"> Set Boolean True </param>
        /// <returns name="ProjectIds"> Returns Project Ids in GBS Web Service List.</returns> 
        /// <returns name="ProjectTitles"> Returns Project Titles in GBS Web Service List.</returns> 
        /// <returns name="ProjectDateAdded"> Returns Project's date of added or created List.</returns> 
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


        // NODE: Create gbXML from Mass
        /// <summary> 
        /// Create gbXML file from Mass and saves to a local location 
        /// </summary>
        /// <param name="FilePath"> Specify the file path location to save gbXML file </param>
        /// <param name="MassFamilyInstance"> Input Mass Id </param>
        /// <param name="Run"> Set Boolean True. Default is false </param>
        /// <returns name="report"> Success? </returns>
        /// <returns name="gbXMLPath"></returns>
        [MultiReturn("report", "gbXMLPath")]
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

            //local variables
            Document RvtDoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument.Document;

            //enable the analytical model in the document if it isn't already
            try
            {
                TransactionManager.Instance.EnsureInTransaction(RvtDoc);
                EnergyDataSettings energyData = EnergyDataSettings.GetFromDocument(RvtDoc);
                if (energyData != null)
                {
                    energyData.SetCreateAnalyticalModel(true);
                }
                TransactionManager.Instance.TransactionTaskDone();
            }
            catch (Exception)
            {
                throw new Exception("Something went wrong when trying to enable the energy model.");
            }

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
                { "report", message},
                { "gbXMLPath", path} 
            };
        }


        // NODE: Create gbXML from Zones
        /// <summary>
        /// Exports gbXML file from Zones
        /// </summary>
        /// <param name="FilePath"> Specify the file path location to save gbXML file </param>
        /// <param name="ZoneIds"> Input Zone IDs</param>
        /// <param name="Run">Set Boolean True. Default is false </param>
        /// <returns name="report"> Success? </returns>
        /// <returns name="gbXMLPath"></returns>
        [MultiReturn("report", "gbXMLPath")]
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
                { "report", message},
                { "gbXMLPath", path} 
            };

        
        }

        // NODE: GBS_Base Run
        /// <summary>
        /// Creates a new Project and Uploads gbXML to run the energy analysis of base model and the alternates
        /// </summary>
        /// <param name="ProjectTitle"> Title of the project created in GBS Web Services, creates new if not created already </param>
        /// <param name="gbXMLPath"> File path location of gbXML file</param>
        /// <returns name="ProjectId"> Returns Project ID </returns>
        /// /// <returns name="RunId"> Returns  Run ID </returns>
        [MultiReturn("ProjectId", "RunId")]
        public static Dictionary<string, object> GBS_BaseRun(string ProjectTitle, string gbXMLPath)
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
                _CallPostApi(requestUri, typeof(NewProjectItem), _CreateProjectItem(ProjectTitle, false, BuildingTypeId, ScheduleId, lat, lon, utilityCost.ElecCost, utilityCost.FuelCost));
                
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

        // NODE: GBS_Get Run List
        /// <summary>
        /// Gets Run List from GBS Web Service
        /// </summary>
        /// <param name="ProjectId"> Input Project ID</param>
        /// <returns name = "RunIds"> Returns Run IDs </returns>
        /// <returns name = "AltRunIds"> Returns Alternate Run IDs </returns>
        /// <returns name = "RunNames"> Returns Run Names </returns>
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

        // NODE: GBS_ Get Run Summary Results
        /// <summary>
        /// Gets the Run Summary Results of given RunId
        /// </summary>
        /// <param name="RunId"> Input Run Id </param>
        /// <param name="AltRunId"> Input Alternate Run Id. Default is 0, Base Run </param>
        /// <returns name ="RunTitle"> Title of Run </returns>
        /// <returns name ="Location"> Location </returns>
        /// <returns name ="BuildingType"> Building Type</returns>
        /// <returns name ="ProjectTemplate"> Project Template Applied </returns>
        /// <returns name ="FloorArea"> Floor Area + Unit </returns>
        /// <returns name ="ElectricCost"> Electric Cost + Unit </returns>
        /// <returns name ="AnnualEnergyCost"> Annual Energy Cost + Unit </returns>
        /// <returns name ="LifecycleCost"> Life Cycle Cost + Unit </returns>
        /// <returns name ="AnnualCO2EmissionsElectric"> Annual CO2 Emissions Electric Cost + Unit </returns>
        /// <returns name ="AnnualCO2EmissionsOnsiteFuel"> Annual CO2 Emissions Onsite Fuel Cost + Unit </returns>
        /// <returns name ="AnnualCO2EmissionsLargeSUVEquivalent"> Annual CO2 Emissions Large SUV Equivalent Cost + Unit </returns>
        [MultiReturn("RunTitle", "Location", "BuildingType","ProjectTemplate","FloorArea", "ElectricCost", "AnnualEnergyCost","LifecycleCost","AnnualCO2EmissionsElectric","AnnualCO2EmissionsOnsiteFuel","AnnualCO2EmissionsLargeSUVEquivalent")]
        public static Dictionary<string, object> GetRunSummaryResult(int RunId , int AltRunId = 0)
        {
            // Initiate the Revit Auth
            InitRevitAuthProvider();

            //Get results Summary of given RunID & AltRunID
            string requestGetRunSummaryResultsUri = GBSUri.GBSAPIUri +
                                     string.Format(APIV1Uri.GetRunSummaryResultsUri, RunId, AltRunId, "json");
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
        /// Gets Results object and Building summary
        /// <para> Use .... nodes to parse the Results info of the specific run</para>
        /// </summary>
        /// <param name="RunID"> Input Run Id </param>
        /// <param name="AltRunID"> Input Alternate Id </param>
        /// <returns></returns>
        [MultiReturn("Results","BuildingType","Location","FloorArea","BuildingSummary")]
        public static Dictionary<string, object> GetEnergyandCarbonResults(int RunID, int AltRunID = 0)
        {
            // Initiate the Revit Auth
            InitRevitAuthProvider();

            //Get results Summary of given RunID & AltRunID
            string requestGetRunSummaryResultsUri = GBSUri.GBSAPIUri +
                                     string.Format(APIV1Uri.GetRunSummaryResultsUri, RunID, AltRunID, "json");
            HttpWebResponse response2 = (HttpWebResponse)_CallGetApi(requestGetRunSummaryResultsUri);
            Stream responseStream2 = response2.GetResponseStream();
            StreamReader reader2 = new StreamReader(responseStream2);
            string resultSummary = reader2.ReadToEnd();
            RunResultSummary runResultSummary = DataContractJsonDeserialize<RunResultSummary>(resultSummary);

            string buildingsummary = "Number of People : " + runResultSummary.BuildingSummary.NumberOfPeople.Value + " " + runResultSummary.BuildingSummary.NumberOfPeople.Units + ",\n" +
                "Average Lighting Power Density : " + runResultSummary.BuildingSummary.AvgLightingPowerDensity.Value + " " + runResultSummary.BuildingSummary.AvgLightingPowerDensity.Units + ",\n" +
                "Average Equipment Power Density : " + runResultSummary.BuildingSummary.AvgEquipmentPowerDensity.Value + " " + runResultSummary.BuildingSummary.AvgEquipmentPowerDensity.Units + ",\n" +
                "Specific Fan Flow : " + runResultSummary.BuildingSummary.SpecificFanFlow.Value + " " + runResultSummary.BuildingSummary.SpecificFanFlow.Units + ",\n" +
                "Specific Fan Power : " + runResultSummary.BuildingSummary.SpecificFanPower.Value + " " + runResultSummary.BuildingSummary.SpecificFanPower.Units + ",\n" +
                "Specific Cooling : " + runResultSummary.BuildingSummary.SpecificCooling.Value + " " + runResultSummary.BuildingSummary.SpecificCooling.Units + ",\n" +
                "Specific Heating : " + runResultSummary.BuildingSummary.SpecificHeating.Value + " " + runResultSummary.BuildingSummary.SpecificHeating.Units + ",\n" +
                "Total Fan Flow : " + runResultSummary.BuildingSummary.TotalFanFlow.Value + " " + runResultSummary.BuildingSummary.TotalFanFlow.Units + ",\n" +
                "Total Cooling Capacity : " + runResultSummary.BuildingSummary.TotalCoolingCapacity.Value + " " + runResultSummary.BuildingSummary.TotalCoolingCapacity.Units + ",\n" +
                "Total Heating Capacity : " + runResultSummary.BuildingSummary.TotalHeatingCapacity.Value + " " + runResultSummary.BuildingSummary.TotalHeatingCapacity.Units + ".\n";

            //Populate outputs
            return new Dictionary<string, object>
            {
                { "Results",runResultSummary},
                { "BuildingType", runResultSummary.BuildingType},
                { "Location", runResultSummary.Location},
                { "FloorArea", runResultSummary.FloorArea.Value + " " + runResultSummary.FloorArea.Units },
                { "BuildingSummary", buildingsummary}

            };
        }

        // NODE: Get Run Result TO DO: work with GBS Team about API calls
        /// <summary>
        /// Get Run Result 
        /// </summary>
        /// <param name="RunId"> Input Run ID</param>
        /// <param name="AltRunId"> Input Alternate Run ID </param>
        /// <param name="resulttype"> Result type gbxml or doe2 or inp </param>
        /// <param name="FilePath"> Set File location to download the file </param>
        /// <returns name="report"> string. </returns>
        public static string GetRunResult(int RunId, int AltRunId, string resulttype , string FilePath) // result type gbxml/doe2/eplus
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
                string zipFileName = Path.Combine(FilePath, string.Format("RunResults_{0}_{1}_{2}.zip", RunId, AltRunId, resulttype));

                using (var fs = File.Create(zipFileName))
                {
                    stream.CopyTo(fs);
                }
                
                if (File.Exists(zipFileName))
                { report = "The Analysis result file " + resulttype + " was successfully downloaded!"; }
  
            }

            return report ;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="ProjectId"></param>
        /// <param name="ProjectTitle"></param>
        /// <param name="Connect"></param>
        /// <returns></returns>
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




        //*************** PRIVATE METHODS ***************//

        // GBS Authentification 
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

        #region DataContracts Items Methods
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
                // Elcin: ElectCost, FuelCost, CultureInfo not required if no value should se the default values ! Ask GBS Team!
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
        #endregion

    }
}
