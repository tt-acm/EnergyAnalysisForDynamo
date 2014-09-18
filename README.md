GBSforDynamo
============

GBSforDynamo is a parametric interface for [Autodesk Green Building Studio](https://gbs.autodesk.com/GBS/), built on top of [Dynamo](http://dynamobim.org/) and [Vasari](http://autodeskvasari.com/)/[Revit](http://www.autodesk.com/products/revit-family/overview).  The project will enable parametric energy modeling and whole-building energy analysis workflows in Dynamo 0.7.2 and Vasari/Revit.

The project is being developed in C# using Visual Studio, and will work with Dynamo 0.7.2, and Vasari Beta 3.0 and/or Revit 2014.  The project consists of two libraries; one is a [zero-touch library](https://github.com/DynamoDS/Dynamo/wiki/Zero-Touch-Plugin-Development) containing most of the nodes, the other is a UI library containing a few nodes with dropdown elements.  


We are developing nodes in three main categories:

 - Parametric Energy Modeling.  These nodes will allow conceptual energy models in the Revit / Vasari massing environment to be driven on a zone-by-zone and surface-by-surface level of detail, and will expose control of the project’s default energy settings from within Dynamo.  For example, you will be able to drive the glazing percentage of a surface based on orientation, or set the space type of a zone based on elevation.  Please see the video at the bottom of this post for an example.

 - gbXML compilation and upload to Green Building Studio.  These nodes will convert an analytical model in Revit/Vasari into a gbXML file, which can be saved locally or uploaded to GBS to be run on the cloud.  

 - Green Building Studio analysis results query and visualization.  These nodes will query the GBS web service and return numeric results that can be used for data visualization.  We have to see exactly what we can get at via the API, but we hope to be able to expose results data at the level of zones and timesteps.  All of the nodes that interact with the GBS web service will use the Autodesk Single Sign On credentials from Revit/Vasari for authentication.


GBSforDynamo is developed and maintained by [Thornton Tomasetti](http://www.thorntontomasetti.com/)’s [CORE studio](http://core.thorntontomasetti.com/).  The main developers are:
- [Elcin Ertugrul](https://github.com/eertugrul)
- [Mostapha Sadeghipour Roudsari](https://github.com/mostaphaRoudsari)
- [Benjamin Howes](https://github.com/bhowes-tt)
