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

using System.Web.Services;
using EnergyAnalysisForDynamo.CEAService;

namespace EnergyAnalysisForDynamo
{
    public class VisualizeResults
    {
        private static RevitAuthProvider revitAuthProvider;

        public static void GetChart(int RunId)
        {
            // Credentials or Access Token ?

            SingleSignOnManager.RegisterSingleSignOn();
            revitAuthProvider = revitAuthProvider ?? new RevitAuthProvider(SynchronizationContext.Current);

            // request token ??
            var signedRequestUri = revitAuthProvider.SignRequest(@"https://gbs.autodesk.com/GBS/soap/CEAService.svc?wsdl", HttpMethod.Get, null);

            string urlReturn = string.Empty;
            string errorInfo = string.Empty;
            string cultureinfo = CultureInfo.CurrentCulture.ToString();

            
           
            //CEAService.SolonClient scl = new CEAService.SolonClient();
            //scl.GetSolonPackage(signedRequestUri, RunId, true);

            CEAService.GBS_oGetChartsURLClient cl = new CEAService.GBS_oGetChartsURLClient();
            
            cl.GBS_oGetChartsURL(signedRequestUri, RunId.ToString(), cultureinfo, "in", ref urlReturn, ref errorInfo);
            
        }
        
    }
}
