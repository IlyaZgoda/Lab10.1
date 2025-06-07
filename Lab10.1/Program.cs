using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using System.Drawing;

namespace Lab10._1
{
    public class Program
    {
        public static void Main()
        {
            var nativeWindowSettings = new NativeWindowSettings()
            {
                ClientSize = new Vector2i(800, 600),
                Title = "Bezier Spline"
            };

            using var window = new MainWindow(GameWindowSettings.Default, nativeWindowSettings);
            window.Run();
        }
    }
}
