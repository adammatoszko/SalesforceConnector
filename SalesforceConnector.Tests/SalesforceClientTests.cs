using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using SalesforceConnector.Client;
using SalesforceConnector.Enums;
using SalesforceConnector.Models;
using SalesforceConnector.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SalesforceConnector.Tests
{
    [TestFixture]
    public class SalesforceClientTests
    {
        private SalesforceClient _testedService;
        private IHttpMessageService _messageServiceSub;
        private IOptions<SalesforceConnectorOptions> _optionsSub;
        private SalesforceConnectorOptions _optionsConcreteSub;

        [SetUp]
        public void Setup()
        {
            _optionsConcreteSub = Substitute.For<SalesforceConnectorOptions>();
            _messageServiceSub = Substitute.For<IHttpMessageService>();
            _optionsSub = Substitute.For<IOptions<SalesforceConnectorOptions>>();
            _optionsSub.Value.Returns(_optionsConcreteSub);
            _testedService = new SalesforceClient(_messageServiceSub, _optionsSub);
        }

        public void SetupClient(string content, HttpStatusCode code)
        {
            List<FieldInfo> fields = _testedService.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).ToList();
            FieldInfo client = fields.Find(x => x.Name == "_client");
            HttpResponseMessage msg = new HttpResponseMessage()
            {
                Content = new StringContent(content),
                StatusCode = code
            };
            FakeHandler h = new FakeHandler(msg);
            HttpClient c = new HttpClient(h);
            client.SetValue(_testedService, c);
        }

        [Test]
        public void LogOutAsync_ThrowsWhenStatusCodeNotSuccessful()
        {
            //arrange
            SetupClient("error", HttpStatusCode.BadRequest);
            HttpRequestMessage logoutMsg = new HttpRequestMessage(HttpMethod.Get, "https://someuri");
            _messageServiceSub.BuildLogoutMessage().Returns(logoutMsg);

            //act
            //assert
            Assert.ThrowsAsync(typeof(HttpRequestException), async () =>
            {
                await _testedService.LogOutAsync();
            });
        }

        public static TestSfObject[][] ModifyDataAsyncData = new TestSfObject[][]
        {
            null,
            new TestSfObject[0]
        };

        [TestCaseSource("ModifyDataAsyncData")]
        public async Task ModifyDataAsync_ReturnsEmptyList_WhenEmptyOrNullArrayIsGiven(TestSfObject[] objs)
        {
            //arrange
            //act
            List<DataModificationResultModel> result = await _testedService.ModifyDataAsync(objs, DataModificationType.Update, true);

            //assert
            CollectionAssert.IsEmpty(result);
        }

        [Test]
        public async Task ModifyDataAsync_SplitsLargeArraysCorrectly()
        {
            //arrange
            SetupClient(string.Empty, HttpStatusCode.OK);
            _messageServiceSub.BuildDataChangeMessageAsync<TestSfObject>(Arg.Any<TestSfObject[]>(), Arg.Any<HttpMethod>(), Arg.Any<bool>()).Returns(_ =>
            {
                return new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    Content = new StringContent(string.Empty),
                    RequestUri = new Uri("https://someuri")
                };
            });
            var objs = new TestSfObject[201];

            //act
            var result = await _testedService.ModifyDataAsync<TestSfObject>(objs, DataModificationType.Update, true);

            //assert
            var calls = _messageServiceSub.ReceivedCalls().ToList();
            var firstCall = calls[0].GetArguments()[0] as TestSfObject[];
            var secondCall = calls[2].GetArguments()[0] as TestSfObject[];
            Assert.Multiple(() =>
            {
                Assert.AreEqual(200, firstCall.Length);
                Assert.AreEqual(1, secondCall.Length);
            });
        }


        public static object[] QueryDataAsyncData = new object[]
        {
            new object[]
            {
                true,
                2
            },
            new object[]
            {
                false,
                4
            }
        };

        [TestCaseSource("QueryDataAsyncData")]
        public async Task QueryDataAsync_CreatesResultsCorrectly(bool isDone, int count)
        {
            //arrange
            int iteration = 0;
            _messageServiceSub.BuildQueryMessage(Arg.Any<string>(), Arg.Any<bool>()).Returns(_ =>
            {
                return new HttpRequestMessage(HttpMethod.Get, "https://someuri");
            });
            _messageServiceSub.ProcessResponseAsync<QueryResultModel<TestSfObject>>(Arg.Any<HttpResponseMessage>()).Returns(x =>
            {
                if (isDone)
                {
                    return new QueryResultModel<TestSfObject>()
                    {
                        Done = true,
                        NextRecords = null,
                        Records = new TestSfObject[] { new TestSfObject(), new TestSfObject() },
                        TotalSize = 2
                    };
                }
                else
                {
                    if (iteration == 0)
                    {
                        iteration++;
                        return new QueryResultModel<TestSfObject>()
                        {
                            Done = false,
                            NextRecords = null,
                            Records = new TestSfObject[] { new TestSfObject(), new TestSfObject() },
                            TotalSize = 4
                        };
                    }
                    else
                    {
                        return new QueryResultModel<TestSfObject>()
                        {
                            Done = true,
                            NextRecords = null,
                            Records = new TestSfObject[] { new TestSfObject(), new TestSfObject() },
                            TotalSize = 2
                        };
                    }
                }
            });
            SetupClient(string.Empty, HttpStatusCode.OK);

            //act
            var result = await _testedService.QueryDataAsync<TestSfObject>("");

            //assert
            Assert.AreEqual(count, result.Length);
        }
    }

    public class FakeHandler : HttpMessageHandler
    {
        private HttpResponseMessage _shouldReturn;

        public FakeHandler(HttpResponseMessage shouldReturn)
        {
            _shouldReturn = shouldReturn;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, CancellationToken token)
        {
            return Task.FromResult(_shouldReturn);
        }
    }
}
