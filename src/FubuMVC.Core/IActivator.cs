using FubuMVC.Core.Diagnostics.Packaging;

namespace FubuMVC.Core
{
    /// <summary>
    /// Returned from a bootstrapper.
    /// It's activate method is called, and it is given access to all 
    /// of the packages, and a log object so that it log into the 
    /// central stream.
    /// 
    /// This can be used to load a database with data, install things,
    /// warm a cache up, etc.
    /// </summary>
    public interface IActivator
    {
        void Activate(IActivationLog log, IPerfTimer timer);
    }
}