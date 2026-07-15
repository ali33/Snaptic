using Snaptic.Core.Links;

namespace Snaptic.Core.Tests;

public class LinkValidatorTests
{
    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://example.com")]
    [InlineData("https://example.com/path?q=1#frag")]
    [InlineData("HTTPS://EXAMPLE.COM")]
    [InlineData("  https://example.com  ")]
    public void ChoPhep_http_va_https(string text)
        => Assert.True(LinkValidator.IsOpenableUrl(text));

    [Theory]
    [InlineData("file:///C:/Windows/System32/calc.exe")]
    [InlineData("javascript:alert(1)")]
    [InlineData("steam://run/730")]
    [InlineData("ms-settings:privacy")]
    [InlineData(@"\\attacker\share\payload.exe")]
    [InlineData("ftp://example.com")]
    [InlineData("mailto:a@b.com")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    public void Chan_moi_scheme_khac(string text)
        => Assert.False(LinkValidator.IsOpenableUrl(text));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("chỉ là text thường")]
    [InlineData("example.com")]
    public void Chan_text_khong_phai_url(string? text)
        => Assert.False(LinkValidator.IsOpenableUrl(text));
}
