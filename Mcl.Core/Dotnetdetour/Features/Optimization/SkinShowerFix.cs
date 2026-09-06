using System;
using System.Collections; // 必须引入，为了使用 IDictionary
using System.Collections.Generic;
using System.Reflection;
using System.Windows; // 必须引入，为了使用反射
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using _3DTools;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using WPFLauncher.View.Launcher;
using WPFLauncher.View.Launcher.LaunchPanel;

namespace Mcl.Core.Dotnetdetour.Features.Optizime;

public class SkinShowerFix : IMethodHook
{
	[HookMethod("WPFLauncher.View.Launcher.gb", "z", null)]
	// 注意：这里的 bzp 参数类型改为了 IDictionary，绕过 gb.SkinPart 的访问限制
	public static List<InteractiveVisual3D> z(BitmapSource bzn, SkinGemotry bzo, IDictionary bzp = null)
	{
		// 1. 获取隐藏类 gb 的 Type
		Type gbType = typeof(SkinShower).Assembly.GetType("WPFLauncher.View.Launcher.gb");

		// 2. 反射调用 gb.aa()
		MethodInfo aaMethod = gbType.GetMethod("aa", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		aaMethod?.Invoke(null, null);

		// 提前获取 y 方法、内部字典和枚举 Type，避免在循环中重复获取影响性能
		MethodInfo yMethod = gbType.GetMethod("y", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		
		FieldInfo cField = gbType.GetField("c", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		IDictionary dictC = cField != null ? (IDictionary)cField.GetValue(null) : new Hashtable();

		FieldInfo dField = gbType.GetField("d", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		IDictionary dictD = dField != null ? (IDictionary)dField.GetValue(null) : new Hashtable();

		Type skinPartType = gbType.GetNestedType("SkinPart", BindingFlags.Public | BindingFlags.NonPublic);

		Dictionary<string, SkinBone> dictionary = new Dictionary<string, SkinBone>();
		Dictionary<string, InteractiveVisual3D> dictionary2 = new Dictionary<string, InteractiveVisual3D>();
		double[] array = new double[]
		{
			(double)((bzo.texturewidth == 0) ? 64 : bzo.texturewidth),
			(double)((bzo.textureheight == 0) ? 64 : bzo.textureheight)
		};
		int num = bzn.PixelWidth / bzo.texturewidth;
		List<InteractiveVisual3D> list = new List<InteractiveVisual3D>();
		foreach (SkinBone skinBone in bzo.bones)
		{
			Image image = new Image();
			image.Source = bzn;
			RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
			RenderOptions.SetClearTypeHint(image, ClearTypeHint.Enabled);
			RenderOptions.SetEdgeMode(image, EdgeMode.Aliased);
			InteractiveVisual3D interactiveVisual3D = new InteractiveVisual3D();
			interactiveVisual3D.Visual = image;
			MeshGeometry3D meshGeometry3D = new MeshGeometry3D();
			if (skinBone.cubes != null)
			{
				foreach (SkinCube skinCube in skinBone.cubes)
				{
					double[] array2 = new double[]
					{
						skinCube.origin[0] - skinBone.pivot[0],
						skinCube.origin[1] - skinBone.pivot[1],
						skinCube.origin[2] - skinBone.pivot[2]
					};
					// 3. 反射调用 gb.y(...)
					yMethod?.Invoke(null, new object[] { meshGeometry3D, array2, skinCube.size, skinCube.uv, array, skinCube.inflate, skinCube.mirror });
				}
			}
			interactiveVisual3D.Geometry = meshGeometry3D;
			interactiveVisual3D.IsBackVisible = true;
			dictionary[skinBone.name] = skinBone;
			dictionary2[skinBone.name] = interactiveVisual3D;
			if (bzp != null && skinPartType != null)
			{
				// 4. 反射遍历内部枚举 gb.SkinPart
				foreach (object skinPartObj in Enum.GetValues(skinPartType))
				{
					if (skinBone.name.ToLower().Equals(skinPartObj.ToString().ToLower()))
					{
						// 5. 反射使用 gb.c 字典 (使用 IDictionary.Contains)
						if (dictC.Contains(skinBone.name.ToLower()))
						{
							// bzp.Add(skinPartObj, interactiveVisual3D);
						}
						break;
					}
				}
			}
		}
		foreach (SkinBone skinBone2 in bzo.bones)
		{
			InteractiveVisual3D interactiveVisual3D2 = dictionary2[skinBone2.name];
			Vector3D vector3D = new Vector3D(skinBone2.pivot[0], skinBone2.pivot[1] - 19.0, skinBone2.pivot[2]);
			if (!string.IsNullOrEmpty(skinBone2.parent))
			{
				dictionary2[skinBone2.parent].Children.Add(dictionary2[skinBone2.name]);
				SkinBone skinBone3 = dictionary[skinBone2.parent];
				vector3D = new Vector3D(skinBone2.pivot[0] - skinBone3.pivot[0], skinBone2.pivot[1] - skinBone3.pivot[1], skinBone3.pivot[2] - skinBone2.pivot[2]);
			}
			else
			{
				list.Add(interactiveVisual3D2);
			}
			interactiveVisual3D2.IsBackVisible = true;
			Vector3D vector3D2 = default(Vector3D);
			Point3D point3D = default(Point3D);
			
			// 6. 反射使用 gb.d 和 gb.c 字典
			if (dictD.Contains(skinBone2.name.ToLower()))
			{
				vector3D2 = (Vector3D)dictD[skinBone2.name.ToLower()];
				point3D = (Point3D)dictC[skinBone2.name.ToLower()];
			}

			RotateTransform3D rotateTransform3D = new RotateTransform3D(new AxisAngleRotation3D(vector3D2, 0.0), point3D);
			Transform3DGroup transform3DGroup = new Transform3DGroup();
			transform3DGroup.Children.Add(rotateTransform3D);
			if (skinBone2.rotation != null && skinBone2.rotation.Length == 3)
			{
				AxisAngleRotation3D axisAngleRotation3D = new AxisAngleRotation3D(new Vector3D(1.0, 0.0, 0.0), skinBone2.rotation[0]);
				AxisAngleRotation3D axisAngleRotation3D2 = new AxisAngleRotation3D(new Vector3D(0.0, 1.0, 0.0), -skinBone2.rotation[1]);
				AxisAngleRotation3D axisAngleRotation3D3 = new AxisAngleRotation3D(new Vector3D(0.0, 0.0, 1.0), -skinBone2.rotation[2]);
				transform3DGroup.Children.Add(new RotateTransform3D(axisAngleRotation3D));
				transform3DGroup.Children.Add(new RotateTransform3D(axisAngleRotation3D2));
				transform3DGroup.Children.Add(new RotateTransform3D(axisAngleRotation3D3));
			}
			transform3DGroup.Children.Add(new TranslateTransform3D(vector3D));
			transform3DGroup.Children.Add(new ScaleTransform3D());
			interactiveVisual3D2.Transform = transform3DGroup;
		}
		return list;
	}
	
	[HookMethod("WPFLauncher.View.Launcher.gb", "y", null)]
	public static void y(MeshGeometry3D bzg, double[] bzh, double[] bzi, double[] bzj, double[] bzk, double bzl, bool bzm)
	{
		// 1. 反射获取 gb 类 和 x 方法
		Type gbType = typeof(SkinShower).Assembly.GetType("WPFLauncher.View.Launcher.gb");
		MethodInfo xMethod = gbType?.GetMethod("x", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		
		double num = Math.Floor(bzi[2]);   // UV 用 floor
		double num2 = Math.Floor(bzi[0]);
		double num3 = Math.Floor(bzi[1]);
		Point3D point3D = new Point3D(bzh[0] - bzl, bzh[1] - bzl, -bzh[2] - bzl);
		Point3D point3D2 = new Point3D(bzh[0] + bzi[0] + bzl, bzh[1] + bzi[1] + bzl, -bzh[2] + bzi[2] + bzl);
		Point3D point3D3 = new Point3D(point3D.X, point3D2.Y, point3D2.Z - bzi[2]);   // 顶点 z 用原始
		Point3D point3D4 = new Point3D(point3D2.X, point3D.Y, point3D.Z - bzi[2]);    // 顶点 z 用原始
		Point3D point3D5 = new Point3D(point3D3.X, point3D3.Y, point3D3.Z);
		Point3D point3D6 = new Point3D(point3D4.X, point3D3.Y, point3D3.Z);
		Point3D point3D7 = new Point3D(point3D4.X, point3D4.Y, point3D3.Z);
		Point3D point3D8 = new Point3D(point3D3.X, point3D4.Y, point3D3.Z);
		Point3D point3D9 = new Point3D(point3D3.X, point3D3.Y, point3D4.Z);
		Point3D point3D10 = new Point3D(point3D4.X, point3D3.Y, point3D4.Z);
		Point3D point3D11 = new Point3D(point3D4.X, point3D4.Y, point3D4.Z);
		Point3D point3D12 = new Point3D(point3D3.X, point3D4.Y, point3D4.Z);
		double num4 = bzk[0];
		double num5 = bzk[1];
		Point point = new Point(bzj[0], bzj[1]);

		// 定义一个本地辅助函数，用于包装反射调用，让下面的代码更干净
		void InvokeX(Point3D p1, Point3D p2, Point3D p3, Point3D p4, double v1, double v2, double v3, double v4, double v5, double v6, bool flag)
		{
			xMethod?.Invoke(null, new object[] { bzg, p1, p2, p3, p4, v1, v2, v3, v4, v5, v6, flag });
		}

		if (bzm)
		{
			// 2. 将原来的 gb.x 替换为我们的 InvokeX 辅助函数
			InvokeX(point3D9, point3D5, point3D8, point3D12, point.X + num + num2, point.Y + num, point.X + num + num2 + num, point.Y + num + num3, num4, num5, true);
			InvokeX(point3D6, point3D10, point3D11, point3D7, point.X + 0.0, point.Y + num, point.X + num, point.Y + num + num3, num4, num5, true);
			InvokeX(point3D9, point3D10, point3D6, point3D5, point.X + num, point.Y + 0.0, point.X + num + num2, point.Y + num, num4, num5, true);
			InvokeX(point3D8, point3D7, point3D11, point3D12, point.X + num + num2, point.Y + num, point.X + num + num2 + num2, point.Y, num4, num5, true);
			InvokeX(point3D5, point3D6, point3D7, point3D8, point.X + num, point.Y + num, point.X + num + num2, point.Y + num + num3, num4, num5, true);
			InvokeX(point3D10, point3D9, point3D12, point3D11, point.X + num + num2 + num, point.Y + num, point.X + num + num2 + num + num2, point.Y + num + num3, num4, num5, true);
		}
		else
		{
			// 替换 gb.x
			InvokeX(point3D10, point3D6, point3D7, point3D11, point.X + num + num2, point.Y + num, point.X + num + num2 + num, point.Y + num + num3, num4, num5, false);
			InvokeX(point3D5, point3D9, point3D12, point3D8, point.X + 0.0, point.Y + num, point.X + num, point.Y + num + num3, num4, num5, false);
			InvokeX(point3D10, point3D9, point3D5, point3D6, point.X + num, point.Y + 0.0, point.X + num + num2, point.Y + num, num4, num5, false);
			InvokeX(point3D7, point3D8, point3D12, point3D11, point.X + num + num2, point.Y + num, point.X + num + num2 + num2, point.Y, num4, num5, false);
			InvokeX(point3D6, point3D5, point3D8, point3D7, point.X + num, point.Y + num, point.X + num + num2, point.Y + num + num3, num4, num5, false);
			InvokeX(point3D9, point3D10, point3D11, point3D12, point.X + num + num2 + num, point.Y + num, point.X + num + num2 + num + num2, point.Y + num + num3, num4, num5, false);
		}
	}
}