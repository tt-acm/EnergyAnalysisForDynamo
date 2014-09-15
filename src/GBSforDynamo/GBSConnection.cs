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


        // NODE: Create new Project
        /// <summary>
        /// Creates new project in GBS Webservices, returns new Project ID
        /// </summary>
        /// <param name="ProjectTitle"> Title of the project </param>
        /// <returns></returns>
        [MultiReturn("ProjectId")]
        public static Dictionary<string, int> Create_NewProject(string ProjectTitle)
        {
            //Output variable
            int newProjectId = 0;

            #region Setup : Get values from current Revit document

            //local variable to get SiteLocation and Lat & Lon information
            Document RvtDoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument.Document;

            //Load the default energy setting from the active Revit instance
            EnergyDataSettings myEnergySettings = Autodesk.Revit.DB.Analysis.EnergyDataSettings.GetFromDocument(RvtDoc);

            // get BuildingType and ScheduleId from document
            int BuildingTypeId = (int)myEnergySettings.BuildingType;
            int ScheduleId = (int)myEnergySettings.BuildingOperatingSchedule;

            // Angles are in Rdaians when coming from revit API
            // Convert to lat & lon values 
            const double angleRatio = Math.PI / 180; // angle conversion factor

            double lat = RvtDoc.SiteLocation.Latitude / angleRatio;
            double lon = RvtDoc.SiteLocation.Longitude / angleRatio;

            #endregion

            #region Setup : Get default Utility Values

            //1. Initiate the Revit Auth
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

            #endregion

            // 2.  Create A New  Project
            string requestUri = GBSUri.GBSAPIUri + string.Format(APIV1Uri.CreateProjectUri, "xml");

            var response =
                (HttpWebResponse)
                _CallPostApi(requestUri, typeof(NewProjectItem), _CreateProjectItem(ProjectTitle, false, BuildingTypeId, ScheduleId, lat, lon, utilityCost.ElecCost, utilityCost.FuelCost));

            newProjectId = DeserializeHttpWebResponse(response);


            // 3. Populate the Outputs
            return new Dictionary<string, int>
            {
                { "ProjectId", newProjectId}
            };
        }


        // NODE: Create Base Run
        /// <summary>
        /// Creates Base Run and returns Base RunId
        /// </summary>
        /// <param name="ProjectId"> Input Project ID </param>
        /// <param name="gbXMLPath"> Input file path of gbXML File </param>
        /// <returns></returns>
        [MultiReturn("RunId")]
        public static Dictionary<string, int> Create_BaseRun(int ProjectId, string gbXMLPath)
        {
            // Make sure the given file is an .xml
            string extention = Path.GetExtension(gbXMLPath);
            if (extention != ".xml")
            {
                throw new Exception("Make sure to input gbxml file");
            }

            //Output variable
            int newRunId = 0;

            // 1. Initiate the Revit Auth
            InitRevitAuthProvider();

            // 2. Create A Base Run
            string requestCreateBaseRunUri = GBSUri.GBSAPIUri + string.Format(APIV1Uri.CreateBaseRunUri, "xml");

            var response =
                (HttpWebResponse)
                 _CallPostApi(requestCreateBaseRunUri, typeof(NewRunItem), _GetNewRunItem(ProjectId, gbXMLPath));
            newRunId = DeserializeHttpWebResponse(response);

            // 3. Populate the Outputs
            return new Dictionary<string, int>
            {
                { "RunId", newRunId},
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
            List<List<int>> AltRunIds = new List<List<int>>();
            List<List<string>> RunNames = new List<List<string>>();

            //
            foreach (var run in projectRuns)
            {
                if (!runIds.Contains(run.runId))
                {
                    runIds.Add(run.runId);
                } 
            }
            
            // Foreach runId Linq query on Projects Run
            foreach (var runId in runIds)
            {
                //Local variables
                List<int> altRunIds = new List<int>();
                List<string> Names = new List<string>();


                //linq query
                var runs = from run in projectRuns
                           where run.runId == runId
                           select run;

                foreach (var item in runs)
                {
                    altRunIds.Add(item.altRunId);
                    Names.Add(item.name);
                }

                AltRunIds.Add(altRunIds);
                RunNames.Add(Names);
            }


            //Populate outputs
            return new Dictionary<string, object>
            {
                { "RunIds", runIds}, // List
                { "AltRunIds", AltRunIds}, // Array
                { "RunNames", RunNames} // Array

            };
        
        }

        //// NODE: GBS_ Get Run Summary Results
        ///// <summary>
        ///// Gets the Run Summary Results of given RunId
        ///// </summary>
        ///// <param name="RunId"> Input Run Id </param>
        ///// <param name="AltRunId"> Input Alternate Run Id. Default is 0, Base Run </param>
        ///// <returns name ="RunTitle"> Title of Run </returns>
        ///// <returns name ="Location"> Location </returns>
        ///// <returns name ="BuildingType"> Building Type</returns>
        ///// <returns name ="ProjectTemplate"> Project Template Applied </returns>
        ///// <returns name ="FloorArea"> Floor Area + Unit </returns>
        ///// <returns name ="ElectricCost"> Electric Cost + Unit </returns>
        ///// <returns name ="AnnualEnergyCost"> Annual Energy Cost + Unit </returns>
        ///// <returns name ="LifecycleCost"> Life Cycle Cost + Unit </returns>
        ///// <returns name ="AnnualCO2EmissionsElectric"> Annual CO2 Emissions Electric Cost + Unit </returns>
        ///// <returns name ="AnnualCO2EmissionsOnsiteFuel"> Annual CO2 Emissions Onsite Fuel Cost + Unit </returns>
        ///// <returns name ="AnnualCO2EmissionsLargeSUVEquivalent"> Annual CO2 Emissions Large SUV Equivalent Cost + Unit </returns>
        //[MultiReturn("RunTitle", "Location", "BuildingType","ProjectTemplate","FloorArea", "ElectricCost", "AnnualEnergyCost","LifecycleCost","AnnualCO2EmissionsElectric","AnnualCO2EmissionsOnsiteFuel","AnnualCO2EmissionsLargeSUVEquivalent")]
        //public static Dictionary<string, object> GetRunSummaryResult(int RunId , int AltRunId = 0)
        //{
        //    // Initiate the Revit Auth
        //    InitRevitAuthProvider();

        //    //Get results Summary of given RunID & AltRunID
        //    string requestGetRunSummaryResultsUri = GBSUri.GBSAPIUri +
        //                             string.Format(APIV1Uri.GetRunSummaryResultsUri, RunId, AltRunId, "json");
        //    HttpWebResponse response2 = (HttpWebResponse)_CallGetApi(requestGetRunSummaryResultsUri);
        //    Stream responseStream2 = response2.GetResponseStream();
        //    StreamReader reader2 = new StreamReader(responseStream2);
        //    string resultSummary = reader2.ReadToEnd();
        //    RunResultSummary runResultSummary = DataContractJsonDeserialize<RunResultSummary>(resultSummary);

        //    //Populate outputs
        //    return new Dictionary<string, object>
        //    {
        //        { "RunTitle", runResultSummary.Runtitle},
        //        { "Location", runResultSummary.Location},
        //        { "BuildingType", runResultSummary.BuildingType},
        //        { "ProjectTemplate", runResultSummary.ProjectTemplateApplied},
        //        { "FloorArea", runResultSummary.FloorArea.Value + runResultSummary.FloorArea.Units },
        //        { "ElectricCost", runResultSummary.ElectricCost.Value + runResultSummary.ElectricCost.Units },
        //        { "AnnualEnergyCost", runResultSummary.RunEnergyCarbonCostSummary.AnnualEnergyCost },
        //        { "LifecycleCost", runResultSummary.RunEnergyCarbonCostSummary.LifecycleCost},
        //        {"AnnualCO2EmissionsElectric", runResultSummary.RunEnergyCarbonCostSummary.AnnualCO2EmissionsElectric.Value + runResultSummary.RunEnergyCarbonCostSummary.AnnualCO2EmissionsElectric.Units},
        //        {"AnnualCO2EmissionsOnsiteFuel",runResultSummary.RunEnergyCarbonCostSummary.AnnualCO2EmissionsOnsiteFuel.Value + runResultSummary.RunEnergyCarbonCostSummary.AnnualCO2EmissionsOnsiteFuel.Units},
        //        {"AnnualCO2EmissionsLargeSUVEquivalent", runResultSummary.RunEnergyCarbonCostSummary.AnnualCO2EmissionsLargeSUVEquivalent.Value + runResultSummary.RunEnergyCarbonCostSummary.AnnualCO2EmissionsLargeSUVEquivalent.Units}

        //    };
        
        //}


        /// <summary>
        /// Gets Results object and Building summary
        /// </summary>
        /// <para> Use .... nodes to parse the Results info of the specific run</para>
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

            string buildingsummary = "Number of People : " + runResultSummary.BuildingSummary.NumberOfPeople.Value + " " + runResultSummary.BuildingSummary.NumberOfPeople.Units + "\n" +
                "Average Lighting Power Density : " + runResultSummary.BuildingSummary.AvgLightingPowerDensity.Value + " " + runResultSummary.BuildingSummary.AvgLightingPowerDensity.Units + "\n" +
                "Average Equipment Power Density : " + runResultSummary.BuildingSummary.AvgEquipmentPowerDensity.Value + " " + runResultSummary.BuildingSummary.AvgEquipmentPowerDensity.Units + "\n" +
                "Specific Fan Flow : " + runResultSummary.BuildingSummary.SpecificFanFlow.Value + " " + runResultSummary.BuildingSummary.SpecificFanFlow.Units + "\n" +
                "Specific Fan Power : " + runResultSummary.BuildingSummary.SpecificFanPower.Value + " " + runResultSummary.BuildingSummary.SpecificFanPower.Units + "\n" +
                "Specific Cooling : " + runResultSummary.BuildingSummary.SpecificCooling.Value + " " + runResultSummary.BuildingSummary.SpecificCooling.Units + "\n" +
                "Specific Heating : " + runResultSummary.BuildingSummary.SpecificHeating.Value + " " + runResultSummary.BuildingSummary.SpecificHeating.Units + "\n" +
                "Total Fan Flow : " + runResultSummary.BuildingSummary.TotalFanFlow.Value + " " + runResultSummary.BuildingSummary.TotalFanFlow.Units + "\n" +
                "Total Cooling Capacity : " + runResultSummary.BuildingSummary.TotalCoolingCapacity.Value + " " + runResultSummary.BuildingSummary.TotalCoolingCapacity.Units + "\n" +
                "Total Heating Capacity : " + runResultSummary.BuildingSummary.TotalHeatingCapacity.Value + " " + runResultSummary.BuildingSummary.TotalHeatingCapacity.Units + "\n";

            List<object> floorarea = new List<object>();
            floorarea.Add((double)runResultSummary.FloorArea.Value);
            floorarea.Add(runResultSummary.FloorArea.Units);

            //Populate outputs
            return new Dictionary<string, object>
            {
                { "Results",runResultSummary},
                { "BuildingType", runResultSummary.BuildingType},
                { "Location", runResultSummary.Location},
                { "FloorArea", floorarea },
                { "BuildingSummary", buildingsummary}

            };
        }

        /// <summary>
        /// Gets Energy, Carbon Cost Summary
        /// </summary>
        /// <remarks> Estimated Energy and Cost Summary Assumptions:  </remarks>
        /// <remarks> 30-year life and 6.1 % discount rate for costs. Doesnot include electric transmission loses or renewable and natural ventilation potential.</remarks>
        /// <param name="Results"></param>
        /// <returns></returns>
        [MultiReturn("Annual Energy Cost","Lifecycle Cost","Annual CO2 Emissions","Annual Energy", "Lifecycle Energy")]
        public static Dictionary<string, object> GetEnegrgyCarbonCostSummary(RunResultSummary Results)
        {
            // Populate Annual CO2 Emissions
            List<List<object>> annualCO2Emissions = new List<List<object>>();

            List<object> electric = new List<object>();
            electric.Add("Electric - " + Results.RunEnergyCarbonCostSummary.AnnualCO2EmissionsElectric.Units); // Type + Unit
            electric.Add((double)Results.RunEnergyCarbonCostSummary.AnnualCO2EmissionsElectric.Value); // Value
            annualCO2Emissions.Add(electric);

            List<object> onsiteFuel = new List<object>();
            onsiteFuel.Add("Onsite Fuel - " + Results.RunEnergyCarbonCostSummary.AnnualCO2EmissionsOnsiteFuel.Units);
            onsiteFuel.Add((double)Results.RunEnergyCarbonCostSummary.AnnualCO2EmissionsOnsiteFuel.Value);
            annualCO2Emissions.Add(onsiteFuel);

            List<object> largeSUV = new List<object>();
            largeSUV.Add("Large SUV Equivalent - " + Results.RunEnergyCarbonCostSummary.AnnualCO2EmissionsLargeSUVEquivalent.Units);
            largeSUV.Add((double)Results.RunEnergyCarbonCostSummary.AnnualCO2EmissionsLargeSUVEquivalent.Value);
            annualCO2Emissions.Add(largeSUV);

            //string annualCO2Emissions = "Electric : " + Results.RunEnergyCarbonCostSummary.AnnualCO2EmissionsElectric.Value + " " + Results.RunEnergyCarbonCostSummary.AnnualCO2EmissionsElectric.Units + "\n" +
            //                            "Onsite Fuel : " + Results.RunEnergyCarbonCostSummary.AnnualCO2EmissionsOnsiteFuel.Value + " " + Results.RunEnergyCarbonCostSummary.AnnualCO2EmissionsOnsiteFuel.Units + "\n" +
            //                            "Large SUV Equivalent : " + Results.RunEnergyCarbonCostSummary.AnnualCO2EmissionsLargeSUVEquivalent.Value + " " + Results.RunEnergyCarbonCostSummary.AnnualCO2EmissionsLargeSUVEquivalent.Units;


            // Populate Annual Energy
            List<List<object>> annualEnergy = new List<List<object>>();

            List<object> EUI = new List<object>();
            EUI.Add("Energy Use Intensity (EUI) - " + Results.RunEnergyCarbonCostSummary.AnnualEUI.Units);
            EUI.Add((double)Results.RunEnergyCarbonCostSummary.AnnualEUI.Value);
            annualEnergy.Add(EUI);

            List<object> Eelectric = new List<object>();
            Eelectric.Add("Electric - " + Results.RunEnergyCarbonCostSummary.AnnualEnergyElectric.Units);
            Eelectric.Add((double)Results.RunEnergyCarbonCostSummary.AnnualEnergyElectric.Value);
            annualEnergy.Add(Eelectric);

            List<object> Efuel = new List<object>();
            Efuel.Add("Fuel - " + Results.RunEnergyCarbonCostSummary.AnnualEnergyFuel.Units);
            Efuel.Add((double)Results.RunEnergyCarbonCostSummary.AnnualEnergyFuel.Value);
            annualEnergy.Add(Efuel);

            List<object> EPeakDemand = new List<object>();
            EPeakDemand.Add("Annual Peak Demand - " + Results.RunEnergyCarbonCostSummary.AnnualPeakDemand.Units);
            EPeakDemand.Add((double)Results.RunEnergyCarbonCostSummary.AnnualPeakDemand.Value);
            annualEnergy.Add(EPeakDemand);

            //string annualEnergy = "Energy Use Intensity (EUI) : " + Results.RunEnergyCarbonCostSummary.AnnualEUI.Value + " " + Results.RunEnergyCarbonCostSummary.AnnualEUI.Units + "\n" +
            //                      "Electric : " + Results.RunEnergyCarbonCostSummary.AnnualEnergyElectric.Value + " " + Results.RunEnergyCarbonCostSummary.AnnualEnergyElectric.Units + "\n" +
            //                       "Fuel : " + Results.RunEnergyCarbonCostSummary.AnnualEnergyFuel.Value + " " + Results.RunEnergyCarbonCostSummary.AnnualEnergyFuel.Units + "\n" +
            //                       "Annual Peak Demand : " + Results.RunEnergyCarbonCostSummary.AnnualPeakDemand.Value + " " + Results.RunEnergyCarbonCostSummary.AnnualPeakDemand.Units;


            // Populate Life cycle Energy
            List<List<object>> lifecycleEnergy = new List<List<object>>();

            List<object> LElectric = new List<object>();
            LElectric.Add("Electric - " + Results.RunEnergyCarbonCostSummary.LifecycleEnergyElectric.Units);
            LElectric.Add((double)Results.RunEnergyCarbonCostSummary.LifecycleEnergyElectric.Value);
            lifecycleEnergy.Add(LElectric);


            List<object> LFuel = new List<object>();
            LFuel.Add("Fuel - " + Results.RunEnergyCarbonCostSummary.LifecycleEnergyFuel.Units);
            LFuel.Add((double)Results.RunEnergyCarbonCostSummary.LifecycleEnergyFuel.Value);
            lifecycleEnergy.Add(LFuel);

            //string lifecycleEnergy = "Electric : " + Results.RunEnergyCarbonCostSummary.LifecycleEnergyElectric.Value + " " + Results.RunEnergyCarbonCostSummary.LifecycleEnergyElectric.Units + "\n" +
            //                         "Fuel : " + Results.RunEnergyCarbonCostSummary.LifecycleEnergyFuel.Value + " " + Results.RunEnergyCarbonCostSummary.LifecycleEnergyFuel.Units;

            
            
            //Populate Outputs
            return new Dictionary<string, object> 
            { 
                {"Annual Energy Cost",Results.RunEnergyCarbonCostSummary.AnnualEnergyCost.Value}, // how to find the currency ???
                {"Lifecycle Cost",Results.RunEnergyCarbonCostSummary.LifecycleCost},
                {"Annual CO2 Emissions", annualCO2Emissions},
                {"Annual Energy",annualEnergy},
                {"LifeCycle Energy", lifecycleEnergy}

            };
        }

        /// <summary>
        /// Gets Carbon Neutral Potential
        /// </summary>
        /// <param name="Results"></param>
        /// <returns></returns>
        [MultiReturn("Run CO2 Emission","Onsite Renewable Potential", "Natural Ventilation Potential","Onsite Biofuel Use","Net CO2 Emission", "Net Large SUV Equivalent")]
        public static Dictionary<string, object> GetCarbonNeutralPotential(RunResultSummary Results)
        {
            // Populate Carbon Neutral Potential data
            List<object> CO2Emission = new List<Object>();
            CO2Emission.Add(Results.CarbonNeutralPotential.Units);
            CO2Emission.Add((double)Results.CarbonNeutralPotential.RunEmissions.Value);

            List<object> RenewablePotential = new List<object>();
            RenewablePotential.Add(Results.CarbonNeutralPotential.Units);
            RenewablePotential.Add((double)Results.CarbonNeutralPotential.OnsiteRenewablePotentialEmissions.Value);

            List<object> NVentilationPotential = new List<object>();
            NVentilationPotential.Add(Results.CarbonNeutralPotential.Units);
            NVentilationPotential.Add(Results.CarbonNeutralPotential.NaturalVentilationPotentialEmissions.Value);

            List<object> BiofuelUse = new List<object>();
            BiofuelUse.Add(Results.CarbonNeutralPotential.Units);
            BiofuelUse.Add((double)Results.CarbonNeutralPotential.OnsiteBiofuelUseEmissions.Value);

            List<object> NetCO2Emission= new List<object>();
            NetCO2Emission.Add(Results.CarbonNeutralPotential.Units);
            NetCO2Emission.Add((double)Results.CarbonNeutralPotential.NetCO2Emissions.Value);

            List<object> LargeSUV= new List<object>();
            LargeSUV.Add(Results.CarbonNeutralPotential.NetLargeSUVEquivalent.Units);
            LargeSUV.Add((double)Results.CarbonNeutralPotential.NetLargeSUVEquivalent.Value);

            // Populate Outputs
            return new Dictionary<string, object>
            {
                {"Run CO2 Emission",CO2Emission},
                {"Onsite Renewable Potential", RenewablePotential},
                {"Natural Ventilation Potential",NVentilationPotential},
                {"Onsite Biofuel Use",BiofuelUse},
                {"Net CO2 Emission",NetCO2Emission},
                {"Net Large SUV Equivalent", LargeSUV}

            };
        }

        /// <summary>
        /// Get Electric Power Plant Sources in Your Region
        /// </summary>
        /// <param name="Results"></param>
        /// <returns></returns>
        [MultiReturn("Fossil","Nuclear","Hydroelectric","Renewable","Other")]
        public static Dictionary<string, object> GetElectricPowerPlantSources(RunResultSummary Results)
        {
            // Populate Outputs
            return new Dictionary<string, object>
            {
                {"Fossil",Results.ElectricPowerPlantSources.Fossil},
                {"Nuclear",Results.ElectricPowerPlantSources.Nuclear},
                {"Hydroelectric",Results.ElectricPowerPlantSources.Hydroelectric},
                {"Renewable",Results.ElectricPowerPlantSources.Renewable},
                {"Other",Results.ElectricPowerPlantSources.Other},
            };
        }

        /// <summary>
        /// Gets LEED Section
        /// </summary>
        /// <param name="Results"></param>
        /// <returns></returns>
        [MultiReturn("LEED Daylight", "LEED Water Efficiency", "Photovoltaic Potential", "Wind Energy Potential", "Natural Ventilation Potential")] 
        public static Dictionary<string, object> GetLEEDSection(RunResultSummary Results)
        { 

            // Populate Leed Daylight
            List<object> LEEDDaylight = new List<object>();
            LEEDDaylight.Add(Results.LeedSection.LeedDaylight.LeedGScore);
            LEEDDaylight.Add(Results.LeedSection.LeedDaylight.LeedQualify);

            // Populate Leed water Efficiency
            List<List<object>> LeedWaterEfficiency = new List<List<object>>();

            List<object> indoor = new List<object>();
            indoor.Add(Results.LeedSection.LeedWaterEfficiency.IndoorUsage); // Value
            indoor.Add("Indoor - " + Results.LeedSection.LeedWaterEfficiency.SIUnit); // Type + Unit
            indoor.Add(Results.LeedSection.LeedWaterEfficiency.IndoorCost); // Value
            indoor.Add("Indoor - " + Results.LeedSection.LeedWaterEfficiency.CurrencyUnit); // Type + Unit
            LeedWaterEfficiency.Add(indoor);

            List<object> outdoor = new List<object>();
            outdoor.Add(Results.LeedSection.LeedWaterEfficiency.OutdoorUsage); // Value
            outdoor.Add("Outdoor - " + Results.LeedSection.LeedWaterEfficiency.SIUnit); // Type + Unit
            outdoor.Add(Results.LeedSection.LeedWaterEfficiency.OutdoorCost); // Value
            outdoor.Add("Outdoor - " + Results.LeedSection.LeedWaterEfficiency.CurrencyUnit); // Type + Unit
            LeedWaterEfficiency.Add(outdoor);

            List<object> total = new List<object>();
            total.Add(Results.LeedSection.LeedWaterEfficiency.TotalUsage); // Value
            total.Add("Outdoor - " + Results.LeedSection.LeedWaterEfficiency.SIUnit); // Type + Unit
            total.Add(Results.LeedSection.LeedWaterEfficiency.TotalCost); // Value
            total.Add("Outdoor - " + Results.LeedSection.LeedWaterEfficiency.CurrencyUnit); // Type + Unit
            LeedWaterEfficiency.Add(total);

            // Populate Leed Photovoltoic Potential
            List<List<object>> LeedPhotovoltaicPotential = new List<List<object>>();

            List<object> AnnualEnergySaving = new List<object>();
            AnnualEnergySaving.Add(Results.LeedSection.PhotoVoltaicPotential.AnnualEnergySavings); // Value
            AnnualEnergySaving.Add("Annual Energy Savings"); // Type
            LeedPhotovoltaicPotential.Add(AnnualEnergySaving);

            List<object> TotalPanelInstalledCost = new List<object>();
            TotalPanelInstalledCost.Add(Results.LeedSection.PhotoVoltaicPotential.TotalInstalledPanelCost); // Value
            TotalPanelInstalledCost.Add("Total Installed Panel Cost"); // Type
            LeedPhotovoltaicPotential.Add(TotalPanelInstalledCost);

            List<object> NominalRatedPower = new List<object>();
            NominalRatedPower.Add(Results.LeedSection.PhotoVoltaicPotential.NominalRatedPower); // Value
            NominalRatedPower.Add("Nominal Rated Power"); // Type
            LeedPhotovoltaicPotential.Add(NominalRatedPower);

            List<object> TotalPanelArea = new List<object>();
            TotalPanelArea.Add(Results.LeedSection.PhotoVoltaicPotential.TotalPanelArea); // Value
            TotalPanelArea.Add("Total Panel Area"); // Type
            LeedPhotovoltaicPotential.Add(TotalPanelArea);

            List<object> MaxPaybackPeriod = new List<object>();
            MaxPaybackPeriod.Add(Results.LeedSection.PhotoVoltaicPotential.MaxPaybackPeriod); // Value
            MaxPaybackPeriod.Add("Maximum Payback Period"); // Type
            LeedPhotovoltaicPotential.Add(MaxPaybackPeriod);

            List<object> assumption= new List<object>();
            assumption.Add("Assumptions: "+ Results.LeedSection.PhotoVoltaicPotential.Assumption);
            LeedPhotovoltaicPotential.Add(assumption );

            // Populate Wind Energy Potential
            List<object> WindEnergyPotential = new List<object>();
            WindEnergyPotential.Add(Results.LeedSection.WindEnergyPotential.AnnualElectricGeneration); // Value
            WindEnergyPotential.Add("Annual Electric Generation"); // Type
            WindEnergyPotential.Add("Wind Energy Assumptions : A single 15 ft turbine, with cut-in and cut-out winds of 6 mph and 45 mph respectively, and located at the coordinates of the weather data");

            // Populate Natural Ventilation Potential
            List<List<object>> NaturalVentilationPotential = new List<List<object>>();

            List<object> THrsMechCoolReq = new List<object>();
            THrsMechCoolReq.Add(Results.LeedSection.NaturalVentilationPotential.TotalHrsMechanicalCoolingRequired); // Value
            THrsMechCoolReq.Add("Total Hours Mechanical Cooling Required"); // Type
            NaturalVentilationPotential.Add(THrsMechCoolReq);

            List<object> PossibleNaturalVentilation= new List<object>();
            PossibleNaturalVentilation.Add(Results.LeedSection.NaturalVentilationPotential.PossibleNaturalVentilationHrs); // Value
            PossibleNaturalVentilation.Add("Possible Natural Ventilation Hours"); // Type
            NaturalVentilationPotential.Add(PossibleNaturalVentilation);

            List<object> PossibleAnnualElectricEnergy = new List<object>();
            PossibleAnnualElectricEnergy.Add(Results.LeedSection.NaturalVentilationPotential.PossibleAnnualElectricEnergySaving); // Value
            PossibleAnnualElectricEnergy.Add("Possible Annual Electric Energy Savings"); // Type
            NaturalVentilationPotential.Add(PossibleAnnualElectricEnergy);

            List<object> PossibleAnnualElectricCost = new List<object>();
            PossibleAnnualElectricCost.Add(Results.LeedSection.NaturalVentilationPotential.PossibelAnnualElectricCostSavings); // Value
            PossibleAnnualElectricCost.Add("Possible Annual Electric Cost Savings"); // Type
            NaturalVentilationPotential.Add(PossibleAnnualElectricCost);

            List<object> NetHrsMechCoolReq = new List<object>();
            NetHrsMechCoolReq.Add(Results.LeedSection.NaturalVentilationPotential.NetHrsMechanicalCoolingRequired); // Value
            NetHrsMechCoolReq.Add("Net Hours Mechanical Cooling Required"); // Type
            NaturalVentilationPotential.Add(NetHrsMechCoolReq);

            // Populate Outputs
            return new Dictionary<string, object>
            {
                {"LEED Daylight", LEEDDaylight},
                {"LEED Water Efficiency", LeedWaterEfficiency},
                {"Photovolvatic Potential", LeedPhotovoltaicPotential},
                {"Wind Energy Potential", WindEnergyPotential},
                {"Natural Ventilation Potential", NaturalVentilationPotential}
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
            string postString = null;
            try
            {
                var s = new DataContractSerializer(type);
                using (MemoryStream stream = new MemoryStream())
                {
                    s.WriteObject(stream, o);
                    postString = Encoding.UTF8.GetString(stream.ToArray());
                }
            }
            catch (Exception)
            {
                
                throw new Exception("The encoding xml failed ");
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
                CultureInfo = "en-US", // TODO : we shoudl get this from Rvt document ?
                ElecCost = electCost,
                FuelCost = fuelcost
                // Elcin: ElectCost, FuelCost, CultureInfo not required if no value should se the default values ! Ask GBS Team!
            };

            return newProject;
        }

        private static NewRunItem _GetNewRunItem(int projectId, string gbXmlFullPath)
        {
            string gbxmlFile = Path.GetFileName(gbXmlFullPath); // with extension
            string path = Path.GetDirectoryName(gbXmlFullPath); // folder of xml file located

            // this creates the zip file
            ZipUtil.ZipFile(path, gbxmlFile, gbxmlFile + ".zip");
            byte[] fileBuffer = System.IO.File.ReadAllBytes(Path.Combine(path , gbxmlFile + ".zip"));
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
