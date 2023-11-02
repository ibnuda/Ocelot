﻿using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration
{
    public class DynamicsCreatorTests : UnitTest
    {
        private readonly DynamicsCreator _creator;
        private readonly Mock<IRateLimitOptionsCreator> _rloCreator;
        private readonly Mock<IVersionCreator> _versionCreator;
        private readonly Mock<IVersionPolicyCreator> _versionPolicyCreator;
        private List<Route> _result;
        private FileConfiguration _fileConfig;
        private RateLimitOptions _rlo1;
        private RateLimitOptions _rlo2;
        private Version _version;
        private HttpVersionPolicy _versionPolicy;

        public DynamicsCreatorTests()
        {
            _versionCreator = new Mock<IVersionCreator>();
            _versionPolicyCreator = new Mock<IVersionPolicyCreator>();
            _rloCreator = new Mock<IRateLimitOptionsCreator>();
            _creator = new DynamicsCreator(_rloCreator.Object, _versionCreator.Object, _versionPolicyCreator.Object);
        }

        [Fact]
        public void should_return_nothing()
        {
            var fileConfig = new FileConfiguration();

            this.Given(_ => GivenThe(fileConfig))
                .When(_ => WhenICreate())
                .Then(_ => ThenNothingIsReturned())
                .And(_ => ThenTheRloCreatorIsNotCalled())
                .BDDfy();
        }

        [Fact]
        public void should_return_re_routes()
        {
            var fileConfig = new FileConfiguration
            {
                DynamicRoutes = new List<FileDynamicRoute>
                {
                    new()
                    {
                        ServiceName = "1",
                        RateLimitRule = new FileRateLimitRule
                        {
                            EnableRateLimiting = false,
                        },
                        DownstreamHttpVersion = "1.1",
                        DownstreamVersionPolicy = VersionPolicies.Downgradable,
                    },
                    new()
                    {
                        ServiceName = "2",
                        RateLimitRule = new FileRateLimitRule
                        {
                            EnableRateLimiting = true,
                        },
                        DownstreamHttpVersion = "2.0",
                        DownstreamVersionPolicy = VersionPolicies.Upgradeable,
                    },
                },
            };

            this.Given(_ => GivenThe(fileConfig))
                .And(_ => GivenTheRloCreatorReturns())
                .And(_ => GivenTheVersionCreatorReturns())
                .And(_ => GivenTheVersionPolicyCreatorReturns())
                .When(_ => WhenICreate())
                .Then(_ => ThenTheRoutesAreReturned())
                .And(_ => ThenTheRloCreatorIsCalledCorrectly())
                .And(_ => ThenTheVersionCreatorIsCalledCorrectly())
                .BDDfy();
        }

        private void ThenTheRloCreatorIsCalledCorrectly()
        {
            _rloCreator.Verify(x => x.Create(_fileConfig.DynamicRoutes[0].RateLimitRule,
                _fileConfig.GlobalConfiguration), Times.Once);

            _rloCreator.Verify(x => x.Create(_fileConfig.DynamicRoutes[1].RateLimitRule,
                _fileConfig.GlobalConfiguration), Times.Once);
        }

        private void ThenTheVersionCreatorIsCalledCorrectly()
        {
            _versionCreator.Verify(x => x.Create(_fileConfig.DynamicRoutes[0].DownstreamHttpVersion), Times.Once);
            _versionCreator.Verify(x => x.Create(_fileConfig.DynamicRoutes[1].DownstreamHttpVersion), Times.Once);

            _versionPolicyCreator.Verify(x => x.Create(_fileConfig.DynamicRoutes[0].DownstreamVersionPolicy), Times.Once);
            _versionPolicyCreator.Verify(x => x.Create(_fileConfig.DynamicRoutes[1].DownstreamVersionPolicy), Times.Once);
        }

        private void ThenTheRoutesAreReturned()
        {
            _result.Count.ShouldBe(2);
            _result[0].DownstreamRoute[0].EnableEndpointEndpointRateLimiting.ShouldBeFalse();
            _result[0].DownstreamRoute[0].RateLimitOptions.ShouldBe(_rlo1);
            _result[0].DownstreamRoute[0].DownstreamHttpVersion.ShouldBe(_version);
            _result[0].DownstreamRoute[0].DownstreamVersionPolicy.ShouldBe(_versionPolicy);
            _result[0].DownstreamRoute[0].ServiceName.ShouldBe(_fileConfig.DynamicRoutes[0].ServiceName);

            _result[1].DownstreamRoute[0].EnableEndpointEndpointRateLimiting.ShouldBeTrue();
            _result[1].DownstreamRoute[0].RateLimitOptions.ShouldBe(_rlo2);
            _result[1].DownstreamRoute[0].DownstreamHttpVersion.ShouldBe(_version);
            _result[1].DownstreamRoute[0].DownstreamVersionPolicy.ShouldBe(_versionPolicy);
            _result[1].DownstreamRoute[0].ServiceName.ShouldBe(_fileConfig.DynamicRoutes[1].ServiceName);
        }

        private void GivenTheVersionCreatorReturns()
        {
            _version = new Version("1.1");
            _versionCreator.Setup(x => x.Create(It.IsAny<string>())).Returns(_version);
        }

        private void GivenTheVersionPolicyCreatorReturns()
        {
            _versionPolicy = HttpVersionPolicy.RequestVersionOrLower;
            _versionPolicyCreator.Setup(x => x.Create(It.IsAny<string>())).Returns(_versionPolicy);
        }

        private void GivenTheRloCreatorReturns()
        {
            _rlo1 = new RateLimitOptionsBuilder().Build();
            _rlo2 = new RateLimitOptionsBuilder().WithEnableRateLimiting(true).Build();

            _rloCreator
                .SetupSequence(x => x.Create(It.IsAny<FileRateLimitRule>(), It.IsAny<FileGlobalConfiguration>()))
                .Returns(_rlo1)
                .Returns(_rlo2);
        }

        private void ThenTheRloCreatorIsNotCalled()
        {
            _rloCreator.Verify(x => x.Create(It.IsAny<FileRateLimitRule>(), It.IsAny<FileGlobalConfiguration>()), Times.Never);
        }

        private void ThenNothingIsReturned()
        {
            _result.Count.ShouldBe(0);
        }

        private void WhenICreate()
        {
            _result = _creator.Create(_fileConfig);
        }

        private void GivenThe(FileConfiguration fileConfig)
        {
            _fileConfig = fileConfig;
        }
    }
}
