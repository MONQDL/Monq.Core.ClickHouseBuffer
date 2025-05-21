using Monq.Core.ClickHouseBuffer.Attributes;
using Monq.Core.ClickHouseBuffer.Extensions;
using System;
using Xunit;

namespace Monq.Core.ClickHouseBuffer.Tests;

public class ClickHouseBulkModelExtensionsTests
{
    [Fact(DisplayName = "Check the correctness of field-value dictionary extraction from the object.")]
    public void ShouldProperlyCreateDbValuesDictionary()
    {
        var obj = new TestObject();
        var resultValues = obj.ExtractDbColumnValues();
        var resultColumns = obj.ExtractDbColumnNames();

        Assert.Collection(resultColumns,
            (x) => Assert.Equal("publicProp", x),
            (x) => Assert.Equal("privateProp", x),
            (x) => Assert.Equal("publicField", x),
            (x) => Assert.Equal("privateField", x)
        );

        Assert.Collection(resultValues,
            (x) => { Assert.Equal(obj.PublicProp, x); },
            (x) => { Assert.Equal(obj.GetPrivateProp(), x); },
            (x) => { Assert.Equal(obj.PublicField, x); },
            (x) => { Assert.Equal(obj.GetPrivateField(), x); }
        );
    }

    [Fact(DisplayName = "Check returning string.Empty of null-string prop value")]
    public void ShouldProperlyReturnStringEmptyOfNullStringProperty()
    {
        var obj = new TestObject1();
        var resultValues = obj.ExtractDbColumnValues();
        var resultColumns = obj.ExtractDbColumnNames();

        Assert.Collection(resultColumns,
            (x) => Assert.Equal("str", x)
        );

        Assert.Collection(resultValues,
            (x) => { Assert.Equal(string.Empty, x); }
        );
    }

    [Fact(DisplayName = "Check returning string of enum prop value")]
    public void ShouldProperlyReturnStringOfEnumPropertyValue()
    {
        var obj = new TestObject2();
        var resultValues = obj.ExtractDbColumnValues();
        var resultColumns = obj.ExtractDbColumnNames();

        Assert.Collection(resultColumns,
            (x) => Assert.Equal("str", x)
        );

        Assert.Collection(resultValues,
            (x) => { Assert.Equal(TestEnum.Value.ToString(), x); }
        );
    }

    class TestObject
    {
        [ClickHouseColumn("publicProp")]
        public string PublicProp { get; set; } = Guid.NewGuid().ToString();

        [ClickHouseColumn("privateProp")]
        private string PrivateProp { get; set; } = Guid.NewGuid().ToString();

        public string GetPrivateProp() => PrivateProp;

        [ClickHouseColumn("publicField")]
        public string PublicField = Guid.NewGuid().ToString();

        [ClickHouseColumn("privateField")]
        private string _privateField = Guid.NewGuid().ToString();

        public string GetPrivateField() => _privateField;

        public string IgnoredProp { get; set; } = Guid.NewGuid().ToString();
    }

    class TestObject1
    {
        [ClickHouseColumn("str")]
        public string? Str { get; set; } = null;
    }

    class TestObject2
    {
        [ClickHouseColumn("str")]
        public TestEnum? Str { get; set; } = TestEnum.Value;
    }
}
