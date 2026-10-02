using System.Text;
using C3Tracer.Cli.Input;
using Xunit;

namespace C3Tracer.Tests;

public class JsonInputTests
{
    [Theory]
    [InlineData("{", "InvalidJson")]
    [InlineData("{/*comment*/\"classes\":[]}", "InvalidJson")]
    [InlineData("{\"classes\":[],}", "InvalidJson")]
    [InlineData("{\"classes\":[],\"classes\":[]}", "DuplicateProperty")]
    [InlineData("{\"classes\":[],\"wat\":0}", "UnknownProperty")]
    [InlineData("{}", "InvalidShape")]
    [InlineData("{\"classes\":null}", "InvalidShape")]
    [InlineData("{\"classes\":[{\"name\":\"A\",\"bases\":[1]}]}", "InvalidShape")]
    [InlineData("{\"classes\":[{\"name\":null,\"bases\":[]}]}", "InvalidShape")]
    [InlineData("{\"classes\":[{\"name\":\"A\"}]}", "InvalidShape")]
    [InlineData("{\"target\":null,\"classes\":[]}", "InvalidShape")]
    [InlineData("[]", "InvalidShape")]
    [InlineData("{\"classes\":[{\"name\":\"A\",\"name\":\"B\",\"bases\":[]}]}", "DuplicateProperty")]
    public void StrictShape(string json, string code) => Assert.Contains(HierarchyJsonReader.Parse(Encoding.UTF8.GetBytes(json)).Diagnostics, d => d.Code == code);

    [Fact]
    public void EncodingSizeAndDepth()
    {
        Assert.Equal("InvalidJson", Assert.Single(HierarchyJsonReader.Parse(new byte[] { 0xff }).Diagnostics).Code);
        string json = "{\"classes\":[{\"name\":\"A\",\"bases\":[]}]}";
        Assert.Empty(HierarchyJsonReader.Parse(Encoding.UTF8.GetBytes(json.PadRight(HierarchyJsonReader.MaxBytes))).Diagnostics);
        Assert.Equal("LimitExceeded", Assert.Single(HierarchyJsonReader.Parse(new byte[HierarchyJsonReader.MaxBytes + 1]).Diagnostics).Code);
        Assert.Equal("LimitExceeded", Assert.Single(HierarchyJsonReader.Parse(Encoding.UTF8.GetBytes(new string('[', 17) + new string(']', 17))).Diagnostics).Code);
    }
}
