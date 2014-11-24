using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using Autodesk.Revit.DB;
using RevitServices.Persistence;
using Autodesk.DesignScript.Runtime;

namespace EnergyAnalysisForDynamo
{
    /// <summary>
    /// Wrapper class for Autodesk.Revit.DB.ElementId.
    /// </summary>
    [IsVisibleInDynamoLibrary(false)]
    public class ElementId
    {
        //the revit ID that we are wrapping around.
        private Autodesk.Revit.DB.ElementId internalId;

        /// <summary>
        /// The int representation of the [Revit] Element Id.
        /// </summary>
        public int InternalId
        {
            [SupressImportIntoVM]
            get { return internalId.IntegerValue; }
            [SupressImportIntoVM]
            set
            {
                try
                {
                    Document RvtDoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument.Document;
                    Autodesk.Revit.DB.ElementId i = new Autodesk.Revit.DB.ElementId(value);
                    internalId = RvtDoc.GetElement(i).Id;
                }
                catch (Exception ex)
                {
                    throw new Exception("GBSforDynamo internal error - could not find a Revit Element with the specified ElementId.  Here is the actual exception that was thrown by Revit: \n\n" + ex.ToString());
                }
            }
        }

        /// <summary>
        /// Returns a string that represents the current object
        /// </summary>
        /// <returns></returns>
        [SupressImportIntoVM]
        public override string ToString()
        {
            return internalId.ToString();
        }

        /// <summary>
        /// New ElementId instance with int input to set the Id
        /// </summary>
        /// <param name="id">The int representation of a Revit ElementId</param>
        [SupressImportIntoVM]
        public ElementId(int id)
        {
            InternalId = id;
        }

        /// <summary>
        /// Default constructor override
        /// </summary>
        [SupressImportIntoVM]
        public ElementId() { }
    }
}
