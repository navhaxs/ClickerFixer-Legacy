using ClickerFixer.ClickerTargets;
using ClickerFixer.Interception;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace ClickerFixer.Services
{
    class ClickerInputListener: IDisposable {

        // Interception driver context
        private IntPtr _inteceptionContext = IntPtr.Zero;

        /// <summary>
        /// Determines whether the driver traps no keyboard events, all events, or a range of events in-between (down only, up only...etc). Set this before loading otherwise the driver will not filter any events and no keypresses can be sent.
        /// </summary>
        public static KeyboardFilterMode KeyboardFilterMode { get; set; } = KeyboardFilterMode.All;

        // Which device the Interception driver sends events to
        private static int deviceId;

        // Initialized 'clicker target' objects
        private List<IClickerTarget> _targets = new List<IClickerTarget>();

        // Worker thread
        private Thread _InterceptionListenerThread = null;

        // Worker thread cancel token
        private CancellationTokenSource _cancelSource = new CancellationTokenSource();
        
        // Event handler delegate
        public delegate void KeyPressedEventHandler(object sender, KeyPressedHandledEventArgs args);

        // Event handler
        public event KeyPressedEventHandler KeyPressed;

        private const string DEVICES_LIST_FILE = @"devices.txt";

        public ClickerInputListener()
        {
            _InterceptionListenerThread = new Thread(new ThreadStart(ClickerInputEventLoop));
            _InterceptionListenerThread.Priority = ThreadPriority.Highest;
            _InterceptionListenerThread.IsBackground = true;
            _InterceptionListenerThread.Start();
        }

        protected virtual void OnKeyPressed(KeyPressedHandledEventArgs e)
        {
            KeyPressedEventHandler handler = KeyPressed;
            handler?.Invoke(this, e);
        }

        public void Abort()
        {
            _cancelSource.Cancel();
            _InterceptionListenerThread.Abort();
            if (_inteceptionContext != IntPtr.Zero)
            {
                InterceptionDriver.DestroyContext(_inteceptionContext);
            }
            if (_targets != null)
            {
                foreach (IClickerTarget plugin in _targets)
                {
                    plugin.Dispose();
                }
            }
        }

        private void ClickerInputEventLoop()
        {
            // load the list of device strings to test
            if (!File.Exists(DEVICES_LIST_FILE)) {
                // write defaults
                string[] createText = { "HID\\VID_046D&PID_C540" };
                File.WriteAllLines(DEVICES_LIST_FILE, createText);
            }

            string[] deviceStringListToTest = File.ReadAllLines(DEVICES_LIST_FILE);

            // initialize all available 'clicker tagets'
            Type[] typelist = GetClassesInNamespace(Assembly.GetExecutingAssembly(), "ClickerFixer.ClickerTargets");
            for (int i = 0; i < typelist.Length; i++) {
                _targets.Add((IClickerTarget) Activator.CreateInstance(typelist[i]));
            }
            
            // initialize Interception driver
            try
            {
                _inteceptionContext = InterceptionDriver.CreateContext();
            }
            catch (DllNotFoundException e)
            {
                MessageBox.Show($"Failed to load Interception. Please copy interception.dll into {System.Environment.CurrentDirectory}\n\n" + e.Message);
                Application.Exit();
                return;
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to load Interception (interception.dll). Ensure driver installed, and you rebooted after installation.\n" + e.Message);
                Application.Exit();
                return;
            }
            
            if (_inteceptionContext == IntPtr.Zero)
            {
                MessageBox.Show("Failed to load Interception (interception.dll). Ensure driver installed, and you rebooted after installation.");
                Application.Exit();
                return;
            }

            InterceptionDriver.SetFilter(_inteceptionContext, InterceptionDriver.IsKeyboard, (Int32)KeyboardFilterMode);

            Stroke stroke = new Stroke();

            CancellationToken token = _cancelSource.Token;
            while (!token.IsCancellationRequested && InterceptionDriver.Receive(_inteceptionContext, deviceId = InterceptionDriver.Wait(_inteceptionContext), ref stroke, 1) > 0)
            {
                bool isHandled = false;
                string handledByClientName = null;

                if (InterceptionDriver.IsKeyboard(deviceId) > 0 && stroke.Key.State == KeyState.E0)
                {
                    Console.WriteLine($"{InterceptionDriver.GetHardwareStr(_inteceptionContext, deviceId)} Code={stroke.Key.Code} State={stroke.Key.State}");
                    var deviceHwStr = InterceptionDriver.GetHardwareStr(_inteceptionContext, deviceId);

                    // only handle if string test passes:
                    if (Array.Find(deviceStringListToTest, p => deviceHwStr.StartsWith(p)) != null)
                    {

                        foreach (IClickerTarget client in _targets)
                        {
                            if (!client.IsActive())
                            {
                                continue;
                            }
                            else
                            {
                                handledByClientName = client.GetType().Name;

                                System.Diagnostics.Debug.WriteLine($"{handledByClientName} {stroke.Key.Code}");

                                if (stroke.Key.Code == Interception.Keys.Right)
                                {
                                    System.Diagnostics.Debug.WriteLine("Right");
                                    client.SendNext();
                                }
                                else if (stroke.Key.Code == Interception.Keys.Left)
                                {
                                    System.Diagnostics.Debug.WriteLine("Left");
                                    client.SendPrevious();
                                }

                                isHandled = true;
                                break;
                            }
                        }

                        OnKeyPressed(new KeyPressedHandledEventArgs(handledByClientName));
                    }
                }

                // pass-through if unhandled
                // (or your normal PC keyboard won't work!)
                if (!isHandled) {
                    InterceptionDriver.Send(_inteceptionContext, deviceId, ref stroke, 1);
                }
            }

            Abort();
        }

        public void Dispose()
        {
            Abort();
        }
        private Type[] GetClassesInNamespace(Assembly assembly, string nameSpace)
        {
            return
              assembly.GetTypes()
                      .Where(t => !t.IsInterface && String.Equals(t.Namespace, nameSpace, StringComparison.Ordinal))
                      .ToArray();
        }

        public class KeyPressedHandledEventArgs : EventArgs
        {
            public KeyPressedHandledEventArgs(string arg)
            {
                this.HandledByTargetName = arg;
            }

            public string HandledByTargetName { get; }
        }

    }
}
