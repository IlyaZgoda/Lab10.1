using OpenTK.Graphics.OpenGL4;

namespace Lab10._1
{
    public class Shader
    {
        private readonly int handle;
        public int Handle => handle;

        public Shader(string vertexCode, string fragmentCode)
        {
            int vertex = GL.CreateShader(ShaderType.VertexShader);

            GL.ShaderSource(vertex, vertexCode);
            GL.CompileShader(vertex);
            GL.GetShader(vertex, ShaderParameter.CompileStatus, out var success);
            if (success == 0)
                throw new Exception("Vertex shader error: " + GL.GetShaderInfoLog(vertex));

            int fragment = GL.CreateShader(ShaderType.FragmentShader);

            GL.ShaderSource(fragment, fragmentCode);
            GL.CompileShader(fragment);
            GL.GetShader(fragment, ShaderParameter.CompileStatus, out success);
            if (success == 0)
                throw new Exception("Fragment shader error: " + GL.GetShaderInfoLog(fragment));

            handle = GL.CreateProgram();

            GL.AttachShader(handle, vertex);
            GL.AttachShader(handle, fragment);
            GL.LinkProgram(handle);
            GL.DeleteShader(vertex);
            GL.DeleteShader(fragment);
        }

        public void Use() => GL.UseProgram(handle);
        public int GetAttribLocation(string name) => GL.GetAttribLocation(handle, name);
    }
}
