using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

//Revit & Dynamo 
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
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


namespace GBSforDynamo
{
    public static class EnergySettings
    {

        /// Set Energy Settings
        /// 
        /// </summary>
        /// <param name="BldgTyp"></param>
        /// <param name="GlzPer"></param>
        /// <param name="ShadeDepth"></param>
        /// <param name="HVACsys"></param>
        /// <param name="OSchedule"></param>
        /// <returns></returns>
        [MultiReturn("EnergySettings","Bldgtype", "GlzPer", "ShadeDepth", "HvacSystem", "OSchedule")]
        public static Dictionary<string, object> SetEnergySettings(string BldgTyp = "", double GlzPer = 0, double ShadeDepth = 0, string HVACsys = "", string OSchedule = "")
        {
            
            //active document
            Document RvtDoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument.Document;

            //Load the default energy setting from the active Revit instance
            EnergyDataSettings myEnergySettings = Autodesk.Revit.DB.Analysis.EnergyDataSettings.GetFromDocument(RvtDoc);

            // Making Changes on 
            TransactionManager.SetupManager();
            var transManager = TransactionManager.Instance.TransactionWrapper;
            var t = transManager.StartTransaction(RvtDoc);

            try
            {
                // this overwrite the default energy settings
                if (!string.IsNullOrEmpty(BldgTyp))
                {
                    Autodesk.Revit.DB.Analysis.gbXMLBuildingType type;
                    try
                    {
                        type = (Autodesk.Revit.DB.Analysis.gbXMLBuildingType)Enum.Parse(typeof(Autodesk.Revit.DB.Analysis.gbXMLBuildingType), BldgTyp);
                    }
                    catch (Exception)
                    {
                        throw new Exception("Building Type is not found");
                    }
                    myEnergySettings.BuildingType = type;
                }

                if (!string.IsNullOrEmpty(HVACsys))
                {
                    Autodesk.Revit.DB.Analysis.gbXMLBuildingHVACSystem type;
                    try
                    {
                        type = (Autodesk.Revit.DB.Analysis.gbXMLBuildingHVACSystem)Enum.Parse(typeof(Autodesk.Revit.DB.Analysis.gbXMLBuildingHVACSystem), HVACsys);
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

                if (GlzPer != 0.0)
                {
                    // the value should be 0 - 1
                    try
                    {
                        myEnergySettings.PercentageGlazing = GlzPer;
                    }
                    catch (Exception) // 
                    {
                        throw new Exception("The Glazing Percentage input range should be 0 - 1");
                    }
                }

                if (ShadeDepth != 0.0)
                {
                    myEnergySettings.IsGlazingShaded = true;
                    myEnergySettings.ShadeDepth = ShadeDepth;
                }
                else {
                    myEnergySettings.IsGlazingShaded = false;
                }

                t.CommitTransaction();
            }
            catch (Exception ex)
            {
                t.CancelTransaction();
                throw new Exception(ex.ToString());
            }

            //return myEnergySettings;

            return new Dictionary<string, object>
            {
                { "EnergySettings", myEnergySettings},
                { "Bldgtype", Enum.GetName(typeof(gbXMLBuildingType),myEnergySettings.BuildingType)}, 
                { "GlzPer",  myEnergySettings.PercentageGlazing}, 
                { "ShadeDepth",  myEnergySettings.ShadeDepth}, 
                { "HvacSystem",Enum.GetName(typeof(gbXMLBuildingHVACSystem), myEnergySettings.BuildingHVACSystem)},
                { "OSchedule",Enum.GetName(typeof(gbXMLBuildingOperatingSchedule), myEnergySettings.BuildingOperatingSchedule)}
            };
        }

        /// Read Existing Energy Settings
        /// Reads Existing Energy Settings of the document
        /// </summary>
        /// <returns></returns>
        [MultiReturn("Bldgtype", "GlzPer", "ShadeDepth", "HvacSystem", "OSchedule")]
        public static Dictionary<string, object> GetEnergySettings()
        {
            Document RvtDoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument.Document;

            //Load the default energy setting from the active Revit instance
            EnergyDataSettings es = Autodesk.Revit.DB.Analysis.EnergyDataSettings.GetFromDocument(RvtDoc);

            return new Dictionary<string, object>
            {
                { "Bldgtype", Enum.GetName(typeof(gbXMLBuildingType), es.BuildingType)}, 
                { "GlzPer",  es.PercentageGlazing}, 
                { "ShadeDepth",  es.ShadeDepth}, 
                { "HvacSystem",Enum.GetName(typeof(gbXMLBuildingHVACSystem), es.BuildingHVACSystem)},
                { "OSchedule",Enum.GetName(typeof(gbXMLBuildingOperatingSchedule), es.BuildingOperatingSchedule)}
            };
        }

    }

}
