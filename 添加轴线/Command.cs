using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Linq;
using Application = Autodesk.Revit.ApplicationServices.Application;
using Grid = Autodesk.Revit.DB.Grid;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException;

namespace 添加轴线
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {//ui应用程序
        private UIApplication uiapp;

        //应用程序
        private Application app;

        //ui文档
        private UIDocument uidoc;

        //文档
        private Document doc;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            uiapp = commandData.Application;

            app = uiapp.Application;

            uidoc = uiapp.ActiveUIDocument;

            doc = uidoc.Document;

            selectionFilter gridFilter = new selectionFilter();
            using (Transaction t = new Transaction(doc, "偏移轴网"))
            {
                t.Start();

                while (true)
                {
                    try
                    {
                        Reference reference = uidoc.Selection.PickObject(ObjectType.Element, gridFilter);
                        XYZ selectedPoint = uidoc.Selection.PickPoint();
                        //被选中的grid
                        Grid grid = doc.GetElement(reference) as Grid;
                        //新轴网的id
                        ElementId newGridId;
                        if (grid.Curve as Line != null)
                        {
                            OffsetLineGrid offsetGrid = new OffsetLineGrid(selectedPoint, grid);
                            Model newinfo;

                            newinfo = GetNameAndDistance(grid, offsetGrid.OffsetDistance);
                            if (newinfo != null)
                            {
                                offsetGrid.OffsetDistance = newinfo.Distance;
                            }

                            newGridId = ElementTransformUtils.CopyElement(doc, grid.Id, offsetGrid.OffsetDir).First();
                            SetNewName(newGridId, newinfo.Name);
                        }
                        else
                        {
                            OffsetArcGrid offsetGrid = new OffsetArcGrid(selectedPoint, grid);
                            Model newinfo = GetNameAndDistance(grid, offsetGrid.OffsetDistance);

                            newinfo = GetNameAndDistance(grid, offsetGrid.OffsetDistance);
                            if (newinfo != null)
                            {
                                offsetGrid.ArcRadius = newinfo.Distance;
                            }

                            Arc arc = Arc.Create(offsetGrid.ArcCenter, offsetGrid.ArcRadius, offsetGrid.StartAngle, offsetGrid.EndAngle, XYZ.BasisX, XYZ.BasisY);
                            newGridId = Grid.Create(doc, arc).Id;
                            SetNewName(newGridId, newinfo.Name);
                        }

                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        continue;
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("Tip", ex.Message);
                        break;
                    }
                }
                t.Commit();
            }
            return Result.Succeeded;
        }

        private void SetNewName(ElementId newGridId, string name)
        {
            Grid newGrid = doc.GetElement(newGridId) as Grid;
            if (new FilteredElementCollector(doc).OfClass(typeof(Grid)).Cast<Grid>().All(i => i.Name != name))
                newGrid.Name = name;
        }

        private Model GetNameAndDistance(Grid grid, double distance)
        {
            Model model = new Model();
            model.Name = grid.Name;
            model.Distance = UnitUtils.ConvertFromInternalUnits(distance, DisplayUnitType.DUT_MILLIMETERS);

            MainView mainView = new MainView();
            mainView.Name.Text = model.Name;
            mainView.Distance.Text = model.Distance.ToString();
            if (mainView.ShowDialog() == true)
            {
                model.Name = mainView.Name.Text;
                //double distance;
                bool isGetdistance = double.TryParse(mainView.Distance.Text, out distance);
                if (isGetdistance)
                {
                    model.Distance = UnitUtils.ConvertToInternalUnits(distance, DisplayUnitType.DUT_MILLIMETERS);
                    return model;
                }
            }
            return null;
        }

        private void GetDirectionofGrid()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 如果选中的轴线是直线
        /// </summary>
        /// <param name="gird">被选中的轴线</param>
        /// <exception cref="NotImplementedException"></exception>
        private void GetGridisLine(Grid gird)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 如果选中的轴线是弧线
        /// </summary>
        /// <param name="gird">被选中的轴线</param>
        /// <exception cref="NotImplementedException"></exception>
        private void GetGridisArc(Grid gird)
        {
            throw new NotImplementedException();
        }

        public double GetAngleBetweenVectors(XYZ vector1, XYZ vector2)
        { // 计算点积
            double dotProduct = vector1.DotProduct(vector2);
            //计算两个向量的长度
            double magnitude1 = vector1.GetLength();
            double magnitude2 = vector2.GetLength();
            //计算夹角的余弦值
            double cosTheta = dotProduct / (magnitude1 * magnitude2);
            //确保余弦值在有效范围内
            // cosTheta = Math.Max(-1.0, Math.Min(1.0, cosTheta));
            //计算夹角的弧度
            double thetaInRadians = Math.Acos(cosTheta);
            return thetaInRadians;
        }

        public double GetCosineOfVectors(XYZ vector1, XYZ vector2)
        { // 计算点积
            double dotProduct = vector1.DotProduct(vector2);
            // 计算两个向量的长度
            double length1 = vector1.GetLength();
            double length2 = vector2.GetLength();
            // 计算余弦值
            return dotProduct / (length1 * length2);
        }

       

        /// <summary>
        /// 轴线过滤器
        /// </summary>
        private class selectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element element)
            {
                if (element.Category?.Name == "轴网")
                {
                    return true;
                }
                return false;
            }

            public bool AllowReference(Reference refer, XYZ point)
            {
                return false;
            }
        }
    }
}