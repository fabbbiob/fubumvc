using System;
using System.Collections.Generic;
using FubuCore.Binding;
using FubuMVC.Core;
using FubuMVC.Core.Behaviors;
using FubuMVC.Core.Http;
using FubuMVC.Core.Registration.Nodes;
using FubuMVC.Core.Registration.Routes;
using FubuMVC.Core.Runtime;
using NUnit.Framework;
using Rhino.Mocks;
using Shouldly;
using StructureMap.Pipeline;

namespace FubuMVC.Tests.Runtime
{
    [TestFixture]
    public class BehaviorInvokerTester
    {
        private IServiceFactory theFactory;
        private BehaviorChain theChain;
        private BehaviorInvoker theInvoker;
        private IActionBehavior theBehavior;
        private TypeArguments theArguments;
        private Dictionary<string, object> theRouteData;

        [SetUp]
        public void SetUp()
        {
            theFactory = MockRepository.GenerateMock<IServiceFactory>();
            theChain = new RoutedChain("something");

            theRouteData = new Dictionary<string, object>();

            theArguments = new TypeArguments();
            theBehavior = MockRepository.GenerateMock<IActionBehavior>();

            theFactory.Stub(x => x.BuildBehavior(theArguments, theChain.UniqueId))
                .Return(theBehavior);

            theInvoker = new BehaviorInvoker(theFactory, theChain);
        }

        [Test]
        public void invoke_happy_path_calls_the_behavior()
        {
            theInvoker.Invoke(theArguments, theRouteData);
            theBehavior.AssertWasCalled(x => x.Invoke());
        }

        [Test]
        public void invoke_happy_path_will_put_the_current_chain_together()
        {
            theInvoker.Invoke(theArguments, theRouteData);
            theArguments.Get<ICurrentChain>().ShouldBeOfType<CurrentChain>()
                .ResourceHash().ShouldBe(new CurrentChain(theChain, theRouteData).ResourceHash());
                
        }

        [Test]
        public void invoke_happy_path_with_a_continuation_filter()
        {
            theChain.AddFilter(new StubBehaviorInvocationFilter(DoNext.Continue));

            theInvoker.Invoke(theArguments, theRouteData);
            theBehavior.AssertWasCalled(x => x.Invoke());
        }

        [Test]
        public void invoke_happy_path_with_multiple_continuation_filter()
        {
            theChain.AddFilter(new StubBehaviorInvocationFilter(DoNext.Continue));
            theChain.AddFilter(new StubBehaviorInvocationFilter(DoNext.Continue));
            theChain.AddFilter(new StubBehaviorInvocationFilter(DoNext.Continue));

            theInvoker.Invoke(theArguments, theRouteData);
            theBehavior.AssertWasCalled(x => x.Invoke());
        }

        [Test]
        public void invoke_with_a_filter_that_says_stop()
        {
            theChain.AddFilter(new StubBehaviorInvocationFilter(DoNext.Stop));

            theInvoker.Invoke(theArguments, theRouteData);
            theBehavior.AssertWasNotCalled(x => x.Invoke());
        }

        [Test]
        public void invoke_with_a_filter_that_says_stop_2()
        {
            theChain.AddFilter(new StubBehaviorInvocationFilter(DoNext.Continue));
            theChain.AddFilter(new StubBehaviorInvocationFilter(DoNext.Continue));
            theChain.AddFilter(new StubBehaviorInvocationFilter(DoNext.Stop));

            theInvoker.Invoke(theArguments, theRouteData);
            theBehavior.AssertWasNotCalled(x => x.Invoke());
        }
    }

    public class StubBehaviorInvocationFilter : IBehaviorInvocationFilter
    {
        private readonly DoNext _returnValue;

        public StubBehaviorInvocationFilter(DoNext returnValue)
        {
            _returnValue = returnValue;
        }

        public DoNext Filter(TypeArguments arguments)
        {
            return _returnValue;
        }
    }
}