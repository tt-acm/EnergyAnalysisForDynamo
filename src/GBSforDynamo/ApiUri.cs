using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using GBS.Connection;

namespace GBSforDynamo
{
    internal class GBSUri
    {
        internal const string GBSAPIUri = @"https://gbs.autodesk.com/gbs/api";
        internal const string DashboardAPIUri = @"https://gbs.autodesk.com/Dashboard/OpenAPI";
    }

    internal static class APIV1Uri
    {
        //internal static string OpenApiUri = @"https://gbs.autodesk.com/gbs/api"; //GBSUri.GBSAPIUri;

        internal static string CreateProjectUri = @"/v1/project/create/{0}"; // 0-xml/json
        internal static string CreateBaseRunUri = @"/v1/run/create/base/{0}"; // 0-xml/json
        internal static string DeleteProjectUri = @"/v1/project/delete/{0}/{1}"; //0-projectid
        internal static string GetDefaultUtilityCost = @"/v1/project/defaultUtilityCost/{0}/{1}/{2}/{3}"; ///{buildingTypeId}/{latitude}/{longitude}
        internal static string GetBuildingTypesUri = @"/v1/project/buildingTypeList/{0}"; // 0 = response format "json"  or default is xml
        internal static string GetScheduleListUri = @"/v1/project/scheduleList/{0}"; // 0 = response format "json"  or default is xml
        internal static string GetProjectList = @"/v1/project/list/{0}"; //0 xml/json
        internal static string GetUtilityDataSetListUri = @"/v1/project/utilityDataSets/{0}/{1}"; // 0 = projectId, 1 = response format xml/json
        internal static string UploadUtilityDataSetUri = @"/projects/{0}/UtilityDataSet"; // 0 = projectId
        internal static string GetProjectRunListUri = @"/v1/project/runs/{0}/{1}"; // 0 = projectId, 1 = response format xml/json
        internal static string GetRunStatus = @"/v1/run/status/{0}/{1}/{2}";// 0 = runId, 1 =altrunid, 2 = response format xml/json
        internal static string GetRunResultsUri = @"/v1/run/results/{0}/{1}/{2}"; // 0 = runId, 1 = altRunId, 2=response payload (gbxml||doe2)
        internal static string GetRunSummaryResultsUri = @"/v1/run/results/summary/{0}/{1}/{2}"; // 0 = runId, 1 = altRunId, response format xml/json

    }

    internal class APIResultTypeUri
    {
        internal const string Uri = @"/resulttype";
        internal const string RulerUri = @"/getResultTypeConfigurationRules/json";
        internal const string DataUri = @"/getResultTypeData?returnTypeformat=json";
    }

}
