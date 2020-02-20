using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows.Forms;
using System;

namespace ClickerFixer.Utils
{
    static class MutexUtil
    {
        public static void WrapSingleInstance(Action action)
        {
            // https://stackoverflow.com/a/229567/
            string appGuid =
                ((GuidAttribute)Assembly.GetExecutingAssembly().
                    GetCustomAttributes(typeof(GuidAttribute), false).
                        GetValue(0)).Value.ToString();

            // unique id for global mutex - Global prefix means it is global to the machine
            string mutexId = string.Format("Global\\{{{0}}}", appGuid);

            // Need a place to store a return value in Mutex() constructor call
            bool createdNew;

            var allowEveryoneRule =
                new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid
                                                           , null)
                                   , MutexRights.FullControl
                                   , AccessControlType.Allow
                                   );
            var securitySettings = new MutexSecurity();
            securitySettings.AddAccessRule(allowEveryoneRule);

            using (var mutex = new Mutex(false, mutexId, out createdNew, securitySettings))
            {
                var hasHandle = false;
                try
                {
                    try
                    {
                        hasHandle = mutex.WaitOne(0, false);
                        if (hasHandle == false)
                        {
                            MessageBox.Show("ClickerFixer is already running! Only a single instance can run.\nClickerFixer can be quit from the system tray icon.");
                            return;
                        }
                    }
                    catch (AbandonedMutexException)
                    {
                        hasHandle = true;
                    }

                    action();
                }
                finally
                {
                    if (hasHandle)
                        mutex.ReleaseMutex();
                }
            }
        }
    }
}
