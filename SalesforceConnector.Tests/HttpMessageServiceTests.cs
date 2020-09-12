using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using SalesforceConnector.Models;
using SalesforceConnector.Services;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;

namespace SalesforceConnector.Tests
{
    [TestFixture]
    public class HttpMessageServiceTests
    {
        private const string LOGIN_MESSAGE = "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\">" +
            "<s:Body xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" +
            "<login xmlns=\"urn:enterprise.soap.sforce.com\">" +
            "<username>{0}</username>" +
            "<password>{1}</password>" +
            "</login></s:Body>" +
            "</s:Envelope>";
        private const string CHARSET = "utf-8";
        private const string SOAP_ACTION_KEY = "SOAPAction";
        private const string SOAP_ACTION_VALUE = "\"\"";
        private const string MEDIA_TYPE_XML = "text/xml";
        private const string LOGIN_ENDPOINT = "https://login.salesforce.com/services/Soap/c/49.0/";
        private const string LOGOUT_ENDPOINT = "https://login.salesforce.com/services/oauth2/revoke?token=";
        private const string USERNAME = "username";
        private const string PASSWORD = "pass";
        private const string REST_QUERY_URL = "/services/data/v48.0/query/?q=";
        private const string UPDATE_URL = "/services/data/v48.0/composite/sobjects";
        private const string MEDIA_TYPE_JSON = "application/json";
        private const string IDS = "?ids=";
        private const string ALL_OR_NONE_FALSE = "&allOrNone=false";
        private const string API_VERSION = "49.0";
        private const bool IS_PRODUCTION = true;

        private const string EXPECTED_BEARER = "Bearer abc123";
        private const string EXPECTED_ENDPOINT = "endpoint";

        private HttpMessageService _testedService;
        private IOptions<SalesforceConnectorOptions> _optionsSub;
        private ILogger<HttpMessageService> _loggerSub;

        [SetUp]
        public void Setup()
        {
            _optionsSub = Substitute.For<IOptions<SalesforceConnectorOptions>>();
            _loggerSub = Substitute.For<ILogger<HttpMessageService>>();
            SetupOptions();
            _testedService = new HttpMessageService(_optionsSub, _loggerSub);
        }

        private void SetupOptions()
        {
            SalesforceConnectorOptions opts = new SalesforceConnectorOptions();
            opts.IsProduction = IS_PRODUCTION;
            opts.ApiVersion = API_VERSION;
            opts.Username = USERNAME;
            opts.Password = PASSWORD;
            _optionsSub.Value.Returns(opts);
        }

        private void SetupFields()
        {
            string sessionId = "sessionId";
            AuthenticationHeaderValue header = new AuthenticationHeaderValue("Bearer", "abc123");
            List<FieldInfo> fields = _testedService.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static).ToList();
            FieldInfo headerField = fields.Find(x => x.Name == "_authHeader");
            FieldInfo endpointField = fields.Find(x => x.Name == "_requestEndpoint");
            FieldInfo sessionField = fields.Find(x => x.Name == "_sessionId");
            headerField.SetValue(_testedService, header);
            endpointField.SetValue(_testedService, EXPECTED_ENDPOINT);
            sessionField.SetValue(_testedService, sessionId);
        }

        [Test]
        public async Task BuildLoginMessage_CreatesMessageWithCorrectOptions()
        {
            //arrange
            string message = string.Format(LOGIN_MESSAGE, USERNAME, PASSWORD);

            //act
            HttpRequestMessage result = _testedService.BuildLoginMessage();

            //assert
            string content = await result.Content.ReadAsStringAsync();
            Assert.Multiple(() =>
            {
                Assert.AreEqual(message, content);
                Assert.AreEqual(LOGIN_ENDPOINT, result.RequestUri.OriginalString);
            });
        }

        [Test]
        public void BuildLoginMessage_CreatesMessageWithCorrectHeaders()
        {
            //arrange
            //act
            HttpRequestMessage result = _testedService.BuildLoginMessage();

            //assert
            Assert.Multiple(() =>
            {
                Assert.AreEqual(CHARSET, result.Content.Headers.ContentType.CharSet);
                Assert.AreEqual(MEDIA_TYPE_XML, result.Content.Headers.ContentType.MediaType);
                Assert.AreEqual(SOAP_ACTION_VALUE, result.Headers.GetValues(SOAP_ACTION_KEY).First());
            });
        }

        [Test]
        public void BuildLoginMessage_CreatesMessageWithCorrectMethod()
        {
            //arrange
            //act
            HttpRequestMessage result = _testedService.BuildLoginMessage();

            //assert
            Assert.AreSame(HttpMethod.Post, result.Method);
        }

        [Test]
        public void BuildLogoutMessage_CreatesMessageWithCorrectOptions()
        {
            //arrange
            SetupFields();
            string uriValue = LOGOUT_ENDPOINT + "sessionId";

            //act
            HttpRequestMessage result = _testedService.BuildLogoutMessage();

            //assert
            Assert.AreEqual(uriValue, result.RequestUri.OriginalString);
        }

        [Test]
        public void BuildLogoutMessage_CreatesMessageWithCorrectMethod()
        {
            //arrange
            //act
            HttpRequestMessage result = _testedService.BuildLogoutMessage();

            //assert
            Assert.AreSame(HttpMethod.Get, result.Method);
        }

        [Test]
        public async Task ProcessLoginResponseAsync_ThrowsWhenStatusCodeNotSuccessful()
        {
            //arrange
            HttpResponseMessage msg = new HttpResponseMessage();
            msg.Content = new StringContent(string.Empty);
            msg.StatusCode = HttpStatusCode.BadRequest;

            //act
            //assert
            Assert.ThrowsAsync(typeof(HttpRequestException), async () =>
            {
                await _testedService.ProcessLoginResponseAsync(msg, default);
            });
        }

        [Test]
        public async Task ProcessLoginResponseAsync_RetrievesDataCorrectlyFromResponse()
        {
            //arrange
            string session = "abc123";
            string endpoint = "https://someurl.com";
            string bearer = "Bearer " + session;
            string content = $"testtest<sessionId>{session}</sessionId>testtest<serverUrl>{endpoint}/services/Soap/c/</serverUrl>testtest";
            HttpResponseMessage msg = new HttpResponseMessage();
            msg.StatusCode = HttpStatusCode.OK;
            msg.Content = new StringContent(content);
            List<FieldInfo> fields = _testedService.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static).ToList();
            FieldInfo sessionField = fields.Find(x => x.Name == "_sessionId");
            FieldInfo endpointField = fields.Find(x => x.Name == "_requestEndpoint");
            FieldInfo headerField = fields.Find(x => x.Name == "_authHeader");

            //act
            await _testedService.ProcessLoginResponseAsync(msg, default);

            //assert
            Assert.Multiple(() =>
            {
                Assert.AreEqual(session, sessionField.GetValue(_testedService));
                Assert.AreEqual(endpoint, endpointField.GetValue(_testedService));
                Assert.AreEqual(bearer, (headerField.GetValue(_testedService) as AuthenticationHeaderValue).ToString());
            });
        }

        public static IEnumerable<object[]> BuildQueryMessageData
        {
            get
            {
                yield return new object[] { "regularQuery",
                                            false,
                                            $"{EXPECTED_ENDPOINT}{REST_QUERY_URL}regularQuery" };

                yield return new object[] { "queryMore",
                                            true,
                                            "endpointqueryMore" };
            }
        }

        [TestCaseSource("BuildQueryMessageData")]
        public void BuildQueryMessage_BuildsCorrectQueryMessage(string query, bool isQueryMore, string expectedUri)
        {
            SetupFields();

            //act
            HttpRequestMessage msg = _testedService.BuildQueryMessage(query, isQueryMore);

            //assert
            Assert.Multiple(() =>
            {
                Assert.AreEqual(EXPECTED_BEARER, msg.Headers.Authorization.ToString());
                Assert.AreEqual(expectedUri, msg.RequestUri.OriginalString);
            });
        }

        public static object[] BuildDataChangeMessageAsyncMethods = new[]
        {
            HttpMethod.Post,
            HttpMethod.Patch,
            HttpMethod.Delete
        };

        public static TestSfObject[] BuildDataChangeMessageAsyncData = new TestSfObject[]
        {
            new TestSfObject() { Id = "a1" },
            new TestSfObject() { Id = "a2" }
        };

        [TestCaseSource("BuildDataChangeMessageAsyncMethods")]
        public async Task BuildDataChangeMessageAsync_BuildsMessages_WithCorrectMethod(object method)
        {
            //arrange
            SetupFields();
            HttpMethod expectedMethod = method as HttpMethod;

            //act
            HttpRequestMessage result = await _testedService.BuildDataChangeMessageAsync(BuildDataChangeMessageAsyncData, expectedMethod, true, default);

            //assert
            Assert.AreSame(expectedMethod, result.Method);
        }

        [Test]
        public async Task BuildDataChangeMessageAsync_BuildsPostPatchMessage_WithCorrectHeadersAndUri()
        {
            //arrange
            string uri = EXPECTED_ENDPOINT + UPDATE_URL;
            SetupFields();
            MediaTypeHeaderValue expectedMediaType = new MediaTypeHeaderValue(MEDIA_TYPE_JSON) { CharSet = CHARSET };

            //act
            HttpRequestMessage result = await _testedService.BuildDataChangeMessageAsync(BuildDataChangeMessageAsyncData, HttpMethod.Post, true, default);

            //assert
            Assert.Multiple(() =>
            {
                Assert.AreEqual(uri, result.RequestUri.OriginalString);
                Assert.AreEqual(expectedMediaType, result.Content.Headers.ContentType);
                Assert.AreEqual(EXPECTED_BEARER, result.Headers.Authorization.ToString());
            });
        }

        [Test]
        public async Task BuildDataChangeMessageAsync_BuildsDeleteMessage_WithCorrectHeadersAndUri()
        {
            //arrange
            string a1 = "a1";
            string a2 = "a2";
            string uri = $"{EXPECTED_ENDPOINT}{UPDATE_URL}{IDS}{a1},{a2}{ALL_OR_NONE_FALSE}";
            SetupFields();

            //act
            HttpRequestMessage result = await _testedService.BuildDataChangeMessageAsync(BuildDataChangeMessageAsyncData, HttpMethod.Delete, false, default);

            //assert
            Assert.Multiple(() =>
            {
                Assert.AreEqual(uri, result.RequestUri.OriginalString);
                Assert.AreEqual(EXPECTED_BEARER, result.Headers.Authorization.ToString());
            });
        }

        [Test]
        public async Task BuildDataChangeMessageAsync_BuildsPostPatchMessage_WithCorrectContent()
        {
            //arrange
            SetupFields();
            string expected = "{\"allOrNone\":false,\"records\":[{\"Id\":\"a1\"},{\"Id\":\"a2\"}]}";

            //act
            HttpRequestMessage result = await _testedService.BuildDataChangeMessageAsync(BuildDataChangeMessageAsyncData, HttpMethod.Post, false, default);

            //assert
            Assert.AreEqual(expected, await result.Content.ReadAsStringAsync());
        }

        [Test]
        public async Task ProcessResponseAsync_ThrowsWhenStatusCodeNotSuccessful()
        {
            //arrange
            var message = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("error")
            };

            //act
            //assert
            Assert.ThrowsAsync(typeof(HttpRequestException), async () =>
            {
                await _testedService.ProcessResponseAsync<TestSfObject>(message, default);
            });
        }

        [Test]
        public async Task ProcessResponseAsync_DeserializesDataCorrectly()
        {
            //arrange
            string content = "[{\"Id\": \"1a\"}, {\"Id\": \"2a\"}]";
            var msg = new HttpResponseMessage()
            {
                Content = new StringContent(content),
                StatusCode = HttpStatusCode.OK
            };

            //act
            TestSfObject[] result = await _testedService.ProcessResponseAsync<TestSfObject[]>(msg, default);

            //assert
            Assert.Multiple(() =>
            {
                CollectionAssert.IsNotEmpty(result);
                Assert.AreEqual("1a", result[0].Id);
                Assert.AreEqual("2a", result[1].Id);
            });
        }
    }

    public class TestSfObject : SalesforceObjectModel
    {
    }
}
