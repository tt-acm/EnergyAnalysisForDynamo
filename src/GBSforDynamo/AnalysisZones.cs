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
using Autodesk.DesignScript.Runtime;
using Autodesk.DesignScript.Interfaces;
using Autodesk.DesignScript.Geometry;
using System.Text.RegularExpressions;


namespace GBSforDynamo
{
    public static class AnalysisZones
    {
        [MultiReturn("MassFamilyInstance", "ZoneIds", "SurfaceIds")]
        public static Dictionary<string, object> CreateFromMassAndLevels(AbstractFamilyInstance MassFamilyInstance = null, List<Revit.Elements.Element> Levels = null)
        {
            //local varaibles
            Document RvtDoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument.Document;
            EnergyAnalysisDetailModel em = null;
            

            //make mass instance and levels mandatory inputs
            if (MassFamilyInstance == null || Levels == null)
            {
                throw new Exception("MassFamily Instance and Levels are mandatory inputs");
            }

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
                TransactionManager.Instance.EnsureInTransaction(RvtDoc);
                EnergyDataSettings energyData = EnergyDataSettings.GetFromDocument(RvtDoc);
                if (energyData != null)
                {
                    energyData.SetCreateAnalyticalModel(true);
                }
                TransactionManager.Instance.TransactionTaskDone();
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
            ElementId myEnergyModelId = MassEnergyAnalyticalModel.GetMassEnergyAnalyticalModelIdForMassInstance(RvtDoc, MassFamilyInstance.InternalElement.Id);
            MassEnergyAnalyticalModel mea = (MassEnergyAnalyticalModel)RvtDoc.GetElement(myEnergyModelId);

            //if mea is null, we should throw a good error message.  Seems to be working in Revit now that we are regenerating.
            if (mea == null)
            {
                throw new Exception("Could not get the MassEnergyAnalyticalModel from the mass.");
            }

            //get the zone ids from our Mass's MassEnergyAnalyticalModel object
            //we'll use these to draw zones in another component - not sure if we can use them to drive opening / shading params
            List<ElementId> zoneIds = mea.GetMassZoneIds().ToList();

            //get the MassSurfaceData ids of the definitions belonging to external faces
            //we'll output these, and then try to visualize the faces and change parameters in another component

            //get references to the faces using the mass - we need these to get at the surface data
            IList<Reference> faceRefs = mea.GetReferencesToAllFaces();

            //some faces share massSurfaceData definitions - here we're pulling out unique data definitions.  not totally sure how this all works yet...
            Dictionary<int, MassSurfaceData> mySurfaceData = new Dictionary<int, MassSurfaceData>();
            foreach (var fr in faceRefs)
            {
                ElementId id = mea.GetMassSurfaceDataIdForReference(fr);
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
            List<ElementId> surfaceIds = new List<ElementId>();
            foreach (var s in extSurfList)
            {
                surfaceIds.Add(s.Id);
            }
            #endregion


            return new Dictionary<string, object>
            {
                {"MassFamilyInstance", MassFamilyInstance},
                {"ZoneIds", zoneIds},
                {"SurfaceIds", surfaceIds}
            };
        }

        [MultiReturn("MassFamilyInstance", "ZoneIds", "SurfaceIds")]
        public static Dictionary<string, object> CreateFromMass(AbstractFamilyInstance MassFamilyInstance = null)
        {
            //local varaibles
            Document RvtDoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument.Document;
            

            //get the id of the analytical model associated with that mass
            ElementId myEnergyModelId = MassEnergyAnalyticalModel.GetMassEnergyAnalyticalModelIdForMassInstance(RvtDoc, MassFamilyInstance.InternalElement.Id);
            MassEnergyAnalyticalModel mea = (MassEnergyAnalyticalModel)RvtDoc.GetElement(myEnergyModelId);

            //throw an error if we can't get the analytical model - it must be enabled in Revit.
            if (mea == null)
            {
                throw new Exception("Could not get the MassEnergyAnalyticalModel from the mass - Enable the Energy Model in Revit/Vasari, and make sure the Mass has at least one Mass Floor.");
            }


            //get the zone ids from our Mass's MassEnergyAnalyticalModel object
            //we'll use these to draw zones in another component - not sure if we can use them to drive opening / shading params
            List<ElementId> zoneIds = mea.GetMassZoneIds().ToList();



            //get the MassSurfaceData ids of the definitions belonging to external faces
            //we'll output these, and then try to visualize the faces and change parameters in another component

            //get references to the faces using the mass - we need these to get at the surface data
            IList<Reference> faceRefs = mea.GetReferencesToAllFaces();

            //some faces share massSurfaceData definitions - here we're pulling out unique data definitions.  not totally sure how this all works yet...
            Dictionary<int, MassSurfaceData> mySurfaceData = new Dictionary<int, MassSurfaceData>();
            foreach (var fr in faceRefs)
            {
                ElementId id = mea.GetMassSurfaceDataIdForReference(fr);
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
            List<ElementId> surfaceIds = new List<ElementId>();
            foreach (var s in extSurfList)
            {
                surfaceIds.Add(s.Id);
            }


            return new Dictionary<string, object>
            {
                {"MassFamilyInstance", MassFamilyInstance},
                {"ZoneIds", zoneIds},
                {"SurfaceIds", surfaceIds}
            };
        }

        public static ElementId SetSurfaceParameters(ElementId SurfaceId = null, double glazingPercent = 0.4, double shadingDepth = 0.0, double sillHeight = 3.0, string ConstType = "default")
        {
            //local varaibles
            Document RvtDoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument.Document;
            MassSurfaceData surf = null;

            //try to get the MassSurfaceData object from the document
            try
            {
                surf = (MassSurfaceData)RvtDoc.GetElement(SurfaceId);
                if (surf == null)throw new Exception();
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
                surf.SillHeight = sillHeight;

                //set glazing percentage
                surf.PercentageGlazing = glazingPercent;

                //set shading if positive
                if (shadingDepth > 0)
                {
                    surf.IsGlazingShaded = true;
                    surf.ShadeDepth = shadingDepth; 
                }

                //set conceptual construction if not empty
                if (!string.IsNullOrEmpty(ConstType) && ConstType != "default")
                {
                    ElementId myTypeId = getConceptualConstructionIdFromName(RvtDoc, ConstType);
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


        public static ElementId SetZoneParameters(ElementId ZoneId, string SpaceType = "", string ConditionType = "")
        {
            
            //local varaibles
            Document RvtDoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument.Document;
            MassZone zone = null;

            
            //try to get MassZone using the ID
            try
            {
                zone = (MassZone)RvtDoc.GetElement(ZoneId);

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


        [MultiReturn("SurfaceIds", "SpaceType", "conditionType")]
        public static Dictionary<string, object> DecomposeMassZone(ElementId ZoneId = null)
        {
            // local variables
            Document RvtDoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument.Document;
            List<ElementId> faceIds = new List<ElementId>();
            MassZone zone = null;
            gbXMLConditionType conditionType = gbXMLConditionType.NoConditionType;
            gbXMLSpaceType spaceType = gbXMLSpaceType.NoSpaceType;

            // get zone data from the document using the id
            try
            {
                zone = (MassZone)RvtDoc.GetElement(ZoneId);

                if (zone == null) throw new Exception();
            }
            catch (Exception)
            {
                throw new Exception("Couldn't find a zone object with Id #: " + ZoneId.ToString());
            }

            /*
            
            // here I want to get all the external surfaces for the zone but I couldn't figure it out

            // this is the solution 1 which doesn't really work!
            // there are 3 different ids in MassSurface
            // 1. referenceID which returns the ID to the MassEnergyAnalyticalMode
            // 2. Id which is the Id of the surface itself
            // and finally 3. uniqueId which is a GUID and doesn't help
            // get analytical model Id
            ElementId meaID = zone.MassEnergyAnalyticalModelId;
            
            // get the mass energy model
            MassEnergyAnalyticalModel mea = (MassEnergyAnalyticalModel)RvtDoc.GetElement(meaID);

            // get all face references
            IList<Reference> faceRefs = mea.GetReferencesToAllFaces();

            // this is so confusing but I just trust what you have done so far. Hopefully there is an easier/cleaner way to do this.
            Dictionary<int, MassSurfaceData> mySurfaceData = new Dictionary<int, MassSurfaceData>();
            foreach (var fr in faceRefs)
            {
                ElementId id = mea.GetMassSurfaceDataIdForReference(fr);
                if (!mySurfaceData.ContainsKey(id.IntegerValue))
                {
                    MassSurfaceData d = (MassSurfaceData)RvtDoc.GetElement(id);
                    mySurfaceData.Add(id.IntegerValue, d);
                }
            }

            //filter by category = mass exterior wall
            var allSurfsList = mySurfaceData.Values.ToList();
            var extSurfList = from n in allSurfsList
                              where n.Category.Name == "Mass Exterior Wall" && n.ReferenceElementId == ZoneId
                              select n;


            // this is solution two that doesn't work either. face.ElementId returns zoneID and face.LinkedElementId returns -1!

            IList<Reference> analysisFaces = zone.GetReferencesToEnergyAnalysisFaces();
            
            // collect id of surfaces
            foreach (var face in analysisFaces)
            {   
                if (face.LinkedElementId != null) //face element IDs are identical with zoneID!
                {   
                    faceIds.Add(face.LinkedElementId);
                }
            }
            */

            // assign condition type
            conditionType = zone.ConditionType;

            // assign space type
            spaceType = zone.SpaceType;

            // return outputs
            return new Dictionary<string, object>
            {
                {"SurfaceIds", faceIds},
                {"SpaceType", spaceType},
                {"conditionType", conditionType}
            };

        }

        public static Autodesk.DesignScript.Geometry.Mesh DrawAnalysisSurface(ElementId SurfaceId)
        {
            //local varaibles
            Document RvtDoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument.Document;
            MassSurfaceData surf = null;
            ElementId myEnergyModelId = null;
            
            //try to get the MassSurfaceData object from the document
            try
            {
                surf = (MassSurfaceData)RvtDoc.GetElement(SurfaceId);
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
                // MassEnergyAnalyticalModel.GetMassEnergyAnalyticalModelIdForMassInstance(RvtDoc, MassFamilyInstance.InternalElement.Id);
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

        public static Autodesk.DesignScript.Geometry.Point AnalysisSurfacePoint(AbstractFamilyInstance MassFamilyInstance = null, ElementId SurfaceId = null)
        {
            //local varaibles
            Document RvtDoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument.Document;
            MassSurfaceData surf = null;
            ElementId myEnergyModelId = null;

            //try to get the element id of the MassEnergyAnalyticalModel - we need this to pull faces from
            try
            {
                myEnergyModelId = MassEnergyAnalyticalModel.GetMassEnergyAnalyticalModelIdForMassInstance(RvtDoc, MassFamilyInstance.InternalElement.Id);
                if (myEnergyModelId == null) throw new Exception();
            }
            catch (Exception)
            {
                throw new Exception("Couldn't find a MassEnergyAnalyticalModel object belonging to the Mass instance with Id #: " + MassFamilyInstance.InternalElement.Id.ToString());
            }

            //try to get the MassSurfaceData object from the document
            try
            {
                surf = (MassSurfaceData)RvtDoc.GetElement(SurfaceId);
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

        public static Autodesk.DesignScript.Geometry.Vector AnalysisSurfaceVector(AbstractFamilyInstance MassFamilyInstance = null, ElementId SurfaceId = null)
        {
            //local varaibles
            Document RvtDoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument.Document;
            MassSurfaceData surf = null;
            ElementId myEnergyModelId = null;

            //try to get the element id of the MassEnergyAnalyticalModel - we need this to pull faces from
            try
            {
                myEnergyModelId = MassEnergyAnalyticalModel.GetMassEnergyAnalyticalModelIdForMassInstance(RvtDoc, MassFamilyInstance.InternalElement.Id);
                if (myEnergyModelId == null) throw new Exception();
            }
            catch (Exception)
            {
                throw new Exception("Couldn't find a MassEnergyAnalyticalModel object belonging to the Mass instance with Id #: " + MassFamilyInstance.InternalElement.Id.ToString());
            }

            //try to get the MassSurfaceData object from the document
            try
            {
                surf = (MassSurfaceData)RvtDoc.GetElement(SurfaceId);
                if (surf == null) throw new Exception();
            }
            catch (Exception)
            {
                throw new Exception("Couldn't find a MassSurfaceData object with Id #: " + SurfaceId.ToString());
            }

            //get the smallest face
            Autodesk.Revit.DB.Face bigFace = GetLargestFace(RvtDoc, surf, myEnergyModelId);
            XYZ normal = bigFace.ComputeNormal(new Autodesk.Revit.DB.UV(0.5, 0.5));
            normal = normal.Normalize();
            return Autodesk.DesignScript.Geometry.Vector.ByCoordinates(normal.X, normal.Y, normal.Z, true);
        }

//        public static List<Autodesk.DesignScript.Geometry.Mesh> DrawAnalysisZone(AbstractFamilyInstance MassFamilyInstance = null, ElementId ZoneId, double offset = 1.0)
        public static List<Autodesk.DesignScript.Geometry.Mesh> DrawAnalysisZone(ElementId ZoneId, double offset = 1.0)
        {
            //local varaibles
            Document RvtDoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument.Document;
            MassZone zone = null;
            ElementId myEnergyModelId = null;

            // get zone data from the document using the id
            try
            {
                zone = (MassZone)RvtDoc.GetElement(ZoneId);

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

            /*
            //try to get the MassSurfaceData object from the document
            try
            {
                zone = (MassZone)RvtDoc.GetElement(ZoneId);
                if (zone == null) throw new Exception();
            }
            catch (Exception)
            {
                throw new Exception("Couldn't find a MassSurfaceData object with Id #: " + ZoneId.ToString());
            }
            */

            ////get the lowest face from the zone.
            //Autodesk.Revit.DB.Face lowestFace = null;

            ////get references to all of the faces
            //IList<Reference> faceRefs = zone.GetReferencesToEnergyAnalysisFaces();
            //double lowestFaceHeight = 1000000.0;
            //foreach (var faceRef in faceRefs)
            //{
            //    //get the actual face
            //    Autodesk.Revit.DB.Face face = (Autodesk.Revit.DB.Face)zone.GetGeometryObjectFromReference(faceRef);

            //    //get it's height.
            //    double h = GetAverageFaceHeight(face);

            //    //if lower than others, make it the lowest face
            //    if (h < lowestFaceHeight)
            //    {
            //        lowestFaceHeight = h;
            //        lowestFace = face;
            //    }
            //}

                //for debugging - let's make sure we are getting the bottom of the zone
            //Autodesk.Revit.DB.Mesh prettyMesh = lowestFace.Triangulate();
            //return Revit.GeometryConversion.RevitToProtoMesh.ToProtoType(prettyMesh);
            

            ////draw a polyline from the outline of the lowest face
            //List<Autodesk.DesignScript.Geometry.Point> designScriptPoints = GetPolygon(lowestFace.EdgeLoops.get_Item(0));
            //List<Autodesk.DesignScript.Geometry.Point> culledPoints = CullRepeats(designScriptPoints);
            //Autodesk.DesignScript.Geometry.PolyCurve myOutline = Autodesk.DesignScript.Geometry.PolyCurve.ByPoints(culledPoints, true);
            ////offset it in a bit, and move it up a bit
            //var myOffset = myOutline.Offset(offset);
            //return myOffset;

            //draw a line from one of it's corner points to a little bit below the level above

            //loft out a solid and return


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

        private static Autodesk.Revit.DB.Face GetSmallestFace(Document RvtDoc, MassSurfaceData surf, ElementId myEnergyModelId)
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

        private static Autodesk.Revit.DB.Face GetLargestFace(Document RvtDoc, MassSurfaceData surf, ElementId myEnergyModelId)
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

        private static ElementId getConceptualConstructionIdFromName(Document RvtDoc, string name)
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
                ElementId id = null;
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
    }
}
