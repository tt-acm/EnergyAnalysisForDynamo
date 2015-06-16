using System;
using System.Collections.Generic;
using System.Globalization;
using DSCore;
using DSCoreNodesUI;
using Dynamo.Models;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using ProtoCore.AST.AssociativeAST;

namespace EnergyAnalysisForDynamo_UI
{
    /// <summary>
    /// Middleman class for creating dropdown nodes in EA4D, using enums as inputs.
    /// </summary>
    public abstract class EA4DDropdownBase : DSDropDownBase
    {
        /// <summary>
        /// Constructor for this abstract class.  Pass in the name of the node, and the enum to convert
        /// </summary>
        /// <param name="value">The name of the node</param>
        /// <param name="e">The enum to populate the dropdown list with</param>
        public EA4DDropdownBase(string value, Enum e) : base(value) 
        {
            stringsFromEnum(e);
        }

        /// <summary>
        /// A local variable to store the list of strings representing the enum
        /// </summary>
        private List<string> myDropdownItems = new List<string>();

        /// <summary>
        /// Populate our local list of strings using the enum that was passed into the constructor
        /// </summary>
        /// <param name="e"></param>
        private void stringsFromEnum(Enum e) 
        {
            foreach (var i in Enum.GetValues(e.GetType()))
            {
                myDropdownItems.Add(i.ToString());
            }
        }

        /// <summary>
        /// The populate items override.  Not sure why this gets called before the constructor, but it does!
        /// </summary>
        public override void PopulateItems()
        {
            Items.Clear();
            foreach (var i in myDropdownItems)
            {
                Items.Add(new DynamoDropDownItem(i, i)); 
            }
            SelectedIndex = 0;
        }

        /// <summary>
        /// Absolutely no clue what this does.  I found an example here and modified it: https://github.com/DynamoDS/DynamoRevit/blob/Revit2015/src/Libraries/RevitNodesUI/RevitDropDown.cs
        /// Ian also helped with this link... https://github.com/DynamoDS/Dynamo/commit/19d37337742f87bbf4bc6283de10ee7bbf7927a1  looks like everything is working again now
        /// </summary>
        /// <param name="inputAstNodes"></param>
        /// <returns></returns>
        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            if (Items.Count == 0 || Items.Count == -1)
            {
                PopulateItems();
            }

            var stringNode = AstFactory.BuildStringNode((string)Items[SelectedIndex].Item);
            var assign = AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), stringNode);

            return new List<AssociativeNode> {assign};
        }
    }


    [NodeName("Building Type Dropdown")]
    [NodeCategory("EnergyAnalysisForDynamo.EnergySettings")]
    [NodeDescription("Select a building type to use with the GBSforDynamo Energy Settings node.")]
    [IsDesignScriptCompatible]
    public class BuildingTypeDropdown : EA4DDropdownBase
    {
        public BuildingTypeDropdown() : base(">", new gbXMLBuildingType()) { }
    }

    [NodeName("HVAC System Type Dropdown")]
    [NodeCategory("EnergyAnalysisForDynamo.EnergySettings")]
    [NodeDescription("Select a HVAC System type to use with the GBSforDynamo Energy Settings node.")]
    [IsDesignScriptCompatible]
    public class HVACtypeDropdown : EA4DDropdownBase
    {
        public HVACtypeDropdown() : base(">", new gbXMLBuildingHVACSystem()) { }
    }

    [NodeName("Operating Schedules Dropdown")]
    [NodeCategory("EnergyAnalysisForDynamo.EnergySettings")]
    [NodeDescription("Select an Operating Schedule to use with the Energy Settings node.")]
    [IsDesignScriptCompatible]
    public class OperatingSchedulesDropdown : EA4DDropdownBase
    {
        public OperatingSchedulesDropdown() : base(">", new gbXMLBuildingOperatingSchedule()) { }
    }

    [NodeName("Conceptual Wall Construction Types Dropdown")]
    [NodeCategory("EnergyAnalysisForDynamo.EnergySettings")]
    [NodeDescription("Select a Conceptual Construction Type to use with the Set Surface Parameters node.")]
    [IsDesignScriptCompatible]
    public class ConcWallConstTypeDropdown : EA4DDropdownBase
    {
        public ConcWallConstTypeDropdown() : base(">", new ConceptualConstructionWallType()) { }
    }
    
    [NodeName("Conceptual Glazing Construction Types Dropdown")]
    [NodeCategory("EnergyAnalysisForDynamo.EnergySettings")]
    [NodeDescription("Select a Conceptual Construction Type to use with the Set Surface Parameters node.")]
    [IsDesignScriptCompatible]
    public class ConcGlazingConstTypeDropdown : EA4DDropdownBase
    {
        public ConcGlazingConstTypeDropdown() : base(">", new ConceptualConstructionWindowSkylightType()) { }
    }

    [NodeName("Conceptual Floor Construction Types Dropdown")]
    [NodeCategory("EnergyAnalysisForDynamo.EnergySettings")]
    [NodeDescription("Select a Conceptual Construction Type to use with the Set Surface Parameters node.")]
    [IsDesignScriptCompatible]
    public class ConcFloorConstTypeDropdown : EA4DDropdownBase
    {
        public ConcFloorConstTypeDropdown() : base(">", new ConceptualConstructionFloorSlabType()) { }
    }

    [NodeName("Conceptual Roof Construction Types Dropdown")]
    [NodeCategory("EnergyAnalysisForDynamo.EnergySettings")]
    [NodeDescription("Select a Conceptual Construction Type to use with the Set Surface Parameters node.")]
    [IsDesignScriptCompatible]
    public class ConcRoofConstTypeDropdown : EA4DDropdownBase
    {
        public ConcRoofConstTypeDropdown() : base(">", new ConceptualConstructionRoofType()) { }
    }

    [NodeName("Space Types Dropdown")]
    [NodeCategory("EnergyAnalysisForDynamo.EnergySettings")]
    [NodeDescription("Select a Space Type to use with the Set Zone Parameters node.")]
    [IsDesignScriptCompatible]
    public class SpaceTypeDropdown : EA4DDropdownBase
    {
        public SpaceTypeDropdown() : base(">", new gbXMLSpaceType()) { }
    }

    [NodeName("Condition Types Dropdown")]
    [NodeCategory("EnergyAnalysisForDynamo.EnergySettings")]
    [NodeDescription("Select a Condition Type to use with the Zone Parameters node.")]
    [IsDesignScriptCompatible]
    public class ConditionType : EA4DDropdownBase
    {
        public ConditionType() : base(">", new gbXMLConditionType()) { }
    }

    [NodeName("Energy Data File Types Dropdown")]
    [NodeCategory("EnergyAnalysisForDynamo.GetAnalysisResults")]
    [NodeDescription("Select a Energy Data Type to use with Get Energy Model Files Node.")]
    [IsDesignScriptCompatible]
    public class EnergyDataFileTypes : EA4DDropdownBase
    {
        public EnergyDataFileTypes() : base(">", new EnergyDataFileType()) { }
    }

    public enum EnergyDataFileType
    {
        gbXML,
        doe2,
        eplus
    }
}
