﻿using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using StbImageSharp;
using System.IO;

namespace OpenTKGame
{
    public class Game : GameWindow
    {
        // ------------------------------------------------------
        // Debug toggle + bounding box offset + scaling
        // ------------------------------------------------------
        private bool showCollisions = true;

        // Offsets let you nudge the bounding box edges inward or outward individually.
        private float playerCollisionOffsetLeft = 0.00f;
        private float playerCollisionOffsetRight = 0.00f;
        private float playerCollisionOffsetTop = 0.00f;
        private float playerCollisionOffsetBottom = 0.00f;

        // Scale factors for collision shape
        private float playerCollisionScaleX = 1.0f;
        private float playerCollisionScaleY = 1.0f;

        // ------------------------------------------------------
        // Existing fields
        // ------------------------------------------------------
        private int _vao, _vbo, ebo, _shaderProgram;
        private int texture, vao3, vbo3, shaderProgram;
        private int playerVao, playerVbo, playerEbo, playerTexture;
        private int backgroundVao, backgroundVbo, backgroundEbo, backgroundTexture;

        // The bounding boxes for squares
        private List<SquareData> squares = new List<SquareData>();

        private string boxTextureLoc = @"C:\Users\chris\OneDrive\Desktop\OpenTkProject\ConsoleApp1\ConsoleApp1\Textures\box.png";
        private SquareData boxSquare;

        // Player geometry
        float[] playerVertices =
        {
            -0.03f,  0.04f,  0.0f, 0.0f,
             0.03f,  0.04f,  1.0f, 0.0f,
             0.03f, -0.04f,  1.0f, 1.0f,
            -0.03f, -0.04f,  0.0f, 1.0f
        };
        uint[] playerIndices = { 0, 1, 2, 2, 3, 0 };

        // Background geometry
        float[] backgroundVertices =
        {
            -1.0f, -1.0f,   0.0f, 0.0f,
             1.0f, -1.0f,   1.0f, 0.0f,
             1.0f,  1.0f,   1.0f, 1.0f,
            -1.0f,  1.0f,   0.0f, 1.0f
        };
        uint[] backgroundIndices = { 0, 1, 2, 2, 3, 0 };

        // Gun geometry
        float[] vertices3 =
        {
            -0.5f, -0.5f,   0.0f, 0.0f,
             0.5f, -0.5f,   1.0f, 0.0f,
             0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f,  0.5f,   0.0f, 1.0f
        };
        uint[] indices3 = { 0, 1, 2, 2, 3, 0 };

        // Internal array for player's bounding geometry (for movement)
        // We'll do collision vs squares & screen edges only.
        private readonly float[] _vertices =
        {
           -0.03f,  0.04f,  0.0f,
            0.03f,  0.04f,  0.0f,
            0.03f, -0.04f,  0.0f,
           -0.03f, -0.04f,  0.0f
        };

        // Debug rendering shader + VAO for bounding boxes
        private int debugShader;
        private int debugVao;
        private int debugVbo;

        public Game(int width, int height, string title)
            : base(GameWindowSettings.Default, new NativeWindowSettings() { Size = (width, height), Title = title })
        {
            CenterWindow();
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            Console.WriteLine($"OpenGL Version: {GL.GetString(StringName.Version)}");
            GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);

            // 1) Create + Use main shader
            shaderProgram = CreateShader();
            GL.UseProgram(shaderProgram);

            // 2) Player Setup
            playerVao = GL.GenVertexArray();
            playerVbo = GL.GenBuffer();
            playerEbo = GL.GenBuffer();
            playerTexture = LoadTexture(@"C:\Users\chris\OneDrive\Desktop\OpenTkProject\ConsoleApp1\ConsoleApp1\Textures\SoliderRotated.png");

            GL.BindVertexArray(playerVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, playerVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, playerVertices.Length * sizeof(float), playerVertices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, playerEbo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, playerIndices.Length * sizeof(uint), playerIndices, BufferUsageHint.StaticDraw);

            int posLocationPlayer = GL.GetAttribLocation(shaderProgram, "aPosition");
            GL.EnableVertexAttribArray(posLocationPlayer);
            GL.VertexAttribPointer(posLocationPlayer, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);

            int texLocationPlayer = GL.GetAttribLocation(shaderProgram, "aTexCoord");
            GL.EnableVertexAttribArray(texLocationPlayer);
            GL.VertexAttribPointer(texLocationPlayer, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
            GL.BindVertexArray(0);

            // 3) Shotgun Setup
            vao3 = GL.GenVertexArray();
            vbo3 = GL.GenBuffer();
            ebo3 = GL.GenBuffer(); // ebo3 instead of ebo2, just to avoid confusion
            texture = LoadTexture(@"C:\Users\chris\OneDrive\Desktop\OpenTkProject\ConsoleApp1\ConsoleApp1\Textures\shotgun2.png");

            GL.BindVertexArray(vao3);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo3);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices3.Length * sizeof(float), vertices3, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo3);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices3.Length * sizeof(uint), indices3, BufferUsageHint.StaticDraw);

            int posLocationShotgun = GL.GetAttribLocation(shaderProgram, "aPosition");
            GL.EnableVertexAttribArray(posLocationShotgun);
            GL.VertexAttribPointer(posLocationShotgun, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);

            int texLocationShotgun = GL.GetAttribLocation(shaderProgram, "aTexCoord");
            GL.EnableVertexAttribArray(texLocationShotgun);
            GL.VertexAttribPointer(texLocationShotgun, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
            GL.BindVertexArray(0);

            // 4) Background Setup
            backgroundVao = GL.GenVertexArray();
            backgroundVbo = GL.GenBuffer();
            backgroundEbo = GL.GenBuffer();
            backgroundTexture = LoadTexture(@"C:\Users\chris\OneDrive\Desktop\OpenTkProject\ConsoleApp1\ConsoleApp1\Textures\tileable_grass.png");

            GL.BindVertexArray(backgroundVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, backgroundVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, backgroundVertices.Length * sizeof(float), backgroundVertices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, backgroundEbo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, backgroundIndices.Length * sizeof(uint), backgroundIndices, BufferUsageHint.StaticDraw);

            int posLocationBg = GL.GetAttribLocation(shaderProgram, "aPosition");
            GL.EnableVertexAttribArray(posLocationBg);
            GL.VertexAttribPointer(posLocationBg, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);

            int texLocationBg = GL.GetAttribLocation(shaderProgram, "aTexCoord");
            GL.EnableVertexAttribArray(texLocationBg);
            GL.VertexAttribPointer(texLocationBg, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
            GL.BindVertexArray(0);

            // 5) Enable alpha blending
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // 6) Create a square lower on screen so it doesn't overlap the player
            boxSquare = CreateSquare(0.0f, -0.4f, 0.15f, 0.15f, boxTextureLoc);
            squares.Add(boxSquare);

            // 7) Debug rendering shader + VAO
            debugShader = CreateDebugShader();
            debugVao = GL.GenVertexArray();
            debugVbo = GL.GenBuffer();
            GL.BindVertexArray(debugVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, debugVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, 4 * 2 * sizeof(float), IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.BindVertexArray(0);
        }

        // We need an int for the shotgun EBO too (previous code used ebo2).
        private int ebo3;

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            bool blendWasEnabled = GL.IsEnabled(EnableCap.Blend);
            bool depthTestWasEnabled = GL.IsEnabled(EnableCap.DepthTest);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);

            // Use main shader
            GL.UseProgram(shaderProgram);

            // 1) Draw background
            GL.BindVertexArray(backgroundVao);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, backgroundTexture);
            GL.DrawElements(PrimitiveType.Triangles, backgroundIndices.Length, DrawElementsType.UnsignedInt, 0);

            // 2) Draw squares
            foreach (var sq in squares)
            {
                GL.BindVertexArray(sq.Vao);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, sq.Texture);
                GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
            }

            // 3) Draw player
            GL.BindVertexArray(playerVao);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, playerTexture);
            GL.DrawElements(PrimitiveType.Triangles, playerIndices.Length, DrawElementsType.UnsignedInt, 0);

            // 4) Draw shotgun
            GL.BindVertexArray(vao3);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.DrawElements(PrimitiveType.Triangles, indices3.Length, DrawElementsType.UnsignedInt, 0);

            // 5) Debug bounding boxes if desired
            if (showCollisions)
            {
                GL.UseProgram(debugShader);
                int colorLoc = GL.GetUniformLocation(debugShader, "color");
                // White w/ 50% alpha
                GL.Uniform4(colorLoc, new Vector4(1f, 1f, 1f, 0.5f));

                GL.BindVertexArray(debugVao);

                // Player bounding box
                (float pLeft, float pRight, float pBottom, float pTop) = GetPlayerBoundingBox();
                DrawBox(pLeft, pBottom, pRight, pTop);

                // Square bounding boxes
                foreach (var sq in squares)
                {
                    float sqLeft = sq.CenterX - sq.HalfW;
                    float sqRight = sq.CenterX + sq.HalfW;
                    float sqBottom = sq.CenterY - sq.HalfH;
                    float sqTop = sq.CenterY + sq.HalfH;
                    DrawBox(sqLeft, sqBottom, sqRight, sqTop);
                }
            }

            GL.BindVertexArray(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.UseProgram(0);

            if (!blendWasEnabled) GL.Disable(EnableCap.Blend);
            if (depthTestWasEnabled) GL.Enable(EnableCap.DepthTest);

            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            // 1) Player bounding box with offsets + scaling
            (float playerLeft, float playerRight, float playerBottom, float playerTop) = GetPlayerBoundingBox();

            KeyboardState k = KeyboardState;
            MouseState mouse = MouseState;
            Vector2 mousePos = mouse.Position;
            float normalizedX = (mousePos.X / Size.X) * 2.0f - 1.0f;
            float normalizedY = 1.0f - (mousePos.Y / Size.Y) * 2.0f;

            float step = 0.00005f;
            Vector2 movement = Vector2.Zero;

            // Check if we can move up
            if (k.IsKeyDown(Keys.W))
            {
                if (CanMove(playerLeft, playerRight, playerTop, playerBottom, 0f, step))
                {
                    movement.Y += step;
                }
            }
            if (k.IsKeyDown(Keys.S))
            {
                if (CanMove(playerLeft, playerRight, playerTop, playerBottom, 0f, -step))
                {
                    movement.Y -= step;
                }
            }
            if (k.IsKeyDown(Keys.A))
            {
                if (CanMove(playerLeft, playerRight, playerTop, playerBottom, -step, 0f))
                {
                    movement.X -= step;
                }
            }
            if (k.IsKeyDown(Keys.D))
            {
                if (CanMove(playerLeft, playerRight, playerTop, playerBottom, step, 0f))
                {
                    movement.X += step;
                }
            }

            // 2) Apply movement to the internal bounding geometry
            for (int i = 0; i < _vertices.Length; i += 3)
            {
                _vertices[i] += movement.X;
                _vertices[i + 1] += movement.Y;
            }

            // 3) Recompute center + rotation
            float playerCenterX = (_vertices[0] + _vertices[3]) * 0.5f;
            float playerCenterY = (_vertices[1] + _vertices[4]) * 0.5f;
            Vector2 direction = new Vector2(normalizedX - playerCenterX, normalizedY - playerCenterY);
            float playerRotation = MathF.Atan2(direction.Y, direction.X);

            float playerWidth = 0.20f;
            float playerHeight = 0.20f;
            float cosA = MathF.Cos(playerRotation);
            float sinA = MathF.Sin(playerRotation);

            // Update playerVertices for rendering (rotation)
            // top-left
            playerVertices[0] = playerCenterX + (-playerWidth / 2) * cosA - (playerHeight / 2) * sinA;
            playerVertices[1] = playerCenterY + (-playerWidth / 2) * sinA + (playerHeight / 2) * cosA;
            // top-right
            playerVertices[4] = playerCenterX + (playerWidth / 2) * cosA - (playerHeight / 2) * sinA;
            playerVertices[5] = playerCenterY + (playerWidth / 2) * sinA + (playerHeight / 2) * cosA;
            // bottom-right
            playerVertices[8] = playerCenterX + (playerWidth / 2) * cosA - (-playerHeight / 2) * sinA;
            playerVertices[9] = playerCenterY + (playerWidth / 2) * sinA + (-playerHeight / 2) * cosA;
            // bottom-left
            playerVertices[12] = playerCenterX + (-playerWidth / 2) * cosA - (-playerHeight / 2) * sinA;
            playerVertices[13] = playerCenterY + (-playerWidth / 2) * sinA + (-playerHeight / 2) * cosA;

            // 4) Gun positioning
            float gunWidthScale = 0.18f;
            float gunHeightScale = 0.18f;
            float gunRotationScale = 1.0f;
            float gunXOffset = 0.13f;
            float gunYOffset = 0.0f;
            float halfGunWidth = 0.5f * gunWidthScale;
            float halfGunHeight = 0.5f * gunHeightScale;
            float gunOffsetX = gunXOffset * cosA - gunYOffset * sinA;
            float gunOffsetY = gunXOffset * sinA + gunYOffset * cosA;
            float gunCenterX = playerCenterX + gunOffsetX;
            float gunCenterY = playerCenterY + gunOffsetY;
            float gunCos = MathF.Cos(playerRotation * gunRotationScale);
            float gunSin = MathF.Sin(playerRotation * gunRotationScale);

            vertices3[0] = gunCenterX + (-halfGunWidth) * gunCos - (-halfGunHeight) * gunSin;
            vertices3[1] = gunCenterY + (-halfGunWidth) * gunSin + (-halfGunHeight) * gunCos;

            vertices3[4] = gunCenterX + (halfGunWidth) * gunCos - (-halfGunHeight) * gunSin;
            vertices3[5] = gunCenterY + (halfGunWidth) * gunSin + (-halfGunHeight) * gunCos;

            vertices3[8] = gunCenterX + (halfGunWidth) * gunCos - (halfGunHeight) * gunSin;
            vertices3[9] = gunCenterY + (halfGunWidth) * gunSin + (halfGunHeight) * gunCos;

            vertices3[12] = gunCenterX + (-halfGunWidth) * gunCos - (halfGunHeight) * gunSin;
            vertices3[13] = gunCenterY + (-halfGunWidth) * gunSin + (halfGunHeight) * gunCos;

            // 5) Update GL buffers
            GL.BindBuffer(BufferTarget.ArrayBuffer, playerVbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, playerVertices.Length * sizeof(float), playerVertices);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo3);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, vertices3.Length * sizeof(float), vertices3);

            GL.BindBuffer(BufferTarget.ArrayBuffer, backgroundVbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, backgroundVertices.Length * sizeof(float), backgroundVertices);
        }

        /// <summary>
        /// Combines offset + scaling logic for player's collision bounding box.
        /// </summary>
        private (float left, float right, float bottom, float top) GetPlayerBoundingBox()
        {
            float rawLeft = _vertices[0];
            float rawRight = _vertices[3];
            float rawTop = _vertices[1];
            float rawBottom = _vertices[7];

            // Center & half-size
            float centerX = (rawLeft + rawRight) * 0.5f;
            float centerY = (rawTop + rawBottom) * 0.5f;

            float halfW = (rawRight - rawLeft) * 0.5f;
            float halfH = (rawTop - rawBottom) * 0.5f;

            // Scale
            halfW *= playerCollisionScaleX;
            halfH *= playerCollisionScaleY;

            // Rebuild
            float left = centerX - halfW;
            float right = centerX + halfW;
            float top = centerY + halfH;
            float bottom = centerY - halfH;

            // Offsets
            left += playerCollisionOffsetLeft;
            right -= playerCollisionOffsetRight;
            top -= playerCollisionOffsetTop;
            bottom += playerCollisionOffsetBottom;

            return (left, right, bottom, top);
        }

        /// <summary>
        /// Checks collisions vs screen boundaries and squares only.
        /// </summary>
        private bool CanMove(float playerLeft, float playerRight, float playerTop, float playerBottom, float dx, float dy)
        {
            float newLeft = playerLeft + dx;
            float newRight = playerRight + dx;
            float newBottom = playerBottom + dy;
            float newTop = playerTop + dy;

            // 1) Screen bounds
            if (newLeft < -1f || newRight > 1f || newBottom < -1f || newTop > 1f)
                return false;

            // 2) Squares
            foreach (var sq in squares)
            {
                float sqLeft = sq.CenterX - sq.HalfW;
                float sqRight = sq.CenterX + sq.HalfW;
                float sqBottom = sq.CenterY - sq.HalfH;
                float sqTop = sq.CenterY + sq.HalfH;

                bool overlapH = (newRight > sqLeft && newLeft < sqRight);
                bool overlapV = (newTop > sqBottom && newBottom < sqTop);
                if (overlapH && overlapV)
                {
                    return false;
                }
            }

            return true;
        }

        private void DrawBox(float left, float bottom, float right, float top)
        {
            float[] verts = new float[]
            {
                left,  bottom,
                right, bottom,
                right, top,
                left,  top
            };

            GL.BindBuffer(BufferTarget.ArrayBuffer, debugVbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, verts.Length * sizeof(float), verts);

            // Triangle fan for 4 vertices => 2 triangles
            GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            // Remove references to old buffers you didn't want
            GL.DeleteBuffer(_vbo);
            GL.DeleteVertexArray(_vao);
            GL.DeleteBuffer(ebo);

            GL.DeleteProgram(_shaderProgram);

            GL.DeleteBuffer(playerVbo);
            GL.DeleteVertexArray(playerVao);
            GL.DeleteBuffer(playerEbo);

            GL.DeleteBuffer(backgroundVbo);
            GL.DeleteVertexArray(backgroundVao);
            GL.DeleteBuffer(backgroundEbo);

            GL.DeleteProgram(shaderProgram);

            // Also delete squares
            foreach (var sq in squares)
            {
                GL.DeleteBuffer(sq.Vbo);
                GL.DeleteBuffer(sq.Ebo);
                GL.DeleteVertexArray(sq.Vao);
            }

            // Debug resources
            GL.DeleteBuffer(debugVbo);
            GL.DeleteVertexArray(debugVao);
            GL.DeleteProgram(debugShader);

            // The shotgun buffers
            GL.DeleteBuffer(vbo3);
            GL.DeleteBuffer(ebo3);
            GL.DeleteVertexArray(vao3);
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        }

        // A struct storing data for each “square” obstacle
        public struct SquareData
        {
            public int Vao;
            public int Vbo;
            public int Ebo;
            public int Texture;

            public float CenterX;
            public float CenterY;
            public float HalfW;
            public float HalfH;
        }

        public SquareData CreateSquare(float centerX, float centerY, float width, float height, string texturePath)
        {
            float halfW = width * 0.5f;
            float halfH = height * 0.5f;

            float[] vertices =
            {
                centerX - halfW, centerY - halfH,  0.0f, 0.0f,
                centerX + halfW, centerY - halfH,  1.0f, 0.0f,
                centerX + halfW, centerY + halfH,  1.0f, 1.0f,
                centerX - halfW, centerY + halfH,  0.0f, 1.0f
            };

            uint[] indices = { 0, 1, 2, 2, 3, 0 };

            int vao = GL.GenVertexArray();
            int vbo = GL.GenBuffer();
            int ebo = GL.GenBuffer();

            int texture = LoadTexture(texturePath);

            GL.BindVertexArray(vao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            int posLocation = GL.GetAttribLocation(shaderProgram, "aPosition");
            GL.EnableVertexAttribArray(posLocation);
            GL.VertexAttribPointer(posLocation, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);

            int texLocation = GL.GetAttribLocation(shaderProgram, "aTexCoord");
            GL.EnableVertexAttribArray(texLocation);
            GL.VertexAttribPointer(texLocation, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));

            GL.BindVertexArray(0);

            return new SquareData
            {
                Vao = vao,
                Vbo = vbo,
                Ebo = ebo,
                Texture = texture,
                CenterX = centerX,
                CenterY = centerY,
                HalfW = halfW,
                HalfH = halfH
            };
        }

        private string LoadShaderSource(string filePath)
        {
            string projectDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..");
            string fullPath = Path.Combine(projectDir, "Shaders", filePath);
            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"Shader file not found: {fullPath}");
                return "";
            }
            return File.ReadAllText(fullPath);
        }

        private int LoadTexture(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine($"ERROR: Texture file not found at {path}. Loading error texture.");
                path = Path.Combine(Path.GetDirectoryName(path), "error.png");
                if (!File.Exists(path))
                {
                    Console.WriteLine($"ERROR: Error texture file not found at {path}. Cannot load texture.");
                    return 0;
                }
            }

            StbImage.stbi_set_flip_vertically_on_load(1);
            int id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);
            using (Stream stream = File.OpenRead(path))
            {
                ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                if (image == null)
                {
                    Console.WriteLine("ERROR: Failed to load texture, unknown image type.");
                    return 0;
                }
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0,
                              OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
            }
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            return id;
        }

        private int CreateDebugShader()
        {
            string vs = @"
            #version 330 core
            layout(location = 0) in vec2 aPosition;
            void main()
            {
                gl_Position = vec4(aPosition, 0.0, 1.0);
            }";

            string fs = @"
            #version 330 core
            out vec4 FragColor;
            uniform vec4 color;
            void main()
            {
                FragColor = color;
            }";

            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vs);
            GL.CompileShader(vertexShader);

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fs);
            GL.CompileShader(fragmentShader);

            int prog = GL.CreateProgram();
            GL.AttachShader(prog, vertexShader);
            GL.AttachShader(prog, fragmentShader);
            GL.LinkProgram(prog);

            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            return prog;
        }

        private int CreateShader()
        {
            string vertexShaderSource = @"
                #version 330 core
                layout (location=0) in vec2 aPosition;
                layout (location=1) in vec2 aTexCoord;
                out vec2 TexCoord;
                void main()
                {
                    gl_Position = vec4(aPosition, 0.0, 1.0);
                    TexCoord = aTexCoord;
                }";

            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderSource);
            GL.CompileShader(vertexShader);

            string fragmentShaderSource = @"
                #version 330 core
                out vec4 FragColor;
                in vec2 TexCoord;
                uniform sampler2D texture1;
                void main()
                {
                    FragColor = texture(texture1, TexCoord);
                }";

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderSource);
            GL.CompileShader(fragmentShader);

            int sp = GL.CreateProgram();
            GL.AttachShader(sp, vertexShader);
            GL.AttachShader(sp, fragmentShader);
            GL.LinkProgram(sp);

            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            return sp;
        }
    }
}
