using FubuMVC.Core.Registration;
using FubuMVC.Core.UI.Elements;
using FubuMVC.Core.UI.Security;
using HtmlTags.Conventions;

namespace FubuMVC.Core.UI
{
    public class UIServiceRegistry : ServiceRegistry
    {
        public UIServiceRegistry()
        {
            SetServiceIfNone<IFieldAccessService, FieldAccessService>();
            SetServiceIfNone<IFieldAccessRightsExecutor, FieldAccessRightsExecutor>();

            SetServiceIfNone<IElementNamingConvention, DefaultElementNamingConvention>();

            SetServiceIfNone(typeof (IElementGenerator<>), typeof (ElementGenerator<>));

            SetServiceIfNone(typeof (ITagGenerator<>), typeof (TagGenerator<>));

            AddService<ITagRequestActivator, ElementRequestActivator>();
            AddService<ITagRequestActivator, ServiceLocatorTagRequestActivator>();

            SetServiceIfNone<ITagGeneratorFactory, TagGeneratorFactory>();

            SetServiceIfNone<ITagRequestBuilder, TagRequestBuilder>();
        }
    }
}