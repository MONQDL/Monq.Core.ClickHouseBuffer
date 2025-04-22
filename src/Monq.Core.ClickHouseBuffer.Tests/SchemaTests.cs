using Monq.Core.ClickHouseBuffer.Schemas;
using Monq.Core.ClickHouseBuffer.Tests.Models;
using System;
using Xunit;

namespace Monq.Core.ClickHouseBuffer.Tests;

public class SchemaTests
{
    [Fact]
    public void ShouldProperlyScanAssembly()
    {
        ClickHouseSchemaConfig.GlobalSettings.Scan(this.GetType().Assembly);

        Assert.Equal(2, ClickHouseSchemaConfig.RulesMap.Count);
    }

    [Fact]
    public void ShouldProperlyScanAssemblyMultipleTimes()
    {
        ClickHouseSchemaConfig.GlobalSettings.Scan(this.GetType().Assembly);
        ClickHouseSchemaConfig.GlobalSettings.Scan(this.GetType().Assembly);

        Assert.Equal(2, ClickHouseSchemaConfig.RulesMap.Count);
    }

    [Fact]
    public void ShouldProperlyGetColumns()
    {
        ClickHouseSchemaConfig.GlobalSettings.Scan(this.GetType().Assembly);

        var now = DateTimeOffset.UtcNow;
        var e = new MyEvent()
        {
            StreamId = 10,
            AggregatedAt = now,
            StreamName = "Stream10",
            UserspaceId = 1,
            Value = """
            91.98.35.212 - - [23/Jan/2019:14:45:13 +0330] "GET /static/css/font/wyekan/font.ttf HTTP/1.1" 200 27459 "https://znbl.ir/static/bundle-bundle_site_head.css" "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36" "-"
            """
        };

        var columns = e.ClickHouseColumns("logs");

        Assert.Collection(columns,
            x => Assert.Equal("_rawJson", x),
            x => Assert.Equal("_userspaceId", x),
            x => Assert.Equal("_aggregatedAt", x),
            x => Assert.Equal("_streamName", x),
            x => Assert.Equal("_streamId", x));
    }

    [Fact]
    public void ShouldProperlyGetValues()
    {
        ClickHouseSchemaConfig.GlobalSettings.Scan(this.GetType().Assembly);

        var now = DateTimeOffset.UtcNow;
        const string rawJson = """
            91.98.35.212 - - [23/Jan/2019:14:45:13 +0330] "GET /static/css/font/wyekan/font.ttf HTTP/1.1" 200 27459 "https://znbl.ir/static/bundle-bundle_site_head.css" "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36" "-"
            """;
        const long userspaceId = 1;
        const string streamName = "Stream10";
        const long streamId = 10;
        var e = new MyEvent()
        {
            StreamId = streamId,
            AggregatedAt = now,
            StreamName = streamName,
            UserspaceId = userspaceId,
            Value = rawJson
        };

        var values = e.ClickHouseValues("logs");

        Assert.Collection(values,
            x => Assert.Equal(rawJson, x),
            x => Assert.Equal(userspaceId, x),
            x => Assert.Equal(now, x),
            x => Assert.Equal(streamName, x),
            x => Assert.Equal(streamId, x));
    }
}
