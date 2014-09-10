using System;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using Autodesk.DesignScript.Runtime;

using System.Collections.Generic;
using System.Globalization;


namespace GBSforDynamo.DataContracts
{
    [DataContract(Namespace = DataContractNamespace.Namespace)]
    internal class Project
    {
        [DataMember(EmitDefaultValue = true)]
        public int Id { get; set; }

        [DataMember]
        public string Title { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "ID"), DataMember]
        public int ProjectRightsID { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public DateTime? DateAdded { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "ID"), DataMember(EmitDefaultValue = false)]
        public int? BuildingTypeID { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "ID"), DataMember(EmitDefaultValue = false)]
        public int? ScheduleID { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public double? Latitude { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public double? Longitude { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "ID"), DataMember(EmitDefaultValue = false)]
        public int? WeatherID { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Elec"), DataMember(EmitDefaultValue = false)]
        public double? ElecCost { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public double? FuelCost { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "ISO"), DataMember(EmitDefaultValue = false)]
        public string CurrencyISO { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string CurrencyName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? RunCount { get; set; }
    }

    [CollectionDataContract(Namespace = DataContractNamespace.Namespace)]
    internal class Projects : Collection<Project>
    {

    }

    internal class DataContractNamespace
    {
        public const string Namespace = @"http://gbs.autodesk.com/gbs/api/DataContract/";
    }

    [DataContract(Namespace = DataContractNamespace.Namespace)]
    internal class NewProjectItem
    {
        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public bool Demo { get; set; }

        [DataMember]
        public int BuildingTypeId { get; set; }

        [DataMember]
        public int ScheduleId { get; set; }

        [DataMember]
        public double Latitude { get; set; }

        [DataMember]
        public double Longitude { get; set; }

        [DataMember]
        public float ElecCost { get; set; }

        [DataMember]
        public float FuelCost { get; set; }

        [DataMember]
        public string CultureInfo { get; set; }
    }

    [DataContract(Namespace = DataContractNamespace.Namespace)]
    internal class NewRunItem
    {
        [DataMember]
        public int ProjectId { get; set; }

        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public string Base64EncodedGbxml { get; set; }

        [DataMember]
        public string UtilityId { get; set; }

    }

    [DataContract(Namespace = DataContractNamespace.Namespace)]
    internal class ProjectRun
    {
        [DataMember]
        public int runId { get; set; }

        [DataMember]
        public int altRunId { get; set; }

        [DataMember]
        public string name { get; set; }

        [DataMember]
        public bool useSI { get; set; }

        [DataMember]
        public int status { get; set; }
    }

    [DataContract(Namespace = DataContractNamespace.Namespace)]
    internal class BuildingType
    {
        [DataMember]
        public int BuildingTypeId { get; set; }

        [DataMember]
        public string BuildingTypeName { get; set; }
    }

    [DataContract(Namespace = DataContractNamespace.Namespace)]
    internal class ScheduleItem
    {
        [DataMember]
        public int ScheduleId { get; set; }

        [DataMember]
        public string ScheduleName { get; set; }

        [DataMember]
        public string ScheduleDescription { get; set; }
    }

    [CollectionDataContract(Namespace = DataContractNamespace.Namespace, ItemName = "Value")]
    internal class MonthlyUseCollection : Collection<float>
    {
    }

    [DataContract(Namespace = DataContractNamespace.Namespace)]
    internal enum Unit
    {
        [EnumMember]
        None = 0,
        [EnumMember]
        ShortTons,
        [EnumMember]
        ShortTonsPerYear,
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Tonnes")]
        [EnumMember]
        MetricTonnes,
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Tonnes")]
        [EnumMember]
        MetricTonnesPerYear,
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SUVs")]
        [EnumMember]
        SUVsPerYear,
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "k")]
        [EnumMember]
        kW,
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Wh")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "k")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Wh")]
        [EnumMember]
        kWh,
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Wh")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "k")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Wh")]
        [EnumMember]
        kWhPerYear,
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Wh")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Wh")]
        [EnumMember]
        MWh,
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Therms")]
        [EnumMember]
        Therms,
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Therm")]
        [EnumMember]
        PerTherm,
        [EnumMember]
        PerMJ,
        [EnumMember]
        SquareMeters,
        [EnumMember]
        SquareFeet,
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Wh")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Wh")]
        [EnumMember]
        PerkWh,
        [EnumMember]
        MJ,
        [EnumMember]
        Btu,
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "k")]
        [EnumMember]
        kBtu,
        [EnumMember]
        MBtu,
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Wh")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "k")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Wh")]
        [EnumMember]
        kWhPerSquareMeterPerYear,
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Wh")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "k")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Wh")]
        [EnumMember]
        kWhPerSquareFootPerYear,
        [EnumMember]
        MJPerSquareMeterPerYear,
        [EnumMember]
        MJPerSquareFootPerYear,
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "k")]
        [EnumMember]
        kBtuPerSquareMeterPerYear,
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "k")]
        [EnumMember]
        kBtuPerSquareFootPerYear,
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "kg")]
        [EnumMember]
        kgPerYear,
        [EnumMember]
        WattsPerSquareFoot,
        [EnumMember]
        WattsPerSquareMeter,
        [EnumMember]
        Knots,
        [EnumMember]
        KilometersPerHour,
        [EnumMember]
        MetersPerSecond,
        [EnumMember]
        Hours,
        [EnumMember]
        HoursAbbreviated,
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Celcius")]
        [EnumMember]
        DegreesCelcius,
        [EnumMember]
        DegreesFahrenheit,
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "BTU")]
        [EnumMember]
        BTUPerHourPerSquareFoot,
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Humididty")]
        [EnumMember]
        RelativeHumididtyAbbreviated,
        [EnumMember]
        Centimeters,
        [EnumMember]
        Inches,
        [EnumMember]
        People,
        [EnumMember]
        Joules,
        [EnumMember]
        Gal,
        [EnumMember]
        Liters,
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "k")]
        [EnumMember]
        kBtuPerSquareFeetPerYear,
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Metres")]
        [EnumMember]
        MJPerSquareMetresPerYear,
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Therm")]
        [EnumMember]
        Therm,
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Litre")]
        [EnumMember]
        Litre,
        [EnumMember]
        Gallon,
        [EnumMember]
        PerOccupant,
        [EnumMember]
        PerSquareMeters,
        [EnumMember]
        PerSquareFeet,
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "k")]
        [EnumMember]
        kBtuPerHour,
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "CFM")]
        [EnumMember]
        CFM,
        [EnumMember]
        BtuPerHour,
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "GPM")]
        [EnumMember]
        GPM,
        //Adding units for Building Summary section
        [EnumMember]
        LPerSecPerSquareMeters,
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cfm")]
        [EnumMember]
        CfmPerSquareFeet,
        [EnumMember]
        WattsPerLPerSec,
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cfm")]
        [EnumMember]
        WattsPerCfm,
        [EnumMember]
        SquareMetersPerKilowatt,
        [EnumMember]
        SquareFeetPerTon,
        [EnumMember]
        SquareFeetPerBtu,
        [EnumMember]
        LPerSec,
        [EnumMember]
        KiloBtuHour,
        [EnumMember]
        WattsPerSquareMeterKelvin
    }

    [DataContract(Namespace = DataContractNamespace.Namespace)]
    internal class DefaultUtilityItem
    {
        [DataMember]
        public float ElecCost { get; set; }

        [DataMember]
        public float FuelCost { get; set; }
    }

    [DataContract(Namespace = DataContractNamespace.Namespace)]
    internal class UtilityItem
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly"), DataMember]
        public MonthlyUseCollection MonthlyUse { get; set; }
        [DataMember]
        public float AnnualCost { get; set; }
        [DataMember]
        public Unit Unit { get; set; }
    }

    [DataContract(Namespace = DataContractNamespace.Namespace)]
    internal class UtilityDataSet
    {
        [DataMember]
        public int ProjectId { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public int StartYear { get; set; }
        [DataMember]
        public int StartMonth { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Elec")]
        [DataMember]
        public UtilityItem ElecUsage { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Elec")]
        [DataMember]
        public UtilityItem ElecDemand { get; set; }
        [DataMember]
        public UtilityItem FuelUsage { get; set; }
    }

    [DataContract(Namespace = DataContractNamespace.Namespace)]
    internal class UtilityDataSetInfoItem
    {
        [DataMember]
        public Guid HistoryBillDataId { get; set; }

        [DataMember]
        public string Name { get; set; }
    }

    [DataContract(Namespace = DataContractNamespace.Namespace)]
    [IsVisibleInDynamoLibrary(false)]
    public class RunResultSummary
    {
        [DataMember]
        public string Runtitle { get; set; }

        [DataMember]
        public string ProjectTemplateApplied { get; set; }

        [DataMember]
        public string Location { get; set; }

        [DataMember]
        public string BuildingType { get; set; }

        [DataMember]
        public DataMeasurement FloorArea { get; set; }

        [DataMember]
        public DataMeasurement ElectricCost { get; set; }

        [DataMember]
        public DataMeasurement FuelCost { get; set; }

        [DataMember]
        public UtilityInformation UtilityLinkData { get; set; }

        [DataMember]
        internal SimulationEnergyCarbonCostSummary RunEnergyCarbonCostSummary { get; set; }

        //[DataMember]
        //public EnergyCarbonCostSummary AltRunSummary { get; set; }

        [DataMember]
        public SimulationCarbonNeutralPotential CarbonNeutralPotential { get; set; }

        [DataMember]
        public ElectricPowerPlantSource ElectricPowerPlantSources { get; set; }

        [DataMember]
        public List<HydronicEquipment> HydronicEquipmentList { get; set; }

        [DataMember]
        public List<AirEquipment> AirEquipmentList { get; set; }

        [DataMember]
        public BuildingStatisticSummary BuildingSummary { get; set; }

        //[DataMember]
        //public ConstructionDataInformation ConstructionData { get; set; }

        //[DataMember]
        //public LeedSection LeedSection { get; set; }
    }

    [DataContract(Namespace = DataContractNamespace.Namespace)]
    internal class LeedSection
    {
        [DataMember]
        public LeedDaylight LeedDaylight { get; set; }

        [DataMember]
        public NaturalVentilationPotential NaturalVentilationPotential { get; set; }

        [DataMember]
        public WindEnergyPotential WindEnergyPotential { get; set; }

        [DataMember]
        public PhotoVoltaicPotential PhotoVoltaicPotential { get; set; }

        [DataMember]
        public LeedWaterEfficiency LeedWaterEfficiency { get; set; }

        //[DataMember]
        //public EncryptedId EncryptedId { get; set; }
    }

    [DataContract(Namespace = DataContractNamespace.Namespace)]
    internal class SimulationEnergyCarbonCostSummary
    {

        [DataMember]
        public System.Globalization.CultureInfo CurrencyCulture { get; set; }

        [DataMember]
        public double? AnnualEnergyCost { get; set; }

        [DataMember]
        public double? LifecycleCost { get; set; }

        [DataMember]
        public DataMeasurement AnnualCO2EmissionsElectric { get; set; }

        [DataMember]
        public DataMeasurement AnnualCO2EmissionsOnsiteFuel { get; set; }

        [DataMember]
        public DataMeasurement AnnualCO2EmissionsLargeSUVEquivalent { get; set; }

        [DataMember]
        public DataMeasurement AnnualEnergyElectric { get; set; }

        [DataMember]
        public DataMeasurement AnnualEnergyFuel { get; set; }

        [DataMember]
        public DataMeasurement AnnualPeakDemand { get; set; }

        [DataMember]
        public DataMeasurement LifecycleEnergyElectric { get; set; }

        [DataMember]
        public DataMeasurement LifecycleEnergyFuel { get; set; }

        [DataMember]
        public DataMeasurement AnnualEUI { get; set; }
    }

    [DataContract(Namespace = DataContractNamespace.Namespace)]
    [IsVisibleInDynamoLibrary(false)]
    public class SimulationCarbonNeutralPotential
    {
        [DataMember]
        public string Units { get; set; }

        [DataMember]
        public double? RunEmissions { get; set; }

        //[DataMember]
        //public double? AltRunEmissions { get; set; }

        [DataMember]
        public double? OnsiteRenewablePotentialEmissions { get; set; }

        [DataMember]
        public double? NaturalVentilationPotentialEmissions { get; set; }

        [DataMember]
        public double? OnsiteBiofuelUseEmissions { get; set; }

        [DataMember]
        public double? NetCO2Emissions { get; set; }

        [DataMember]
        public DataMeasurement NetLargeSUVEquivalent { get; set; }
    }

    [DataContract(Namespace = DataContractNamespace.Namespace)]
    [IsVisibleInDynamoLibrary(false)]
    public class ElectricPowerPlantSource
    {
        [DataMember]
        public double Fossil { get; set; }

        [DataMember]
        public double Nuclear { get; set; }

        [DataMember]
        public double Hydroelectric { get; set; }

        [DataMember]
        public double Renewable { get; set; }

        [DataMember]
        public double Other { get; set; }
    }

    [DataContract(Namespace = DataContractNamespace.Namespace)]
    internal class LeedDaylight
    {
        [DataMember]
        public string LeedGScore { get; set; }

        [DataMember]
        public string LeedQualify { get; set; }
    }

    [DataContract(Namespace = DataContractNamespace.Namespace)]
    internal class NaturalVentilationPotential
    {
        [DataMember]
        public string TotalHrsMechanicalCoolingRequired { get; set; }

        [DataMember]
        public string PossibleNaturalVentilationHrs { get; set; }

        [DataMember]
        public string PossibleAnnualElectricEnergySaving { get; set; }

        [DataMember]
        public string PossibelAnnualElectricCostSavings { get; set; }

        [DataMember]
        public string NetHrsMechanicalCoolingRequired { get; set; }
    }

    [DataContract(Namespace = DataContractNamespace.Namespace)]
    internal class WindEnergyPotential
    {
        [DataMember]
        public string AnnualElectricGeneration { get; set; }
    }

    [DataContract(Namespace = DataContractNamespace.Namespace)]
    internal class PhotoVoltaicPotential
    {
        [DataMember]
        public string AnnualEnergySavings { get; set; }

        [DataMember]
        public string TotalInstalledPanelCost { get; set; }

        [DataMember]
        public string NominalRatedPower { get; set; }

        [DataMember]
        public string TotalPanelArea { get; set; }

        [DataMember]
        public string MaxPaybackPeriod { get; set; }

        [DataMember]
        public string Assumption { get; set; }
    }

    [DataContract(Namespace = DataContractNamespace.Namespace)]
    internal class LeedWaterEfficiency
    {
        [DataMember]
        public string IndoorUsage { get; set; }

        [DataMember]
        public string OutdoorUsage { get; set; }

        [DataMember]
        public string TotalUsage { get; set; }

        [DataMember]
        public string IndoorCost { get; set; }

        [DataMember]
        public string OutdoorCost { get; set; }

        [DataMember]
        public string TotalCost { get; set; }

        [DataMember]
        public string SIUnit { get; set; }

        [DataMember]
        public string CurrencyUnit { get; set; }
    }

    //public class EnergyEndUseChart
    //{
    //    public String  RunTitle { get; set; }
    //    public int RunId { get; set; }
    //    public int AltRunId { get; set; }
    //    public String ElectChartURL { get; set; }
    //    public String  FuelChartURL { get; set; }
    //}

    [DataContract(Namespace = DataContractNamespace.Namespace)]
    [IsVisibleInDynamoLibrary(false)]
    public class BuildingStatisticSummary
    {
        [DataMember]
        public DataMeasurement NumberOfPeople { get; set; }

        //[DataMember]
        //public sbyte NumberOfPeopleImage { get; set; }

        [DataMember]
        public DataMeasurement AvgLightingPowerDensity { get; set; }

        [DataMember]
        public sbyte AvgLightingPowerDensityImage { get; set; }

        [DataMember]
        public DataMeasurement AvgEquipmentPowerDensity { get; set; }

        //[DataMember]
        //public sbyte AvgEquipmentPowerDensityImage { get; set; }

        [DataMember]
        public DataMeasurement SpecificFanFlow { get; set; }

        //[DataMember]
        //public sbyte SpecificFanFlowImage { get; set; }

        [DataMember]
        public DataMeasurement SpecificFanPower { get; set; }

        //[DataMember]
        //public sbyte SpecificFanPowerImage { get; set; }

        [DataMember]
        public DataMeasurement SpecificCooling { get; set; }

        //[DataMember]
        //public sbyte SpecificCoolingImage { get; set; }

        [DataMember]
        public DataMeasurement SpecificHeating { get; set; }

        //[DataMember]
        //public sbyte SpecificHeatingImage { get; set; }

        [DataMember]
        public DataMeasurement TotalFanFlow { get; set; }

        [DataMember]
        public DataMeasurement TotalCoolingCapacity { get; set; }

        [DataMember]
        public DataMeasurement TotalHeatingCapacity { get; set; }
    }

    #region Air & Hydronic Equipment

    [DataContract(Namespace = DataContractNamespace.Namespace)]
    [IsVisibleInDynamoLibrary(false)]
    public class AirEquipment
    {
        [DataMember]
        public string AirloopId { get; set; }

        [DataMember]
        public string SystemType { get; set; }

        [DataMember]
        public string ToolTipMarkup { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Type { get; set; }

        [DataMember]
        public string Tooltipheader { get; set; }

        [DataMember]
        public string TooltipText { get; set; }

        [DataMember]
        public DataMeasurement SupplyFanFlow { get; set; }

        [DataMember]
        public DataMeasurement AnnualSupplyFanRuntime { get; set; }

        [DataMember]
        public DataMeasurement CoolingCapacity { get; set; }

        [DataMember]
        public DataMeasurement heatingCapacity { get; set; }
    }

    [DataContract(Namespace = DataContractNamespace.Namespace)]
    [IsVisibleInDynamoLibrary(false)]
    public class HydronicEquipment
    {
        [DataMember]
        public string HydronicEquipmentID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public DataMeasurement BoilerCapacity { get; set; }

        [DataMember]
        public DataMeasurement AverageDemand { get; set; }

        [DataMember]
        public DataMeasurement PumpFlow { get; set; }

        [DataMember]
        public DataMeasurement ElectricChillerCapacity { get; set; }

        [DataMember]
        public DataMeasurement AbsorptionChillerCapacity { get; set; }

        [DataMember]
        public DataMeasurement CoolingTowerCapacity { get; set; }

        [DataMember]
        public string Approach { get; set; }

        [DataMember]
        public string Tooltipheader { get; set; }

        [DataMember]
        public string TooltipText { get; set; }
    }

    [DataContract(Namespace = DataContractNamespace.Namespace)]
    [IsVisibleInDynamoLibrary(false)]
    public class DataMeasurement
    {
        [DataMember]
        public double? Value { get; set; }

        [DataMember]
        public string Units { get; set; }
    }

    #region Construction

    [DataContract(Namespace = DataContractNamespace.Namespace)]
    internal class SurfaceTypeData
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public double? UValue { get; set; }

        [DataMember]
        public string ToolTip { get; set; }

        [DataMember]
        public string ToolTipHeader { get; set; }

        [DataMember]
        public DataMeasurement Area { get; set; }

        [DataMember]
        public string ToolTipDivId { get; set; }
    }

    [DataContract(Namespace = DataContractNamespace.Namespace)]
    internal enum WindowOrientation
    {
        [EnumMember]
        South,
        [EnumMember]
        NonSouth,
        [EnumMember]
        North,
        [EnumMember]
        NonNorth
    }

    [DataContract(Namespace = DataContractNamespace.Namespace)]
    internal class WindowTypeData
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public DataMeasurement UValue { get; set; }

        [DataMember]
        public double SHGC { get; set; }

        [DataMember]
        public double? Vlt { get; set; }

        [DataMember]
        public DataMeasurement Area { get; set; }

        [DataMember]
        public WindowOrientation Orientation { get; set; }
    }

    [DataContract(Namespace = DataContractNamespace.Namespace)]
    internal class WindowData
    {
        [DataMember]
        public List<WindowTypeData> SouthFacingWindows { get; set; }

        [DataMember]
        public List<WindowTypeData> NonSouthFacingWindows { get; set; }

        [DataMember]
        public List<WindowTypeData> NorthFacingWindows { get; set; }

        [DataMember]
        public List<WindowTypeData> NonNorthFacingWindows { get; set; }
    }

    [DataContract(Namespace = DataContractNamespace.Namespace)]
    [IsVisibleInDynamoLibrary(false)]
    internal class ConstructionDataInformation
    {
        //[DataMember]
        //public bool IsBaseRun { get; set; }

        //[DataMember]
        //public EncryptedId EncryptedId { get; set; }

        #region Surface Types

        [DataMember]
        public List<SurfaceTypeData> Roofs { get; set; }

        [DataMember]
        public List<SurfaceTypeData> Ceilings { get; set; }

        [DataMember]
        public List<SurfaceTypeData> ExteriorWalls { get; set; }

        [DataMember]
        public List<SurfaceTypeData> InteriorWalls { get; set; }

        [DataMember]
        public List<SurfaceTypeData> InteriorFloors { get; set; }

        [DataMember]
        public List<SurfaceTypeData> RaisedFloors { get; set; }

        [DataMember]
        public List<SurfaceTypeData> SlabsOnGrade { get; set; }

        [DataMember]
        public List<SurfaceTypeData> UndergroundCeilings { get; set; }

        [DataMember]
        public List<SurfaceTypeData> UndergroundWalls { get; set; }

        [DataMember]
        public List<SurfaceTypeData> UndergroundSlabs { get; set; }

        [DataMember]
        public List<SurfaceTypeData> AirWalls { get; set; }

        #endregion

        #region Openings

        [DataMember]
        public List<SurfaceTypeData> NonslidingDoors { get; set; }

        #endregion

        #region Windows

        [DataMember]
        public WindowData SlidingDoors { get; set; }

        [DataMember]
        public WindowData AirOpenings { get; set; }

        [DataMember]
        public WindowData FixedWindows { get; set; }

        [DataMember]
        public WindowData OperableWindows { get; set; }

        [DataMember]
        public WindowData FixedSkylights { get; set; }

        [DataMember]
        public WindowData OperableSkylights { get; set; }

        #endregion
    }

    #endregion

    //    [DataContract(Namespace = DataContractNamespace.Namespace)]
    //public class EncryptedId
    //{
    //    [DataMember]
    //    public string RunId { get; set; }

    //    [DataMember]
    //    public string AltRunId { get; set; }
    //}

    #endregion

    [DataContract(Namespace = DataContractNamespace.Namespace)]
    [IsVisibleInDynamoLibrary(false)]
    public class UtilityInformation
    {
        [DataMember]
        public Guid UtilityId { get; set; }

        [DataMember]
        public string UtilityName { get; set; }
    }
}