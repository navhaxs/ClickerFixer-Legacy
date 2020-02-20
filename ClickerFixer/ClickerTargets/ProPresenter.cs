using ClickerFixer.Interop;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using WebSocketSharp;

namespace ClickerFixer.ClickerTargets
{
    class ProPresenter : IClickerTarget, IDisposable
    {

        public ProPresenter()
        {
            _worker = new Thread(WebsocketWorkerLoop);
            _worker.Start();
        }

        void IDisposable.Dispose()
        {
            _cancelSource.Cancel();        // Stop Websocket loop

            SendToWebsockets(null);      // Signal the consumer to exit.
            _worker.Join();         // Wait for the consumer's thread to finish.
            _wh.Close();            // Release any OS resources.
        }

        const string MAINOUTPUTWINDOW_CLASSNAME = "ssDVIOutput0";
        const string PROCESSNAME = "propresenter";

        bool IClickerTarget.IsActive()
        {
            // Search for the main ProPresenter output window, and check if its window is "visible"

            Process[] p_list = Process.GetProcessesByName(PROCESSNAME);
            foreach (Process p in p_list)
            {
                var x = GetRootWindowsOfProcess(p.Id);

                foreach (IntPtr hWnd in x)
                {
                    int nRet;
                    // Pre-allocate 256 characters, since this is the maximum class name length.
                    StringBuilder ClassName = new StringBuilder(256);

                    // Get the window class name
                    nRet = WindowsInterop.User32.GetClassName(hWnd, ClassName, ClassName.Capacity);

                    if (nRet != 0 && ClassName.ToString().Equals(MAINOUTPUTWINDOW_CLASSNAME))
                    {
                        return WindowsInterop.User32.IsWindowVisible(hWnd);
                    }
                }
            }

            return false;
        }

        void IClickerTarget.SendNext()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>
            {
                { "action", "presentationTriggerNext"}
            };

            string msg = JsonConvert.SerializeObject(obj);
            {
                SendToWebsockets(msg);
            }
        }

        void IClickerTarget.SendPrevious()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>
            {
                { "action", "presentationTriggerPrevious"}
            };

            string msg = JsonConvert.SerializeObject(obj);
            {
                SendToWebsockets(msg);
            }
        }


        #region Output window detection helpers

        static List<IntPtr> GetRootWindowsOfProcess(int pid)
        {
            List<IntPtr> rootWindows = GetChildWindows(IntPtr.Zero);
            List<IntPtr> dsProcRootWindows = new List<IntPtr>();
            foreach (IntPtr hWnd in rootWindows)
            {
                uint lpdwProcessId;
                WindowsInterop.User32.GetWindowThreadProcessId(hWnd, out lpdwProcessId);
                if (lpdwProcessId == pid)
                    dsProcRootWindows.Add(hWnd);
            }
            return dsProcRootWindows;
        }

        public static List<IntPtr> GetChildWindows(IntPtr parent)
        {
            List<IntPtr> result = new List<IntPtr>();
            GCHandle listHandle = GCHandle.Alloc(result);
            try
            {
                WindowsInterop.Win32Callback childProc = new WindowsInterop.Win32Callback(EnumWindow);
                WindowsInterop.User32.EnumChildWindows(parent, childProc, GCHandle.ToIntPtr(listHandle));
            }
            finally
            {
                if (listHandle.IsAllocated)
                    listHandle.Free();
            }
            return result;
        }

        private static bool EnumWindow(IntPtr handle, IntPtr pointer)
        {
            GCHandle gch = GCHandle.FromIntPtr(pointer);
            List<IntPtr> list = gch.Target as List<IntPtr>;
            if (list == null)
            {
                throw new InvalidCastException("GCHandle Target could not be cast as List<IntPtr>");
            }
            list.Add(handle);
            return true;
        }

        #endregion

        #region Worker Thread (WebSocket)
        public void SendToWebsockets(string task)
        {
            lock (_locker) _tasks.Enqueue(task);
            _wh.Set();
        }

        EventWaitHandle _wh = new AutoResetEvent(false);
        Thread _worker;
        readonly object _locker = new object();
        Queue<string> _tasks = new Queue<string>();
        CancellationTokenSource _cancelSource = new CancellationTokenSource();

        void WebsocketWorkerLoop()
        {
            WebSocket ws = null;

            CancellationToken token = _cancelSource.Token;

            while (!token.IsCancellationRequested) {

                // Get next task for this worker loop
                string task = null;
                lock (_locker)
                {
                    if (_tasks.Count > 0)
                    {
                        task = _tasks.Dequeue();
                        if (task == null) return;
                    }
                }
                    
                if (task != null)
                {
                    MaybeConnect(ws);
                    ws.Send(task);
                }
                else
                {
                    // No more tasks - wait for a signal
                    _wh.WaitOne();
                }
            }
        }

        private void MaybeConnect(WebSocket ws)
        {
            // if the Websocket is closed, try connecting
            if (ws == null || !ws.IsAlive) {
                if (ws != null) {
                    ws.Close();
                }

                ws = new WebSocket($"ws://localhost:{AppConfig.port}/remote");
                ws.Connect();

                // application level authentication to ProPresnter, using the password.
                Dictionary<string, object> obj = new Dictionary<string, object>
                        {
                            { "action", "authenticate" },
                            { "protocol", "600" },
                            { "password", AppConfig.password }
                        };

                string json = JsonConvert.SerializeObject(obj);
                {
                    ws.Send(json);
                }
            }

        }
        #endregion
    }
}
