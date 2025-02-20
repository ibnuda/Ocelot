﻿using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Multiplexer;

namespace Ocelot.UnitTests.Multiplexing
{
    public class MultiplexingMiddlewareTests
    {
        private readonly MultiplexingMiddleware _middleware;
        private Ocelot.DownstreamRouteFinder.DownstreamRouteHolder _downstreamRoute;
        private int _count;
        private readonly Mock<IResponseAggregator> _aggregator;
        private readonly Mock<IResponseAggregatorFactory> _factory;
        private readonly HttpContext _httpContext;
        private readonly RequestDelegate _next;
        private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
        private readonly Mock<IOcelotLogger> _logger;

        public MultiplexingMiddlewareTests()
        {
            _httpContext = new DefaultHttpContext();
            _factory = new Mock<IResponseAggregatorFactory>();
            _aggregator = new Mock<IResponseAggregator>();
            _factory.Setup(x => x.Get(It.IsAny<Route>())).Returns(_aggregator.Object);
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory.Setup(x => x.CreateLogger<MultiplexingMiddleware>()).Returns(_logger.Object);
            _next = context => Task.FromResult(_count++);
            _middleware = new MultiplexingMiddleware(_next, _loggerFactory.Object, _factory.Object);
        }

        [Fact]
        public void should_multiplex()
        {
            var route = new RouteBuilder().WithDownstreamRoute(new DownstreamRouteBuilder().Build()).WithDownstreamRoute(new DownstreamRouteBuilder().Build()).Build();

            this.Given(x => GivenTheFollowing(route))
                .When(x => WhenIMultiplex())
                .Then(x => ThePipelineIsCalled(2))
                .BDDfy();
        }

        [Fact]
        public void should_not_multiplex()
        {
            var route = new RouteBuilder().WithDownstreamRoute(new DownstreamRouteBuilder().Build()).Build();

            this.Given(x => GivenTheFollowing(route))
                .When(x => WhenIMultiplex())
                .Then(x => ThePipelineIsCalled(1))
                .BDDfy();
        }

        private void GivenTheFollowing(Route route)
        {
            _downstreamRoute = new Ocelot.DownstreamRouteFinder.DownstreamRouteHolder(new List<PlaceholderNameAndValue>(), route);
            _httpContext.Items.UpsertDownstreamRoute(_downstreamRoute);
        }

        private void WhenIMultiplex()
        {
            _middleware.Invoke(_httpContext).GetAwaiter().GetResult();
        }

        private void ThePipelineIsCalled(int expected)
        {
            _count.ShouldBe(expected);
        }
    }
}
