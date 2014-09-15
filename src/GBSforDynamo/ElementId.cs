using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using Autodesk.Revit.DB;
using RevitServices.Persistence;
using Autodesk.DesignScript.Runtime;

namespace GBSforDynamo
{
    /// <summary>
    /// Wrapper class for Autodesk.Revit.DB.ElementId.
    /// </summary>
    public class ElementId
    {
        //the revit ID that we are wrapping around.
        private Autodesk.Revit.DB.ElementId internalId;

        //access to the internal ID using an int.
        public int InternalId
        {
            [IsVisibleInDynamoLibrary(false)]
            get { return internalId.IntegerValue; }
            [IsVisibleInDynamoLibrary(false)]
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
                    throw new Exception("GBSforDynamo internal error - could not find a Revit Element with the specified ElementId.  Here is the exception that was thrown: \n\n" + ex.ToString());
                }
            }
        }

        //constructor
        [IsVisibleInDynamoLibrary(false)]
        public ElementId(int id)
        {
            InternalId = id;
        }
        [IsVisibleInDynamoLibrary(false)]
        public ElementId() { }
    }
}
