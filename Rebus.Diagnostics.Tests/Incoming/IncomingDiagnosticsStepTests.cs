using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Diagnostics.Incoming;
using Rebus.Diagnostics.Tests.Outgoing;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.OpenTelemetry.Configuration;
using Rebus.Pipeline;
using Rebus.Retry.Simple;
using Rebus.Transport;
using Rebus.Transport.InMem;

namespace Rebus.Diagnostics.Tests.Incoming
{
    [TestFixture]
    public class IncomingDiagnosticsStepTests
    {
        
        [OneTimeSetUp]
        public static void ListenForRebus()
        {
            TestHelpers.ListenForRebus();
        }

        
        [Test]
        public async Task StartsActivityWhenTraceStateHeaderIsSet()
        {
            var activity = new Activity("MyOperation");
            activity.SetIdFormat(ActivityIdFormat.W3C); // Only the default on .net 5. Below that Hierarchical is the default
            activity.Start();

            Assume.That(activity.Id, Is.Not.Null);
            var headers = new Dictionary<string, string>
            {
                {Headers.Type, "MyType"},
                {Headers.Intent, Headers.IntentOptions.PublishSubscribe},
                {Headers.MessageId, "MyMessage"},
                {RebusDiagnosticConstants.TraceStateHeaderName, activity.Id!}
            };

            activity.Stop();

            var transportMessage = new TransportMessage(headers, Array.Empty<byte>());

            var scope = new RebusTransactionScope();
            var context = new IncomingStepContext(transportMessage, scope.TransactionContext);

            var step = new IncomingDiagnosticsStep();
            var callbackInvoked = false;
            await step.Process(context, () =>
            {
                callbackInvoked = true;
                Assert.That(Activity.Current!.ParentId, Is.EqualTo(activity.Id));
                return Task.CompletedTask;
            });
            
            Assert.That(callbackInvoked);
        }

        [Test]
        public async Task StartsAnEntireNewActivityIfNoActivityIsCurrentlyActive()
        {
            var headers = new Dictionary<string, string>
            {
                {Headers.Type, "MyType"},
                {Headers.Intent, Headers.IntentOptions.PublishSubscribe},
                {Headers.MessageId, "MyMessage"},
            };
            
            
            var transportMessage = new TransportMessage(headers, Array.Empty<byte>());

            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            var scope = new RebusTransactionScope();
            var context = new IncomingStepContext(transportMessage, scope.TransactionContext);

            var step = new IncomingDiagnosticsStep();
            var callbackInvoked = false;
            await step.Process(context, () =>
            {
                callbackInvoked = true;
                Assert.That(Activity.Current, Is.Not.Null);
                Assert.That(Activity.Current!.RootId, Is.EqualTo(Activity.Current.TraceId.ToString()));
                return Task.CompletedTask;
            });
            
            Assert.That(callbackInvoked);
        }

        [Test]
        public async Task ActivityIsAvailableInRetryLogging()
        {
            using var tracerProvider = Sdk.CreateTracerProviderBuilder()
                .AddRebusInstrumentation()
                .Build();

            using var receiverActivator = new BuiltinHandlerActivator();

            string handlerReceivedTraceId = "";

            var handleDone = new TaskCompletionSource<bool>();
            receiverActivator.Handle((string _) =>
            {
                handlerReceivedTraceId = Activity.Current!.TraceId.ToString();

                handleDone.TrySetResult(true);
                throw new Exception("Ohh nooo");
            });
            
            var network = new InMemNetwork();

            var logger = new TestLogger();
            
            
            var receiver = Configure.With(receiverActivator)
                .Logging(l =>
                {
                    l.Register<IRebusLoggerFactory>(_ => new TestLoggerFactory(logger));
                })
                .Transport(t => t.UseInMemoryTransport(network, "Receiver"))
                .Options(o =>
                {
                    o.RetryStrategy(maxDeliveryAttempts: 1);
                    o.EnableDiagnosticSources();
                })
                .Start();


            string operationTraceId = "";
            await Task.Run(async () =>
            {
                var activity = new Activity("MyOperation");
                activity.SetIdFormat(ActivityIdFormat
                    .W3C); // Only the default on .net 5. Below that Hierarchical is the default
                activity.Start();

                operationTraceId = activity.TraceId.ToString();

                await receiver.SendLocal("hej med dig!"); 
            });

            await handleDone.Task;
            await Task.Delay(50);
            
            Assert.That(handlerReceivedTraceId, Is.EqualTo(operationTraceId));

            // Unhandled exception is the only message logged at warning level
            var warning = logger.TraceIds.Single(t => t.Level == LogLevel.Warn);

            Assert.That(warning.TraceId, Is.EqualTo(operationTraceId));
        }
        
        [Test]
        public async Task DefaultsWhenIntentIsNotSet()
        {
            var headers = new Dictionary<string, string>
            {
                {Headers.Type, "MyType"},
                {Headers.MessageId, "MyMessage"},
            };

            var transportMessage = new TransportMessage(headers, Array.Empty<byte>());

            var scope = new RebusTransactionScope();
            var context = new IncomingStepContext(transportMessage, scope.TransactionContext);

            var step = new IncomingDiagnosticsStep();
            var callbackInvoked = false;
            await step.Process(context, () =>
            {
                callbackInvoked = true;
                Assert.That(Activity.Current!.Kind, Is.EqualTo(ActivityKind.Consumer));
                return Task.CompletedTask;
            });
            
            Assert.That(callbackInvoked);
        }

        [Test]
        public async Task StartsAnEntireNewActivsssityIfNoActivityIsCurrentlyActive()
        {
            var meterObserver = new MeterObserver();

            var headers = new Dictionary<string, string>
                          {
                              {Headers.Type, "MyType"},
                              {Headers.Intent, Headers.IntentOptions.PublishSubscribe},
                              {Headers.MessageId, "MyMessage"},
                          };

            var transportMessage = new TransportMessage(headers, Array.Empty<byte>());

            var scope = new RebusTransactionScope();
            var context = new IncomingStepContext(transportMessage, scope.TransactionContext);

            var step = new IncomingDiagnosticsStep();
            await step.Process(context, () => Task.CompletedTask);

            Assert.That(meterObserver.InstrumentCalled("incoming", RebusDiagnosticConstants.MessageCountMeterNameTemplate), Is.True);
            Assert.That(meterObserver.InstrumentCalled("incoming", RebusDiagnosticConstants.MessageDelayMeterNameTemplate), Is.True);
            Assert.That(meterObserver.InstrumentCalled("incoming", RebusDiagnosticConstants.MessageSizeMeterNameTemplate), Is.True);
        }

        private class TestLoggerFactory : IRebusLoggerFactory
        {
            private TestLogger _logger;

            public TestLoggerFactory(TestLogger logger)
            {
                _logger = logger;
            }

            public ILog GetLogger<T>()
            {
                return _logger;
            }
        }

        private enum LogLevel
        {
            Debug, 
            Info,
            Warn,
            Error
        }
        
        private class TestLogger : ILog
        {
            public List<(LogLevel Level, string Message, string TraceId)> TraceIds = new();

            private void StoreTraceId(LogLevel level, string message)
            {
                if (Activity.Current == null)
                {
                    TraceIds.Add((level, message, "NO-TRACE"));
                }
                else
                {
                    var traceId = Activity.Current!.TraceId.ToString();
                    TraceIds.Add((level, message, traceId));
                }
            }
            
            public void Debug(string message, params object[] objs)
            {
                StoreTraceId(LogLevel.Debug, RenderString(message, objs));
            }

            public void Info(string message, params object[] objs)
            {
                StoreTraceId(LogLevel.Info, RenderString(message, objs));
            }

            public void Warn(string message, params object[] objs)
            {
                StoreTraceId(LogLevel.Warn, RenderString(message, objs));
            }

            public void Warn(Exception exception, string message, params object[] objs)
            {
                StoreTraceId(LogLevel.Warn, RenderString(message, objs));
            }

            public void Error(string message, params object[] objs)
            {
                StoreTraceId(LogLevel.Error, RenderString(message, objs));
            }

            public void Error(Exception exception, string message, params object[] objs)
            {
                StoreTraceId(LogLevel.Error, RenderString(message, objs));
            }

            public override string ToString()
            {
                return string.Join("\n", TraceIds);
            }
            
            #region "Borrowed" from the Rebus source code, please ignore
            internal static readonly Regex PlaceholderRegex = new Regex(@"{\w*[\:(\w|\.|\d|\-)*]+}", RegexOptions.Compiled);
        /// <summary>
        ///     Renders the <paramref name="message" /> string by replacing placeholders on the form <code>{whatever}</code> with
        ///     the
        ///     string representation of each object from <paramref name="objs" />. Note that the actual content of the
        ///     placeholders
        ///     is ignored - i.e. it doesn't matter whether it says <code>{0}</code>, <code>{name}</code>, or
        ///     <code>{whatvgejigoejigoejigoe}</code>
        ///     - values are interpolated based on their order regardless of the name of the placeholder.
        /// </summary>
        private string RenderString(string message, object[] objs)
        {
            try
            {
                var index = 0;
                return PlaceholderRegex.Replace(message, match =>
                {
                    try
                    {
                        var value = objs[index];
                        index++;

                        var format = match.Value.Substring(1, match.Value.Length - 2)
                            .Split(':')
                            .Skip(1)
                            .FirstOrDefault();

                        return FormatObject(value, format);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        return "???";
                    }
                });
            }
            catch
            {
                return message;
            }
        }

        /// <summary>
        ///     Formatter function that is invoked for each object value to be rendered into a string while interpolating log lines
        /// </summary>
        private string FormatObject(object obj, string format)
        {
            if (obj is string) return $@"""{obj}""";
            if (obj is IEnumerable)
            {
                var valueStrings = ((IEnumerable) obj).Cast<object>().Select(o => FormatObject(o, format));

                return $"[{string.Join(", ", valueStrings)}]";
            }

            if (obj is DateTime) return ((DateTime) obj).ToString(format ?? "O");
            if (obj is DateTimeOffset) return ((DateTimeOffset) obj).ToString(format ?? "O");
            if (obj is IFormattable) return ((IFormattable) obj).ToString(format, CultureInfo.InvariantCulture);
            if (obj is IConvertible) return ((IConvertible) obj).ToString(CultureInfo.InvariantCulture);
            return obj.ToString();
        }

        #endregion
        }
    }
}
