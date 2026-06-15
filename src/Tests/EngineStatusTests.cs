using Xunit;
using TranslateTool.Services;

namespace TranslateTool.Tests;

public class EngineStatusTests
{
    [Fact]
    public void GetAll_ReturnsThreeEngines()
    {
        var statuses = EngineStatus.GetAll();
        Assert.Equal(3, statuses.Length);
    }

    [Fact]
    public void GetAll_ContainsBaiduGoogleMicrosoft()
    {
        var statuses = EngineStatus.GetAll();
        var names = statuses.Select(s => s.Name).ToArray();

        Assert.Contains("baidu", names);
        Assert.Contains("google", names);
        Assert.Contains("microsoft", names);
    }

    [Fact]
    public void GetAll_BaiduHasLabel()
    {
        var statuses = EngineStatus.GetAll();
        var baidu = statuses.First(s => s.Name == "baidu");

        Assert.Equal("百度翻译", baidu.Label);
        Assert.NotNull(baidu.Note);
    }

    [Fact]
    public void GetAll_GoogleIsNotStable()
    {
        var statuses = EngineStatus.GetAll();
        var google = statuses.First(s => s.Name == "google");

        Assert.False(google.IsStable);
        Assert.NotNull(google.Note);
    }

    [Fact]
    public void GetAll_MicrosoftIsNotStable()
    {
        var statuses = EngineStatus.GetAll();
        var microsoft = statuses.First(s => s.Name == "microsoft");

        Assert.False(microsoft.IsStable);
        Assert.NotNull(microsoft.Note);
    }

    [Fact]
    public void GetStatusIcon_Stable_ReturnsCheckmark()
    {
        var status = new EngineStatus { IsStable = true };
        Assert.Equal("✅", status.GetStatusIcon());
    }

    [Fact]
    public void GetStatusIcon_NotStable_ReturnsWarning()
    {
        var status = new EngineStatus { IsStable = false };
        Assert.Equal("⚠️", status.GetStatusIcon());
    }

    [Fact]
    public void ToString_ContainsLabelAndNote()
    {
        var status = new EngineStatus
        {
            Label = "测试引擎",
            IsStable = true,
            Note = "测试备注"
        };

        var str = status.ToString();
        Assert.Contains("测试引擎", str);
        Assert.Contains("测试备注", str);
        Assert.Contains("✅", str);
    }

    [Fact]
    public void Baidu_WithoutCredentials_IsNotStable()
    {
        // 默认情况下（未设置凭据），百度引擎应该是不稳定的
        var statuses = EngineStatus.GetAll();
        var baidu = statuses.First(s => s.Name == "baidu");

        // 注意：如果在测试前调用了 SetCredentials，这个测试会失败
        // 这是预期行为，因为 BaiduTranslator.HasCredentials 是静态的
        Assert.False(baidu.IsStable);
        Assert.Contains("免费Web接口", baidu.Note!);
    }
}
