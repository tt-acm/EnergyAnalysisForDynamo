using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

//Revit & Dynamo 
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.UI;
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
using Autodesk.DesignScript.Runtime;
using Revit.GeometryConversion;


namespace EnergyAnalysisForDynamo
{
    public static class EnergySettings
    {
        /// <summary>
        /// Gets existing Energy Data Settings from current document
        /// </summary>
        /// <returns></returns>
        [MultiReturn("BldgType", "GlzPer", "ShadeDepth", "HVACSystem", "OSchedule")]
        public static Dictionary<string, object> GetEnergySettings()
        {
            // Get current document
            Document RvtDoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument.Document;

            // Load the default energy setting from the active Revit instance
            EnergyDataSettings es = Autodesk.Revit.DB.Analysis.EnergyDataSettings.GetFromDocument(RvtDoc);


            return new Dictionary<string, object>
            {
                { "BldgType", Enum.GetName(typeof(gbXMLBuildingType), es.BuildingType)}, 
                { "GlzPer",  es.PercentageGlazing}, 
                { "ShadeDepth",  es.ShadeDepth * UnitConverter.HostToDynamoFactor}, 
                { "HVACSystem",Enum.GetName(typeof(gbXMLBuildingHVACSystem), es.BuildingHVACSystem)},
                { "OSchedule",Enum.GetName(typeof(gbXMLBuildingOperatingSchedule), es.BuildingOperatingSchedule)}
            };


            // User Visible Versions NOTE: this available in only Revit 2015 API
            //EnergyDataSettings es = EnergyDataSettings.GetFromDocument(RvtDoc);

            //es.get_Parameter(BuiltInParameter.ENERGY_ANALYSIS_HVAC_SYSTEM).AsValueString();

        }

        /// <summary>
        /// Sets the Enegry Data Settings
        /// </summary>
        /// <param name="BldgTyp"> Input Building Type </param>
        /// <param name="GlzPer">Input glazing percentage (range: 0 to 1) </param>
        /// <param name="ShadeDepth">Shading Depth, specified as a double.  We assume the double value represents a length using Dynamo's current length unit.</param>
        /// <param name="SkylightPer">Input skylight percentage (range: 0 to 1)</param>
        /// <param name="HVACSystem">Input Building HVAC system</param>
        /// <param name="OSchedule">Input Building Operating Schedule</param>
        /// <returns></returns>
        [MultiReturn("EnergySettings", "report")]
        public static Dictionary<string, object> SetEnergySettings(string BldgTyp = "", double GlzPer = 0, double ShadeDepth = 0, double SkylightPer = 0, string HVACSystem = "", string OSchedule = "")
        {

            //Get active document
            Document RvtDoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument.Document;

            //Load the default energy setting from the active Revit instance
            EnergyDataSettings myEnergySettings = Autodesk.Revit.DB.Analysis.EnergyDataSettings.GetFromDocument(RvtDoc);

            //make sure we are in a transaction
            TransactionManager.Instance.EnsureInTransaction(RvtDoc);

            if (!string.IsNullOrEmpty(BldgTyp))
            {
                Autodesk.Revit.DB.Analysis.gbXMLBuildingType type;
                try
                {
                    type = (Autodesk.Revit.DB.Analysis.gbXMLBuildingType)Enum.Parse(typeof(Autodesk.Revit.DB.Analysis.gbXMLBuildingType), BldgTyp);
                }
                catch (Exception)
                {
                    throw new Exception("Building type is not found");
                }
                myEnergySettings.BuildingType = type;
            }

            if (!string.IsNullOrEmpty(HVACSystem))
            {
                Autodesk.Revit.DB.Analysis.gbXMLBuildingHVACSystem type;
                try
                {
                    type = (Autodesk.Revit.DB.Analysis.gbXMLBuildingHVACSystem)Enum.Parse(typeof(Autodesk.Revit.DB.Analysis.gbXMLBuildingHVACSystem), HVACSystem);
                }
                catch (Exception)
                {
                    throw new Exception("HVAC system is not found");
                }
                myEnergySettings.BuildingHVACSystem = type;
            }

            if (!string.IsNullOrEmpty(OSchedule))
            {
                Autodesk.Revit.DB.Analysis.gbXMLBuildingOperatingSchedule type;
                try
                {
                    type = (Autodesk.Revit.DB.Analysis.gbXMLBuildingOperatingSchedule)Enum.Parse(typeof(Autodesk.Revit.DB.Analysis.gbXMLBuildingOperatingSchedule), OSchedule);
                }
                catch (Exception)
                {
                    throw new Exception("Operating Schedule is not found");
                }
                myEnergySettings.BuildingOperatingSchedule = type;
            }

            if (GlzPer > 0.0 && GlzPer <= 1.0)
            {
                try
                {
                    myEnergySettings.PercentageGlazing = GlzPer;
                }
                catch (Exception)
                {
                    throw new Exception("The Glazing Percentage input range should be 0 - 1");
                }
            }

            if (ShadeDepth > 0.0)
            {
                myEnergySettings.IsGlazingShaded = true;
                myEnergySettings.ShadeDepth = ShadeDepth * UnitConverter.DynamoToHostFactor;
            }
            else
            {
                myEnergySettings.IsGlazingShaded = false;
                myEnergySettings.ShadeDepth = 0;
            }

            // add skylight percentage
            myEnergySettings.PercentageSkylights = SkylightPer;

            //done with the transaction 
            TransactionManager.Instance.TransactionTaskDone();

            // Report 
            string report = "Building type is " + Enum.GetName(typeof(gbXMLBuildingType), myEnergySettings.BuildingType) + ".\n" +
                "Glazing percentage is set to " + myEnergySettings.PercentageGlazing.ToString() + ".\n" +
                "Shading depth is " + myEnergySettings.ShadeDepth.ToString() + ".\n" +
                "Current HVAC system is " + Enum.GetName(typeof(gbXMLBuildingHVACSystem), myEnergySettings.BuildingHVACSystem) + ".\n" +
                "Current Operating Schedule is " + Enum.GetName(typeof(gbXMLBuildingOperatingSchedule), myEnergySettings.BuildingOperatingSchedule) + ".";

            return new Dictionary<string, object>
            {
                { "EnergySettings", myEnergySettings},
                { "report", report}
            };
        }

    }
}
