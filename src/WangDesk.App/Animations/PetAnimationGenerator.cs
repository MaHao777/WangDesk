using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WangDesk.App.Animations;

/// <summary>
/// 宠物动画帧生成器
/// </summary>
public class PetAnimationGenerator
{
    private double _animationPhase; // 替换原有的 _frameIndex，使用连续相位
    private double _cpuUsage;
    private readonly Random _random = new();

    // 配色方案
    private readonly SolidColorBrush _furBrushLight;
    private readonly SolidColorBrush _furBrushDark;
    private readonly SolidColorBrush _eyeBrush;
    private readonly SolidColorBrush _noseBrush;
    private readonly SolidColorBrush _highlightBrush; // 眼睛高光
    private readonly SolidColorBrush _tongueBrush;    // 舌头
    private readonly global::System.Windows.Media.Pen _outlinePen;

    public PetAnimationGenerator()
    {
        // 优化配色：暖色调更明显，看起来更像泰迪/贵宾
        _furBrushLight = new SolidColorBrush(global::System.Windows.Media.Color.FromRgb(210, 140, 70)); // 亮部更金黄
        _furBrushDark = new SolidColorBrush(global::System.Windows.Media.Color.FromRgb(140, 80, 40));   // 暗部更深沉
        _eyeBrush = new SolidColorBrush(global::System.Windows.Media.Color.FromRgb(20, 20, 20));        //非纯黑，柔和一点
        _noseBrush = new SolidColorBrush(global::System.Windows.Media.Color.FromRgb(40, 20, 10));
        _highlightBrush = global::System.Windows.Media.Brushes.White;
        _tongueBrush = new SolidColorBrush(global::System.Windows.Media.Color.FromRgb(255, 100, 120));
        
        _outlinePen = new global::System.Windows.Media.Pen(global::System.Windows.Media.Brushes.Transparent, 0);
        
        // 冻结笔刷以提高性能
        _furBrushLight.Freeze();
        _furBrushDark.Freeze();
        _eyeBrush.Freeze();
        _noseBrush.Freeze();
        _highlightBrush.Freeze();
        _tongueBrush.Freeze();
        _outlinePen.Freeze();
    }

    /// <summary>
    /// CPU使用率 (0-100)
    /// </summary>
    public double CpuUsage
    {
        get => _cpuUsage;
        set => _cpuUsage = Math.Clamp(value, 0, 100);
    }

    /// <summary>
    /// 生成下一帧宠物图标
    /// </summary>
    public DrawingImage GenerateNextFrame()
    {
        // CPU使用率直接映射到奔跑速度
        // 0% CPU → 慢跑 (步长0.05)，100% CPU → 狂奔 (步长0.6)
        double step = 0.05 + (CpuUsage / 100.0) * 0.55;
        
        _animationPhase += step;
        
        // 防止数值溢出
        if (_animationPhase > 10000) _animationPhase -= 10000;

        var drawingGroup = new DrawingGroup();
        DrawRunningTeddy(drawingGroup, _animationPhase);
        return new DrawingImage(drawingGroup);
    }

    /// <summary>
    /// 绘制蓬松圆球（模拟毛发 - 优化版）
    /// </summary>
    private void DrawFluffyBall(DrawingGroup group, double x, double y, double width, double height, global::System.Windows.Media.Brush brush)
    {
        // 1. 基础形状 (主体)
        group.Children.Add(new GeometryDrawing(brush, null, 
            new EllipseGeometry(new Rect(x, y, width, height))));
        
        // 2. 边缘修饰 (增加更多小圆，位置更随机自然，模拟毛茸茸感)
        double r = width * 0.18; // 稍微调大一点边缘毛发
        
        // 定义8个方向的偏移系数，使毛发分布更圆润
        var offsets = new (double dx, double dy)[] 
        {
            (0.1, 0), (0.9, 0),       // 左右偏上
            (0, 0.4), (1, 0.4),       // 左右中部
            (0.15, 0.8), (0.85, 0.8), // 左右偏下
            (0.5, -0.1), (0.5, 0.9)   // 上下顶端
        };

        foreach (var (dx, dy) in offsets)
        {
             // 简单的内部计算替代之前的硬编码
             double circleX = x + width * dx - r;
             double circleY = y + height * dy - r;
             group.Children.Add(new GeometryDrawing(brush, null, 
                new EllipseGeometry(new Rect(circleX, circleY, r * 2, r * 2))));
        }
    }
    
    /// <summary>
    /// 绘制光滑椭圆（用于头部、耳朵等不需要毛发突起的部位）
    /// </summary>
    private void DrawSmoothBall(DrawingGroup group, double x, double y, double width, double height, global::System.Windows.Media.Brush brush)
    {
        group.Children.Add(new GeometryDrawing(brush, null, 
            new EllipseGeometry(new Rect(x, y, width, height))));
    }

    // 增加一个辅助方法绘制简单的圆
    private void DrawCircle(DrawingGroup group, global::System.Windows.Media.Brush brush, double cx, double cy, double r)
    {
        group.Children.Add(new GeometryDrawing(brush, null, new EllipseGeometry(new global::System.Windows.Point(cx, cy), r, r)));
    }

    private void DrawRunningTeddy(DrawingGroup group, double phase)
    {
        double centerX = 50;
        double centerY = 48;
        
        // 奔跑动作参数
        double runCycle = phase * 4.0;
        
        double bounce = Math.Abs(Math.Sin(runCycle)) * 5;
        double tilt = Math.Sin(runCycle) * 5;

        var bodyTransform = new RotateTransform(tilt, centerX, centerY);

        // 1. 关键连接点
        global::System.Windows.Point hipRaw = new(centerX - 16, centerY + 10 - bounce);
        global::System.Windows.Point shoulderRaw = new(centerX + 16, centerY + 10 - bounce);
        global::System.Windows.Point neckRaw = new(centerX + 20, centerY - 8 - bounce);
        global::System.Windows.Point tailRaw = new(centerX - 20, centerY - 2 - bounce);

        // 2. 变换坐标
        global::System.Windows.Point hipPos = bodyTransform.Transform(hipRaw);
        global::System.Windows.Point shoulderPos = bodyTransform.Transform(shoulderRaw);
        global::System.Windows.Point headPos = bodyTransform.Transform(neckRaw);
        global::System.Windows.Point tailPos = bodyTransform.Transform(tailRaw);

        // 3. 腿部角度 (对侧运动)
        double legRange = 35;
        double frontLegL = Math.Sin(runCycle) * legRange;
        double frontLegR = Math.Sin(runCycle + Math.PI) * legRange;
        double backLegL = Math.Sin(runCycle + Math.PI * 0.8) * legRange;
        double backLegR = Math.Sin(runCycle - Math.PI * 0.2) * legRange;

        // --- 绘制层级：远→近 ---

        // 远端腿 (深色区分远近)
        DrawLeg(group, new global::System.Windows.Point(hipPos.X - 3, hipPos.Y), backLegL, _furBrushDark);
        DrawLeg(group, new global::System.Windows.Point(shoulderPos.X - 3, shoulderPos.Y), frontLegL, _furBrushDark);

        // 尾巴（根部贴身体左边缘，向上延伸并摆动）
        double tailSwing = Math.Sin(phase * 6.0) * 25; // 摆动角度±25°
        double tailBaseX = tailPos.X;
        double tailBaseY = tailPos.Y;
        var tailGeom = new EllipseGeometry(new Rect(tailBaseX - 5, tailBaseY - 16, 10, 18));
        tailGeom.Transform = new RotateTransform(tailSwing, tailBaseX, tailBaseY); // 以根部为支点
        group.Children.Add(new GeometryDrawing(_furBrushDark, null, tailGeom));

        // 身体 (旋转的椭圆，上下窄一些)
        var bodyGeom = new EllipseGeometry(new Rect(centerX - 20, centerY - 4 - bounce, 48, 26));
        bodyGeom.Transform = bodyTransform;
        group.Children.Add(new GeometryDrawing(_furBrushLight, null, bodyGeom));

        // 近端腿 (亮色)
        DrawLeg(group, new global::System.Windows.Point(hipPos.X + 3, hipPos.Y), backLegR, _furBrushLight);
        DrawLeg(group, new global::System.Windows.Point(shoulderPos.X + 3, shoulderPos.Y), frontLegR, _furBrushLight);

        // --- 侧脸头部（减小偏移，紧贴身体）---
        double headDrawX = headPos.X; 
        double headDrawY = headPos.Y - 8;
        DrawRunningSideHead(group, headDrawX, headDrawY, runCycle);
    }

    private void DrawLeg(DrawingGroup group, global::System.Windows.Point joint, double angle, global::System.Windows.Media.Brush brush)
    {
        double legWidth = 9;  // 腿加粗一点
        double legLength = 16;

        var groupTransform = new TransformGroup();
        groupTransform.Children.Add(new RotateTransform(angle, joint.X, joint.Y));

        // 腿部几何体
        var legGeom = new RectangleGeometry(new Rect(joint.X - legWidth / 2, joint.Y, legWidth, legLength), 4, 4);
        legGeom.Transform = groupTransform;
        group.Children.Add(new GeometryDrawing(brush, null, legGeom));

        // 脚掌
        var pawGeom = new EllipseGeometry(new Rect(joint.X - 5, joint.Y + legLength - 3, 10, 8));
        pawGeom.Transform = groupTransform;
        group.Children.Add(new GeometryDrawing(brush, null, pawGeom));
        
        // 覆盖关节处，看起来更圆滑
        group.Children.Add(new GeometryDrawing(brush, null, new EllipseGeometry(joint, legWidth/2, legWidth/2)));
    }

    /// <summary>
    /// 绘制奔跑中的侧脸头部（面朝右）
    /// </summary>
    private void DrawRunningSideHead(DrawingGroup group, double hx, double hy, double runCycle)
    {
        // 耳朵向后下方四十五度飘动
        double earSway = Math.Sin(runCycle * 1.2) * 4;
        // 耳朵固定端在头顶
        double earBaseX = hx - 4;
        double earBaseY = hy - 12;
        // 尖端向左下方延伸（后下45°）
        double earTipX = earBaseX - 12 - earSway;
        double earTipY = earBaseY + 12 + earSway;  // 向下延伸

        // --- 头部主体（先画，作为底层）---
        DrawSmoothBall(group, hx - 10, hy - 18, 28, 32, _furBrushLight);

        // --- 耳朵（后画，覆盖在头上层）---
        // 用一个倾斜的椭圆：中心在根部和尖端之间
        double earCX = (earBaseX + earTipX) / 2;
        double earCY = (earBaseY + earTipY) / 2;
        double earLen = 20;
        double earWid = 10;
        // 计算倾斜角度（从根部指向尖端）
        double earAngle = Math.Atan2(earTipY - earBaseY, earTipX - earBaseX) * 180 / Math.PI;
        var earGeom = new EllipseGeometry(new global::System.Windows.Point(earCX, earCY), earLen / 2, earWid / 2);
        earGeom.Transform = new RotateTransform(earAngle, earCX, earCY);
        group.Children.Add(new GeometryDrawing(_furBrushDark, null, earGeom));

        // --- 口鼻部 (明显向右突出，用深色底 + 亮色面形成立体感) ---
        // 口鼻深色底（轮廓）
        DrawSmoothBall(group, hx + 12, hy - 6, 20, 16, _furBrushDark);
        // 口鼻亮色面（覆盖上半部分）
        DrawSmoothBall(group, hx + 13, hy - 7, 18, 12, _furBrushLight);

        // --- 面部细节 ---
        // 眼睛 (侧面只看到一只，偏右)
        DrawCircle(group, _eyeBrush, hx + 6, hy - 6, 2.8);
        DrawCircle(group, _highlightBrush, hx + 5.2, hy - 7.2, 0.9); // 高光

        // 鼻子 (在口鼻最前端)
        DrawCircle(group, _noseBrush, hx + 22, hy - 1, 2.5);

        // 嘴巴线 (用一个细长深色椭圆模拟)
        group.Children.Add(new GeometryDrawing(_noseBrush, null,
            new EllipseGeometry(new Rect(hx + 14, hy + 2, 8, 1.5))));

        // 舌头 (从嘴巴下方伸出)
        double tongueFlap = Math.Sin(runCycle * 0.8) * 1.5; // 舌头随跑步轻微颤动
        group.Children.Add(new GeometryDrawing(_tongueBrush, null,
            new EllipseGeometry(new Rect(hx + 16, hy + 3 + tongueFlap, 5, 7))));
    }

    private void DrawFaceFeatures(DrawingGroup group, double faceCenterX, double faceCenterY, bool isRunning = false)
    {
        // 眼睛 (适当间距，加高光)
        double eyeY = faceCenterY - 3;
        double eyeSpacing = 7; // 眼距缩小，更集中可爱
        
        // 左眼
        DrawCircle(group, _eyeBrush, faceCenterX - eyeSpacing, eyeY, 3.0);
        DrawCircle(group, _highlightBrush, faceCenterX - eyeSpacing - 0.8, eyeY - 1.2, 1.0);

        // 右眼
        DrawCircle(group, _eyeBrush, faceCenterX + eyeSpacing, eyeY, 3.0);
        DrawCircle(group, _highlightBrush, faceCenterX + eyeSpacing - 0.8, eyeY - 1.2, 1.0);

        // 鼻子 (椭圆，紧凑)
        group.Children.Add(new GeometryDrawing(_noseBrush, null, 
            new EllipseGeometry(new Rect(faceCenterX - 3.5, faceCenterY + 4, 7, 5))));

        // 奔跑时吐舌头
        if (isRunning)
        {
            group.Children.Add(new GeometryDrawing(_tongueBrush, null, 
                new EllipseGeometry(new Rect(faceCenterX - 2, faceCenterY + 8, 5, 7))));
        }
    }
}
