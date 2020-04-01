using System;
using System.Threading.Tasks;
using zipkin4net;
using zipkin4net.Tracers.Zipkin;
using zipkin4net.Transport.Http;

namespace Sample1
{
    class Program
    {
        static async Task Main(string[] args)
        {
            TraceManager.SamplingRate = 1.0f;
            var httpSender = new HttpZipkinSender("https://zipkin-sandbox.imedidata.net/", "application/json");
            var tracer = new ZipkinTracer(httpSender, new JSONSpanSerializer());
            TraceManager.RegisterTracer(tracer);
            TraceManager.Start(new ZipkinConsoleLogger());

            // producer side
            var rootTrace = Trace.Create();
            Trace.Current = rootTrace;
            rootTrace.Record(Annotations.ServerRecv());
            rootTrace.Record(Annotations.ServiceName("checkmate-local"));
            rootTrace.Record(Annotations.Rpc("Request to update Object X in platform"));
            await Task.Delay(200);

                var plinthServerTrace = rootTrace.Child();
                Trace.Current = plinthServerTrace;
                plinthServerTrace.Record(Annotations.ServerRecv());
                plinthServerTrace.Record(Annotations.ServiceName("plinth-local"));
                plinthServerTrace.Record(Annotations.Rpc("Update Object X"));
                await Task.Delay(200);

                Trace.Current = plinthServerTrace;
                plinthServerTrace.Record(Annotations.ServerSend());

                await Task.Delay(200);

                var archonServerTrace = rootTrace.Child();
                Trace.Current = archonServerTrace;
                archonServerTrace.Record(Annotations.ServerRecv());
                archonServerTrace.Record(Annotations.ServiceName("archon-local"));
                archonServerTrace.Record(Annotations.Rpc("Request to broadcast Update Object X"));
                await Task.Delay(100);

                    var rawTraceFromArchon = archonServerTrace.Child();
                    Trace.Current = rawTraceFromArchon;
                    rawTraceFromArchon.Record(Annotations.ProducerStart());
                    rawTraceFromArchon.Record(Annotations.ServiceName("archon-local"));
                    rawTraceFromArchon.Record(Annotations.Rpc("Send message that Object X was updated."));
                    await Task.Delay(200);
                    rawTraceFromArchon.Record(Annotations.ProducerStop());

                Trace.Current = rootTrace;
                archonServerTrace.Record(Annotations.ServerSend());

            Trace.Current = rootTrace;
            rootTrace.Record(Annotations.ServerSend());

            await Task.Delay(1000);

            // iss side
            var traceFromArchon = Trace.CreateFromId(
                new SpanState(
                    rawTraceFromArchon.CurrentSpan.TraceId,
                    rawTraceFromArchon.CurrentSpan.ParentSpanId,
                    rawTraceFromArchon.CurrentSpan.SpanId,
                    isSampled: true,
                    isDebug: false));
            var issProcessMessageTrace = traceFromArchon.Child();
            Trace.Current = issProcessMessageTrace;
            issProcessMessageTrace.Record(Annotations.ConsumerStart());
            issProcessMessageTrace.Record(Annotations.ServiceName("iss-local"));
            issProcessMessageTrace.Record(Annotations.Rpc("Process message"));
            issProcessMessageTrace.Record(Annotations.Tag("sample-tag", "xxxxxxx"));
            await Task.Delay(1000);

                // send to rave
                var issRequestingToRaveTrace = issProcessMessageTrace.Child();
                Trace.Current = issRequestingToRaveTrace;
                issRequestingToRaveTrace.Record(Annotations.ClientSend());
                issRequestingToRaveTrace.Record(Annotations.ServiceName("iss-local"));
                issRequestingToRaveTrace.Record(Annotations.Rpc("Update Object X from client"));
                await Task.Delay(100);

                    // rave edc api side
                    var raveProcessMessageTrace = Trace.CreateFromId(
                        new SpanState(
                            issRequestingToRaveTrace.CurrentSpan.TraceId,
                            issRequestingToRaveTrace.CurrentSpan.ParentSpanId,
                            issRequestingToRaveTrace.CurrentSpan.SpanId,
                            isSampled: true,
                            isDebug: false));
                    Trace.Current = raveProcessMessageTrace;
                    raveProcessMessageTrace.Record(Annotations.ServerRecv());
                    raveProcessMessageTrace.Record(Annotations.ServiceName("plinth-local"));
                    raveProcessMessageTrace.Record(Annotations.Rpc("Fetch Object X from Platform"));
                    raveProcessMessageTrace.Record(Annotations.Tag("sample-tag", "yyyyy"));
                    await Task.Delay(500);
                    raveProcessMessageTrace.Record(Annotations.ServerSend());

                await Task.Delay(100);
                Trace.Current = issRequestingToRaveTrace;
                issRequestingToRaveTrace.Record(Annotations.ClientRecv());

                // send to app2
                var issRequestingToApp2Trace = issProcessMessageTrace.Child();
                Trace.Current = issRequestingToApp2Trace;
                issRequestingToApp2Trace.Record(Annotations.ClientSend());
                issRequestingToApp2Trace.Record(Annotations.ServiceName("iss-local"));
                issRequestingToApp2Trace.Record(Annotations.Rpc("Update Object X from client"));
                await Task.Delay(100);

                    // another app api side
                    var app2ProcessMessageTrace = Trace.CreateFromId(
                        new SpanState(
                            issRequestingToApp2Trace.CurrentSpan.TraceId,
                            issRequestingToApp2Trace.CurrentSpan.ParentSpanId,
                            issRequestingToApp2Trace.CurrentSpan.SpanId,
                            isSampled: true,
                            isDebug: false));
                    Trace.Current = app2ProcessMessageTrace;
                    app2ProcessMessageTrace.Record(Annotations.ServerRecv());
                    app2ProcessMessageTrace.Record(Annotations.ServiceName("rave-api-local"));
                    app2ProcessMessageTrace.Record(Annotations.Rpc("Update Object X in Rave"));
                    app2ProcessMessageTrace.Record(Annotations.Tag("sample-tag", "yyyyy"));
                    await Task.Delay(900);
                    app2ProcessMessageTrace.Record(Annotations.ServerSend());

                await Task.Delay(100);
                Trace.Current = issRequestingToApp2Trace;
                issRequestingToApp2Trace.Record(Annotations.ClientRecv());

            Trace.Current = issProcessMessageTrace;
            issProcessMessageTrace.Record(Annotations.ConsumerStop());

            await Task.Delay(5000);

            TraceManager.Stop();
            await Task.Delay(1000);
        }
    }

    public class ZipkinConsoleLogger : ILogger
    {
        public void LogError(string message)
        {
            Console.WriteLine(message);
        }

        public void LogInformation(string message)
        {
            Console.WriteLine(message);
        }

        public void LogWarning(string message)
        {
            Console.WriteLine(message);
        }
    }
}
