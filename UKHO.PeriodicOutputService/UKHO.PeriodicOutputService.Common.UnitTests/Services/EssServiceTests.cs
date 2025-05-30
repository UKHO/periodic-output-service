﻿using System;
using System.Net;
using System.Text;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Ess;
using UKHO.PeriodicOutputService.Common.Models.Ess.Response;
using UKHO.PeriodicOutputService.Common.Services;

namespace UKHO.PeriodicOutputService.Common.UnitTests.Services
{
    [TestFixture]
    public class EssServiceTests
    {
        private IOptions<EssApiConfiguration> _fakeEssApiConfiguration;
        private IEssApiClient _fakeEssApiClient;
        private IAuthEssTokenProvider _fakeAuthTokenProvider;
        private ILogger<EssService> _fakeLogger;

        private IEssService _essService;
        public string fulfilmentExceptionMessage = "There has been a problem in creating your exchange set, so we are unable to fulfil your request at this time. Please contact UKHO Customer Services quoting error code : {0} and correlation ID : {1}";

        [SetUp]
        public void Setup()
        {
            _fakeEssApiConfiguration = Options.Create(new EssApiConfiguration() { EssClientId = "ClientId2" });
            _fakeEssApiClient = A.Fake<IEssApiClient>();
            _fakeAuthTokenProvider = A.Fake<IAuthEssTokenProvider>();
            _fakeLogger = A.Fake<ILogger<EssService>>();

            _essService = new EssService(_fakeLogger, _fakeEssApiConfiguration, _fakeEssApiClient, _fakeAuthTokenProvider);
        }

        [Test]
        public void Does_Constructor_Throws_ArgumentNullException_When_Paramter_Is_Null()
        {
            var execption = Assert.Throws<ArgumentNullException>(
                () => new EssService(null, _fakeEssApiConfiguration, _fakeEssApiClient, _fakeAuthTokenProvider));
            Assert.That(execption.ParamName, Is.EqualTo("logger"));

            execption = Assert.Throws<ArgumentNullException>(
                () => new EssService(_fakeLogger, null, _fakeEssApiClient, _fakeAuthTokenProvider));
            Assert.That(execption.ParamName, Is.EqualTo("essApiConfiguration"));

            execption = Assert.Throws<ArgumentNullException>(
               () => new EssService(_fakeLogger, _fakeEssApiConfiguration, null, _fakeAuthTokenProvider));
            Assert.That(execption.ParamName, Is.EqualTo("essApiClient"));

            execption = Assert.Throws<ArgumentNullException>(
               () => new EssService(_fakeLogger, _fakeEssApiConfiguration, _fakeEssApiClient, null));
            Assert.That(execption.ParamName, Is.EqualTo("authEssTokenProvider"));
        }

        [Test]
        public async Task DoesGetProductIdentifiersData_Returns_ValidData_WhenValidProductIdentifiersArePassed()
        {
            A.CallTo(() => _fakeEssApiClient.PostProductIdentifiersDataAsync
            (A<string>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                  .Returns(new HttpResponseMessage()
                  {
                      StatusCode = HttpStatusCode.OK,
                      RequestMessage = new HttpRequestMessage()
                      {
                          RequestUri = new Uri("http://test.com")
                      },
                      Content = new StringContent(JsonConvert.SerializeObject(GetValidExchangeSetGetBatchResponse()))
                  });

            ExchangeSetResponseModel response = await _essService.PostProductIdentifiersData(GetProductIdentifiers());
            Assert.Multiple(() =>
            {
                Assert.That(response?.ExchangeSetCellCount, Is.EqualTo(GetProductIdentifiers().Count));
                Assert.That(!string.IsNullOrEmpty(response?.Links?.ExchangeSetFileUri?.Href), Is.True);
                Assert.That(response?.RequestedProductsNotInExchangeSet, Is.Null);
            });

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to post productidentifiers to ESS completed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(() => _fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task DoesGetProductIdentifiersData_Returns_ValidData_WhenInvalidProductIdentifiersArePassed()
        {
            A.CallTo(() => _fakeEssApiClient.PostProductIdentifiersDataAsync
            (A<string>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                  .Returns(new HttpResponseMessage()
                  {
                      StatusCode = HttpStatusCode.OK,
                      RequestMessage = new HttpRequestMessage()
                      {
                          RequestUri = new Uri("http://test.com")
                      },
                      Content = new StringContent(JsonConvert.SerializeObject(GetInValidExchangeSetGetBatchResponse()))
                  });

            ExchangeSetResponseModel response = await _essService.PostProductIdentifiersData(GetProductIdentifiers());

            Assert.Multiple(() =>
            {
                Assert.That(response?.ExchangeSetCellCount, !Is.EqualTo(GetProductIdentifiers().Count));
                Assert.That(response?.RequestedProductsNotInExchangeSet, !Is.Null);
            });

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Failed to post productidentifiers to ESS | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
            ).MustNotHaveHappened();

            A.CallTo(() => _fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public void DoesGetProductIdentifiersData_LogsError_When_ResponseStatus_Is_Not_OK()
        {
            A.CallTo(() => _fakeEssApiClient.PostProductIdentifiersDataAsync
            (A<string>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                  .Returns(new HttpResponseMessage()
                  {
                      StatusCode = HttpStatusCode.NotFound,
                      RequestMessage = new HttpRequestMessage()
                      {
                          RequestUri = new Uri("http://test.com")
                      },
                      Content = new StringContent(JsonConvert.SerializeObject(GetValidExchangeSetGetBatchResponse())),
                  });

            Assert.ThrowsAsync<FulfilmentException>(
                 () => _essService.PostProductIdentifiersData(GetProductIdentifiers()));

            A.CallTo(_fakeLogger).Where(call =>
             call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Error
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Failed to post productidentifiers to ESS | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
             ).MustHaveHappenedOnceExactly();

            A.CallTo(() => _fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public void DoesGetProductIdentifiersData_LogsError_When_ResponseStatus_Is_NotModified()
        {
            A.CallTo(() => _fakeEssApiClient.PostProductIdentifiersDataAsync
            (A<string>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                  .Returns(new HttpResponseMessage()
                  {
                      StatusCode = HttpStatusCode.NotModified,

                      RequestMessage = new HttpRequestMessage()
                      {
                          RequestUri = new Uri("http://test.com")
                      },
                      Content = new StringContent(JsonConvert.SerializeObject(GetValidExchangeSetGetBatchResponse())),
                  });

            Assert.ThrowsAsync<FulfilmentException>(
                 () => _essService.PostProductIdentifiersData(GetProductIdentifiers()));

            A.CallTo(_fakeLogger).Where(call =>
             call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Error
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Exchange set not modified | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
             ).MustHaveHappenedOnceExactly();

            A.CallTo(() => _fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
        }


        [Test]
        public async Task DoesGetProductDataSinceDateTime_Returns_ValidData_WhenValidProductIdentifiersArePassed()
        {
            A.CallTo(() => _fakeEssApiClient.GetProductDataSinceDateTime
            (A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                  .Returns(new HttpResponseMessage()
                  {
                      StatusCode = HttpStatusCode.OK,
                      RequestMessage = new HttpRequestMessage()
                      {
                          RequestUri = new Uri("http://test.com")
                      },
                      Content = new StringContent(JsonConvert.SerializeObject(GetValidExchangeSetGetBatchResponse())),
                      Headers = { Date = DateTime.UtcNow }
                  });

            ExchangeSetResponseModel response = await _essService.GetProductDataSinceDateTime(DateTime.UtcNow.AddDays(-7).ToString("R"));

            Assert.Multiple(() =>
            {
                Assert.That(!string.IsNullOrEmpty(response?.Links?.ExchangeSetFileUri?.Href), Is.True);
                Assert.That(response?.RequestedProductsNotInExchangeSet, Is.Null);
            });

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Failed to post productidentifiers to ESS | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
            ).MustNotHaveHappened();

            A.CallTo(() => _fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task DoesGetProductDataSinceDateTime_Returns_ValidData_WhenInvalidProductIdentifiersArePassed()
        {
            A.CallTo(() => _fakeEssApiClient.GetProductDataSinceDateTime
            (A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                  .Returns(new HttpResponseMessage()
                  {
                      StatusCode = HttpStatusCode.OK,
                      RequestMessage = new HttpRequestMessage()
                      {
                          RequestUri = new Uri("http://test.com")
                      },
                      Content = new StringContent(JsonConvert.SerializeObject(GetInValidExchangeSetGetBatchResponse())),
                      Headers = { Date = DateTime.UtcNow }
                  });

            ExchangeSetResponseModel response = await _essService.GetProductDataSinceDateTime(DateTime.UtcNow.AddDays(-7).ToString("R"));
            Assert.Multiple(() =>
            {
                Assert.That(response?.ExchangeSetCellCount, !Is.EqualTo(GetProductIdentifiers().Count));
                Assert.That(response?.RequestedProductsNotInExchangeSet, !Is.Null);
            });

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ESS request to create exhchange set for data since {SinceDateTime} completed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(() => _fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public void DoesGetProductDataSinceDateTime_LogsError_When_ResponseStatus_Is_Not_OK()
        {
            A.CallTo(() => _fakeEssApiClient.GetProductDataSinceDateTime
            (A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                  .Returns(new HttpResponseMessage()
                  {
                      StatusCode = HttpStatusCode.NotFound,
                      RequestMessage = new HttpRequestMessage()
                      {
                          RequestUri = new Uri("http://test.com")
                      },
                      Content = new StringContent(JsonConvert.SerializeObject(GetValidExchangeSetGetBatchResponse())),
                  });

            Assert.ThrowsAsync<FulfilmentException>(() => _essService.GetProductDataSinceDateTime(DateTime.UtcNow.AddDays(-7).ToString("R")));

            A.CallTo(_fakeLogger).Where(call =>
             call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Error
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Failed to create exchange set for data since {SinceDateTime} | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
             ).MustHaveHappenedOnceExactly();

            A.CallTo(() => _fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task DoesGetGetProductDataProductVersions_Returns_ValidData_WhenValidProductVersionsArePassed()
        {
            A.CallTo(() => _fakeEssApiClient.GetProductDataProductVersion
            (A<string>.Ignored, A<List<ProductVersion>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                  .Returns(new HttpResponseMessage()
                  {
                      StatusCode = HttpStatusCode.OK,
                      RequestMessage = new HttpRequestMessage()
                      {
                          RequestUri = new Uri("http://test.com")
                      },
                      Content = new StringContent(JsonConvert.SerializeObject(GetValidExchangeSetGetBatchResponse())),
                      Headers = { Date = DateTime.UtcNow }
                  });

            ExchangeSetResponseModel response = await _essService.GetProductDataProductVersions(new ProductVersionsRequest
            {
                ProductVersions = new List<ProductVersion>
                                                                                                    {
                                                                                                         new ProductVersion
                                                                                                         {
                                                                                                             ProductName="ABC000001",
                                                                                                             EditionNumber=31,
                                                                                                             UpdateNumber = 10
                                                                                                         }
                                                                                                    }
            });

            Assert.Multiple(() =>
            {
                Assert.That(!string.IsNullOrEmpty(response?.Links?.ExchangeSetFileUri?.Href), Is.True);
                Assert.That(response?.RequestedProductsNotInExchangeSet, Is.Null);
            });

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ESS request to create exchange set for product version started | {DateTime} | _X-Correlation-ID : {CorrelationId}"
                ).MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
               call.Method.Name == "Log"
               && call.GetArgument<LogLevel>(0) == LogLevel.Information
               && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ESS request to create exchange set for product version completed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
               ).MustHaveHappened();

            A.CallTo(() => _fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public void DoesGetGetProductDataProductVersions_Returns_ValidData_When_Response_Status_Is_Not_Ok()
        {
            A.CallTo(() => _fakeEssApiClient.GetProductDataProductVersion
           (A<string>.Ignored, A<List<ProductVersion>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                 .Returns(new HttpResponseMessage()
                 {
                     StatusCode = HttpStatusCode.NotModified,
                     RequestMessage = new HttpRequestMessage()
                     {
                         RequestUri = new Uri("http://test.com")
                     },
                     Content = new StringContent(JsonConvert.SerializeObject(GetValidExchangeSetGetBatchResponse())),
                 });

            Assert.ThrowsAsync<FulfilmentException>(() => _essService.GetProductDataProductVersions(new ProductVersionsRequest
            {
                ProductVersions = new List<ProductVersion>
                                                                                                    {
                                                                                                         new ProductVersion
                                                                                                         {
                                                                                                             ProductName="ABC000001",
                                                                                                             EditionNumber=31,
                                                                                                             UpdateNumber = 10
                                                                                                         }
                                                                                                    }
            }));

            A.CallTo(_fakeLogger).Where(call =>
             call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Error
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Failed to create exchange set for product version | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
             ).MustHaveHappenedOnceExactly();

            A.CallTo(() => _fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(ExchangeSetStandard.S63)]
        [TestCase(ExchangeSetStandard.S57)]
        public async Task DoesGetProductIdentifiersData_Returns_ValidData_WhenValidProductIdentifiersAndOptionalParameterArePassed(ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => _fakeEssApiClient.PostProductIdentifiersDataAsync
            (A<string>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                  .Returns(new HttpResponseMessage()
                  {
                      StatusCode = HttpStatusCode.OK,
                      RequestMessage = new HttpRequestMessage()
                      {
                          RequestUri = new Uri("http://test.com")
                      },
                      Content = new StringContent(JsonConvert.SerializeObject(GetValidExchangeSetGetBatchResponse()))
                  });

            ExchangeSetResponseModel response = await _essService.PostProductIdentifiersData(GetProductIdentifiers(), exchangeSetStandard.ToString());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.ExchangeSetCellCount, Is.EqualTo(GetProductIdentifiers().Count));
                Assert.That(!string.IsNullOrEmpty(response?.Links?.ExchangeSetFileUri?.Href));
                Assert.That(response?.RequestedProductsNotInExchangeSet, Is.EqualTo(null));
            }

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to post {ProductIdentifiersCount} productidentifiers to ESS started | {DateTime} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to post productidentifiers to ESS completed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(() => _fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(ExchangeSetStandard.S63)]
        [TestCase(ExchangeSetStandard.S57)]
        public async Task DoesGetProductDataSinceDateTime_Returns_ValidData_WhenValidProductIdentifiersAndOptionalParameterArePassed(ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => _fakeEssApiClient.GetProductDataSinceDateTime
            (A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                  .Returns(new HttpResponseMessage()
                  {
                      StatusCode = HttpStatusCode.OK,
                      RequestMessage = new HttpRequestMessage()
                      {
                          RequestUri = new Uri("http://test.com")
                      },
                      Content = new StringContent(JsonConvert.SerializeObject(GetValidExchangeSetGetBatchResponse())),
                      Headers = { Date = DateTime.UtcNow }
                  });

            ExchangeSetResponseModel response = await _essService.GetProductDataSinceDateTime(DateTime.UtcNow.AddDays(-7).ToString("R"), exchangeSetStandard.ToString());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(!string.IsNullOrEmpty(response?.Links?.ExchangeSetFileUri?.Href));
                Assert.That(response?.RequestedProductsNotInExchangeSet, Is.EqualTo(null));
            }

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ESS request to create exchange set for data since {SinceDateTime} started | {DateTime} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Failed to post productidentifiers to ESS | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
            ).MustNotHaveHappened();

            A.CallTo(() => _fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(ExchangeSetStandard.S63)]
        [TestCase(ExchangeSetStandard.S57)]
        public async Task DoesGetGetProductDataProductVersions_Returns_ValidData_WhenValidProductVersionsAndOptionalParameterArePassed(ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => _fakeEssApiClient.GetProductDataProductVersion
            (A<string>.Ignored, A<List<ProductVersion>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                  .Returns(new HttpResponseMessage()
                  {
                      StatusCode = HttpStatusCode.OK,
                      RequestMessage = new HttpRequestMessage()
                      {
                          RequestUri = new Uri("http://test.com")
                      },
                      Content = new StringContent(JsonConvert.SerializeObject(GetValidExchangeSetGetBatchResponse())),
                      Headers = { Date = DateTime.UtcNow }
                  });

            ExchangeSetResponseModel response = await _essService.GetProductDataProductVersions(new ProductVersionsRequest
            {
                ProductVersions = new List<ProductVersion>
                                                                                                    {
                                                                                                         new ProductVersion
                                                                                                         {
                                                                                                             ProductName="ABC000001",
                                                                                                             EditionNumber=31,
                                                                                                             UpdateNumber = 10
                                                                                                         }
                                                                                                    }
            }, exchangeSetStandard.ToString());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(!string.IsNullOrEmpty(response?.Links?.ExchangeSetFileUri?.Href));
                Assert.That(response?.RequestedProductsNotInExchangeSet, Is.EqualTo(null));
            }

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ESS request to create exchange set for product version started | {DateTime} | _X-Correlation-ID : {CorrelationId}"
                ).MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
               call.Method.Name == "Log"
               && call.GetArgument<LogLevel>(0) == LogLevel.Information
               && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ESS request to create exchange set for product version completed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
               ).MustHaveHappened();

            A.CallTo(() => _fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(HttpStatusCode.BadRequest, "BadRequest")]
        [TestCase(HttpStatusCode.Unauthorized, "Unauthorized")]
        [TestCase(HttpStatusCode.InternalServerError, "InternalServerError")]
        [TestCase(HttpStatusCode.ServiceUnavailable, "ServiceUnavailable")]
        public void WhenProductIdentifiersRequestOtherThan200_ThenPostProductIdentifiersReturnsReturnsFulfilmentException(HttpStatusCode statusCode, string content)
        {
            A.CallTo(() => _fakeEssApiClient.PostProductIdentifiersDataAsync
            (A<string>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                  .Returns(new HttpResponseMessage()
                  {
                      StatusCode = statusCode,
                      RequestMessage = new HttpRequestMessage()
                      {
                          RequestUri = new Uri("http://test.com")
                      },
                      Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(content)))
                  });

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>(),
                 async delegate { await _essService.PostProductIdentifiersData(new List<string> { }, ExchangeSetStandard.S63.ToString()); });

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to post {ProductIdentifiersCount} productidentifiers to ESS started | {DateTime} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Failed to post productidentifiers to ESS | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(() => _fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(HttpStatusCode.BadRequest, "BadRequest")]
        [TestCase(HttpStatusCode.Unauthorized, "Unauthorized")]
        [TestCase(HttpStatusCode.InternalServerError, "InternalServerError")]
        [TestCase(HttpStatusCode.ServiceUnavailable, "ServiceUnavailable")]
        public void DoesGetProductDataSinceDateTime_Returns_FulfilmentException_When_Response_Status_Is_Not_Ok(HttpStatusCode statusCode, string content)
        {
            A.CallTo(() => _fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored, A<string>.Ignored)).Returns("InvalidToken");

            A.CallTo(() => _fakeEssApiClient.GetProductDataSinceDateTime
            (A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                  .Returns(new HttpResponseMessage()
                  {
                      StatusCode = statusCode,
                      RequestMessage = new HttpRequestMessage()
                      {
                          RequestUri = new Uri("http://test.com")
                      },
                      Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(content))),
                  });

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>(),
                 async delegate { await _essService.GetProductDataSinceDateTime(DateTime.UtcNow.ToString("R"), ExchangeSetStandard.S63.ToString()); });

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ESS request to create exchange set for data since {SinceDateTime} started | {DateTime} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
             call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Error
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Failed to create exchange set for data since {SinceDateTime} | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
             ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(HttpStatusCode.BadRequest, "BadRequest")]
        [TestCase(HttpStatusCode.Unauthorized, "Unauthorized")]
        [TestCase(HttpStatusCode.InternalServerError, "InternalServerError")]
        [TestCase(HttpStatusCode.ServiceUnavailable, "ServiceUnavailable")]
        public void DoesGetGetProductDataProductVersions_Returns_FulfilmentException_When_Response_Status_Is_Not_Ok(HttpStatusCode statusCode, string content)
        {
            A.CallTo(() => _fakeEssApiClient.GetProductDataProductVersion
           (A<string>.Ignored, A<List<ProductVersion>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                 .Returns(new HttpResponseMessage()
                 {
                     StatusCode = statusCode,
                     RequestMessage = new HttpRequestMessage()
                     {
                         RequestUri = new Uri("http://test.com")
                     },
                     Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(content))),
                 });

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>(),
                async delegate
                {
                    await _essService.GetProductDataProductVersions(new ProductVersionsRequest
                    {
                        ProductVersions = new List<ProductVersion>
                                                                                                    {
                                                                                                         new ProductVersion
                                                                                                         {
                                                                                                             ProductName="ABC000001",
                                                                                                             EditionNumber=3,
                                                                                                             UpdateNumber = 10
                                                                                                         }
                                                                                                    }
                    }, ExchangeSetStandard.S63.ToString());
                });

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ESS request to create exchange set for product version started | {DateTime} | _X-Correlation-ID : {CorrelationId}"
                ).MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
             call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Error
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Failed to create exchange set for product version | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
             ).MustHaveHappenedOnceExactly();

            A.CallTo(() => _fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        private ExchangeSetResponseModel GetValidExchangeSetGetBatchResponse() => new()
        {
            ExchangeSetCellCount = GetProductIdentifiers().Count,
            RequestedProductCount = GetProductIdentifiers().Count,
            Links = new Links
            {
                ExchangeSetBatchDetailsUri = new LinkSetBatchDetailsUri
                {
                    Href = "http://test1.com"
                },
                ExchangeSetBatchStatusUri = new LinkSetBatchStatusUri
                {
                    Href = "http://test2.com"
                },
                ExchangeSetFileUri = new LinkSetFileUri
                {
                    Href = "http://test3.com"
                }
            }
        };

        private ExchangeSetResponseModel GetInValidExchangeSetGetBatchResponse() => new()
        {
            ExchangeSetCellCount = 0,
            RequestedProductCount = GetProductIdentifiers().Count,
            Links = new Links
            {
                ExchangeSetBatchDetailsUri = new LinkSetBatchDetailsUri
                {
                    Href = "http://test1.com"
                },
                ExchangeSetBatchStatusUri = new LinkSetBatchStatusUri
                {
                    Href = "http://test2.com"
                },
                ExchangeSetFileUri = new LinkSetFileUri
                {
                    Href = "http://test3.com"
                }
            },
            RequestedProductsNotInExchangeSet = GetRequestedProductsNotInExchangeSet()
        };

        private IEnumerable<RequestedProductsNotInExchangeSet> GetRequestedProductsNotInExchangeSet()
        {
            RequestedProductsNotInExchangeSet[] requestedProductsNotInExchangeSet = new RequestedProductsNotInExchangeSet[]
            {
                new RequestedProductsNotInExchangeSet {  ProductName="1US2ARCGD", Reason ="invalidProduct"}
            };

            return requestedProductsNotInExchangeSet;
        }

        private List<string> GetProductIdentifiers()
        {
            return new List<string>
            {
                "US2ARCGD",
                "CA379151",
                "DE110000"
            };
        }
    }
}
