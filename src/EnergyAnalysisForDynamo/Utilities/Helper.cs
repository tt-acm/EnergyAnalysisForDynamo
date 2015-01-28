using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Autodesk.DesignScript.Runtime;
using System.Net;

// Revit
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using RevitServices.Persistence;

//Revit Services
using RevitServices;

//AuthHelper
using EnergyAnalysisforDynamoAuthHelper;

using EnergyAnalysisForDynamo;
using EnergyAnalysisForDynamo.DataContracts;


namespace EnergyAnalysisForDynamo.Utilities
{
    [IsVisibleInDynamoLibrary(false)]
    public static class Helper
    {
        //RevitAuthProvider
        private static RevitAuthProvider revitAuthProvider;
        
        // get surface ids from analytical energy model based on type
        public static List<EnergyAnalysisForDynamo.ElementId> GetSurfaceIdsFromMassEnergyAnalyticalModelBasedOnType(MassEnergyAnalyticalModel MassEnergyAnalyticalModel, string SurfaceTypeName = "Mass Exterior Wall")
        {
            
            //get the MassSurfaceData ids of the definitions belonging to external faces
            //we'll output these, and then try to visualize the faces and change parameters in another component

            //get references to the faces using the mass - we need these to get at the surface data
            IList<Reference> faceRefs = MassEnergyAnalyticalModel.GetReferencesToAllFaces();
            
            //list to collect ids of matching surfaces
            List<Autodesk.Revit.DB.ElementId> SelectedSurfaceIds = new List<Autodesk.Revit.DB.ElementId>();

            //some faces supposedly share massSurfaceData definitions (although i think they are all unique in practice) - here we're pulling out unique data definitions.  
            Dictionary<int, MassSurfaceData> mySurfaceData = new Dictionary<int, MassSurfaceData>();
            foreach (var fr in faceRefs)
            {
                Autodesk.Revit.DB.ElementId id = MassEnergyAnalyticalModel.GetMassSurfaceDataIdForReference(fr);
                if (!mySurfaceData.ContainsKey(id.IntegerValue))
                {
                    MassSurfaceData d = (MassSurfaceData)MassEnergyAnalyticalModel.Document.GetElement(id);
                    
                    // add to dictionary to be able to keep track of ids
                    mySurfaceData.Add(id.IntegerValue, d);
                    
                    if (d.Category.Name == SurfaceTypeName)
                    {
                        // collect the id
                        SelectedSurfaceIds.Add(id);
                    }
                }
            }


            List<ElementId> outSelectedSurfaceIds = SelectedSurfaceIds.Select(e => new ElementId(e.IntegerValue)).ToList();

            return outSelectedSurfaceIds;
        }

        // get surface ids from zone based on type
        public static List<EnergyAnalysisForDynamo.ElementId> GetSurfaceIdsFromZoneBasedOnType(MassZone MassZone, string SurfaceTypeName = "Mass Exterior Wall")
        {
            //some faces supposedly share massSurfaceData definitions (although i think they are all unique in practice) - here we're pulling out unique data definitions.  
            Dictionary<int, Autodesk.Revit.DB.Element> mySurfaceData = new Dictionary<int, Autodesk.Revit.DB.Element>();
            List<Autodesk.Revit.DB.ElementId> SurfaceIds = new List<Autodesk.Revit.DB.ElementId>();
            
            //get references to all of the faces
            IList<Reference> faceRefs = MassZone.GetReferencesToEnergyAnalysisFaces();
            
            foreach (var faceRef in faceRefs)
            {
                var srfType = faceRef.GetType();
                string refType = faceRef.ElementReferenceType.ToString();

                //get the element ID of the MassSurfaceData object associated with this face
                Autodesk.Revit.DB.ElementId id = MassZone.GetMassDataElementIdForZoneFaceReference(faceRef);

                //add it to our dict if it isn't already there
                if (!mySurfaceData.ContainsKey(id.IntegerValue))
                {   
                    Autodesk.Revit.DB.Element mySurface = MassZone.Document.GetElement(id);
                    //add id and surface to dictionary to keep track of data
                    mySurfaceData.Add(id.IntegerValue, mySurface);
                    
                    if (mySurface.Category.Name == SurfaceTypeName)
                        {                      
                            // collect the id
                            SurfaceIds.Add(id);
                        }

                }
            }
            
            //loop over the output lists, and wrap them in our ElementId wrapper class
            List<ElementId> outSurfaceIds = SurfaceIds.Select(e => new ElementId(e.IntegerValue)).ToList();

            return outSurfaceIds;
        }

        // GBS Authentification 
        public static void InitRevitAuthProvider()
        {
            SingleSignOnManager.RegisterSingleSignOn();
            revitAuthProvider = revitAuthProvider ?? new RevitAuthProvider(SynchronizationContext.Current);
        }

        // Check if the Project has been already created
        public static bool IsProjectAlreadyExist(string NewProjectName) // true the project is existing, false is a new project
        {
            bool IsExisting = false;

            // Initiate the Revit Auth
            InitRevitAuthProvider();

            // Request 
            string requestUri = GBSUri.GBSAPIUri + string.Format(APIV1Uri.GetProjectList, "json");

            HttpWebResponse response = (HttpWebResponse)_CallGetApi(requestUri);
            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream);
            string result = reader.ReadToEnd();
            List<Project> projectList = DataContractJsonDeserialize<List<Project>>(result);


            try
            {
                var project = (from pr in projectList
                               where pr.Title == NewProjectName
                               select pr).First();

                if (project != null)
                {
                    IsExisting = true;
                }

            }
            catch (Exception)
            {

            }

            return IsExisting;
        }

        // Get Existing Projects from GBS
        public static List<Project> GetExistingProjectsTitles()
        {
            // Request 
            string requestUri = GBSUri.GBSAPIUri + string.Format(APIV1Uri.GetProjectList, "json");

            HttpWebResponse response = (HttpWebResponse)_CallGetApi(requestUri);
            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream);
            string result = reader.ReadToEnd();
            List<Project> projectList = DataContractJsonDeserialize<List<Project>>(result);

            return projectList;
        }

        // Remap Revit Building type to GBS 
        public static int RemapBldgType(string RvtBldType)
        {
            int GBSBldgEnum = 1;

            // Initiate the Revit Auth
            InitRevitAuthProvider();


            // Get Building Types

            // Request 
            string requestUri = GBSUri.GBSAPIUri + string.Format(APIV1Uri.GetBuildingTypesUri, "json");

            HttpWebResponse response = (HttpWebResponse)_CallGetApi(requestUri);
            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream);
            string result = reader.ReadToEnd();
            
            List<DataContracts.BuildingType> buildingTypeList = DataContractJsonDeserialize<List<DataContracts.BuildingType>>(result);
            
            DataContracts.BuildingType bldgType;

            try
            {
                bldgType = (from BldType in buildingTypeList
                            where BldType.BuildingTypeName == RvtBldType
                            select BldType).First();
            }
            catch (Exception)
            {
                throw new Exception("The Building Type is not defined in GBS");
            }


            if (bldgType != null)
            {
                GBSBldgEnum = bldgType.BuildingTypeId;
            }


            return GBSBldgEnum;

        }

        //Remap Revit Operating Schedule to GBS __ This is slopy mapping ! 
        public static int RemapScheduleType(int RvtSchdlType)
        {
            int gbsSchdlTyp = 1; // default

            if (RvtSchdlType == 0) // Default
            {
                gbsSchdlTyp = 1;
            }
            else if (RvtSchdlType == 1) //TwentyFourHourSevenDayFacility 24/7
            {
                gbsSchdlTyp = 2;
            }
            else if (RvtSchdlType == 2) // TwentyFourHourSixDayFacility 24/6
            {
                gbsSchdlTyp = 3;
            }
            else if (RvtSchdlType == 3) // TwentyFourHourHourFiveDayFacility 24/5
            {
                gbsSchdlTyp = 4;
            }
            else if (RvtSchdlType == 4) // TwelveHourSevenDayFacility 12/7
            {
                gbsSchdlTyp = 5;
            }
            else if (RvtSchdlType == 5) // TwelveHourSixDayFacility 12/6
            {
                gbsSchdlTyp = 6;
            }
            else if (RvtSchdlType == 6) // TwelveHourFiveDayFacility 12/5
            {
                gbsSchdlTyp = 7;
            }
            else if (RvtSchdlType == 7) // KindergartenThruTwelveGradeSchool k12
            {
                gbsSchdlTyp = 8;
            }
            else if (RvtSchdlType == 8) // YearRoundSchool
            {
                gbsSchdlTyp = 9;
            }
            else if (RvtSchdlType == 9) //TheaterPerformingArts
            {
                gbsSchdlTyp = 10;
            }
            else if (RvtSchdlType == 10) // Worship
            {
                gbsSchdlTyp = 11;
            }
            else if (RvtSchdlType == 11) //NON
            {
                gbsSchdlTyp = 1;
            }

            return gbsSchdlTyp;

        }

        #region API Web Requests

        /// <summary>
        /// Turn on and off MassRuns in project level
        /// </summary>
        /// <param name="Run"></param>
        /// <param name="ProjectId"></param>
        /// <returns></returns>
        public static void _ExecuteMassRuns(bool Run = true, int ProjectId = 0)
        {   
            // For more information eead this post on Autodesk's blog
            // http://autodesk.typepad.com/bpa/2013/05/new-update-on-gbs-adn-api.html

            // Get Uri for updating mass run in project level
            // You can control mass run either in project level or in user level. The process is similar but the Uri is different.
            // To update massrun is user level look for APIV1Uri.ControlMassRunInUserLevel
            string ControlMassRuns = GBSUri.GBSAPIUri + 
                                     string.Format(APIV1Uri.ControlMassRunInProjectLevel, ProjectId.ToString());
            
            // First Get request is to get the permission
            // Sign URL using Revit auth
            var MassRunRequestUri = revitAuthProvider.SignRequest(ControlMassRuns, HttpMethod.Get, null);

            // Send the request to GBS
            var request = (HttpWebRequest)System.Net.WebRequest.Create(MassRunRequestUri);
            request.Timeout = 300000;
            request.Method = "GET";
            // request.PreAuthenticate = true;
            request.ContentType = "application/xml";

            // get the response
            WebResponse response = request.GetResponse();

            // read the response
            // StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            // reader.ReadToEnd();

            // Now that we have the permission let's change it based on user's request for this project
            // Sign URL using Revit auth
            var MassRunUpdateUri = revitAuthProvider.SignRequest(ControlMassRuns, HttpMethod.Put, null);
            
            // Send the request to GBS
            var changeRequest = (HttpWebRequest)System.Net.WebRequest.Create(MassRunUpdateUri);
            changeRequest.Timeout = 300000;
            changeRequest.Method = "PUT";
            changeRequest.PreAuthenticate = true;
            changeRequest.ContentType = "application/xml";
            
            using (Stream requestStream = changeRequest.GetRequestStream())


            using (StreamWriter requestWriter = new StreamWriter(requestStream))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    DataContractSerializer serializer = new DataContractSerializer((typeof(bool)));

                    // Here we set up the parameters
                    serializer.WriteObject(ms, Run);

                    byte[] postData = ms.ToArray();
                    requestStream.Write(postData, 0, postData.Length);
                    
                }
            }

            // get response
            WebResponse changeResponse = changeRequest.GetResponse();
            
            // read the response
            //using (StreamReader responseReader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
            //{
            //    responseReader.ReadToEnd();
            //}

        }


        public static WebResponse _CallGetApi(string requestUri)
        {
            // Sign URL using Revit auth
            var signedRequestUri = revitAuthProvider.SignRequest(requestUri, HttpMethod.Get, null);

            // Send request to GBS
            System.Net.WebRequest request = System.Net.WebRequest.Create(signedRequestUri);
            request.Timeout = 300000;
            WebResponse response = request.GetResponse();

            return response;
        }

        public static WebResponse _CallPostApi(string requestUri, System.Type type, object o)
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
            request.Timeout = 300000;
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

        public static T DataContractJsonDeserialize<T>(string response)
        {
            using (MemoryStream stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(response)))
            {
                DataContractJsonSerializer serialize = new DataContractJsonSerializer(typeof(T));
                return (T)serialize.ReadObject(stream);
            }
        }

        public static int DeserializeHttpWebResponse(HttpWebResponse response)
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

        public static T DataContractDeserialize<T>(string response)
        {
            using (var stream = new MemoryStream(Encoding.Default.GetBytes(response)))
            {
                var serialize = new DataContractSerializer(typeof(T));
                return (T)serialize.ReadObject(stream);
            }
        }

        #endregion

        #region DataContracts Items Methods
        public static NewProjectItem _CreateProjectItem(string title, Boolean demo, int bldgTypeId, int scheduleId, double lat, double lon, float electCost, float fuelcost)
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

        public static NewRunItem _GetNewRunItem(int projectId, string gbXmlFullPath)
        {
            string gbxmlFile = Path.GetFileName(gbXmlFullPath); // with extension
            string path = Path.GetDirectoryName(gbXmlFullPath); // folder of xml file located

            // this creates the zip file
            ZipUtil.ZipFile(path, gbxmlFile, gbxmlFile + ".zip");
            byte[] fileBuffer = System.IO.File.ReadAllBytes(Path.Combine(path, gbxmlFile + ".zip"));
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
