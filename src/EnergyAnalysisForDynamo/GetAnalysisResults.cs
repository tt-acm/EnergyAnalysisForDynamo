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
    public static class GetAnalysisResults
    {
        // NODE: GBS-Get Project List
        /// <summary> 
        /// Returns Project Lists from GBS web service
        /// </summary> 
        /// <param name="Connect"> Set Boolean True </param>
        /// <returns name="ProjectIds"> Returns Project Ids in GBS Web Service List.</returns> 
        /// <returns name="ProjectTitles"> Returns Project Titles in GBS Web Service List.</returns> 
        /// <returns name="ProjectDateAdded"> Returns Project's date of added or created List.</returns> 
        [MultiReturn("ProjectIds", "ProjectTitles", "ProjectDateAdded")]
        public static Dictionary<string, object> GetProjectsList(bool Connect = false)
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
            Helper.InitRevitAuthProvider();

            // Request 
            string requestUri = GBSUri.GBSAPIUri + string.Format(APIV1Uri.GetProjectList, "json");

            HttpWebResponse response = (HttpWebResponse)Helper._CallGetApi(requestUri);
            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream);
            string result = reader.ReadToEnd();
            List<Project> projectList = Helper.DataContractJsonDeserialize<List<Project>>(result);
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


        // NODE: GBS_Get Run Ids
        /// <summary>
        /// Gets Run List of specific project from GBS Web Service
        /// </summary>
        /// <param name="ProjectId"> Input Project ID</param>
        /// <returns name = "RunIds"> Returns Run IDs </returns>
        /// <returns name = "ParametricRunIds"> Returns Alternate Run IDs </returns>
        /// <returns name = "RunNames"> Returns Run Names </returns>
        [MultiReturn("RunNames", "RunIds", "ParametricRunIds")]
        public static Dictionary<string, object> GetRunIds(int ProjectId)
        {
            // Initiate the Revit Auth
            Helper.InitRevitAuthProvider();

            string requestUri = GBSUri.GBSAPIUri + string.Format(APIV1Uri.GetProjectRunListUri, ProjectId.ToString(), "json");
            HttpWebResponse response = (HttpWebResponse)Helper._CallGetApi(requestUri);
            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream);
            string projectRunListJson = reader.ReadToEnd();

            //TextWriterTraceListener tr2 = new TextWriterTraceListener(System.IO.File.CreateText("C:\\00_demo\\Output.txt"));
            //Debug.Listeners.Add(tr2);
            Debug.WriteLine(projectRunListJson);
            Debug.Flush();

            List<ProjectRun> projectRuns = Helper.DataContractJsonDeserialize<List<ProjectRun>>(projectRunListJson);

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
                { "RunNames", RunNames}, // Array
                { "RunIds", runIds}, // List
                { "ParametricRunIds", AltRunIds}

            };

        }


        /// <summary>
        /// Loads analysis results from Green Building Studio for a specific project ID
        /// </summary>
        /// <para> Use .... nodes to parse the Results info of the specific run</para>
        /// <param name="RunID"> Input Run Id </param>
        /// <param name="ParametricRunID"> Input an Id for one of the parametric runs</param>
        /// <returns></returns>
        [MultiReturn("Results", "BuildingType", "Location", "FloorArea", "BuildingSummary")]
        public static Dictionary<string, object> LoadAnalysisResults(int RunID, int ParametricRunID = 0)
        {
            // Initiate the Revit Auth
            Helper.InitRevitAuthProvider();

            //Get results Summary of given RunID & AltRunID
            string requestGetRunSummaryResultsUri = GBSUri.GBSAPIUri +
                                     string.Format(APIV1Uri.GetRunSummaryResultsUri, RunID, ParametricRunID, "json");
            HttpWebResponse response2 = (HttpWebResponse)Helper._CallGetApi(requestGetRunSummaryResultsUri);
            Stream responseStream2 = response2.GetResponseStream();
            StreamReader reader2 = new StreamReader(responseStream2);
            string resultSummary = reader2.ReadToEnd();
            RunResultSummary runResultSummary = Helper.DataContractJsonDeserialize<RunResultSummary>(resultSummary);

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
        /// Gets Carbon Neutral Potential from the Results, The output is based on project location and won't change based on your design option
        /// </summary>
        /// <param name="Results"></param>
        /// <returns></returns>
        [MultiReturn("Run CO2 Emission", "Onsite Renewable Potential", "Natural Ventilation Potential", "Onsite Biofuel Use", "Net CO2 Emission", "Net Large SUV Equivalent")]
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

            List<object> NetCO2Emission = new List<object>();
            NetCO2Emission.Add(Results.CarbonNeutralPotential.Units);
            NetCO2Emission.Add((double)Results.CarbonNeutralPotential.NetCO2Emissions.Value);

            List<object> LargeSUV = new List<object>();
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
        /// Gets Energy, Carbon Cost Summary
        /// </summary>
        /// <remarks> Estimated Energy and Cost Summary Assumptions:  </remarks>
        /// <remarks> 30-year life and 6.1 % discount rate for costs. Does not include electric transmission loses or renewable and natural ventilation potential.</remarks>
        /// <param name="Results"></param>
        /// <returns></returns>
        [MultiReturn("Annual Energy Cost", "Lifecycle Cost", "Annual CO2 Emissions", "Annual Energy", "Lifecycle Energy")]
        public static Dictionary<string, object> GetEnergyCarbonCostSummary(RunResultSummary Results)
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
        /// Gets Electric Power Plant Sources in Your Region
        /// </summary>
        /// <param name="Results"></param>
        /// <returns></returns>
        [MultiReturn("Fossil", "Nuclear", "Hydroelectric", "Renewable", "Other")]
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
        public static Dictionary<string, object> GetLEEDPotential(RunResultSummary Results)
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

            List<object> assumption = new List<object>();
            assumption.Add("Assumptions: " + Results.LeedSection.PhotoVoltaicPotential.Assumption);
            LeedPhotovoltaicPotential.Add(assumption);

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

            List<object> PossibleNaturalVentilation = new List<object>();
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
        /// Download gbXML, inp or idf files from Green Building Studio
        /// </summary>
        /// <param name="RunId"> Input Run ID</param>
        /// <param name="ParametricRunId"> Input ID for one of the parametric runs. Default is set to 0</param>
        /// <param name="FileType"> Result type gbxml or doe2 or inp </param>
        /// <param name="FilePath"> Set File location to download the file </param>
        /// <returns name="report"> string. </returns>
        public static string GetEnergyModelFiles(int RunId, string FileType, string Directory, int ParametricRunId = 0) // result type gbxml/doe2/eplus
        {
            // Initiate the Revit Auth
            Helper.InitRevitAuthProvider();

            // report
            string report = " The request is failed!";

            // Get result of given RunId
            string requestGetRunResultsUri = GBSUri.GBSAPIUri +
                                    string.Format(APIV1Uri.GetSimulationRunFile, RunId, ParametricRunId, FileType);

            HttpWebResponse response = (HttpWebResponse)Helper._CallGetApi(requestGetRunResultsUri);
            Stream stream = response.GetResponseStream();
            
            StreamReader reader = new StreamReader(stream);
            string result = reader.ReadToEnd();

            SimulationRunFile srf = Helper.DataContractJsonDeserialize<SimulationRunFile>(result);

            // Get directory and create zip file location
            string folder= Path.GetDirectoryName(Directory);
            string zipFileName = Path.Combine(folder, srf.FileName);

            System.IO.File.WriteAllBytes(zipFileName, srf.FileStream);

            if (File.Exists(zipFileName))
            { report = "The Analysis result file " + FileType + " was successfully downloaded!"; }

            return report;
        }
    }
}
