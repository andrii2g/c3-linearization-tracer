using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using C3Tracer.Cli.Reporting;
using C3Tracer.Core.Tracing;

namespace C3Tracer.Cli.Rendering;

public static class JsonReportWriter
{
    public static string Render(ReportDocument report)
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
        options.Converters.Add(new EventConverter());
        return JsonSerializer.Serialize(report, options).Replace("\r\n", "\n", StringComparison.Ordinal) + "\n";
    }

    private sealed class EventConverter : JsonConverter<TraceEvent>
    {
        public override TraceEvent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new NotSupportedException("Report events are write-only.");

        public override void Write(Utf8JsonWriter writer, TraceEvent value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("sequenceNumber", value.SequenceNumber);
            writer.WriteString("kind", value.Kind);
            writer.WriteString("className", value.ClassName);
            void Field<T>(string name, T content) { writer.WritePropertyName(name); JsonSerializer.Serialize(writer, content, options); }
            switch (value)
            {
                case ClassStarted e: Field("bases", e.Bases); break;
                case BaseResultReused e: Field("baseName", e.BaseName); Field("order", e.Order); break;
                case MergeStarted e: Field("sequences", e.Sequences); break;
                case StepStarted e: Field("mergeStep", e.MergeStep); Field("cursors", e.Cursors); break;
                case CandidateExamined e:
                    Field("mergeStep", e.MergeStep); Field("sourceSequenceId", e.SourceSequenceId); Field("candidate", e.Candidate);
                    Field("accepted", e.Accepted); Field("blockers", e.Blockers); break;
                case CandidateSelected e:
                    Field("mergeStep", e.MergeStep); Field("candidate", e.Candidate); Field("cursorsAfter", e.CursorsAfter);
                    Field("partialOrderLength", e.PartialOrderLength); break;
                case ClassCompleted e: Field("order", e.Order); break;
                case ClassFailed e: Field("failure", e.Failure); break;
                default: throw new InvalidOperationException("Unknown event kind.");
            }
            writer.WriteEndObject();
        }
    }
}
