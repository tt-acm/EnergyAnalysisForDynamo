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
            
            //glazing percentage
            var myTargetGlazingPercentage = GetPreviewValue("83f5eb3b-234f-4081-8461-bd1af9ae6708");
            var es = Autodesk.Revit.DB.Analysis.EnergyDataSettings.GetFromDocument(DocumentManager.Instance.CurrentUIDocument.Document);
            if ((double)myTargetGlazingPercentage != es.PercentageGlazing)
            {
                Assert.Fail();
            }


            //if we got here, nothing failed.
            Assert.Pass();

        }

        /// <summary>
        /// 
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
            var mySurface = (MassSurfaceData)DocumentManager.Instance.CurrentUIDocument.Document.GetElement(new Autodesk.Revit.DB.ElementId(205245));


            //Glazing percentage

            //get the target glazing percentage
            double myTargetGlazingPercentage = (double)GetPreviewValue("f4b472cb-7f0e-487d-8ff3-1b951d0a9f8b");

            //do the target and the actual match?
            if (myTargetGlazingPercentage != mySurface.PercentageGlazing)
            {
                Assert.Fail();
            }


            //Shading Depth


            //Sill Height


            //Conceptual Construction


            //if we got here, nothing failed.
            Assert.Pass();

        }
    }
}
