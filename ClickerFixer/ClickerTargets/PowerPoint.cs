using System;
using System.Linq;
using PPt = NetOffice.PowerPointApi;
using NetOffice.PowerPointApi;

namespace ClickerFixer.ClickerTargets
{
    class PowerPoint : IClickerTarget
    {
        // NOTE: Wrap most things in try-catch, as the Office COM API can throw exceptions rather unexpectedly
        bool IClickerTarget.IsActive()
        {
            try
            {
                PPt.Application pptApplication = PPt.Application.GetActiveInstance();

                if (pptApplication == null)
                {
                    return false;
                }

                // Check if there is an active slide show
                return (pptApplication.SlideShowWindows.Count > 0);
            }
            catch (Exception)
            {

            }

            return false;
        }

        void IDisposable.Dispose()
        {
        }

        void IClickerTarget.SendNext()
        {
            try
            {
                SlideShowWindow w = GetActiveSlideShowWindow();

                // Prevent click-through of "End of slide show, click to exit." placeholder screen
                if (w != null && w.View.State != PPt.Enums.PpSlideShowState.ppSlideShowDone)
                {
                    w.View.Next();
                }
            }
            catch (Exception)
            {

            }
        }

        void IClickerTarget.SendPrevious()
        {
            try
            {
                SlideShowWindow w = GetActiveSlideShowWindow();

                if (w != null)
                {
                    w.View.Previous();                   
                }
            }
            catch (Exception)
            {

            }
        }

        #region Helpers

        /// <summary>
        /// Get the active PowerPoint slide show window object
        /// 
        /// If there are multiple active slide shows, only the first slide show window object will be returned
        /// </summary>
        /// <returns>SlideShowWindow, or null if none</returns>
        private static SlideShowWindow GetActiveSlideShowWindow()
        {
            try
            {
                PPt.Application pptApplication = PPt.Application.GetActiveInstance();

                if (pptApplication == null)
                {
                    return null;
                }

                SlideShowWindow w = (SlideShowWindow)pptApplication.SlideShowWindows.FirstOrDefault();

                return w;
            }
            catch (Exception)
            {
                return null;
            }
        }
        #endregion Helpers
    }
}

