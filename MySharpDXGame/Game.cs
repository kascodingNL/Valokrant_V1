using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using SharpDX.RawInput;
using System;
using System.Drawing;
using System.Linq;
using D3D11 = SharpDX.Direct3D11;
using System.Windows.Forms;
using SharpDX.Direct2D1;
using System.Collections.Generic;
using Valokrant.V1.PhysX;
using Jitter.Collision.Shapes;
using ObjParse;
using SharpDX.DirectInput;

namespace Valokrant.V1
{
	public class Game : IDisposable
	{
		private RenderForm renderForm;

        public Dictionary<int, GameObject> gameobjects = new Dictionary<int, GameObject>();

		private const int Width = 1920;
		private const int Height = 1080;

		private D3D11.Device d3dDevice;
		private D3D11.DeviceContext d3dDeviceContext;
		private SwapChain swapChain;
		private D3D11.RenderTargetView renderTargetView;
		private Viewport viewport;

		// Shaders
		private D3D11.VertexShader vertexShader;
		private D3D11.PixelShader pixelShader;
		private ShaderSignature inputSignature;
		private D3D11.InputLayout inputLayout;

		private D3D11.InputElement[] inputElements = new D3D11.InputElement[] 
		{
			new D3D11.InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, D3D11.InputClassification.PerVertexData, 0),
			new D3D11.InputElement("COLOR", 0, Format.R32G32B32A32_Float, 12, 0, D3D11.InputClassification.PerVertexData, 0)
		};

        public PhysicsScene PhysicsScene;

        // Triangle vertices
        /*private VertexPositionColor[] vertices = new VertexPositionColor[] {
            new VertexPositionColor(new Vector3(-0.5f, 0.5f, 0.0f), SharpDX.Color.Red),
            new VertexPositionColor(new Vector3(0.5f, 0.5f, 0.0f), SharpDX.Color.Green),
            new VertexPositionColor(new Vector3(0.0f, -0.5f, 0.0f), SharpDX.Color.Blue) };*/

		private D3D11.Buffer triangleVertexBuffer;

        public WindowInfo windowInfo;

        public Vector2 mousePos;
        public Surface framerateSurface;

        public Joystick joystick;
        public Keyboard keyboard;

		/// <summary>
		/// Create and initialize a new game.
		/// </summary>
		public Game(string windowName, int version)
		{
            PhysicsScene = new PhysicsScene(true);

            gameobjects.Add(0, new GameObject());

            gameobjects[0].components.Add(new Rigidbody(new BoxShape(1, 1, 1), this));

            var vertArr = new VertexPositionColor[]
            {
                                            new VertexPositionColor(new Vector3(-0.1f, 0.1f, 0.0f), SharpDX.Color.Red),
            new VertexPositionColor(new Vector3(0.1f, 0.1f, 0.0f), SharpDX.Color.Green),
            new VertexPositionColor(new Vector3(0.0f, -0.1f, 0.0f + updateMargin), SharpDX.Color.Blue),
            };

            gameobjects[0].components.Add(new MeshRenderer(vertArr));

            SharpDX.RawInput.Device.RegisterDevice(SharpDX.Multimedia.UsagePage.Generic, SharpDX.Multimedia.UsageId.GenericMouse, SharpDX.RawInput.DeviceFlags.None);
            SharpDX.RawInput.Device.RegisterDevice(SharpDX.Multimedia.UsagePage.Generic, SharpDX.Multimedia.UsageId.GenericKeyboard, SharpDX.RawInput.DeviceFlags.None);

            SharpDX.RawInput.Device.KeyboardInput += Device_KeyboardInput;
            SharpDX.RawInput.Device.MouseInput += Device_MouseInput;

            windowInfo = new WindowInfo()
            {
                WindowName = windowName,
                startTime = DateTime.Now
            };
			// Set window properties
			renderForm = new RenderForm(windowInfo.WindowName + " V" + version);
			renderForm.ClientSize = new Size(Width, Height);
			renderForm.AllowUserResizing = false;

			InitializeDeviceResources();
            InitializeTriangle(vertArr);
			InitializeShaders();

            //Input device initialization
            var directInput = new DirectInput();

            var joystickGuid = Guid.Empty;

            foreach (var deviceInstance in directInput.GetDevices(SharpDX.DirectInput.DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices))
                joystickGuid = deviceInstance.InstanceGuid;

            // If Gamepad not found, look for a Joystick
            if (joystickGuid == Guid.Empty)
                foreach (var deviceInstance in directInput.GetDevices(SharpDX.DirectInput.DeviceType.Joystick, DeviceEnumerationFlags.AllDevices))
                    joystickGuid = deviceInstance.InstanceGuid;

            // If Joystick not found, throws an error
            if (joystickGuid == Guid.Empty)
            {
                Console.WriteLine("No joystick/Gamepad found.");
            }
            else
            {
                joystick = new Joystick(directInput, joystickGuid);
                joystick.Properties.BufferSize = 128;

                // Acquire the joystick
                joystick.Acquire();

                keyboard = new Keyboard(directInput);

                keyboard.Properties.BufferSize = 128;
                keyboard.Acquire();
            }
        }

        private void Device_MouseInput(object sender, MouseInputEventArgs e)
        {
            e.Mode = MouseMode.MoveAbsolute;
            mousePos += new Vector2(e.X * .003f, -e.Y * .003f);
        }

        private Keys keys;
        private void Device_KeyboardInput(object sender, KeyboardInputEventArgs e)
        {
            keys = e.Key;
            float dt = -(float)(DateTime.Now - lastDraw).TotalMilliseconds;

            ((Rigidbody)gameobjects[0].components[1]).jitterBody.LinearVelocity += new Jitter.LinearMath.JVector(0, 1, 0);

            var pos = ((Rigidbody)gameobjects[0].components[1]).jitterBody.Position;
            ((Transform)gameobjects[0].components[0]).position = new Vector3(pos.X, pos.Y, pos.Z);
        }

        /// <summary>
        /// Start the game.
        /// </summary>
        public void Run()
		{
			// Start the render loop
			RenderLoop.Run(renderForm, RenderCallback);
		}

		private void RenderCallback()
		{
			Draw();
		}

		private void InitializeDeviceResources()
		{
			ModeDescription backBufferDesc = new ModeDescription(Width, Height, new Rational(60, 1), Format.R8G8B8A8_UNorm);
			
			// Descriptor for the swap chain
			SwapChainDescription swapChainDesc = new SwapChainDescription()
			{
				ModeDescription = backBufferDesc,
				SampleDescription = new SampleDescription(1, 0),
				Usage = Usage.RenderTargetOutput,
				BufferCount = 1,
				OutputHandle = renderForm.Handle,
				IsWindowed = true
			};

			// Create device and swap chain
			D3D11.Device.CreateWithSwapChain(DriverType.Hardware, D3D11.DeviceCreationFlags.None, swapChainDesc, out d3dDevice, out swapChain);
			d3dDeviceContext = d3dDevice.ImmediateContext;

			viewport = new Viewport(0, 0, Width, Height);
			d3dDeviceContext.Rasterizer.SetViewport(viewport);

			// Create render target view for back buffer
			using(D3D11.Texture2D backBuffer = swapChain.GetBackBuffer<D3D11.Texture2D>(0))
			{
				renderTargetView = new D3D11.RenderTargetView(d3dDevice, backBuffer);
			}
		}

		private void InitializeShaders()
		{
			// Compile the vertex shader code
			using(var vertexShaderByteCode = ShaderBytecode.CompileFromFile("vertexShader.hlsl", "main", "vs_4_0", ShaderFlags.Debug))
			{
				// Read input signature from shader code
				inputSignature = ShaderSignature.GetInputSignature(vertexShaderByteCode);

				vertexShader = new D3D11.VertexShader(d3dDevice, vertexShaderByteCode);
			}

			// Compile the pixel shader code
			using(var pixelShaderByteCode = ShaderBytecode.CompileFromFile("pixelShader.hlsl", "main", "ps_4_0", ShaderFlags.Debug))
			{
				pixelShader = new PixelShader(d3dDevice, pixelShaderByteCode);
			}

			// Set as current vertex and pixel shaders
			d3dDeviceContext.VertexShader.Set(vertexShader);
			d3dDeviceContext.PixelShader.Set(pixelShader);

			d3dDeviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

			// Create the input layout from the input signature and the input elements
			inputLayout = new InputLayout(d3dDevice, inputSignature, inputElements);

			// Set input layout to use
			d3dDeviceContext.InputAssembler.InputLayout = inputLayout;
		}

		private void InitializeTriangle(VertexPositionColor[] vertices)
		{
			// Create a vertex buffer, and use our array with vertices as data
			if(vertices.Length > 0)
            {
                if (triangleVertexBuffer != null)
                {
                    triangleVertexBuffer.Dispose();
                    triangleVertexBuffer = D3D11.Buffer.Create(d3dDevice, D3D11.BindFlags.VertexBuffer, vertices, usage: ResourceUsage.Dynamic, accessFlags: CpuAccessFlags.Write,
    optionFlags: ResourceOptionFlags.None);
                }
                else
                {
                    //Initialize GPU triangle buffer for first time. First time only because after that it gets dynamically updated into ram.
                    triangleVertexBuffer = D3D11.Buffer.Create(d3dDevice, D3D11.BindFlags.VertexBuffer, vertices, usage: ResourceUsage.Dynamic, accessFlags: CpuAccessFlags.Write,
                        optionFlags: ResourceOptionFlags.None);
                }
            }
		}
        public float updateMargin;
        public bool marginBack = false;

        public Vector3 lastTriPos;
        public Vector3 objPos;

        public DateTime lastDraw;

        public int drawCount;

        public VertexPositionColor[] lastVertexes;

        public JoystickUpdate[] joystickUpdates;
        public KeyboardUpdate[] keyboardUpdates;

        public DateTime lastJoyChange = DateTime.Now;

		/// <summary>
		/// Draw the game.
		/// </summary>
		private void Draw()
        {
            float dt = (float) (DateTime.Now - lastDraw).TotalMilliseconds;
            float framerate = 1000 / dt;

            lastDraw = DateTime.Now;

            PhysicsScene.Simulate(1/60f);

            //Joystick polling
            if(joystick != null)
            {
                joystick.Poll();

                joystickUpdates = joystick.GetBufferedData();

                foreach (var state in joystickUpdates)
                {
                    //Console.WriteLine(state);

                    if(state.Offset == JoystickOffset.RotationY)
                    {
                        float dt1 = (float)(DateTime.Now - lastJoyChange).TotalSeconds;
                        ((Rigidbody)gameobjects[0].components[1]).jitterBody.LinearVelocity += new Jitter.LinearMath.JVector(0, (state.Value * dt) / 150000, 0);
                        lastJoyChange = DateTime.Now;

                        var pos = ((Rigidbody)gameobjects[0].components[1]).jitterBody.Position;
                        ((Transform)gameobjects[0].components[0]).position = new Vector3(pos.X, pos.Y, pos.Z);
                    }

                    if(state.Offset == JoystickOffset.RotationX)
                    {

                    }
                }
            }

            //Keyboard polling
            if (keyboard != null)
            {
                keyboard.Poll();

                keyboardUpdates = keyboard.GetBufferedData();

                foreach (var update in keyboardUpdates)
                {
                    Console.WriteLine(update);

                    if (update.Key == Key.W)
                    {
                        ((Rigidbody)gameobjects[0].components[1]).jitterBody.LinearVelocity += new Jitter.LinearMath.JVector(0, 1, 0);

                        var pos = ((Rigidbody)gameobjects[0].components[1]).jitterBody.Position;
                        ((Transform)gameobjects[0].components[0]).position = new Vector3(pos.X, pos.Y, pos.Z);
                    }
                }
            }

            //Console.WriteLine("dt: " + dt + ", frame_rate: " + framerate);
            //objPos += marginBack ? new Vector3(-.1f, 0, 0) : new Vector3(.1f, 0, 0);
            List<VertexPositionColor> batchedVerts = new List<VertexPositionColor>();

            foreach(GameObject go in gameobjects.Values)
            {
                Transform transform = null;
                foreach(var component in go.components)
                {
                    if(component is Transform)
                    {
                        transform = (Transform)component;
                        //Console.WriteLine(transform.position);
                    }
                    if (component is Rigidbody)
                    {
                        var body = (Rigidbody)component;
                        //Console.WriteLine(body.jitterBody.Position);

                        transform.position = new Vector3(body.jitterBody.Position.X, body.jitterBody.Position.Y, body.jitterBody.Position.Z);
                    }
                    if (component is MeshRenderer)
                    {
                        var verts = ((MeshRenderer)component).vertices;
                        for (int i = 0; i < verts.Length; i++)
                        {
                            var cachedPos = verts[i].Position;
                            var cachedColor = verts[i].Color;

                            //verts[i] = new VertexPositionColor(cachedPos + transform.position, cachedColor);
                            //Console.WriteLine(verts[i].Position);
                            //Console.WriteLine("Batch " + i);
                            batchedVerts.Add(new VertexPositionColor(cachedPos + transform.position, cachedColor));
                        }
                    }
                }
            }
            var vertices = batchedVerts.ToArray();
            if (lastVertexes != null)
            {
                InitializeTriangle(vertices);
                lastVertexes = vertices;
            }
            else
            {
                lastVertexes = vertices;
            }

            // Set render targets
            d3dDeviceContext.OutputMerger.SetRenderTargets(renderTargetView);

            // Clear the screen
            d3dDeviceContext.ClearRenderTargetView(renderTargetView, new SharpDX.Color(32, 103, 178));

            // Set vertex buffer
            d3dDeviceContext.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(triangleVertexBuffer, Utilities.SizeOf<VertexPositionColor>(), 0));

			// Draw the triangle
			d3dDeviceContext.Draw(vertices.Count(), 0);

			// Swap front and back buffer
			swapChain.Present(1, PresentFlags.None);
		}

		public void Dispose()
		{
			inputLayout.Dispose();
			inputSignature.Dispose();
			triangleVertexBuffer.Dispose();
			vertexShader.Dispose();
			pixelShader.Dispose();
			renderTargetView.Dispose();
			swapChain.Dispose();
			d3dDevice.Dispose();
			d3dDeviceContext.Dispose();
			renderForm.Dispose();
		}
	}
}
