using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using DSCore;
using DSCoreNodesUI;
using Dynamo.Models;
using Dynamo.Nodes;
using Dynamo.Utilities;
using ProtoCore.AST.AssociativeAST;
using RevitServices.Persistence;
using RevitServices.Transactions;
using Revit.Elements;
using Revit.GeometryConversion;
using Autodesk.DesignScript.Runtime;
using Autodesk.DesignScript.Interfaces;
using Autodesk.DesignScript.Geometry;
using System.Text.RegularExpressions;


namespace GBSforDynamo
{
    /// <summary>
    /// Contains Dynamo nodes that deal with a Revit model's Analysis zones and surfaces
    /// </summary>
    public static class AnalysisZones
    {
        /// <summary>
        /// Draws a point around the center of an analysis surface.  Useful for sorting/grouping surfaces upstream of a SetSurfaceParameters node.
        /// </summary>
        /// <param name="SurfaceId">The ElementId of the surface to create a point from.  Get this from the AnalysisZones > CreateFrom* > SurfaceIds output list</param>
        /// <returns></returns>
        public static Autodesk.DesignScript.Geometry.Point AnalysisSurfacePoint(ElementId SurfaceId)
        {
            //local varaibles
            Document RvtDoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument.Document;
            MassSurfaceData surf = null;
            Autodesk.Revit.DB.ElementId myEnergyModelId = null;

            //try to get the MassSurfaceData object from the document
            try
            {
                surf = (MassSurfaceData)RvtDoc.GetElement(new Autodesk.Revit.DB.ElementId(SurfaceId.InternalId));
                if (surf == null) throw new Exception();
            }
            catch (Exception)
            {
                throw new Exception("Couldn't find a MassSurfaceData object with Id #: " + SurfaceId.ToString());
            }

            //try to get the element id of the MassEnergyAnalyticalModel - we need this to pull faces from
            try
            {
                myEnergyModelId = surf.ReferenceElementId;
                if (myEnergyModelId == null) throw new Exception();
            }
            catch (Exception)
            {
                throw new Exception("Couldn't find a MassEnergyAnalyticalModel object belonging to the Mass instance with Id #: " + surf.ReferenceElementId.ToString());
            }

            //try to get the MassSurfaceData object from the document
            try
            {
                surf = (MassSurfaceData)RvtDoc.GetElement(new Autodesk.Revit.DB.ElementId(SurfaceId.InternalId));
                if (surf == null) throw new Exception();
            }
            catch (Exception)
            {
                throw new Exception("Couldn't find a MassSurfaceData object with Id #: " + SurfaceId.ToString());
            }

            //get the smallest face
            Autodesk.Revit.DB.Face smallFace = GetSmallestFace(RvtDoc, surf, myEnergyModelId);

            //get the average point of all points on the face
            Autodesk.DesignScript.Geometry.Point outPoint = getAveragePointFromFace(smallFace);
            return outPoint;
        }

        /// <summary>
        /// Returns a vector represnting the normal of an analysis surface.  Useful for sorting/grouping surfaces upstream of a SetSurfaceParameters node.
        /// </summary>
        /// <param name="SurfaceId">The ElementId of the surface to create a vector from.  Get this from AnalysisZones > CreateFrom* > SurfaceIds output list</param>
        /// <returns></returns>
        public static Autodesk.DesignScript.Geometry.Vector AnalysisSurfaceVector(ElementId SurfaceId)
        {
            //local varaibles
            Document RvtDoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument.Document;
            MassSurfaceData surf = null;
            Autodesk.Revit.DB.ElementId myEnergyModelId = null;

            //try to get the MassSurfaceData object from the document
            try
            {
                surf = (MassSurfaceData)RvtDoc.GetElement(new Autodesk.Revit.DB.ElementId(SurfaceId.InternalId));
                if (surf == null) throw new Exception();
            }
            catch (Exception)
            {
                throw new Exception("Couldn't find a MassSurfaceData object with Id #: " + SurfaceId.ToString());
            }

            //try to get the element id of the MassEnergyAnalyticalModel - we need this to pull faces from
            try
            {
                myEnergyModelId = surf.ReferenceElementId;
                if (myEnergyModelId == null) throw new Exception();
            }
            catch (Exception)
            {
                throw new Exception("Couldn't find a MassEnergyAnalyticalModel object belonging to the Mass instance with Id #: " + surf.ReferenceElementId.ToString());
            }

            //try to get the MassSurfaceData object from the document
            try
            {
                surf = (MassSurfaceData)RvtDoc.GetElement(new Autodesk.Revit.DB.ElementId(SurfaceId.InternalId));
                if (surf == null) throw new Exception();
            }
            catch (Exception)
            {
                throw new Exception("Couldn't find a MassSurfaceData object with Id #: " + SurfaceId.ToString());
            }

            //get the smallest face
            Autodesk.Revit.DB.Face bigFace = GetLargestFace(RvtDoc, surf, myEnergyModelId);

            // Find the face normal at the center of the face
            BoundingBoxUV bbox = bigFace.GetBoundingBox();
            // center of the face in the UV of the face
            Autodesk.Revit.DB.UV center = new Autodesk.Revit.DB.UV((bbox.Max.U - bbox.Min.U) / 2 + bbox.Min.U, (bbox.Max.V - bbox.Min.V) / 2 + bbox.Min.V);
            XYZ faceNormal = bigFace.ComputeNormal(center);
            XYZ normal = faceNormal.Normalize();
            return Autodesk.DesignScript.Geometry.Vector.ByCoordinates(normal.X, normal.Y, normal.Z, true);
        }
        
        /// <summary>
        /// Creates mass floors and analysis zones from a [conceptual mass] family instance and a list of levels.
        /// </summary>
        /// <param name="MassFamilyInstance">The conceptual mass family instance to create zones from</param>
        /// <param name="Levels">A list of levels to create mass floors with</param>
        /// <returns></returns>
        [MultiReturn("MassFamilyInstance", "ZoneIds", "SurfaceIds")]
        public static Dictionary<string, object> CreateFromMassAndLevels(AbstractFamilyInstance MassFamilyInstance, List<Revit.Elements.Element> Levels)
        {
            //local varaibles
            Document RvtDoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument.Document;
            EnergyAnalysisDetailModel em = null;

            #region Mass Floors and Energy Model
            //create mass floors
            try
            {
                TransactionManager.Instance.EnsureInTransaction(RvtDoc);
                foreach (var l in Levels)
                {
                    MassInstanceUtils.AddMassLevelDataToMassInstance(RvtDoc, MassFamilyInstance.InternalElement.Id, l.InternalElement.Id);
                }
                TransactionManager.Instance.TransactionTaskDone();
            }
            catch (Exception)
            {
                throw new Exception("Something went wrong when trying to create mass floors.");
            }

            //enable the analytical model in the document if it isn't already
            try
            {
                ActivateEnergyModel(RvtDoc);
            }
            catch (Exception)
            {
                throw new Exception("Something went wrong when trying to enable the energy model.");
            }

            //try to create an energy analysis model in the document
            try
            {
                TransactionManager.Instance.EnsureInTransaction(RvtDoc);
                EnergyAnalysisDetailModelOptions opt = new EnergyAnalysisDetailModelOptions();
                opt.Tier = EnergyAnalysisDetailModelTier.NotComputed; 
                em = EnergyAnalysisDetailModel.Create(RvtDoc, opt);
                TransactionManager.Instance.TransactionTaskDone();
                DocumentManager.Regenerate();
            }
            catch (Exception)
            {
                throw new Exception("Something went wrong when trying to create an energy model from your mass.");
            }
            #endregion

            #region Output Zone and Surface IDs
            //ok now we should be able to get all of the zones and surfaces associated
            //with our mass, and output them for use downstream


            //get the id of the analytical model associated with that mass
            Autodesk.Revit.DB.ElementId myEnergyModelId = MassEnergyAnalyticalModel.GetMassEnergyAnalyticalModelIdForMassInstance(RvtDoc, MassFamilyInstance.InternalElement.Id);
            MassEnergyAnalyticalModel mea = (MassEnergyAnalyticalModel)RvtDoc.GetElement(myEnergyModelId);

            //if mea is null, we should throw a good error message.  Seems to be working in Revit now that we are regenerating.
            if (mea == null)
            {
                throw new Exception("Could not get the MassEnergyAnalyticalModel from the mass.");
            }

            //get the zone ids from our Mass's MassEnergyAnalyticalModel object
            //we'll use these to draw zones in another component - not sure if we can use them to drive opening / shading params
            List<Autodesk.Revit.DB.ElementId> zoneIds = mea.GetMassZoneIds().ToList();

            //get the MassSurfaceData ids of the definitions belonging to external faces
            //we'll output these, and then try to visualize the faces and change parameters in another component

            //get references to the faces using the mass - we need these to get at the surface data
            IList<Reference> faceRefs = mea.GetReferencesToAllFaces();

            //some faces share massSurfaceData definitions - here we're pulling out unique data definitions.  not totally sure how this all works yet...
            Dictionary<int, MassSurfaceData> mySurfaceData = new Dictionary<int, MassSurfaceData>();
            foreach (var fr in faceRefs)
            {
                Autodesk.Revit.DB.ElementId id = mea.GetMassSurfaceDataIdForReference(fr);
                if (!mySurfaceData.ContainsKey(id.IntegerValue))
                {
                    MassSurfaceData d = (MassSurfaceData)RvtDoc.GetElement(id);
                    mySurfaceData.Add(id.IntegerValue, d);
                }
            }

            //filter by category = mass exterior wall
            var allSurfsList = mySurfaceData.Values.ToList();
            var extSurfList = from n in allSurfsList
                              where n.Category.Name == "Mass Exterior Wall"
                              select n;

            //output list of face Ids
            List<Autodesk.Revit.DB.ElementId> surfaceIds = new List<Autodesk.Revit.DB.ElementId>();
            foreach (var s in extSurfList)
            {
                surfaceIds.Add(s.Id);
            }


            //loop over the output lists, and wrap them in our ElementId wrapper class
            List<ElementId> outZoneIds = zoneIds.Select(e => new ElementId(e.IntegerValue)).ToList();
            List<ElementId> outSurfaceIds = surfaceIds.Select(e => new ElementId(e.IntegerValue)).ToList();


            #endregion


            return new Dictionary<string, object>
            {
                {"MassFamilyInstance", MassFamilyInstance},
                {"ZoneIds", outZoneIds},
                {"SurfaceIds", outSurfaceIds}
            };
        }

        /// <summary>
        /// Creates analysis zones from a [conceptual mass] family instance which already contains at least one mass floor.
        /// </summary>
        /// <param name="MassFamilyInstance">The conceptual mass family instance to create zones from</param>
        /// <returns></returns>
        [MultiReturn("MassFamilyInstance", "ZoneIds", "SurfaceIds")]
        public static Dictionary<string, object> CreateFromMass(AbstractFamilyInstance MassFamilyInstance)
        {
            //local varaibles
            Document RvtDoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument.Document;

            //enable the analytical model in the document if it isn't already
            try
            {
                ActivateEnergyModel(RvtDoc);
            }
            catch (Exception)
            {
                throw new Exception("Something went wrong when trying to enable the energy model.");
            }

            //get the id of the analytical model associated with that mass
            Autodesk.Revit.DB.ElementId myEnergyModelId = MassEnergyAnalyticalModel.GetMassEnergyAnalyticalModelIdForMassInstance(RvtDoc, MassFamilyInstance.InternalElement.Id);
            MassEnergyAnalyticalModel mea = (MassEnergyAnalyticalModel)RvtDoc.GetElement(myEnergyModelId);

            //throw an error if we can't get the analytical model - it must be enabled in Revit.
            if (mea == null)
            {
                throw new Exception("Could not get the MassEnergyAnalyticalModel from the mass - make sure the Mass has at least one Mass Floor.");
            }


            //get the zone ids from our Mass's MassEnergyAnalyticalModel object
            //we'll use these to draw zones in another component - not sure if we can use them to drive opening / shading params
            List<Autodesk.Revit.DB.ElementId> zoneIds = mea.GetMassZoneIds().ToList();



            //get the MassSurfaceData ids of the definitions belonging to external faces
            //we'll output these, and then try to visualize the faces and change parameters in another component

            //get references to the faces using the mass - we need these to get at the surface data
            IList<Reference> faceRefs = mea.GetReferencesToAllFaces();

            //some faces supposedly share massSurfaceData definitions (although i think they are all unique in practice) - here we're pulling out unique data definitions.  
            Dictionary<int, MassSurfaceData> mySurfaceData = new Dictionary<int, MassSurfaceData>();
            foreach (var fr in faceRefs)
            {
                Autodesk.Revit.DB.ElementId id = mea.GetMassSurfaceDataIdForReference(fr);
                if (!mySurfaceData.ContainsKey(id.IntegerValue))
                {
                    MassSurfaceData d = (MassSurfaceData)RvtDoc.GetElement(id);
                    mySurfaceData.Add(id.IntegerValue, d);
                }
            }

            //filter by category = mass exterior wall
            var allSurfsList = mySurfaceData.Values.ToList();
            var extSurfList = from n in allSurfsList
                              where n.Category.Name == "Mass Exterior Wall"
                              select n;

            //output list
            List<Autodesk.Revit.DB.ElementId> surfaceIds = new List<Autodesk.Revit.DB.ElementId>();
            foreach (var s in extSurfList)
            {
                surfaceIds.Add(s.Id);
            }

            //loop over the output lists, and wrap them in our ElementId wrapper class
            List<ElementId> outZoneIds = zoneIds.Select(e => new ElementId(e.IntegerValue)).ToList();
            List<ElementId> outSurfaceIds = surfaceIds.Select(e => new ElementId(e.IntegerValue)).ToList();


            return new Dictionary<string, object>
            {
                {"MassFamilyInstance", MassFamilyInstance},
                {"ZoneIds", outZoneIds},
                {"SurfaceIds", outSurfaceIds}
            };
        }

        /// <summary>
        /// Exposes an analysis zone's properties, including the zone's exterior face element ids.
        /// </summary>
        /// <param name="ZoneId">The ElementId of the zone to inspect.  Get this from the AnalysisZones > CreateFrom* > ZoneIds output list</param>
        /// <returns></returns>
        [MultiReturn("SurfaceIds", "SpaceType", "conditionType")]
        public static Dictionary<string, object> DecomposeMassZone(ElementId ZoneId)
        {
            // local variables
            Document RvtDoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument.Document;
            MassZone zone = null;
            Autodesk.Revit.DB.ElementId myEnergyModelId = null;
            gbXMLConditionType conditionType = gbXMLConditionType.NoConditionType;
            gbXMLSpaceType spaceType = gbXMLSpaceType.NoSpaceType;

            // get zone data from the document using the id
            try
            {
                zone = (MassZone)RvtDoc.GetElement(new Autodesk.Revit.DB.ElementId(ZoneId.InternalId));

                if (zone == null) throw new Exception();
            }
            catch (Exception)
            {
                throw new Exception("Couldn't find a zone object with Id #: " + ZoneId.ToString());
            }

            //get external faces belonging to this zone
            #region Get faces belonging to this zone
		
            //try to get the element id of the MassEnergyAnalyticalModel - we need this to pull faces from
            try
            {
                myEnergyModelId = zone.MassEnergyAnalyticalModelId;
                if (myEnergyModelId == null) throw new Exception();
            }
            catch (Exception)
            {
                throw new Exception("Couldn't find a MassEnergyAnalyticalModel object belonging to the Mass instance with Id #: " + zone.MassEnergyAnalyticalModelId.ToString());
            }

            //some faces supposedly share massSurfaceData definitions (although i think they are all unique in practice) - here we're pulling out unique data definitions.  
            Dictionary<int, MassSurfaceData> mySurfaceData = new Dictionary<int, MassSurfaceData>();

            //get references to all of the faces
            IList<Reference> faceRefs = zone.GetReferencesToEnergyAnalysisFaces();
            foreach (var faceRef in faceRefs)
            {
                //get the element ID of the MassSurfaceData object associated with this face
                Autodesk.Revit.DB.ElementId id = zone.GetMassDataElementIdForZoneFaceReference(faceRef);
                //add it to our dict if it isn't already there
                if (!mySurfaceData.ContainsKey(id.IntegerValue))
                {
                    MassSurfaceData d = (MassSurfaceData)RvtDoc.GetElement(id);
                    mySurfaceData.Add(id.IntegerValue, d);
                }
            }

            //filter by category = mass exterior wall
            var allSurfsList = mySurfaceData.Values.ToList();
            var extSurfList = from n in allSurfsList
                              where n.Category.Name == "Mass Exterior Wall"
                              select n;

            //list of element Ids to wrap and output
            List<Autodesk.Revit.DB.ElementId> surfaceIds = extSurfList.Select(e => e.Id).ToList();

            //loop over the output lists, and wrap them in our ElementId wrapper class
            List<ElementId> outSurfaceIds = surfaceIds.Select(e => new ElementId(e.IntegerValue)).ToList();
 
	        #endregion

            // assign condition type
            conditionType = zone.ConditionType;

            // assign space type
            spaceType = zone.SpaceType;

            // return outputs
            return new Dictionary<string, object>
            {
                {"SurfaceIds", outSurfaceIds},
                {"SpaceType", spaceType},
                {"conditionType", conditionType}
            };

        }

        /// <summary>
        /// Draws a mesh in Dynamo representing an analysis surface.  Useful when trying to identify a surface to modify.
        /// </summary>
        /// <param name="SurfaceId">The ElementId of the surface to draw.  Get this from AnalysisZones > CreateFrom* > SurfaceIds output list</param>
        /// <returns></returns>
        public static Autodesk.DesignScript.Geometry.Mesh DrawAnalysisSurface(ElementId SurfaceId)
        {
            //local varaibles
            Document RvtDoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument.Document;
            MassSurfaceData surf = null;
            Autodesk.Revit.DB.ElementId myEnergyModelId = null;

            //try to get the MassSurfaceData object from the document
            try
            {
                surf = (MassSurfaceData)RvtDoc.GetElement(new Autodesk.Revit.DB.ElementId(SurfaceId.InternalId));
                if (surf == null) throw new Exception();
            }
            catch (Exception)
            {
                throw new Exception("Couldn't find a MassSurfaceData object with Id #: " + SurfaceId.ToString());
            }

            //try to get the element id of the MassEnergyAnalyticalModel - we need this to pull faces from
            try
            {
                myEnergyModelId = surf.ReferenceElementId;
                if (myEnergyModelId == null) throw new Exception();
            }
            catch (Exception)
            {
                throw new Exception("Couldn't find a MassEnergyAnalyticalModel object belonging to the Mass instance with Id #: " + surf.ReferenceElementId.ToString());
            }


            //get the smallest face
            Autodesk.Revit.DB.Face smallFace = GetSmallestFace(RvtDoc, surf, myEnergyModelId);

            Autodesk.Revit.DB.Mesh prettyMesh = smallFace.Triangulate();
            return Revit.GeometryConversion.RevitToProtoMesh.ToProtoType(prettyMesh);
        }

        /// <summary>
        /// Draws an analysis zone in Dynamo.  Use this to identify which zone is which in the CreateFromMass/CreateFromMassAndLevels 'ZoneIds' output list.
        /// </summary>
        /// <param name="ZoneId">The ElementId of the zone to draw.  Get this from the AnalysisZones > CreateFrom* > ZoneIds output list</param>
        /// <returns>A list of Dynamo meshes for each zone.</returns>
        public static List<Autodesk.DesignScript.Geometry.Mesh> DrawAnalysisZone(ElementId ZoneId)
        {
            //local varaibles
            Document RvtDoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument.Document;
            MassZone zone = null;
            Autodesk.Revit.DB.ElementId myEnergyModelId = null;

            // get zone data from the document using the id
            try
            {
                zone = (MassZone)RvtDoc.GetElement(new Autodesk.Revit.DB.ElementId(ZoneId.InternalId));

                if (zone == null) throw new Exception();
            }
            catch (Exception)
            {
                throw new Exception("Couldn't find a zone object with Id #: " + ZoneId.ToString());
            }


            //try to get the element id of the MassEnergyAnalyticalModel - we need this to pull faces from
            try
            {
                myEnergyModelId = zone.MassEnergyAnalyticalModelId;
                // myEnergyModelId = MassEnergyAnalyticalModel.GetMassEnergyAnalyticalModelIdForMassInstance(RvtDoc, MassFamilyInstance.InternalElement.Id);
                if (myEnergyModelId == null) throw new Exception();
            }
            catch (Exception)
            {
                //throw new Exception("Couldn't find a MassEnergyAnalyticalModel object belonging to the Mass instance with Id #: " + MassFamilyInstance.InternalElement.Id.ToString());
                throw new Exception("Couldn't find a MassEnergyAnalyticalModel object belonging to the Mass instance with Id #: " + zone.MassEnergyAnalyticalModelId.ToString());
            }

            //return a list of all fo the mesh faces for each zone
            List<Autodesk.DesignScript.Geometry.Mesh> outMeshes = new List<Autodesk.DesignScript.Geometry.Mesh>();
            //get references to all of the faces
            IList<Reference> faceRefs = zone.GetReferencesToEnergyAnalysisFaces();
            foreach (var faceRef in faceRefs)
            {
                //get the actual face and add the converted version to our list
                Autodesk.Revit.DB.Face face = (Autodesk.Revit.DB.Face)zone.GetGeometryObjectFromReference(faceRef);
                outMeshes.Add(Revit.GeometryConversion.RevitToProtoMesh.ToProtoType(face.Triangulate()));
            }
            return outMeshes;
        }

        /// <summary>
        /// Sets an exterior surface's energy parameters
        /// </summary>
        /// <param name="SurfaceId">The ElementId of the surface to modify.  Get this from the AnalysisZones > CreateFrom* > SurfaceIds output list</param>
        /// <param name="glazingPercent">Percentage of glazed area.  Should be a double between 0.0 - 1.0</param>
        /// <param name="shadingDepth">Shading Depth, specified as a double.  We assume the double value represents a length using Dynamo's current length unit.</param>
        /// <param name="sillHeight">Target sill height, specified as a double.  We assume the double value represents a length using Dynamo's current length unit.</param>
        /// <param name="ConstType">Conceptual Construction Type.  Use the Conceptual Construction Types Dropdown node from our EnergySettings tab to specify a value.</param>
        /// <returns></returns>
        public static ElementId SetSurfaceParameters(ElementId SurfaceId, double glazingPercent = 0.4, double shadingDepth = 0.0, double sillHeight = 0.0, string ConstType = "default")
        {
            //local varaibles
            Document RvtDoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument.Document;
            MassSurfaceData surf = null;

            //try to get the MassSurfaceData object from the document
            try
            {
                surf = (MassSurfaceData)RvtDoc.GetElement(new Autodesk.Revit.DB.ElementId(SurfaceId.InternalId));
                if (surf == null) throw new Exception();
            }
            catch (Exception)
            {
                throw new Exception("Couldn't find a MassSurfaceData object with Id #: " + SurfaceId.ToString());
            }

            //defense
            if (!(glazingPercent > 0.0 && glazingPercent <= 1.0))
            {
                throw new Exception("Glazing percentage must be between 0.0 and 1.0");
            }
            if (shadingDepth < 0.0)
            {
                throw new Exception("Shading Depth must be positive");
            }
            if (sillHeight < 0.0)
            {
                throw new Exception("Sill Height must be positive");
            }

            try
            {
                //start a transaction task
                TransactionManager.Instance.EnsureInTransaction(RvtDoc);

                //change the 'Values' param to 1 - by surface
                var val = surf.get_Parameter("Values");
                if (val != null)
                {
                    val.Set(1);
                }

                //set target sill height 
                surf.SillHeight = sillHeight * UnitConverter.DynamoToHostFactor;

                //set glazing percentage
                surf.PercentageGlazing = glazingPercent;

                //set shading if positive
                if (shadingDepth > 0)
                {
                    surf.IsGlazingShaded = true;
                    surf.ShadeDepth = shadingDepth * UnitConverter.DynamoToHostFactor;
                }

                //set conceptual construction if not empty
                if (!string.IsNullOrEmpty(ConstType) && ConstType != "default")
                {
                    Autodesk.Revit.DB.ElementId myTypeId = getConceptualConstructionIdFromName(RvtDoc, ConstType);
                    if (myTypeId != null)
                    {
                        surf.IsConceptualConstructionByEnergyData = false;
                        surf.ConceptualConstructionId = myTypeId;
                    }
                }

                //done with transaction task
                TransactionManager.Instance.TransactionTaskDone();

            }
            catch (Exception)
            {
                throw new Exception("Something went wrong when trying to set the parameters on surface # " + SurfaceId.ToString());
            }

            //return the surface ID so the surface can be used downstream
            return SurfaceId;
        }

        /// <summary>
        /// Sets an analysis zone's energy parameters
        /// </summary>
        /// <param name="ZoneId">The ElementId of the zone to modify.  Get this from the AnalysisZones > CreateFrom* > ZoneIds output list</param>
        /// <param name="SpaceType">Sets the zone's space type.  Use the Space Types Dropdown node from our EnergySetting tab to specify a value.</param>
        /// <param name="ConditionType">Sets the zone's condition type.  Use the Condition Types Dropdown node from our EnergySetting tab to specify a value.</param>
        /// <returns></returns>
        public static ElementId SetZoneParameters(ElementId ZoneId, string SpaceType = "", string ConditionType = "")
        {

            //local varaibles
            Document RvtDoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument.Document;
            MassZone zone = null;


            //try to get MassZone using the ID
            try
            {
                zone = (MassZone)RvtDoc.GetElement(new Autodesk.Revit.DB.ElementId(ZoneId.InternalId));

                if (zone == null) throw new Exception();
            }
            catch (Exception)
            {
                throw new Exception("Couldn't find a zone object with Id #: " + ZoneId.ToString());
            }


            //defense
            if (!string.IsNullOrEmpty(ConditionType) && !(gbXMLConditionType.IsDefined(typeof(gbXMLConditionType), ConditionType)))
            {
                throw new Exception(ConditionType.ToString() + " is not a valid condition type. Use conditionTypes dropdown to input a valid condition type.");
            }

            if (!string.IsNullOrEmpty(SpaceType) && !(gbXMLSpaceType.IsDefined(typeof(gbXMLSpaceType), SpaceType)))
            {
                throw new Exception(SpaceType.ToString() + " is not a valid space type. Use spaceTypes dropdown to input a valid space type.");
            }

            try
            {
                //start a transaction task
                TransactionManager.Instance.EnsureInTransaction(RvtDoc);

                //set condiotn type
                if (!string.IsNullOrEmpty(ConditionType))
                {
                    zone.ConditionType = (gbXMLConditionType)Enum.Parse(typeof(gbXMLConditionType), ConditionType);
                }

                //set space type
                if (!string.IsNullOrEmpty(SpaceType))
                {
                    zone.SpaceType = (gbXMLSpaceType)Enum.Parse(typeof(gbXMLSpaceType), SpaceType);
                }

                //done with transaction task
                TransactionManager.Instance.TransactionTaskDone();

            }
            catch (Exception)
            {
                throw new Exception("Something went wrong when trying to set the parameters on zone # " + ZoneId.ToString());
            }

            //return the zone ID so the zone can be used downstream
            return ZoneId;
        }

        /// <summary>
        /// returns the average height of a face by averaging it's points' v values (for ruled faces - origin is returned fro planar faces.
        /// </summary>
        /// <param name="f">the face</param>
        /// <returns>average height of the face, or it's origin height</returns>
        private static double GetAverageFaceHeight(Autodesk.Revit.DB.Face f)
        {
            double d = 0.0;

            //if face is a ruled face, average the z value of it's mesh vertices
            RuledFace rf = f as RuledFace;
            if (rf != null)
            {
                double total = 0.0;
                int i = 1;
                foreach (XYZ v in rf.Triangulate().Vertices)
                {
                    total = total + v.Z;
                    i++;
                }

                //return the average
                d = total / (double)i;
            }
            else // if it isn't a ruled face, treat it as planar and return the origin
            {
                PlanarFace pf = f as PlanarFace;
                if (pf != null)
                {
                    double total = 0.0;
                    int i = 1;
                    foreach (XYZ v in pf.Triangulate().Vertices)
                    {
                        total = total + v.Z;
                        i++;
                    }

                    //return the average
                    d = total / (double)i;
                }
            }

            return d;
        }

        /// <summary>
        /// get a list of points representing an edge array
        /// found on the building coder:
        /// http://thebuildingcoder.typepad.com/blog/2011/07/
        /// </summary>
        /// <param name="ea"></param>
        /// <returns></returns>
        private static List<Autodesk.DesignScript.Geometry.Point> GetPolygon(EdgeArray ea)
        {
            int n = ea.Size;

            List<XYZ> polygon = new List<XYZ>(n);

            foreach (Autodesk.Revit.DB.Edge e in ea)
            {
                IList<XYZ> pts = e.Tessellate();

                n = polygon.Count;

                if (0 < n)
                {
                    polygon.RemoveAt(n - 1);
                }
                polygon.AddRange(pts);
            }
            n = polygon.Count;

            polygon.RemoveAt(n - 1);

            //return polygon;

            //convert polygon to designscript points and return
            List<Autodesk.DesignScript.Geometry.Point> outPoitns = new List<Autodesk.DesignScript.Geometry.Point>();
            foreach (var p in polygon)
            {
                outPoitns.Add(Autodesk.DesignScript.Geometry.Point.ByCoordinates(p.X, p.Y, p.Z));
            }
            return outPoitns;
        }

        private static List<Autodesk.DesignScript.Geometry.Point> CullRepeats(List<Autodesk.DesignScript.Geometry.Point> pts)
        {
            List<Autodesk.DesignScript.Geometry.Point> outPts = new List<Autodesk.DesignScript.Geometry.Point>();
            foreach (var p in pts)
            {
                if (!PointIsInList(p, outPts))
                {
                    outPts.Add(p);
                }
            }
            return outPts;
        }

        private static bool PointIsInList(Autodesk.DesignScript.Geometry.Point pt, List<Autodesk.DesignScript.Geometry.Point> list)
        {
            foreach (var a in list)
            {
                if (PointsEqual(a, pt))
                {
                    return true;
                }
            }
            return false;
        }
        
        private static bool PointsEqual(Autodesk.DesignScript.Geometry.Point a, Autodesk.DesignScript.Geometry.Point b)
        {
            if (
                Math.Round(a.X, 3) == Math.Round(b.X, 3) && 
                Math.Round(a.Y, 3) == Math.Round(b.Y, 3) && 
                Math.Round(a.Z, 3) == Math.Round(b.Z, 3)
                )
                {
                    return true;
                }
            return false;
        }

        private static Autodesk.DesignScript.Geometry.Point getAveragePointFromFace(Autodesk.Revit.DB.Face f)
        {
            //the point to return 
            Autodesk.DesignScript.Geometry.Point p = null;

            //if face is a ruled face
            RuledFace rf = f as RuledFace;
            if (rf != null)
            {
                //units seem to be messed up...  convert to a designscript mesh first, then pull from that
                Autodesk.DesignScript.Geometry.Mesh m = Revit.GeometryConversion.RevitToProtoMesh.ToProtoType(rf.Triangulate());
                var points = m.VertexPositions;


                int numVertices = points.Count();
                double x = 0, y = 0, z = 0;
                foreach (var v in points)
                {
                    x = x + v.X;
                    y = y + v.Y;
                    z = z + v.Z;
                }
                x = x / numVertices;
                y = y / numVertices;
                z = z / numVertices;
                p = Autodesk.DesignScript.Geometry.Point.ByCoordinates(x, y, z);
                
            }
            else // if it isn't a ruled face, treat it as planar 
            {
                PlanarFace pf = f as PlanarFace;
                if (pf != null)
                {
                    //units seem to be messed up...  convert to a designscript mesh first, then pull from that
                    Autodesk.DesignScript.Geometry.Mesh m = Revit.GeometryConversion.RevitToProtoMesh.ToProtoType(pf.Triangulate());
                    var points = m.VertexPositions;


                    int numVertices = points.Count();
                    double x = 0, y = 0, z = 0;
                    foreach (var v in points)
                    {
                        x = x + v.X;
                        y = y + v.Y;
                        z = z + v.Z;
                    }
                    x = x / numVertices;
                    y = y / numVertices;
                    z = z / numVertices;
                    p = Autodesk.DesignScript.Geometry.Point.ByCoordinates(x, y, z);
                }
            }
            return p;
        }

        private static Autodesk.Revit.DB.Face GetSmallestFace(Document RvtDoc, MassSurfaceData surf, Autodesk.Revit.DB.ElementId myEnergyModelId)
        {
            //the data object contains references to faces.  For every face, draw a surface or something with designscript
            IList<Reference> faceRefs = surf.GetFaceReferences();
            Autodesk.Revit.DB.Face smallFace = null;
            double smallFaceArea = 1000000.0;
            foreach (var fr in faceRefs)
            {
                //get the smallest face
                Autodesk.Revit.DB.Face pretty = (Autodesk.Revit.DB.Face)RvtDoc.GetElement(myEnergyModelId).GetGeometryObjectFromReference(fr);
                if (pretty.Area < smallFaceArea)
                {
                    smallFaceArea = pretty.Area;
                    smallFace = pretty;
                }
            }
            return smallFace;
        }

        private static Autodesk.Revit.DB.Face GetLargestFace(Document RvtDoc, MassSurfaceData surf, Autodesk.Revit.DB.ElementId myEnergyModelId)
        {
            //the data object contains references to faces.  For every face, draw a surface or something with designscript
            IList<Reference> faceRefs = surf.GetFaceReferences();
            Autodesk.Revit.DB.Face bigFace = null;
            double bigFaceArea = 0.0;
            foreach (var fr in faceRefs)
            {
                //get the smallest face
                Autodesk.Revit.DB.Face pretty = (Autodesk.Revit.DB.Face)RvtDoc.GetElement(myEnergyModelId).GetGeometryObjectFromReference(fr);
                if (pretty.Area > bigFaceArea)
                {
                    bigFaceArea = pretty.Area;
                    bigFace = pretty;
                }
            }
            return bigFace;
        }

        private static Autodesk.Revit.DB.ElementId getConceptualConstructionIdFromName(Document RvtDoc, string name)
        {
            try
            {
                //query the revit doc for all elements of type ConceptualConstructionWallType
                FilteredElementCollector col = new FilteredElementCollector(RvtDoc);
                col.OfCategory(BuiltInCategory.OST_MassWallsAll);
                var ids = col.ToElements();
                var i = ids.GetEnumerator();

                //find the id of the element with the same name as the arg
                //had a hard time dealing with the dash in the name - it was either a em dash or a horiz bar
                Autodesk.Revit.DB.ElementId id = null;
                Regex rgx = new Regex("[^a-zA-Z0-9]");
                while (i.MoveNext())
                {
                    string matcher = rgx.Replace(i.Current.Name, "");
                    if (matcher.ToLower() == name.ToLower())
                    {
                        id = i.Current.Id;
                        break;
                    }

                }
                if (id != null)
                {
                    return id;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception) { return null; } 
        }

        /// <summary>
        /// Activate the Energy Model in a Revit document
        /// </summary>
        /// <param name="RvtDoc">The project document to activate the energy model in</param>
        [SupressImportIntoVM]
        public static void ActivateEnergyModel(Document RvtDoc)
        {
            //try to get at least one MassEnergyAnalyticalModel object in the doc.  if there is one there, we don't need to turn on the energy model
            FilteredElementCollector col = new FilteredElementCollector(RvtDoc);
            var meas = col.OfClass(typeof(MassEnergyAnalyticalModel)).ToElementIds();
            if (meas.Count != 0)
            {
                return;
            }

            //if we make it here, turn on the Analytical model, and regenerate the doc
            TransactionManager.Instance.EnsureInTransaction(RvtDoc);
            EnergyDataSettings energyData = EnergyDataSettings.GetFromDocument(RvtDoc);
            if (energyData != null)
            {
                energyData.SetCreateAnalyticalModel(true);
            }
            TransactionManager.Instance.TransactionTaskDone();
            DocumentManager.Regenerate();
        }
    }
}
