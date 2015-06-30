using System;
using System.Collections.Generic;
using System.Globalization;
using DSCore;
using DSCoreNodesUI;
using Dynamo.Models;
using Dynamo.Utilities;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using ProtoCore.AST.AssociativeAST;

namespace EnergyAnalysisForDynamo_UI
{

    [NodeName("Building Type Dropdown")]
    [NodeCategory("EnergyAnalysisForDynamo.EnergySettings")]
    [NodeDescription("Select a building type to use with the GBSforDynamo Energy Settings node.")]
    [IsDesignScriptCompatible]
    public class BuildingTypeDropdown : DSDropDownBase
    {
        public BuildingTypeDropdown() : base(">") { }

        public override void PopulateItems()
        {
            //clear items
            Items.Clear();

            //set up the collection
            var newItems = new List<DynamoDropDownItem>();
            foreach (var j in Enum.GetValues(new gbXMLBuildingType().GetType())) //PAss in the enum here!
            {
                newItems.Add(new DynamoDropDownItem(j.ToString(), j.ToString()));
            }
            Items.AddRange(newItems);

            //set the selected index to 0
            SelectedIndex = 0;
        }

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            // Build an AST node for the type of object contained in your Items collection.

            var intNode = AstFactory.BuildStringNode((string)Items[SelectedIndex].Item);
            var assign = AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), intNode);

            return new List<AssociativeNode> {assign};
        }
        
    }

    [NodeName("HVAC System Type Dropdown")]
    [NodeCategory("EnergyAnalysisForDynamo.EnergySettings")]
    [NodeDescription("Select a HVAC System type to use with the GBSforDynamo Energy Settings node.")]
    [IsDesignScriptCompatible]
    public class HVACtypeDropdown : DSDropDownBase
    {
        public HVACtypeDropdown() : base(">") { }

        public override void PopulateItems()
        {
            //clear items
            Items.Clear();

            //set up the collection
            var newItems = new List<DynamoDropDownItem>();
            foreach (var j in Enum.GetValues(new gbXMLBuildingHVACSystem().GetType())) //PAss in the enum here!
            {
                newItems.Add(new DynamoDropDownItem(j.ToString(), j.ToString()));
            }
            Items.AddRange(newItems);

            //set the selected index to 0
            SelectedIndex = 0;
        }

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            // Build an AST node for the type of object contained in your Items collection.

            var intNode = AstFactory.BuildStringNode((string)Items[SelectedIndex].Item);
            var assign = AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), intNode);

            return new List<AssociativeNode> {assign};
        }
    }

    [NodeName("Operating Schedules Dropdown")]
    [NodeCategory("EnergyAnalysisForDynamo.EnergySettings")]
    [NodeDescription("Select an Operating Schedule to use with the Energy Settings node.")]
    [IsDesignScriptCompatible]
    public class OperatingSchedulesDropdown : DSDropDownBase
    {
        public OperatingSchedulesDropdown() : base(">") { }

        public override void PopulateItems()
        {
            //clear items
            Items.Clear();

            //set up the collection
            var newItems = new List<DynamoDropDownItem>();
            foreach (var j in Enum.GetValues(new gbXMLBuildingOperatingSchedule().GetType())) //PAss in the enum here!
            {
                newItems.Add(new DynamoDropDownItem(j.ToString(), j.ToString()));
            }
            Items.AddRange(newItems);

            //set the selected index to 0
            SelectedIndex = 0;
        }

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            // Build an AST node for the type of object contained in your Items collection.

            var intNode = AstFactory.BuildStringNode((string)Items[SelectedIndex].Item);
            var assign = AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), intNode);

            return new List<AssociativeNode> {assign};
        }
    }

    [NodeName("Conceptual Wall Construction Types Dropdown")]
    [NodeCategory("EnergyAnalysisForDynamo.EnergySettings")]
    [NodeDescription("Select a Conceptual Construction Type to use with the Set Surface Parameters node.")]
    [IsDesignScriptCompatible]
    public class ConcWallConstTypeDropdown : DSDropDownBase
    {
        public ConcWallConstTypeDropdown() : base(">") { }

        public override void PopulateItems()
        {
            //clear items
            Items.Clear();

            //set up the collection
            var newItems = new List<DynamoDropDownItem>();
            foreach (var j in Enum.GetValues(new ConceptualConstructionWallType().GetType())) //PAss in the enum here!
            {
                newItems.Add(new DynamoDropDownItem(j.ToString(), j.ToString()));
            }
            Items.AddRange(newItems);

            //set the selected index to 0
            SelectedIndex = 0;
        }

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            // Build an AST node for the type of object contained in your Items collection.

            var intNode = AstFactory.BuildStringNode((string)Items[SelectedIndex].Item);
            var assign = AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), intNode);

            return new List<AssociativeNode> {assign};
        }
    }
    
    [NodeName("Conceptual Glazing Construction Types Dropdown")]
    [NodeCategory("EnergyAnalysisForDynamo.EnergySettings")]
    [NodeDescription("Select a Conceptual Construction Type to use with the Set Surface Parameters node.")]
    [IsDesignScriptCompatible]
    public class ConcGlazingConstTypeDropdown : DSDropDownBase
    {
        public ConcGlazingConstTypeDropdown() : base(">") { }

        public override void PopulateItems()
        {
            //clear items
            Items.Clear();

            //set up the collection
            var newItems = new List<DynamoDropDownItem>();
            foreach (var j in Enum.GetValues(new ConceptualConstructionWindowSkylightType().GetType())) //PAss in the enum here!
            {
                newItems.Add(new DynamoDropDownItem(j.ToString(), j.ToString()));
            }
            Items.AddRange(newItems);

            //set the selected index to 0
            SelectedIndex = 0;
        }

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            // Build an AST node for the type of object contained in your Items collection.

            var intNode = AstFactory.BuildStringNode((string)Items[SelectedIndex].Item);
            var assign = AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), intNode);

            return new List<AssociativeNode> {assign};
        }
    }

    [NodeName("Conceptual Floor Construction Types Dropdown")]
    [NodeCategory("EnergyAnalysisForDynamo.EnergySettings")]
    [NodeDescription("Select a Conceptual Construction Type to use with the Set Surface Parameters node.")]
    [IsDesignScriptCompatible]
    public class ConcFloorConstTypeDropdown : DSDropDownBase
    {
        public ConcFloorConstTypeDropdown() : base(">") { }

        public override void PopulateItems()
        {
            //clear items
            Items.Clear();

            //set up the collection
            var newItems = new List<DynamoDropDownItem>();
            foreach (var j in Enum.GetValues(new ConceptualConstructionFloorSlabType().GetType())) //PAss in the enum here!
            {
                newItems.Add(new DynamoDropDownItem(j.ToString(), j.ToString()));
            }
            Items.AddRange(newItems);

            //set the selected index to 0
            SelectedIndex = 0;
        }

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            // Build an AST node for the type of object contained in your Items collection.

            var intNode = AstFactory.BuildStringNode((string)Items[SelectedIndex].Item);
            var assign = AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), intNode);

            return new List<AssociativeNode> {assign};
        }
    }

    [NodeName("Conceptual Roof Construction Types Dropdown")]
    [NodeCategory("EnergyAnalysisForDynamo.EnergySettings")]
    [NodeDescription("Select a Conceptual Construction Type to use with the Set Surface Parameters node.")]
    [IsDesignScriptCompatible]
    public class ConcRoofConstTypeDropdown : DSDropDownBase
    {
        public ConcRoofConstTypeDropdown() : base(">") { }

        public override void PopulateItems()
        {
            //clear items
            Items.Clear();

            //set up the collection
            var newItems = new List<DynamoDropDownItem>();
            foreach (var j in Enum.GetValues(new ConceptualConstructionRoofType().GetType())) //PAss in the enum here!
            {
                newItems.Add(new DynamoDropDownItem(j.ToString(), j.ToString()));
            }
            Items.AddRange(newItems);

            //set the selected index to 0
            SelectedIndex = 0;
        }

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            // Build an AST node for the type of object contained in your Items collection.

            var intNode = AstFactory.BuildStringNode((string)Items[SelectedIndex].Item);
            var assign = AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), intNode);

            return new List<AssociativeNode> {assign};
        }
    }

    [NodeName("Space Types Dropdown")]
    [NodeCategory("EnergyAnalysisForDynamo.EnergySettings")]
    [NodeDescription("Select a Space Type to use with the Set Zone Parameters node.")]
    [IsDesignScriptCompatible]
    public class SpaceTypeDropdown : DSDropDownBase
    {
        public SpaceTypeDropdown() : base(">") { }

        public override void PopulateItems()
        {
            //clear items
            Items.Clear();

            //set up the collection
            var newItems = new List<DynamoDropDownItem>();
            foreach (var j in Enum.GetValues(new gbXMLSpaceType().GetType())) //PAss in the enum here!
            {
                newItems.Add(new DynamoDropDownItem(j.ToString(), j.ToString()));
            }
            Items.AddRange(newItems);

            //set the selected index to 0
            SelectedIndex = 0;
        }

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            // Build an AST node for the type of object contained in your Items collection.

            var intNode = AstFactory.BuildStringNode((string)Items[SelectedIndex].Item);
            var assign = AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), intNode);

            return new List<AssociativeNode> {assign};
        }
    }

    [NodeName("Condition Types Dropdown")]
    [NodeCategory("EnergyAnalysisForDynamo.EnergySettings")]
    [NodeDescription("Select a Condition Type to use with the Zone Parameters node.")]
    [IsDesignScriptCompatible]
    public class ConditionType : DSDropDownBase
    {
        public ConditionType() : base(">") { }

        public override void PopulateItems()
        {
            //clear items
            Items.Clear();

            //set up the collection
            var newItems = new List<DynamoDropDownItem>();
            foreach (var j in Enum.GetValues(new gbXMLConditionType().GetType())) //PAss in the enum here!
            {
                newItems.Add(new DynamoDropDownItem(j.ToString(), j.ToString()));
            }
            Items.AddRange(newItems);

            //set the selected index to 0
            SelectedIndex = 0;
        }

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            // Build an AST node for the type of object contained in your Items collection.

            var intNode = AstFactory.BuildStringNode((string)Items[SelectedIndex].Item);
            var assign = AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), intNode);

            return new List<AssociativeNode> {assign};
        }
    }

    [NodeName("Energy Data File Types Dropdown")]
    [NodeCategory("EnergyAnalysisForDynamo.GetAnalysisResults")]
    [NodeDescription("Select a Energy Data Type to use with Get Energy Model Files Node.")]
    [IsDesignScriptCompatible]
    public class EnergyDataFileTypes : DSDropDownBase
    {
        public EnergyDataFileTypes() : base(">") { }

        public override void PopulateItems()
        {
            //clear items
            Items.Clear();

            //set up the collection
            var newItems = new List<DynamoDropDownItem>();
            foreach (var j in Enum.GetValues(new EnergyDataFileType().GetType())) //PAss in the enum here!
            {
                newItems.Add(new DynamoDropDownItem(j.ToString(), j.ToString()));
            }
            Items.AddRange(newItems);

            //set the selected index to 0
            SelectedIndex = 0;
        }

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            // Build an AST node for the type of object contained in your Items collection.

            var intNode = AstFactory.BuildStringNode((string)Items[SelectedIndex].Item);
            var assign = AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), intNode);

            return new List<AssociativeNode> {assign};
        }
    }

    public enum EnergyDataFileType
    {
        gbXML,
        doe2,
        eplus
    }
}
