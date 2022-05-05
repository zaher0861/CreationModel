using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreationModelPlugin
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CreationModel : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            CreateModel(doc, 10000, 5000, "Уровень 1", "Уровень 2");
            return Result.Succeeded;
        }
        public void CreateModel(Document doc, double widthValue, double depthValue, string levelName1, string levelName2)
        {
            List<Level> listLevel = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .OfType<Level>()
                .ToList();

            Level level1 = listLevel
                .Where(x => x.Name.Equals(levelName1))
                .FirstOrDefault();
            Level level2 = listLevel
                .Where(x => x.Name.Equals(levelName2))
                .FirstOrDefault();

            double width = UnitUtils.ConvertToInternalUnits(widthValue, UnitTypeId.Millimeters);
            double depth = UnitUtils.ConvertToInternalUnits(depthValue, UnitTypeId.Millimeters);
            double dx = width / 2;
            double dy = depth / 2;

            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dx, -dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(-dx, -dy, 0));

            List<Wall> walls = new List<Wall>();

            Transaction transaction = new Transaction(doc, "Построение стен");
            transaction.Start();
            for (int i = 0; i < 4; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]);
                Wall wall = Wall.Create(doc, line, level1.Id, false);
                walls.Add(wall);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id);
            }
            AddDoor(doc, level1, walls[3]);
            for (int i = 0; i < 3; i++)
            {
                AddWindow(doc, level1, walls[i]);
            }
            AddRoof(doc, level2, walls);
            transaction.Commit();
        }

        private void AddRoof(Document doc, Level level2, List<Wall> walls)
        {
            RoofType roofType = new FilteredElementCollector(doc) 
                .OfClass(typeof(RoofType))
                .OfType<RoofType>()
                .Where(x => x.Name.Equals("Типовой - 400мм"))
                .Where(x => x.FamilyName.Equals("Базовая крыша"))
                .FirstOrDefault();

            CurveArray curveArray = new CurveArray();
            curveArray.Append(Line.CreateBound(new XYZ(-20, -10, 13), new XYZ(-20, 0, 20)));
            curveArray.Append(Line.CreateBound(new XYZ(-20, 0, 20), new XYZ(-20, 10, 13)));

            ReferencePlane plane = doc.Create.NewReferencePlane(new XYZ(0, 0, 0), new XYZ(0, 0, 20), new XYZ(0, 20, 0), doc.ActiveView);
            doc.Create.NewExtrusionRoof(curveArray, plane, level2, roofType, -19, 19);
        }

        private void AddWindow(Document doc, Level level1, Wall wall)
        {
            FamilySymbol windowType = new FilteredElementCollector(doc)
                            .OfClass(typeof(FamilySymbol))
                            .OfCategory(BuiltInCategory.OST_Windows)
                            .OfType<FamilySymbol>()
                            .Where(x => x.Name.Equals("0915 x 0610 мм"))
                            .Where(x => x.FamilyName.Equals("Фиксированные"))
                            .FirstOrDefault();
            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;
            if (!windowType.IsActive)
            {
                windowType.Activate();
            }
            FamilyInstance window = doc.Create.NewFamilyInstance(point, windowType, wall, level1, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
            window.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM).Set(UnitUtils.ConvertToInternalUnits(1200, UnitTypeId.Millimeters));
        }

        private void AddDoor(Document doc, Level level1, Wall wall)
        {
            FamilySymbol doorType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Doors)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0864 x 2134 мм"))
                .Where(x => x.FamilyName.Equals("Одиночные-Щитовые"))
                .FirstOrDefault();
            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;
            if (!doorType.IsActive)
            {
                doorType.Activate();
            }
            FamilyInstance door = doc.Create.NewFamilyInstance(point, doorType, wall, level1, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
        }
    }
}
