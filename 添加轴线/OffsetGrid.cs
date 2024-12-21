using Autodesk.Revit.DB;
using System;

namespace 添加轴线
{
    public abstract class OffsetGrid

    {
        protected OffsetGrid(XYZ selectedPoint, Grid grid)
        {
            SelectedPoint = selectedPoint;
            this.grid = grid;
        }

        #region 属性

        ///<summary>
        /// 轴线的起点
        /// </summary>
        public XYZ StartPoint { get; set; }

        /// <summary>
        /// 轴线的终点
        /// </summary>
        public XYZ EndPoint { get; set; }

        /// <summary>
        /// 新轴线的名字
        /// </summary>
        public string GridName { get; set; }

        /// <summary>
        /// 选中的偏移方向的点
        /// </summary>
        public XYZ SelectedPoint { get; set; }

        /// <summary>
        /// 要偏移的距离
        /// </summary>
        public double OffsetDistance { get; set; }

        /// <summary>
        /// 偏移向量
        /// </summary>
        public XYZ OffsetDir { get; set; }

        /// <summary>
        /// 轴线方向
        /// </summary>
        public XYZ Direction { get; set; }

        #endregion 属性

        /// <summary>
        /// 被选中的轴网
        /// </summary>
        public Grid grid;

        /// <summary>
        /// 计算点到轴线的距离
        /// </summary>
        public abstract void GetDistance();

        /// <summary>
        /// 重新判断轴线起终点
        /// </summary>
        public abstract void GetStartAndEndPoint();

        public abstract void SetOffsetDistance();

        /// <summary>
        /// 点到直线的距离
        /// </summary>
        /// <param name="point"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        public double GetDistanceFromPointToLine(XYZ point, Line line)
        {
            XYZ lineDirection = (line.GetEndPoint(1) - line.GetEndPoint(0)).Normalize();
            XYZ pointToLineStart = point - line.GetEndPoint(0);
            // 投影点到直线起点向量到直线方向向量上
            XYZ projection = lineDirection * (pointToLineStart.DotProduct(lineDirection));
            //计算点到直线的垂直向量
            XYZ perpendicularVector = pointToLineStart - projection;
            // 返回垂直向量的长度，即点到直线的距离
            return perpendicularVector.GetLength();
        }

        /// <summary>
        /// 计算两个向量的夹角
        /// </summary>
        /// <param name="vector1"></param>
        /// <param name="vector2"></param>
        /// <returns></returns>
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
        /// <summary>
        /// 点到曲线的距离
        /// </summary>
        /// <param name="point"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        public double GetDistanceFromPointToCurve(XYZ point, Curve curve)
        { // 获取曲线在点附近的最近点
            IntersectionResult result = curve.Project(point);
            if (result != null)
            {
                XYZ nearestPoint = result.XYZPoint;
                // 计算点到最近点的向量长度
                double distance = point.DistanceTo(nearestPoint);
                return distance;
            }
            else
            {
                // 处理曲线和点不相交的情况
                return 0;
            }
        }
    }

    public class OffsetLineGrid : OffsetGrid
    {
        public OffsetLineGrid(XYZ selectedPoint, Grid grid) : base(selectedPoint, grid)
        {
            GetStartAndEndPoint();
            GetDistance();
            SetOffsetDistance();
        }

        /// <summary>
        /// 选中点与起点组成的线段 与 轴线法向量的夹角（用于计算偏移距离）
        /// </summary>
        public double IncludeAngle { get; set; }

        public override void GetDistance()
        {
            this.OffsetDistance = GetDistanceFromPointToLine(SelectedPoint, Line.CreateBound(StartPoint, EndPoint));
        }

        public override void GetStartAndEndPoint()
        {
            XYZ Point0 = grid.Curve.GetEndPoint(0);
            XYZ Point1 = grid.Curve.GetEndPoint(1);
            if (Math.Abs(Point0.Y - Point1.Y) < 1e-6)
            {
                StartPoint = Point0.X > Point1.X ? Point1 : Point0;
                EndPoint = Point0.X < Point1.X ? Point1 : Point0;
            }
            else if (Math.Abs(Point0.X - Point1.X) < 1e-6)
            {
                StartPoint = Point0.Y > Point1.Y ? Point1 : Point0;
                EndPoint = Point0.Y < Point1.Y ? Point1 : Point0;
            }
            else if (Point0.X < Point1.X)
            {
                StartPoint = Point0;
                EndPoint = Point1;
            }
            else
            {
                StartPoint = Point1;
                EndPoint = Point0;
            }
        }

        public override void SetOffsetDistance()
        {
            XYZ verctor1 = SelectedPoint - StartPoint;
            XYZ verctor2 = EndPoint - StartPoint;

            this.OffsetDir = (EndPoint - StartPoint).CrossProduct(XYZ.BasisZ).Normalize() * OffsetDistance;
            if (verctor1.CrossProduct(verctor2).Z < 0)
            {//根据选取点的位置选择偏移方向
                OffsetDir = -OffsetDir;
            }
        }
    }

    public class OffsetArcGrid : OffsetGrid
    {
        private Arc GridArc;

        public OffsetArcGrid(XYZ selectedPoint, Grid grid) : base(selectedPoint, grid)
        {
            GridArc = grid.Curve as Arc;
           
            ArcCenter = GridArc.Center;
            GetStartAndEndPoint();
            GetDistance();
            SetOffsetDistance();
            SetStartAndEndAngle();
        }

       

        /// <summary>
        /// 弧线轴网的起始角度
        /// </summary>
        public double StartAngle  { get; set; }

        /// <summary>
        /// 弧线轴网的结束角度
        /// </summary>
        public double EndAngle { get; set; }

        /// <summary>
        /// 圆弧的中心点
        /// </summary>
        public XYZ ArcCenter { get; set; }

        /// <summary>
        /// 圆弧的最终半径
        /// </summary>
        public double ArcRadius { get; set; }

        public override void GetDistance()
        {
            this.OffsetDistance = GetDistanceFromPointToCurve(SelectedPoint, grid.Curve);
        }

        public override void GetStartAndEndPoint()
        {
            XYZ Point0 = grid.Curve.GetEndPoint(0);
            XYZ Point1 = grid.Curve.GetEndPoint(1);

            XYZ vector0 = (Point0 - GridArc.Center);
            XYZ vector1 = (Point1 - GridArc.Center);

            XYZ vectorZ = vector0.CrossProduct(vector1);
            if (vectorZ.Z > 0)
            {
                StartPoint = Point0;
                EndPoint = Point1;
               
            }
            else
            {
                StartPoint = Point1;
                EndPoint = Point0;
            }
        }

        public override void SetOffsetDistance()
        {
            XYZ verctor1 = SelectedPoint - ArcCenter;
            double radius = GridArc.Radius;
            //if (verctor1.CrossProduct(verctor2).Z < 0) { }
            if (verctor1.GetLength()<radius)
            {
                OffsetDistance = -OffsetDistance;
            }

            ArcRadius = GridArc.Radius+ OffsetDistance;
        }
        /// <summary>
        /// 设置圆弧的起始角度和结束角度
        /// </summary>
        private void SetStartAndEndAngle()
        {

        }
    }
}