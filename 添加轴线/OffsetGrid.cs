using Autodesk.Revit.DB;

namespace 添加轴线
{
    public abstract class OffsetGrid

    {
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

        public abstract void GetDistance();

        public abstract void GetStartAndEndPoint();

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
    }

    public class OffsetLineGrid : OffsetGrid
    {
        public OffsetLineGrid(XYZ selectedPoint, Grid grid)
        {
            SelectedPoint = selectedPoint;
            GetStartAndEndPoint();
            GetDistance();
            this.grid = grid;
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
           XYZ point0=grid.Curve.GetEndPoint(0);
           XYZ point1=grid.Curve.GetEndPoint(1);
        }
    }

    public class OffsetArcGrid : OffsetGrid
    {
        public OffsetArcGrid(XYZ selectedPoint, Grid grid)
        {
            SelectedPoint = selectedPoint;
        }

        /// <summary>
        /// 弧线轴网的起始角度
        /// </summary>
        public double EndAngle { get; set; }

        /// <summary>
        /// 弧线轴网的结束角度
        /// </summary>
        public double StartAngle { get; set; }

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
            throw new System.NotImplementedException();
        }

        public override void GetStartAndEndPoint()
        {
            throw new System.NotImplementedException();
        }
    }
}