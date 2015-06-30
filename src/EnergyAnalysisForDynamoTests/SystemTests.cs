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
    public class EnergyAnalysisForDynamo_SystemTesting : RevitSystemTestBase
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
            //check for errors and assert accordingly
            string errString = CompileErrorsIntoString();
            if (string.IsNullOrEmpty(errString))
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail(errString);
            }

        }

        /// <summary>
        /// Test for Example file 1b.  Set some parameters on wall and glazing surfaces in Dynamo, and make sure they were applied in Revit.
        /// </summary>
        [Test, TestModel(@".\EnergyAnalysisForDynamo_ex1_simpleRevitMass.rvt")]
        public void SetSurfaceParameters()
        {
            //open and run the example file
            OpenAndRunDynamoDefinition(@".\EnergyAnalysisForDynamo_ex1b_CreateEnergyModelAndSetSurfaceParams.dyn");
            //check for errors and assert accordingly
            string errString = CompileErrorsIntoString();
            if (string.IsNullOrEmpty(errString))
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail(errString);
            }

        }

        /// <summary>
        /// Test for Example file 1c.  Set some parameters on a zone in Dynamo, and make sure they were applied in Revit.
        /// </summary>
        [Test, TestModel(@".\EnergyAnalysisForDynamo_ex1_simpleRevitMass.rvt")]
        public void SetZoneParameters()
        {
            //open and run the example file
            OpenAndRunDynamoDefinition(@".\EnergyAnalysisForDynamo_ex1c_CreateEnergyModelAndSetZoneParams.dyn");
            //check for errors and assert accordingly
            string errString = CompileErrorsIntoString();
            if (string.IsNullOrEmpty(errString))
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail(errString);
            }
        }

        /// <summary>
        /// Test for Example file 1d.  Create a series of iterations to analyze with GBS.
        /// </summary>
        [Test, TestModel(@".\EnergyAnalysisForDynamo_ex1_simpleRevitMass.rvt")]
        public void IterativeAnalysis()
        {
            //open and run the example file
            OpenAndRunDynamoDefinition(@".\EnergyAnalysisForDynamo_ex1d_iterativeAnalysisExample.dyn");
            //check for errors and assert accordingly
            string errString = CompileErrorsIntoString();
            if (string.IsNullOrEmpty(errString))
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail(errString);
            }
        }

        /// <summary>
        /// Test for Example file 1e.  Set surface parametes based on their orientation.
        /// </summary>
        [Test, TestModel(@".\EnergyAnalysisForDynamo_ex1_simpleRevitMass.rvt")]
        public void SetSurfaceParamsByOrientation()
        {
            //open and run the example file
            OpenAndRunDynamoDefinition(@".\EnergyAnalysisForDynamo_ex1e_SetSurfaceParamsByOrientation.dyn");
            //check for errors and assert accordingly
            string errString = CompileErrorsIntoString();
            if (string.IsNullOrEmpty(errString))
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail(errString);
            }
        }

        /// <summary>
        /// Test for example 2a.  Drive glazing percentages on a bunch of surfaces
        /// </summary>
        [Test, TestModel(@".\EnergyAnalysisForDynamo_ex2_fancyRevitMass.rvt")]
        public void DriveGlazingPercentageByOrientation()
        {
            //open and run the example file
            OpenAndRunDynamoDefinition(@".\EnergyAnalysisForDynamo_ex2a_DriveSurfacesByOrientation.dyn");
            //check for errors and assert accordingly
            string errString = CompileErrorsIntoString();
            if (string.IsNullOrEmpty(errString))
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail(errString);
            }
        }

        /// <summary>
        /// Test for example 3a.  Create a GBXML file from a mass
        /// </summary>
        [Test, TestModel(@".\EnergyAnalysisForDynamo_ex3_simpleRevitMassWithFloor.rvt")]
        public void CreateGbxmlFromMass()
        {
            //open and run the example file
            OpenAndRunDynamoDefinition(@".\EnergyAnalysisForDynamo_ex3a_CreategbXMLfromMass.dyn");
            //check for errors and assert accordingly
            string errString = CompileErrorsIntoString();
            if (string.IsNullOrEmpty(errString))
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail(errString);
            }
        }

        /// <summary>
        /// Test for example 3b.  Create a GBXML file from zones
        /// </summary>
        [Test, TestModel(@".\EnergyAnalysisForDynamo_ex3_simpleRevitMassWithFloor.rvt")]
        public void CreateGbxmlFromZones()
        {
            //open and run the example file
            OpenAndRunDynamoDefinition(@".\EnergyAnalysisForDynamo_ex3b_CreategbXMLfromZones.dyn");
            //check for errors and assert accordingly
            string errString = CompileErrorsIntoString();
            if (string.IsNullOrEmpty(errString))
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail(errString);
            }
        }

        /// <summary>
        /// Example 4a.  Create a new project and get a list of projects
        /// </summary>
        [Test, TestModel(@".\EnergyAnalysisForDynamo_ex1_simpleRevitMass.rvt")]
        public void CreateNewGbsProject()
        {
            //open and run the example file
            OpenAndRunDynamoDefinition(@".\EnergyAnalysisForDynamo_ex4a_CreateNewProjectAndGetProjectLists.dyn");
            //check for errors and assert accordingly
            string errString = CompileErrorsIntoString();
            if (string.IsNullOrEmpty(errString))
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail(errString);
            }
        }

        /// <summary>
        /// Example 4b.  Upload gbxml into a new project and create a base run
        /// </summary>
        [Test, TestModel(@".\EnergyAnalysisForDynamo_ex1_simpleRevitMass.rvt")]
        public void UploadGbxmlAndCreateBaseRun()
        {
            //open and run the example file
            OpenAndRunDynamoDefinition(@".\EnergyAnalysisForDynamo_ex4b_UploadgbxmlAndCreateBaseRun.dyn");
            //check for errors and assert accordingly
            string errString = CompileErrorsIntoString();
            if (string.IsNullOrEmpty(errString))
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail(errString);
            }
        }

        /// <summary>
        /// Example 5.  Get the results of an analysis
        /// </summary>
        [Test, TestModel(@".\EnergyAnalysisForDynamo_ex1_simpleRevitMass.rvt")]
        public void GetRunResults()
        {
            //open and run the example file
            OpenAndRunDynamoDefinition(@".\EnergyAnalysisForDynamo_ex5a_GetRunResultsSummary.dyn");
            //check for errors and assert accordingly
            string errString = CompileErrorsIntoString();
            if (string.IsNullOrEmpty(errString))
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail(errString);
            }
        }

        /// <summary>
        /// Example 6.  Download the detailed results of an analysis
        /// </summary>
        [Test, TestModel(@".\EnergyAnalysisForDynamo_ex1_simpleRevitMass.rvt")]
        public void DownloadDetailedResults()
        {
            //open and run the example file
            OpenAndRunDynamoDefinition(@".\EnergyAnalysisForDynamo_ex6_DownloadDetailedResults.dyn");
            
            //check for errors and assert accordingly
            string errString = CompileErrorsIntoString();
            if (string.IsNullOrEmpty(errString))
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail(errString);
            }

        }

        /// <summary>
        /// A utility function to loop over a sample file and list any nodes in error or warning state.
        /// </summary>
        /// <returns></returns>
        private string CompileErrorsIntoString()
        {
            //a string to return
            string errors = null;

            //loop over the active collection of nodes.
            foreach (var i in AllNodes)
            {
                if (IsNodeInErrorOrWarningState(i.GUID.ToString()))
                {
                    errors += "The node called '" + i.NickName + "' failed or threw a warning." + System.Environment.NewLine;
                }
            }

            //return the errors string
            return errors;
        }

    }
}
