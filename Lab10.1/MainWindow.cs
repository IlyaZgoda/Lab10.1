using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Diagnostics;

namespace Lab10._1
{
    public class MainWindow : GameWindow
    {
        private readonly List<Vector2> _controlPoints = [];
        private int _vao, _vbo;
        private Shader _shader;
        private Shader _controlShader;
        private Shader _highlightShader;

        private int _selectedPointIndex = -1;
        private int _hoveredPointIndex = -1;
        private bool _isDragging = false;
        private int _highlightVbo;
        private const float _pickRadius = 0.01f;

        public MainWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings) { }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(1f, 1f, 1f, 1f);
            GL.Enable(EnableCap.ProgramPointSize);  
            GL.Enable(EnableCap.Blend);           
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            LoadShaders();
            SetupBuffers();

            GL.Enable(EnableCap.ProgramPointSize);
            GL.PointSize(5f); 
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            GL.Clear(ClearBufferMask.ColorBufferBit);

            if (_controlPoints.Count >= 4)
            {
                var bezierPoints = new List<Vector2>();

                for (int i = 0; i + 3 < _controlPoints.Count; i += 3)
                {
                    for (float t = 0; t <= 1; t += 0.01f)
                    {
                        var p = CalculateBezierPoint(t, _controlPoints[i], _controlPoints[i + 1], _controlPoints[i + 2], _controlPoints[i + 3]);
                        bezierPoints.Add(p);
                    }
                }

                if (_controlPoints.Count >= 2)
                {
                    DrawControlPoints();
                }

                if (_selectedPointIndex >= 0 && _selectedPointIndex < _controlPoints.Count)
                    DrawHighlightPoint(_controlPoints[_selectedPointIndex], 10f, new Vector3(1f, 0f, 0f));

                else if (_hoveredPointIndex >= 0 && _hoveredPointIndex < _controlPoints.Count)
                    DrawHighlightPoint(_controlPoints[_hoveredPointIndex], 10f, new Vector3(1f, 1f, 0f));

                GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
                GL.BufferData(BufferTarget.ArrayBuffer, bezierPoints.Count * Vector2.SizeInBytes, bezierPoints.ToArray(), BufferUsageHint.DynamicDraw);

                _shader.Use();

                GL.BindVertexArray(_vao);
                GL.DrawArrays(PrimitiveType.LineStrip, 0, bezierPoints.Count);
            }

            GL.DeleteBuffer(_highlightVbo);
            SwapBuffers();
        }

        private void DrawControlPoints()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, _controlPoints.Count * Vector2.SizeInBytes, _controlPoints.ToArray(), BufferUsageHint.DynamicDraw);

            _controlShader.Use();

            GL.BindVertexArray(_vao);
            GL.DrawArrays(PrimitiveType.LineStrip, 0, _controlPoints.Count);
            GL.PointSize(8.0f);
            GL.DrawArrays(PrimitiveType.Points, 0, _controlPoints.Count);
        }

        private void LoadShaders()
        {
            try
            {
                _shader = new Shader(vertexShaderSource, fragmentShaderSource);
                _controlShader = new Shader(vertexShaderSource, controlFragmentShaderSource);
                _highlightShader = new Shader(vertexShaderSource, highlightFragmentShaderSource);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Shader error: {ex.Message}");
            }
        }

        private void SetupBuffers()
        {
            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();
            _highlightVbo = GL.GenBuffer();

            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, 1024 * sizeof(float), IntPtr.Zero, BufferUsageHint.DynamicDraw);

            var location = _shader.GetAttribLocation("aPosition");

            GL.EnableVertexAttribArray(location);
            GL.VertexAttribPointer(location, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
        }
        
        private void DrawHighlightPoint(Vector2 point, float size, Vector3 color)
        {
            GL.PointSize(size);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _highlightVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, Vector2.SizeInBytes, ref point, BufferUsageHint.StreamDraw);
            GL.BindVertexArray(_vao);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            _highlightShader.Use();

            int colorLocation = GL.GetUniformLocation(_highlightShader.Handle, "uColor");

            GL.Uniform3(colorLocation, color);
            GL.DrawArrays(PrimitiveType.Points, 0, 1);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            var mouse = MouseState;

            if (mouse.X < 0 || mouse.Y < 0 || mouse.X >= Size.X || mouse.Y >= Size.Y)
                return;

            float x = (float)(mouse.X / Math.Max(1, Size.X) * 2.0 - 1.0);
            float y = (float)(-mouse.Y / Math.Max(1, Size.Y) * 2.0 + 1.0);
            var mousePos = new Vector2(x, y);

            if (e.Button == MouseButton.Left)
            {
                int closestIndex = -1;
                float minDist = _pickRadius;

                for (int i = 0; i < _controlPoints.Count; i++)
                {
                    float dist = (_controlPoints[i] - mousePos).LengthSquared;
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closestIndex = i;
                    }
                }

                if (closestIndex >= 0)
                {
                    _selectedPointIndex = closestIndex;
                    _isDragging = true;
                }
                else
                {
                    _controlPoints.Add(mousePos);
                }
            }
            else if (e.Button == MouseButton.Right)
            {
                int indexToRemove = _controlPoints.FindIndex(p => (p - mousePos).LengthSquared < _pickRadius);

                if (indexToRemove >= 0 && indexToRemove < _controlPoints.Count)
                {
                    _controlPoints.RemoveAt(indexToRemove);

                    if (_selectedPointIndex == indexToRemove)
                        _selectedPointIndex = -1;
                    if (_hoveredPointIndex == indexToRemove)
                        _hoveredPointIndex = -1;
                }
            }
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);

            if (e.X < 0 || e.Y < 0 || e.X >= Size.X || e.Y >= Size.Y)
                return;

            float pixelRadius = 10.0f;
            float pickRadius = (pixelRadius / Size.X) * 2.0f;

            float x = (float)(e.X / (float)Size.X * 2.0 - 1.0);
            float y = (float)(-e.Y / (float)Size.Y * 2.0 + 1.0);
            var pos = new Vector2(x, y);

            if (_isDragging && _selectedPointIndex >= 0 && _selectedPointIndex < _controlPoints.Count)
            {
                _controlPoints[_selectedPointIndex] = pos;

                if ((_selectedPointIndex - 2) % 3 == 0)
                {
                    if (_selectedPointIndex + 1 < _controlPoints.Count)
                    {
                        int nextSegmentP1 = _selectedPointIndex + 2;

                        if (nextSegmentP1 < _controlPoints.Count)
                        {
                            Vector2 p3 = _controlPoints[_selectedPointIndex + 1];
                            Vector2 p2 = _controlPoints[_selectedPointIndex];
                            _controlPoints[nextSegmentP1] = p3 + (p3 - p2);

                            int prevSegmentP2 = _selectedPointIndex - 4;
                            if (prevSegmentP2 >= 0 && prevSegmentP2 < _controlPoints.Count)
                            {
                                Vector2 p1_next = _controlPoints[nextSegmentP1];
                                _controlPoints[prevSegmentP2] = p3 - (p1_next - p3);
                            }
                        }
                    }
                }
            }

            _hoveredPointIndex = -1;
            float minDist = _pickRadius;

            for (int i = 0; i < _controlPoints.Count; i++)
            {
                float dist = (_controlPoints[i] - pos).LengthSquared;
                if (dist < minDist)
                {
                    minDist = dist;
                    _hoveredPointIndex = i;
                }
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            _isDragging = false;
            _selectedPointIndex = -1;
        }

        private Vector2 CalculateBezierPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            float u = 1 - t;
            float u2 = u * u;
            float t2 = t * t;
            float u3 = u2 * u;
            float t3 = t2 * t;

            return Vector2.Multiply(p0, u3) +
                Vector2.Multiply(p1, 3 * u2 * t) +
                Vector2.Multiply(p2, 3 * u * t2) +
                Vector2.Multiply(p3, t3);
        }

        private const string vertexShaderSource = @"
        #version 330 core
        layout(location = 0) in vec2 aPosition;
        void main()
        {
            gl_Position = vec4(aPosition, 0.0, 1.0);
        }
    ";

        private const string fragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;
        void main()
        {
            FragColor = vec4(0.0, 0.2, 0.8, 1.0);
        }
    ";

        private const string controlFragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;
        void main()
        {
            FragColor = vec4(0.3, 0.3, 0.3, 1.0); 
        }
    ";

        private const string highlightFragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;
        uniform vec3 uColor;
        void main()
        {
            FragColor = vec4(uColor, 1.0); 
        }
    ";
    }
}
