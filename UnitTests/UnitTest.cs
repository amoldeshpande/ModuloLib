using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using ModuloLib;
using Windows.UI;

namespace UnitTests
{
    [TestClass]
    public class ModuloUnitTests
    {
        [TestMethod]
        public async Task TestSerialInit()
        {
            SerialConnection ser = new SerialConnection();
            bool ret = await ser.Initialize(null, 0);
            Assert.IsTrue(ret);
            ser.Close();
            ser.Dispose();
        }
        [TestMethod]
        public async Task DisplayEventsTutorial()
        {
            Port port = new Port();
            Assert.IsTrue(await port.Initialize(null));
            DisplayModulo display = new DisplayModulo();
            Action<Modulo, int> resetLambda = (dsplay, btn) =>
            {
                display.Clear();
                display.SetTextColor(1, 1, 1);
                display.Write("Press Button");
                display.Refresh();
            };
            Assert.IsTrue(display.Initialize(port, null,
                (dsply, btn) =>
                {
                    display.Clear();
                    switch (btn)
                    {
                        case 0:
                            display.SetTextColor(1, 0, 0);
                            break;
                        case 1:
                            display.SetTextColor(0, 1, 0);
                            break;
                        case 2:
                            display.SetTextColor(0, 0, 1);
                            display.ShouldExit = true;
                            break;
                        default:
                            break;
                    }
                    display.Write(" Button " + btn);
                    display.Refresh();
                },
                (dsplay, btn) =>
                {
                    resetLambda(display, btn);
                }
                ));

            resetLambda(display, 0);

            port.RunForever();
            display.Dispose();
            port.Close();
        }
        [TestMethod]
        public async Task KnobDisplayTutorial()
        {
            Port port = new Port();
            Assert.IsTrue(await port.Initialize(null));
            DisplayModulo display = new DisplayModulo();
            Assert.IsTrue(display.Initialize(port));
            Knob knob = new Knob();
            bool buttonRelease = false;
            bool posChange = false;

            Assert.IsTrue(knob.Initialize(port,
                    null,
                    null,
                    (knb) =>
                    {
                        buttonRelease = true;
                    },
                    (knb) =>
                    {
                        posChange = true;
                    }
                    ));
            while (port.Loop())
            {
                if (buttonRelease)
                {
                    display.Clear();
                    display.Write("knob done");
                    display.Refresh();
                    break;
                }
                if (posChange)
                {
                    int angle = knob.GetAngle();
                    display.Clear();
                    display.SetTextColor(1, 0, 0);
                    knob.SetColor(angle * 255 / 360.0f, angle * 255 / 360.0f, angle * 255 / 260.0f);
                    display.Write("knob angle " + angle);
                    display.Refresh();
                    posChange = false;
                }

            }
            display.Dispose();
            knob.Dispose();
            port.Close();
        }
        private void draw_crosshairs(DisplayModulo display, int x, int y, bool selected)
        {
            float[] CROSSHAIR_COLOR = { 0.4f, 0.4f, 0.4f, 1.0f };//     # Gray
            float[] DOT_COLOR_DESELECTED = { 0.4f, 0.4f, 0.4f, 1.0f };//  # Gray
            float[] DOT_COLOR_SELECTED = { 1, 0, 0, 1 };//          # Red
            float[] TEXT_COLOR = { 0, 0.7f, 0, 1 };//               # Green
            int DOT_SIZE_SELECTED = 8;
            int DOT_SIZE_DESELECTED = 4;
            float[] dot_color;
            int dot_size;
            //# Draw crosshairs
            display.Clear();
            display.SetLineColor(CROSSHAIR_COLOR);
            //# Horizontal crosshair
            display.DrawRect(0, (byte)y, display.Width, 1);
            // # Vertical crosshair
            display.DrawRect((byte)x, 0, 1, display.Height);
            // # Dot
            if (selected)
            {
                dot_color = DOT_COLOR_SELECTED;
                dot_size = DOT_SIZE_SELECTED;
            }
            else
            {
                dot_color = DOT_COLOR_DESELECTED;
                dot_size = DOT_SIZE_DESELECTED;
            }
            display.SetFillColor(dot_color);
            display.DrawCircle((sbyte)x, (sbyte)y, (byte)dot_size);
            //# Coordinate value display
            display.SetTextColor(TEXT_COLOR);
            display.SetCursor(0, 0);
            display.Write(String.Format("x={0,2}\ny={1,2}", x, y));
            // # Update the display!
            display.Refresh();
        }
        [TestMethod]
        public async Task JoyStickEventsDisplayTutorial()
        {
            bool shouldExit = false;
            bool joyStickChanged = true;
            Action<Modulo> changeLambda = (joy) => { joyStickChanged = true; };

            Port port = new Port();
            DisplayModulo display = new DisplayModulo();
            Joystick joystick = new Joystick();

            Assert.IsTrue(await port.Initialize(null));
            Assert.IsTrue(display.Initialize(port, null, (dsp, nt) => { shouldExit = true; }));
            Assert.IsTrue(joystick.Initialize(port, null, changeLambda, null, changeLambda));

            byte max_x = (byte)(display.Width - 1);
            byte max_y = (byte)(display.Height - 1);
            while (port.Loop())
            {
                if (joyStickChanged)
                {
                    joyStickChanged = false;
                    int screen_x = (int)(max_x / 2.0f - max_x / 2.0f * joystick.GetHPos());
                    int screen_y = (int)(max_y / 2.0f - max_y / 2.0f * joystick.GetVPos());
                    draw_crosshairs(display, screen_x, screen_y, joystick.GetButtonState());
                }
                if (shouldExit)
                {
                    break;
                }
            }
            display.Dispose();
            joystick.Dispose();
            port.Close();
        }
        [TestMethod]
        public async Task TemperatureProbEventsDisplayTutorial()
        {
            bool shouldExit = false;
            bool tempChanged = true;
            Action<Modulo> changeLambda = (joy) => { tempChanged = true; };

            Port port = new Port();
            DisplayModulo display = new DisplayModulo();
            TemperatureProbe tempProbe = new TemperatureProbe();

            Assert.IsTrue(await port.Initialize(null));
            Assert.IsTrue(display.Initialize(port, null, (dsp, nt) => { shouldExit = true; }));
            Assert.IsTrue(tempProbe.Initialize(port, null, changeLambda));

            display.Clear();
            while (port.Loop())
            {
                if (tempChanged)
                {
                    display.SetCursor(0, 0);
                    display.Write("Temp C " + tempProbe.GetTemperatureCelsius() 
                        + "\r\ndeg F " + tempProbe.GetTemperatureFahrenheit());
                    display.Refresh();
                }
                if (shouldExit)
                {
                    break;
                }
            }
            display.Dispose();
            tempProbe.Dispose();
            port.Close();
        }
        [TestMethod]
        public async Task IRRemoteEventsDisplayTutorial()
        {
            bool shouldExit = false;
            bool stringChanged = false;
            String codeString = String.Empty;
            Action<IRRemote, sbyte, int, ushort[], int> dataReceivedCallback
                    = (rem, protocol, value, extdata, exlen) =>
                    {
                        codeString = String.Format("Protocol {0:X} {1:X} ex len {2}", protocol, value, exlen);
                        System.Diagnostics.Debug.WriteLine("String " + codeString);
                        stringChanged = true;
                    };
            Port port = new Port();
            DisplayModulo display = new DisplayModulo();
            IRRemote irRemote = new IRRemote();
            Assert.IsTrue(await port.Initialize(null));
            Assert.IsTrue(display.Initialize(port, null, (dsp, nt) => { shouldExit = true; }));
            Assert.IsTrue(irRemote.Initialize(port, null, dataReceivedCallback));

            display.Clear();
            while (port.Loop())
            {
                if (stringChanged)
                {
                    display.SetCursor(0, 0);
                    display.Write(codeString);
                    display.Refresh();
                    stringChanged = false;
                }
                if (shouldExit)
                {
                    break;
                }
            }
            display.Dispose();
            irRemote.Dispose();
            port.Close();
        }
    }
}
