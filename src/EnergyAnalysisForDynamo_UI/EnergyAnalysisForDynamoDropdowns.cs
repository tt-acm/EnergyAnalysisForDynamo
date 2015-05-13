using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using DSCore;
using DSCoreNodesUI;
using Dynamo.Models;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using ProtoCore.AST.AssociativeAST;

namespace EnergyAnalysisForDynamo_UI
{

    public abstract class EA4DDropdownBase : DSDropDownBase
    {
        public EA4DDropdownBase(string value, Enum e) : base(value) 
        {
            stringsFromEnum(e);
        }

        private List<string> myDropdownItems = new List<string>();

        private void stringsFromEnum(Enum e) 
        {
            foreach (var i in Enum.GetValues(e.GetType()))
            {
                myDropdownItems.Add(i.ToString());
            }
        }

        public override void PopulateItems()
        {
            Items.Clear();
            foreach (var i in myDropdownItems)
            {
                Items.Add(new DynamoDropDownItem(i, i)); 
            }
        }

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            if (Items.Count == 0)
            {
                PopulateItems();
            }
            return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildStringNode(Items[SelectedIndex].Name)) };
        }
    }


    [NodeName("Building Type Dropdown")]
    [NodeCategory("EnergyAnalysisForDynamo.EnergySettings")]
    [NodeDescription("Select a building type to use with the GBSforDynamo Energy Settings node.")]
    [IsDesignScriptCompatible]
    public class BuildingTypeDropdown : EA4DDropdownBase
    {
        public BuildingTypeDropdown() : base("Building Type", new gbXMLBuildingType()) { }
    }

    [NodeName("HVAC System Type Dropdown")]
    [NodeCategory("EnergyAnalysisForDynamo.EnergySettings")]
    [NodeDescription("Select a HVAC System type to use with the GBSforDynamo Energy Settings node.")]
    [IsDesignScriptCompatible]
    public class HVACtypeDropdown : EnumAsString<gbXMLBuildingHVACSystem>
    {
        public HVACtypeDropdown(WorkspaceModel workspace) : base() { }
    }

    [NodeName("Operating Schedules Dropdown")]
    [NodeCategory("EnergyAnalysisForDynamo.EnergySettings")]
    [NodeDescription("Select an Operating Schedule to use with the Energy Settings node.")]
    [IsDesignScriptCompatible]
    public class OperatingSchedulesDropdown : EnumAsString<gbXMLBuildingOperatingSchedule>
    {
        public OperatingSchedulesDropdown(WorkspaceModel workspace) : base() { }
    }

    [NodeName("Conceptual Wall Construction Types Dropdown")]
    [NodeCategory("EnergyAnalysisForDynamo.EnergySettings")]
    [NodeDescription("Select a Conceptual Construction Type to use with the Set Surface Parameters node.")]
    [IsDesignScriptCompatible]
    public class ConcWallConstTypeDropdown : EnumAsString<ConceptualConstructionWallType>
    {
        public ConcWallConstTypeDropdown(WorkspaceModel workspace) : base() { }
    }
    
    [NodeName("Conceptual Glazing Construction Types Dropdown")]
    [NodeCategory("EnergyAnalysisForDynamo.EnergySettings")]
    [NodeDescription("Select a Conceptual Construction Type to use with the Set Surface Parameters node.")]
    [IsDesignScriptCompatible]
    public class ConcGlazingConstTypeDropdown : EnumAsString<ConceptualConstructionWindowSkylightType>
    {
        public ConcGlazingConstTypeDropdown(WorkspaceModel workspace) : base() { }
    }

    [NodeName("Conceptual Floor Construction Types Dropdown")]
    [NodeCategory("EnergyAnalysisForDynamo.EnergySettings")]
    [NodeDescription("Select a Conceptual Construction Type to use with the Set Surface Parameters node.")]
    [IsDesignScriptCompatible]
    public class ConcFloorConstTypeDropdown : EnumAsString<ConceptualConstructionFloorSlabType>
    {
        public ConcFloorConstTypeDropdown(WorkspaceModel workspace) : base() { }
    }

    [NodeName("Conceptual Roof Construction Types Dropdown")]
    [NodeCategory("EnergyAnalysisForDynamo.EnergySettings")]
    [NodeDescription("Select a Conceptual Construction Type to use with the Set Surface Parameters node.")]
    [IsDesignScriptCompatible]
    public class ConcRoofConstTypeDropdown : EnumAsString<ConceptualConstructionRoofType>
    {
        public ConcRoofConstTypeDropdown(WorkspaceModel workspace) : base() { }
    }

    [NodeName("Space Types Dropdown")]
    [NodeCategory("EnergyAnalysisForDynamo.EnergySettings")]
    [NodeDescription("Select a Space Type to use with the Set Zone Parameters node.")]
    [IsDesignScriptCompatible]
    public class SpaceTypeDropdown : EnumAsString<gbXMLSpaceType>
    {
        public SpaceTypeDropdown(WorkspaceModel workspace) : base() { }
    }

    [NodeName("Condition Types Dropdown")]
    [NodeCategory("EnergyAnalysisForDynamo.EnergySettings")]
    [NodeDescription("Select a Condition Type to use with the Zone Parameters node.")]
    [IsDesignScriptCompatible]
    public class ConditionType : EnumAsString<gbXMLConditionType>
    {
        public ConditionType(WorkspaceModel workspace) : base() { }
    }

    [NodeName("Energy Data File Types Dropdown")]
    [NodeCategory("EnergyAnalysisForDynamo.GetAnalysisResults")]
    [NodeDescription("Select a Energy Data Type to use with Get Energy Model Files Node.")]
    [IsDesignScriptCompatible]
    public class EnergyDataFileTypes : EnumAsString<EnergyDataFileType>
    {
        public EnergyDataFileTypes(WorkspaceModel workspace) : base() { }
    }

    public enum EnergyDataFileType
    {
        gbXML,
        doe2,
        eplus
    }
}
