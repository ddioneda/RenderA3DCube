using System;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;

namespace WindowEngine
{
    public class Game : GameWindow
    {
        private int vboHandle; // Vertex Buffer Object handle
        private int vaoHandle; // Vertex Array Object handle
        private int eboHandle; // Element Buffer Object handle
        private int shaderProgramHandle; // Shader program handle

        private int modelLoc, viewLoc, projLoc; // Uniform locations

        public Game()
            : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            this.Size = new Vector2i(1280, 768);
            this.CenterWindow(this.Size);
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.Enable(EnableCap.DepthTest);

            GL.ClearColor(0.5f, 0.7f, 0.8f, 1f);


            float[] vertices = new float[]
            {
                // Front face vertices
                -0.5f, -0.5f,  0.5f,   1f, 0f, 0f,
                 0.5f, -0.5f,  0.5f,   1f, 0f, 0f,
                 0.5f,  0.5f,  0.5f,   1f, 0f, 0f,
                -0.5f,  0.5f,  0.5f,   1f, 0f, 0f,

                // Back face vertices
                -0.5f, -0.5f, -0.5f,   1f, 0f, 0f,
                 0.5f, -0.5f, -0.5f,   1f, 0f, 0f,
                 0.5f,  0.5f, -0.5f,   1f, 0f, 0f,
                -0.5f,  0.5f, -0.5f,   1f, 0f, 0f,
            };

            uint[] indices = new uint[]
            {
                // Front face
                0, 1, 2,
                2, 3, 0,

                // Back face
                4, 5, 6,
                6, 7, 4,

                // Left face
                4, 0, 3,
                3, 7, 4,

                // Right face
                1, 5, 6,
                6, 2, 1,

                // Top face
                3, 2, 6,
                6, 7, 3,

                // Bottom face
                4, 5, 1,
                1, 0, 4
            };

            // Create vbo
            vboHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboHandle);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            // Create ebo
            eboHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboHandle);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            // Create vao
            vaoHandle = GL.GenVertexArray();
            GL.BindVertexArray(vaoHandle);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vboHandle);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboHandle);

            // Position attribute (location = 0)
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // Color attribute (location = 1)
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            // Unbind VAO first (important: unbind VAO before EBO)
            GL.BindVertexArray(0);

            // Unbind buffers for safety
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            // Vertex shader: applies model-view-projection transformations and passes color to fragment shader
            string vertexShaderCode = @"
                #version 330 core
                layout(location = 0) in vec3 aPosition;
                layout(location = 1) in vec3 aColor;

                out vec3 vertexColor;

                uniform mat4 uModel;
                uniform mat4 uView;
                uniform mat4 uProj;

                void main()
                {
                    gl_Position = uProj * uView * uModel * vec4(aPosition, 1.0);
                    vertexColor = aColor; // pass vertex color to fragment shader
                }
            ";

            // Fragment shader: outputs interpolated color
            string fragmentShaderCode = @"
                #version 330 core
                in vec3 vertexColor;
                out vec4 FragColor;

                void main()
                {
                    FragColor = vec4(vertexColor, 1.0);
                }
            ";

            // Compile shaders and check for errors
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderCode);
            GL.CompileShader(vertexShader);
            CheckShaderCompile(vertexShader, "Vertex Shader");

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderCode);
            GL.CompileShader(fragmentShader);
            CheckShaderCompile(fragmentShader, "Fragment Shader");

            // Link shaders into a program
            shaderProgramHandle = GL.CreateProgram();
            GL.AttachShader(shaderProgramHandle, vertexShader);
            GL.AttachShader(shaderProgramHandle, fragmentShader);
            GL.LinkProgram(shaderProgramHandle);

            GL.DetachShader(shaderProgramHandle, vertexShader);
            GL.DetachShader(shaderProgramHandle, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            // Get uniform locations for model, view, projection matrices
            modelLoc = GL.GetUniformLocation(shaderProgramHandle, "uModel");
            viewLoc = GL.GetUniformLocation(shaderProgramHandle, "uView");
            projLoc = GL.GetUniformLocation(shaderProgramHandle, "uProj");
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.UseProgram(shaderProgramHandle);

            Matrix4 model = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(45f)) *
                           Matrix4.CreateRotationX(MathHelper.DegreesToRadians(45f));

            // View matrix: camera looks at origin from z = 3
            Matrix4 view = Matrix4.LookAt(new Vector3(0, 0, 3), Vector3.Zero, Vector3.UnitY);

            // Projection matrix: perspective projection
            Matrix4 proj = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(60f),
                (float)Size.X / Size.Y,
                0.1f,
                100f
            );

            // Send matrices to shader
            GL.UniformMatrix4(modelLoc, false, ref model);
            GL.UniformMatrix4(viewLoc, false, ref view);
            GL.UniformMatrix4(projLoc, false, ref proj);

            // Bind VAO and draw the cube using EBO
            GL.BindVertexArray(vaoHandle);
            GL.DrawElements(PrimitiveType.Triangles, 36, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);

            SwapBuffers();
        }

        protected override void OnUnload()
        {
            // Clean up GPU resources
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.DeleteBuffer(vboHandle);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GL.DeleteBuffer(eboHandle);
            GL.BindVertexArray(0);
            GL.DeleteVertexArray(vaoHandle);
            GL.DeleteProgram(shaderProgramHandle);

            base.OnUnload();
        }

        // Check shader compilation errors
        private void CheckShaderCompile(int shader, string name)
        {
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                Console.WriteLine($"Error compiling {name}: {GL.GetShaderInfoLog(shader)}");
            }
        }
    }
}