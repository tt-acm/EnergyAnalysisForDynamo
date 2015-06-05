using System.IO;
using System.Reflection;
using NUnit.Framework;
using RevitTestServices;
using RTF.Framework;

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
                @"..\..\..\exampleFiles"));
        }

        [Test, TestModel(@".\EnergyAnalysisForDynamo_ex1_simpleRevitMass.rvt")]
        public void SetProjectEnergySettings()
        {
            OpenAndRunDynamoDefinition(@".\EnergyAnalysisForDynamo_ex1a_SetProjectEnergySettings.dyn");
        }
    }
}
