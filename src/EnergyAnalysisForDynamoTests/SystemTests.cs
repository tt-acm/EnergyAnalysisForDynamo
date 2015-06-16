using System.IO;
using System.Reflection;
using NUnit.Framework;
using RevitTestServices;
using RTF.Framework;
using Autodesk.Revit.DB;
using RevitServices.Persistence;
using Autodesk.Revit.DB.Analysis;
using System.Collections.Generic;
using EnergyAnalysisForDynamo;



namespace EnergyAnalysisForDynamoTests
{
    [TestFixture]
    public class SystemTestExample : RevitSystemTestBase
    {
        [SetUp]
        public void Setup()
        {
            // Set the working directory. This will allow you to use the OpenAndRunDynamoDefinition method,
            // specifying a relative path to the .dyn file you want to test.

            var asmDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            workingDirectory = Path.GetFullPath(Path.Combine(asmDirectory,
                @"..\..\..\packages\EnergyAnalysisForDynamo\extra"));
        }

        /// <summary>
        /// Test for Example file 1a.  Set the Revit Project's energy settings, and check to make sure the settings were applied.
        /// </summary>
        [Test, TestModel(@".\EnergyAnalysisForDynamo_ex1_simpleRevitMass.rvt")]
        public void SetProjectEnergySettings()
        {
            //open and run the example file
            OpenAndRunDynamoDefinition(@".\EnergyAnalysisForDynamo_ex1a_SetProjectEnergySettings.dyn");
            

            //check to see that the value[s] that we set with Dynamo actually took in Revit.

            //Revit energy settings object
            var es = Autodesk.Revit.DB.Analysis.EnergyDataSettings.GetFromDocument(DocumentManager.Instance.CurrentUIDocument.Document);


            //glazing percentage
            var myTargetGlazingPercentage = (double)GetPreviewValue("83f5eb3b-234f-4081-8461-bd1af9ae6708");
            if (myTargetGlazingPercentage != es.PercentageGlazing) Assert.Fail();

            //shade depth
            var myTargetShadeDepth = (double)GetPreviewValue("d4373a50-6b22-49ae-a392-90a2ab77148a");
            if (myTargetShadeDepth != es.ShadeDepth) Assert.Fail();

            //skylight percentage
            var myTargetSkylightPercentage = (double)GetPreviewValue("68e4d7c7-bdb7-419a-a77d-17a8850e486f");
            if (myTargetSkylightPercentage != es.PercentageSkylights) Assert.Fail();

            //HVAC system type --- fix dropdowns first

            //Operating Schedule --- fix dropdowns first

            //Core offset amount
            var myTargetCoreOFfset = (double)GetPreviewValue("8972da79-c508-4dd2-ab56-6301cd4e5128");
            if (myTargetCoreOFfset != es.MassZoneCoreOffset) Assert.Fail();

            //Dividie perimeter
            var myTargetDividePerimeter = (bool)GetPreviewValue("8da9e555-e7e4-4143-9b50-ef9b2dd8f069");
            if (myTargetDividePerimeter != es.MassZoneDividePerimeter) Assert.Fail();


            //if we got here, nothing failed.
            Assert.Pass();

        }

        /// <summary>
        /// Test for Example file 1b.  Set some parameters on wall and glazing surfaces in Dynamo, and make sure they were applied in Revit.
        /// </summary>
        [Test, TestModel(@".\EnergyAnalysisForDynamo_ex1_simpleRevitMass.rvt")]
        public void SetSurfaceParameters()
        {
            //open and run the example file
            OpenAndRunDynamoDefinition(@".\EnergyAnalysisForDynamo_ex1b_CreateEnergyModelAndSetSurfaceParams.dyn");

            ////get a handle on the surface we are setting properties on
            //var mySurfaceID = (EnergyAnalysisForDynamo.ElementId)GetPreviewValue("98cb8c9e-8cdd-488a-9e8f-02fb9d84d721");
            //var mySurface = (MassSurfaceData)DocumentManager.Instance.CurrentUIDocument.Document.GetElement(new Autodesk.Revit.DB.ElementId( mySurfaceID.InternalId));

            //BH 20150616 - I couldn't get the above to work - could not get any preview values back from our nodes or from a code block.  
            //Everything came back null.  ???   Hard coding for now...
            var myWallSurface = (MassSurfaceData)DocumentManager.Instance.CurrentUIDocument.Document.GetElement(new Autodesk.Revit.DB.ElementId(205245));
            //var myWindowSurface = (MassSurfaceData)DocumentManager.Instance.CurrentUIDocument.Document.GetElement(new Autodesk.Revit.DB.ElementId(205256));


            //Glazing percentage
            double myTargetGlazingPercentage = (double)GetPreviewValue("f4b472cb-7f0e-487d-8ff3-1b951d0a9f8b");
            if (myTargetGlazingPercentage != myWallSurface.PercentageGlazing) Assert.Fail();

            //Shading Depth
            double myTargetShadingDepth = (double)GetPreviewValue("9299930a-564d-4996-9ef1-2ad42f4e86cd");
            if (myTargetShadingDepth != myWallSurface.ShadeDepth) Assert.Fail();

            //Sill Height
            double myTargetSillHeight = (double)GetPreviewValue("f996a98e-44f0-44c0-9086-7ca30a646454");
            if (myTargetSillHeight != myWallSurface.SillHeight) Assert.Fail();

            //Wall Conceptual Construction --- fix dropdowns first.

            //Window Conceptual Construction --- fix dropdowns first.


            //if we got here, nothing failed.
            Assert.Pass();

        }
    }
}
