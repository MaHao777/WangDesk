using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WangDesk.App.Animations;

/// <summary>
/// 宠物动画帧生成器
/// </summary>
public class PetAnimationGenerator
{
    private int _frameIndex;
    private double _cpuUsage;
    private readonly Random _random = new();

    // 配色方案
    private readonly SolidColorBrush _furBrushLight;
    private readonly SolidColorBrush _furBrushDark;
    private readonly SolidColorBrush _eyeBrush;
    private readonly SolidColorBrush _noseBrush;
    private readonly global::System.Windows.Media.Pen _outlinePen;

    public PetAnimationGenerator()
    {
        // 泰迪红棕色系
        _furBrushLight = new SolidColorBrush(global::System.Windows.Media.Color.FromRgb(160, 100, 50)); // 浅棕
        _furBrushDark = new SolidColorBrush(global::System.Windows.Media.Color.FromRgb(120, 70, 30));  // 深棕
        _eyeBrush = global::System.Windows.Media.Brushes.Black;
        _noseBrush = global::System.Windows.Media.Brushes.Black;
        _outlinePen = new global::System.Windows.Media.Pen(global::System.Windows.Media.Brushes.Transparent, 0); // 无描边，用色块堆叠
        
        // 冻结笔刷以提高性能
        _furBrushLight.Freeze();
        _furBrushDark.Freeze();
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
        _frameIndex++;
        return GenerateFrame(_frameIndex);
    }

    /// <summary>
    /// 生成指定帧的宠物图标
    /// </summary>
    private DrawingImage GenerateFrame(int frameIndex)
    {
        var drawingGroup = new DrawingGroup();
        
        // 绘制背景（透明，但为了方便调试可以临时开启）
        // drawingGroup.Children.Add(new GeometryDrawing(Brushes.LightGray, null, new RectangleGeometry(new Rect(0,0,100,100))));

        if (CpuUsage <= 0.5)
        {
            DrawStandingTeddy(drawingGroup, frameIndex);
        }
        else
        {
            // 根据CPU使用率计算奔跑速度
            var speed = GetRunningSpeed(CpuUsage);
            var runFrame = (int)(frameIndex * speed) % 8;
            DrawRunningTeddy(drawingGroup, runFrame);
        }

        return new DrawingImage(drawingGroup);
    }

    private double GetRunningSpeed(double cpuUsage)
    {
        return 0.2 + (cpuUsage / 100.0) * 1.5;
    }

    /// <summary>
    /// 绘制蓬松圆球（模拟毛发）
    /// </summary>
    private void DrawFluffyBall(DrawingGroup group, double x, double y, double width, double height, global::System.Windows.Media.Brush brush)
    {
        // 主体
        group.Children.Add(new GeometryDrawing(brush, null, 
            new EllipseGeometry(new Rect(x, y, width, height))));
        
        // 周围添加小圆圈增加蓬松感 (简单模拟)
        double r = width / 8;
        group.Children.Add(new GeometryDrawing(brush, null, new EllipseGeometry(new Rect(x - r, y + height/4, r*2, r*2))));
        group.Children.Add(new GeometryDrawing(brush, null, new EllipseGeometry(new Rect(x + width - r, y + height/4, r*2, r*2))));
        group.Children.Add(new GeometryDrawing(brush, null, new EllipseGeometry(new Rect(x + width/4, y - r, r*2, r*2))));
        group.Children.Add(new GeometryDrawing(brush, null, new EllipseGeometry(new Rect(x + width/4, y + height - r, r*2, r*2))));
    }

    private void DrawStandingTeddy(DrawingGroup group, int frame)
    {
        // 呼吸动画
        double breath = Math.Sin(frame * 0.1) * 1.5;

        // 身体 (居中)
        // 使用相对坐标系统，画布约 100x100
        double centerX = 50;
        double centerY = 60;

        // 后腿 (坐姿)
        DrawFluffyBall(group, centerX - 25, centerY + 15, 20, 20, _furBrushDark); // 左后
        DrawFluffyBall(group, centerX + 5, centerY + 15, 20, 20, _furBrushDark);  // 右后

        // 身体
        DrawFluffyBall(group, centerX - 20, centerY - 10 + breath, 40, 45, _furBrushLight);

        // 前腿
        DrawFluffyBall(group, centerX - 18, centerY + 25 + breath, 12, 18, _furBrushLight);
        DrawFluffyBall(group, centerX + 6, centerY + 25 + breath, 12, 18, _furBrushLight);

        // 头部 (大圆头)
        double headY = centerY - 35 + breath;
        DrawFluffyBall(group, centerX - 25, headY, 50, 45, _furBrushLight);

        // 耳朵 (下垂的大耳朵)
        DrawFluffyBall(group, centerX - 35, headY + 10, 15, 25, _furBrushDark); // 左耳
        DrawFluffyBall(group, centerX + 20, headY + 10, 15, 25, _furBrushDark); // 右耳

        // 面部特征
        DrawFaceFeatures(group, centerX, headY + 22); // 眼睛鼻子
        
        // 尾巴 (微微摇动)
        double tailWag = Math.Sin(frame * 0.2) * 2;
        DrawFluffyBall(group, centerX + 18 + tailWag, centerY + 10, 12, 12, _furBrushDark);
    }

    private void DrawRunningTeddy(DrawingGroup group, int runFrame)
    {
        double centerX = 50;
        double centerY = 50;
        
        // 奔跑时的身体起伏
        double bounce = Math.Abs(Math.Sin(runFrame * Math.PI / 4)) * 5;
        double tilt = Math.Sin(runFrame * Math.PI / 4) * 5; // 身体前倾后仰 (角度)

        // 腿部动画计算
        // 0: 后伸, 1: 收回, 2: 前伸, 3: 着地
        // 对角线腿同步
        double phase = runFrame * Math.PI / 4;
        
        double legLfx = Math.Sin(phase) * 15; // 左前 X摆动
        double legLfy = Math.Cos(phase) * 5;  // 左前 Y提腿
        
        double legRfx = Math.Sin(phase + Math.PI) * 15; // 右前
        double legRfy = Math.Cos(phase + Math.PI) * 5;

        double legLbx = Math.Sin(phase + Math.PI) * 15; // 左后
        double legLby = Math.Cos(phase + Math.PI) * 5;

        double legRbx = Math.Sin(phase) * 15; // 右后
        double legRby = Math.Cos(phase) * 5;

        // --- 核心修复：计算因身体旋转导致的腿部根部垂直偏移 ---
        // 角度转弧度
        double tiltRad = tilt * Math.PI / 180.0;
        
        // 前后腿相对于旋转中心(centerX)的水平距离
        // 后腿大概在 centerX - 20 的位置
        // 前腿大概在 centerX + 15 的位置
        double backLegDist = -20; 
        double frontLegDist = 15;

        // 计算旋转产生的 Y 偏移 (Body右侧下降为正)
        // OffsetY = Dist * Sin(Angle)
        double backLegTiltOffset = backLegDist * Math.Sin(tiltRad);
        double frontLegTiltOffset = frontLegDist * Math.Sin(tiltRad);

        // 绘制顺序：后腿 -> 尾巴 -> 身体 -> 头 -> 前腿 -> 耳朵

        // 后腿 (应用 backLegTiltOffset)
        // 基础位置 centerY + 20, 减去 bounce (随身体起伏), 加上 tiltOffset (随身体旋转)
        DrawFluffyBall(group, centerX - 25 + legLbx, centerY + 20 + legLby - bounce + backLegTiltOffset, 12, 18, _furBrushDark);
        DrawFluffyBall(group, centerX - 15 + legRbx, centerY + 20 + legRby - bounce + backLegTiltOffset, 12, 18, _furBrushDark);

        // 尾巴 (剧烈摇晃)
        // 尾巴在身体左侧，同样应用后部的旋转偏移
        double tailWag = Math.Sin(runFrame * 1.5) * 5;
        DrawFluffyBall(group, centerX - 35, centerY + 5 - bounce + backLegTiltOffset + tailWag, 12, 12, _furBrushDark);

        // 身体 (横向椭圆)
        // 旋转变换模拟奔跑姿态
        var bodyGeom = new EllipseGeometry(new Rect(centerX - 25, centerY - 5 - bounce, 50, 35));
        var bodyTransform = new RotateTransform(tilt, centerX, centerY);
        bodyGeom.Transform = bodyTransform;
        group.Children.Add(new GeometryDrawing(_furBrushLight, null, bodyGeom));

        // 头部 (随身体起伏)
        // 头部在身体右侧，应用前部的旋转偏移
        double headX = centerX + 20;
        double headY = centerY - 15 - bounce + frontLegTiltOffset;
        DrawFluffyBall(group, headX - 20, headY - 20, 40, 38, _furBrushLight);

        // 前腿 (应用 frontLegTiltOffset)
        DrawFluffyBall(group, centerX + 10 + legLfx, centerY + 20 + legLfy - bounce + frontLegTiltOffset, 12, 18, _furBrushLight);
        DrawFluffyBall(group, centerX + 20 + legRfx, centerY + 20 + legRfy - bounce + frontLegTiltOffset, 12, 18, _furBrushLight);

        // 耳朵 (随风飘动)
        double earFlap = Math.Sin(runFrame * 0.8) * 3;
        DrawFluffyBall(group, headX - 22, headY - 5 + earFlap, 12, 20, _furBrushDark); // 后耳
        DrawFluffyBall(group, headX + 8, headY - 5 + earFlap, 12, 20, _furBrushDark);  // 前耳

        // 面部
        DrawFaceFeatures(group, headX, headY);
    }

    private void DrawFaceFeatures(DrawingGroup group, double faceCenterX, double faceCenterY)
    {
        // 眼睛
        group.Children.Add(new GeometryDrawing(_eyeBrush, null, 
            new EllipseGeometry(new Rect(faceCenterX - 8, faceCenterY - 5, 4, 4))));
        group.Children.Add(new GeometryDrawing(_eyeBrush, null, 
            new EllipseGeometry(new Rect(faceCenterX + 4, faceCenterY - 5, 4, 4))));

        // 鼻子 (倒三角圆角)
        group.Children.Add(new GeometryDrawing(_noseBrush, null, 
            new EllipseGeometry(new Rect(faceCenterX - 3, faceCenterY + 2, 6, 4))));

        // 嘴巴 (简单的弧线，这里用小圆遮挡模拟)
        // 实际上这么小的图标嘴巴看不清，可以省略或仅画一个小点
    }
}
