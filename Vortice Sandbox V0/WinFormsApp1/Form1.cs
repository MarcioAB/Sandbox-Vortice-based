using System.ComponentModel;
using System.Numerics;

using D3D_MyModels;
using D3D_Mama;

#if D3D12
using static D3D12_Mama.D3D12_Base;
#elif D3D11
using static D3D11_Mama.D3D11_Base;
#endif

#nullable disable

// Vide <DefineConstants> in Directory.Buildprops to define D3D12 or D3D11

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        // SandBox BASIC introduction
        //public MyModel_Nothing model; // No geometry, just the basic clean before each frame.
        //public MyModel_L0A model; // L0: NON-INDEXED introduction (line)
        //public MyModel_L0B model; // L0: NON-INDEXED introduction: 2 drawsets (line)
        //public MyModel_L1 model; // L1: INDEXED introduction (line)
        //public MyModel_L2 model; // L2: MVP introduction (line)
        //public MyModel_L3 model; // L3: TEXTURE introduction (triangle)
        //public MyModel_L4 model; // L4: INSTANCES introduction
        //public MyModel_L5A model; // L5: MULTIDATA introduction (no instanced)
        public MyModel_L5B model; // L5: MULTIDATA introduction (instanced)

        bool maximizedState = false; // not the same as full screen
        bool continuousRender = false;
        long t1;
        int frames_counter = 0;
        System.Threading.Timer timer2;

        public Form1() => InitializeComponent();

        protected override void OnLoad(EventArgs e)
        {
            D3DSetup(Handle, qtdBackBuffers: 4); // using defaults Level_12_0 and 4 back buffers
            model = new();
            UserRender += model.DrawAll;
            Render(); // render 1 frame para deixar de background, uma vez que inicia sem "continuousRender"
            Text = "Use button to turn render ON";
            timer2 = new(Callback1, null, Timeout.Infinite, Timeout.Infinite);
            base.OnLoad(e);
        }

        // to handle just the end of a window resize action
        protected override void OnResizeEnd(EventArgs e)
        {
            D3D_Resize();
            Camera1.UpdateProjection();
            if (!continuousRender) Render(); // render 1 frame para deixar de background
            base.OnResizeEnd(e);
        }

        // to handle maximized window (maximized and not fullscreen window)
        protected override void OnSizeChanged(EventArgs e)
        {
            if(WindowState == FormWindowState.Maximized)
            {
                OnResizeEnd(EventArgs.Empty);
                maximizedState = true;
            }
            else
            {
                if (maximizedState)
                {
                    OnResizeEnd(EventArgs.Empty);
                    maximizedState = false;
                }
            }
            base.OnSizeChanged(e);
        }

        public void RunRender()
        {
            Focus(); // alguma coisa, em algum lugar tira o foco do Form.
            while (continuousRender)
            {
                frames_counter++;
                Render();
                Application.DoEvents();
            }
        }

        protected override void DestroyHandle()
        {
            throw new Exception();
            //base.DestroyHandle();
            //Environment.Exit(0);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            //origem = e.Location;
            base.OnMouseDown(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            var k = 1;
            if (e.Delta < 0) k = -1;
            Camera1.MovePosition(k);
            Camera1.UpdateView();
            if (!continuousRender) Render(); // render 1 frame para deixar de background
            base.OnMouseWheel(e);
        }

        public override string Text { get => base.Text; set => base.Text = value; }

        Point origem;
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var p1 = e.Location.X - origem.X;
                var p2 = e.Location.Y - origem.Y;

                if (p1 > 0) Camera1.MoveRotation(-Vector3.UnitY);
                else if (p1 < 0) Camera1.MoveRotation(Vector3.UnitY);

                if (p2 > 0) Camera1.MoveRotation(-Vector3.UnitX);
                else if (p2 < 0) Camera1.MoveRotation(Vector3.UnitX);

                Camera1.UpdateView();
                if (!continuousRender) Render(); // render 1 frame para deixar de background
                origem = e.Location;
            }
            base.OnMouseMove(e);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            continuousRender = !continuousRender;
            if (continuousRender)
            {
                timer2?.Change(1000, 1000);
                Text = "...";
                t1 = Environment.TickCount64;
                frames_counter = 0;
                RunRender();
            }
            else
            {
                timer2?.Change(Timeout.Infinite, Timeout.Infinite);
                Text = "Use button to turn render ON";
            }
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            model.Dispose();
            D3D_Dispose();
            Environment.Exit(0);
        }

        private void Callback1(object state)
        {
            var t2 = Environment.TickCount64;
            var fps = 1000f / (t2 - t1) * frames_counter;
            var n1 = 1000000f / fps;
            Title( $"{fps:F2} fps {n1:F2} µs  {AuxFunctions.waitCount}");
            frames_counter = 0;
            t1 = t2;
        }
        public void Title(string text) => Invoke(delegate { Text = text; });

    }
}