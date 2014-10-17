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
//using System.Runtime.Serialization.Json;

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
using EnergyAnalysisforDynamoAuthHelper;

//Helper
using EnergyAnalysisForDynamo.Utilities;
using EnergyAnalysisForDynamo.DataContracts;

//DataContract
using Revit.Elements;
using System.Xml.Linq;
using System.Diagnostics;

namespace EnergyAnalysisForDynamo
{
    public static class RunAnalysis
    {
        //RevitAuthProvider
        private static RevitAuthProvider revitAuthProvider;

        // NODE: Create Base Run
        /// <summary>
        /// Creates Base Run and returns Base RunId
        /// </summary>
        /// <param name="ProjectId"> Input Project ID </param>
        /// <param name="gbXMLPath"> Input file path of gbXML File </param>
        /// <param name="ExecuteParametricRuns"> Set to true to execute parametric runs. You can read more about parametric runs here: http://autodesk.typepad.com/bpa/ </param>
        /// <returns></returns>
        [MultiReturn("RunId")]
        public static Dictionary<string, int> RunEnergyAnalysis(int ProjectId, string gbXMLPath, bool ExecuteParametricRuns = false)
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
            Helper.InitRevitAuthProvider();

            // 1.1 Turn off MassRuns
            Helper._ExecuteMassRuns(ExecuteParametricRuns, ProjectId);

            // 2. Create A Base Run
            string requestCreateBaseRunUri = GBSUri.GBSAPIUri + string.Format(APIV1Uri.CreateBaseRunUri, "xml");

            var response =
                (HttpWebResponse)
                 Helper._CallPostApi(requestCreateBaseRunUri, typeof(NewRunItem), Helper._GetNewRunItem(ProjectId, gbXMLPath));
            newRunId = Helper.DeserializeHttpWebResponse(response);

            // 3. Populate the Outputs
            return new Dictionary<string, int>
            {
                { "RunId", newRunId},
            };
        }


        // NODE: Create new Project
        /// <summary>
        /// Creates new project in GBS Webservices, returns new Project ID. Returns ProjectID if the project is already exists.
        /// </summary>
        /// <param name="ProjectTitle"> Title of the project </param>
        /// <returns></returns>
        [MultiReturn("ProjectId")]
        public static Dictionary<string, int> CreateProject(string ProjectTitle)
        {
            //1. Output variable
            int newProjectId = 0;

            //NOTE: GBS allows to duplicate Project Titles !!! from user point of view we would like keep Project Titles Unique.
            //Create Project node return the Id of a project if it already exists. If more than one project with the same name already exist, throw an exception telling the user that multiple projects with that name exist.

            //Check if the project exists
            List<Project> ExtngProjects = Helper.GetExistingProjectsTitles();

            var queryProjects = from pr in ExtngProjects
                                where pr.Title == ProjectTitle
                                select pr;

            if (queryProjects.Any()) // Existing Project
            {
                // check if multiple projects
                if (queryProjects.Count() > 1)
                {
                    // if there are multiple thow and exception
                    throw new Exception("Multiple Projects with this title " + ProjectTitle + " exist. Try with a another name or use GetProjectList Node to get the existing GBS projects' attributes");
                }
                else 
                {
                    newProjectId = queryProjects.First().Id;
                }
            }
            else //Create New Project
            { 
                #region Setup : Get values from current Revit document

                //local variable to get SiteLocation and Lat & Lon information
                Document RvtDoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument.Document;

                //Load the default energy setting from the active Revit instance
                EnergyDataSettings myEnergySettings = Autodesk.Revit.DB.Analysis.EnergyDataSettings.GetFromDocument(RvtDoc);

                // get BuildingType and ScheduleId from document
                // Remap Revit enum/values to GBS enum/ values
                string RvtBldgtype = Enum.GetName(typeof(gbXMLBuildingType), myEnergySettings.BuildingType);
                int BuildingTypeId = Helper.RemapBldgType(RvtBldgtype);
                // this for comparison
                int RvtBuildingTypeId = (int)myEnergySettings.BuildingType;

                // Lets set the schedule ID to 1 for now
                //int ScheduleId = (int)myEnergySettings.BuildingOperatingSchedule;
                int ScheduleId = Helper.RemapScheduleType((int)myEnergySettings.BuildingOperatingSchedule);


                // Angles are in Rdaians when coming from revit API
                // Convert to lat & lon values 
                const double angleRatio = Math.PI / 180; // angle conversion factor

                double lat = RvtDoc.SiteLocation.Latitude / angleRatio;
                double lon = RvtDoc.SiteLocation.Longitude / angleRatio;

                #endregion

                #region Setup : Get default Utility Values

                //1. Initiate the Revit Auth
                Helper.InitRevitAuthProvider();

                // Try to get Default Utility Costs from API 
                string requestGetDefaultUtilityCost = GBSUri.GBSAPIUri + APIV1Uri.GetDefaultUtilityCost;
                string requestUriforUtilityCost = string.Format(requestGetDefaultUtilityCost, BuildingTypeId, lat, lon, "xml");
                HttpWebResponse responseUtility = (HttpWebResponse)Helper._CallGetApi(requestUriforUtilityCost);

                string theresponse = "";
                using (Stream responseStream = responseUtility.GetResponseStream())
                {
                    using (StreamReader streamReader = new StreamReader(responseStream))
                    {
                        theresponse = streamReader.ReadToEnd();
                    }
                }
                DefaultUtilityItem utilityCost = Helper.DataContractDeserialize<DefaultUtilityItem>(theresponse);

                #endregion

                // 2.  Create A New  Project
                string requestUri = GBSUri.GBSAPIUri + string.Format(APIV1Uri.CreateProjectUri, "xml");

                var response =
                    (HttpWebResponse)
                    Helper._CallPostApi(requestUri, typeof(NewProjectItem), Helper._CreateProjectItem(ProjectTitle, false, BuildingTypeId, ScheduleId, lat, lon, utilityCost.ElecCost, utilityCost.FuelCost));

                newProjectId = Helper.DeserializeHttpWebResponse(response);
            }

            // 3. Populate the Outputs
            return new Dictionary<string, int>
            {
                { "ProjectId", newProjectId}
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
        public static Dictionary<string, object> ExportMassToGBXML(string FilePath, AbstractFamilyInstance MassFamilyInstance, Boolean Run = false)
        {
            Boolean IsSuccess = false;

            string FileName = Path.GetFileNameWithoutExtension(FilePath);
            string Folder = Path.GetDirectoryName(FilePath);

            //make RUN? inputs set to True mandatory
            if (Run == false)
            {
                throw new Exception("Set 'Connect' to True!");
            }

            //local variables
            Document RvtDoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument.Document;

            //enable the analytical model in the document if it isn't already
            try
            {
                PrepareEnergyModel.ActivateEnergyModel(RvtDoc);
            }
            catch (Exception)
            {
                throw new Exception("Something went wrong when trying to enable the energy model.");
            }

            //get the id of the analytical model associated with that mass
            Autodesk.Revit.DB.ElementId myEnergyModelId = MassEnergyAnalyticalModel.GetMassEnergyAnalyticalModelIdForMassInstance(RvtDoc, MassFamilyInstance.InternalElement.Id);
            MassEnergyAnalyticalModel mea = (MassEnergyAnalyticalModel)RvtDoc.GetElement(myEnergyModelId);
            ICollection<Autodesk.Revit.DB.ElementId> ZoneIds = mea.GetMassZoneIds();

            MassGBXMLExportOptions gbXmlExportOptions = new MassGBXMLExportOptions(ZoneIds.ToList()); // two constructors 

            RvtDoc.Export(Folder, FileName, gbXmlExportOptions);


            // if the file exists return success message if not return failed message
            string path = Path.Combine(Folder, FileName + ".xml");

            if (System.IO.File.Exists(path))
            {
                // Modify the xml Program Info element, aithorize the
                XmlDocument doc = new XmlDocument();
                doc.Load(path);

                // EE: There must be a shorter way !
                XmlNode node = doc.DocumentElement;

                foreach (XmlNode node1 in node.ChildNodes)
                {
                    foreach (XmlNode node2 in node1.ChildNodes)
                    {
                        if (node2.Name == "ProgramInfo")
                        {
                            foreach (XmlNode childnode in node2.ChildNodes)
                            {
                                if (childnode.Name == "ProductName")
                                {
                                    string productname = "Dynamo _ " + childnode.InnerText;
                                    childnode.InnerText = productname;
                                }
                            }

                        }
                    }
                }

                //doc.DocumentElement.Attributes["ProgramInfo"].ChildNodes[1].Value += "Dynamo ";
                doc.Save(path);

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
        public static Dictionary<string, object> ExportZonesToGBXML(string FilePath, List<ElementId> ZoneIds, Boolean Run = false)
        {
            Boolean IsSuccess = false;

            string FileName = Path.GetFileNameWithoutExtension(FilePath);
            string Folder = Path.GetDirectoryName(FilePath);

            //make RUN? inputs set to True mandatory
            if (Run == false)
            {
                throw new Exception("Set 'Connect' to True!");
            }

            //local varaibles
            Document RvtDoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument.Document;

            //enable the analytical model in the document if it isn't already
            try
            {
                PrepareEnergyModel.ActivateEnergyModel(RvtDoc);
            }
            catch (Exception)
            {
                throw new Exception("Something went wrong when trying to enable the energy model.");
            }

            //convert the ElementId wrapper instances to actual Revit ElementId objects
            List<Autodesk.Revit.DB.ElementId> outZoneIds = ZoneIds.Select(e => new Autodesk.Revit.DB.ElementId(e.InternalId)).ToList();

            // Create gbXML
            MassGBXMLExportOptions gbXmlExportOptions = new MassGBXMLExportOptions(outZoneIds);

            RvtDoc.Export(Folder, FileName, gbXmlExportOptions);


            // if the file exists return success message if not return failed message
            string path = Path.Combine(Folder, FileName + ".xml");

            if (System.IO.File.Exists(path))
            {
                // Modify the xml Program Info element, aithorize the
                XmlDocument doc = new XmlDocument();
                doc.Load(path);

                // EE: There must be a shorter way !
                XmlNode node = doc.DocumentElement;
                foreach (XmlNode node1 in node.ChildNodes)
                {
                    foreach (XmlNode node2 in node1.ChildNodes)
                    {
                        if (node2.Name == "ProgramInfo")
                        {
                            foreach (XmlNode childnode in node2.ChildNodes)
                            {
                                if (childnode.Name == "ProductName")
                                {
                                    string productname = "Dynamo _ " + childnode.InnerText;
                                    childnode.InnerText = productname;
                                }
                            }
                        }
                    }
                }

                doc.Save(path);

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
    }
}
