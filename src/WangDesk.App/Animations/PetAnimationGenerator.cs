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

        // 定义旋转变换 (绕中心点旋转)
        var bodyTransform = new RotateTransform(tilt, centerX, centerY);

        // --- 核心修复：使用变换矩阵计算精确的连接点坐标 ---
        
        // 1. 定义身体上的关键连接点 (相对于 (0,0) 的原始坐标，未旋转，但包含起伏 bounce)
        //    身体中心在 (centerX, centerY - bounce + 10) 左右
        //    髋关节 (后腿根部): 身体左侧
        global::System.Windows.Point hipRaw = new global::System.Windows.Point(centerX - 20, centerY + 15 - bounce);
        //    肩关节 (前腿根部): 身体右侧
        global::System.Windows.Point shoulderRaw = new global::System.Windows.Point(centerX + 15, centerY + 15 - bounce);
        //    颈部 (头部连接点): 身体右上方
        global::System.Windows.Point neckRaw = new global::System.Windows.Point(centerX + 20, centerY - 5 - bounce);
        //    尾部 (尾巴连接点): 身体左侧
        global::System.Windows.Point tailRaw = new global::System.Windows.Point(centerX - 30, centerY + 5 - bounce);

        // 2. 应用旋转变换，获取这一帧的精确屏幕坐标
        global::System.Windows.Point hipPos = bodyTransform.Transform(hipRaw);
        global::System.Windows.Point shoulderPos = bodyTransform.Transform(shoulderRaw);
        global::System.Windows.Point headPos = bodyTransform.Transform(neckRaw);
        global::System.Windows.Point tailPos = bodyTransform.Transform(tailRaw);


        // --- 腿部摆动动画计算 ---
        // 使用角度旋转，让腿与关节绑定而不脱离
        double phase = runFrame * Math.PI / 4;
        double frontLegAngle = Math.Sin(phase) * 35;   // 前腿摆幅
        double backLegAngle = Math.Sin(phase + Math.PI) * 35; // 后腿摆幅


        // --- 开始绘制 ---
        // 绘制顺序：后腿 -> 尾巴 -> 身体 -> 头 -> 前腿 -> 耳朵

        // 后腿 (以变换后的 hipPos 为根部)
        DrawLeg(group, hipPos, backLegAngle, _furBrushDark);
        DrawLeg(group, new global::System.Windows.Point(hipPos.X + 8, hipPos.Y), backLegAngle, _furBrushDark);

        // 尾巴 (以变换后的 tailPos 为根部，附加剧烈摇晃)
        double tailWag = Math.Sin(runFrame * 1.5) * 5;
        // 注意：尾巴也是附着在身体上的，所以基准点是 tailPos
        DrawFluffyBall(group, tailPos.X - 5, tailPos.Y - 5 + tailWag, 12, 12, _furBrushDark);

        // 身体 (横向椭圆)
        // 身体本身直接应用旋转变换
        var bodyGeom = new EllipseGeometry(new Rect(centerX - 25, centerY - 5 - bounce, 50, 35));
        bodyGeom.Transform = bodyTransform;
        group.Children.Add(new GeometryDrawing(_furBrushLight, null, bodyGeom));

        // 头部 (以变换后的 headPos 为中心)
        // 头部稍微偏离连接点一点点，形成自然位置
        double headDrawX = headPos.X; 
        double headDrawY = headPos.Y - 15; // 脖子往上
        DrawFluffyBall(group, headDrawX - 20, headDrawY - 20, 40, 38, _furBrushLight);

        // 前腿 (以变换后的 shoulderPos 为根部)
        DrawLeg(group, shoulderPos, frontLegAngle, _furBrushLight);
        DrawLeg(group, new global::System.Windows.Point(shoulderPos.X + 8, shoulderPos.Y), frontLegAngle, _furBrushLight);

        // 耳朵 (随风飘动)
        double earFlap = Math.Sin(runFrame * 0.8) * 3;
        DrawFluffyBall(group, headDrawX - 22, headDrawY - 5 + earFlap, 12, 20, _furBrushDark); // 后耳
        DrawFluffyBall(group, headDrawX + 8, headDrawY - 5 + earFlap, 12, 20, _furBrushDark);  // 前耳

        // 面部
        DrawFaceFeatures(group, headDrawX, headDrawY);
    }

    private void DrawLeg(DrawingGroup group, global::System.Windows.Point joint, double angle, global::System.Windows.Media.Brush brush)
    {
        double legWidth = 6;
        double legLength = 18;

        var legRect = new RectangleGeometry(new Rect(joint.X - legWidth / 2, joint.Y, legWidth, legLength), 2, 2);

        var transform = new RotateTransform(angle, joint.X, joint.Y);
        legRect.Transform = transform;

        group.Children.Add(new GeometryDrawing(brush, null, legRect));

        // 小脚掌，增加连接感
        var paw = new EllipseGeometry(new Rect(joint.X - 4, joint.Y + legLength - 2, 8, 6));
        paw.Transform = transform;
        group.Children.Add(new GeometryDrawing(brush, null, paw));

        // 关节小圆，避免视觉断裂
        var jointCircle = new EllipseGeometry(new Rect(joint.X - 3, joint.Y - 2, 6, 6));
        group.Children.Add(new GeometryDrawing(brush, null, jointCircle));
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
