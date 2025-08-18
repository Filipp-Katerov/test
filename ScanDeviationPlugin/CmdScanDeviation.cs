using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.PointClouds;
using Autodesk.Revit.DB.Structure;   // Для Transaction
using Autodesk.Revit.UI.Selection;
using Color = Autodesk.Revit.DB.Color;

public class CmdScanDeviation : IExternalCommand
{
    public Result Execute(ExternalCommandData data,
                          ref string message,
                          ElementSet elements)
    {
        UIDocument uidoc = data.Application.ActiveUIDocument;
        Document doc     = uidoc.Document;

        try
        {
            // 1. Выбираем облако точек
            Reference r = uidoc.Selection.PickObject(
                            ObjectType.Element,
                            new PointCloudSelectionFilter(),
                            "Выберите облако точек");
            PointCloudInstance pc = doc.GetElement(r) as PointCloudInstance;

            // 2. Получаем элементы для проверки
            ICollection<ElementId> ids = uidoc.Selection.GetElementIds();
            if (!ids.Any())
                ids = new FilteredElementCollector(doc)
                        .WhereElementIsNotElementType()
                        .Where(e => e.Category != null &&
                                    e.Category.HasMaterialQuantities)
                        .Select(e => e.Id)
                        .ToList();

            // 3. Считаем solids
            Options opt = new Options { DetailLevel = ViewDetailLevel.Fine };
            List<Solid> solids = ids
                .Select(id => doc.GetElement(id))
                .SelectMany(e => e.get_Geometry(opt).OfType<Solid>())
                .Where(s => s.Volume > 0)
                .ToList();

            // 4. Запрашиваем предельное расстояние (мм)
            double thrMm = AskThresholdMm();              // свой InputForm
            double thr   = UnitUtils.ConvertToInternalUnits(
                                 thrMm, DisplayUnitType.DUT_MILLIMETERS);

            // 5. Проходим по точкам
            PointCloudAccess access = pc.GetPointCloudAccess();
            BoundingBoxXYZ bb = pc.get_BoundingBox(null);
            Outline outline   = new Outline(bb.Min, bb.Max);
            PointCloudFilter filter =
               PointCloudFilterFactory.CreateMultiPlaneFilter(outline);
            IList<PointCollection> packs =
                    access.GetPoints(filter, pc.GetTransform());

            List<XYZ> reds = new List<XYZ>();
            List<XYZ> greens = new List<XYZ>();

            foreach (PointCollection pack in packs)
            {
                for (int i = 0; i < pack.Count; ++i)
                {
                    XYZ p = pack.GetPoint(i);
                    double d = MinDistanceToSolids(p, solids);

                    if (d > thr)  reds.Add(p);
                    else          greens.Add(p);
                }
            }

            // 6. Окрашиваем
            using (Transaction t = new Transaction(doc, "Deviation color"))
            {
                t.Start();

                PointCloudOverrideSettings redOvr = new PointCloudOverrideSettings();
                redOvr.SetColor(new Color(255, 0, 0));

                PointCloudOverrideSettings greenOvr = new PointCloudOverrideSettings();
                greenOvr.SetColor(new Color(0, 255, 0));

                pc.SetPointOverrides(reds,   redOvr);
                pc.SetPointOverrides(greens, greenOvr);

                t.Commit();
            }

            return Result.Succeeded;
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        {
            return Result.Cancelled;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }

    /// Минимальное расстояние от точки до списка solids (по их габаритам)
    private static double MinDistanceToSolids(XYZ pt, List<Solid> solids)
    {
        double min = Double.MaxValue;
        foreach (Solid s in solids)
        {
            BoundingBoxXYZ bb = s.GetBoundingBox();
            double d = DistancePointToBox(pt, bb);
            if (d < min) min = d;
        }
        return min;
    }

    /// Расстояние от точки до габаритного блока
    private static double DistancePointToBox(XYZ p, BoundingBoxXYZ bb)
    {
        double dx = Math.Max(0, Math.Max(bb.Min.X - p.X, p.X - bb.Max.X));
        double dy = Math.Max(0, Math.Max(bb.Min.Y - p.Y, p.Y - bb.Max.Y));
        double dz = Math.Max(0, Math.Max(bb.Min.Z - p.Z, p.Z - bb.Max.Z));
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    // Заглушка диалогового окна
    private static double AskThresholdMm()
    {
        // В реальном плагине создайте WPF/WinForms форму.
        return 20.0; // мм
    }
}

/// Фильтр выбора только PointCloudInstance
class PointCloudSelectionFilter : ISelectionFilter
{
    public bool AllowElement(Element elem) => elem is PointCloudInstance;
    public bool AllowReference(Reference reference, XYZ position) => false;
}
