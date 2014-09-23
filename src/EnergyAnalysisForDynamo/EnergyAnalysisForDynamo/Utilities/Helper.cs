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

        public static WebResponse _CallGetApi(string requestUri)
        {
            // Sign URL using Revit auth
            var signedRequestUri = revitAuthProvider.SignRequest(requestUri, HttpMethod.Get, null);

            // Send request to GBS
            System.Net.WebRequest request = System.Net.WebRequest.Create(signedRequestUri);
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
            using (MemoryStream stream = new MemoryStream(System.Text.Encoding.Default.GetBytes(response)))
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
