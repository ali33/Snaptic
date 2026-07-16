using Snaptic.Core.Display;

namespace Snaptic.Core.Tests;

public class DpiMapperTests
{
    // Màn chính: gốc (0,0), 2560x1440 physical, scaling 150%
    private static readonly MonitorInfo Primary =
        new("primary", new PhysicalRect(0, 0, 2560, 1440), 1.5);

    // Màn phụ: nằm bên phải màn chính, gốc physical (2560,0), 1920x1080, scaling 100%
    private static readonly MonitorInfo Secondary =
        new("secondary", new PhysicalRect(2560, 0, 1920, 1080), 1.0);

    [Theory]
    [InlineData(1.0, 100, 100, 100, 100)]
    [InlineData(1.25, 100, 100, 125, 125)]
    [InlineData(1.5, 100, 100, 150, 150)]
    [InlineData(2.0, 100, 100, 200, 200)]
    public void Quy_doi_theo_he_so_scaling(
        double scale, double lx, double ly, int expectedX, int expectedY)
    {
        var monitor = new MonitorInfo("m", new PhysicalRect(0, 0, 3840, 2160), scale);
        var p = DpiMapper.ToGlobalPhysical(monitor, lx, ly);
        Assert.Equal(new PhysicalPoint(expectedX, expectedY), p);
    }

    [Fact]
    public void Cong_them_goc_physical_cua_monitor()
    {
        // Điểm local (50,50) trên màn phụ scaling 1.0 → global (2560+50, 0+50)
        var p = DpiMapper.ToGlobalPhysical(Secondary, 50, 50);
        Assert.Equal(new PhysicalPoint(2610, 50), p);
    }

    [Fact]
    public void Lam_tron_thay_vi_cat_cut()
    {
        var monitor = new MonitorInfo("m", new PhysicalRect(0, 0, 1000, 1000), 1.5);
        // 33 * 1.5 = 49.5 → làm tròn 50, không phải cắt cụt thành 49
        var p = DpiMapper.ToGlobalPhysical(monitor, 33, 33);
        Assert.Equal(new PhysicalPoint(50, 50), p);
    }

    [Fact]
    public void Vung_chon_vat_ngang_2_monitor_khac_scaling()
    {
        // Đây là ca mà "một hệ số chung cho cả rect" sẽ SAI.
        // Góc đầu trên màn chính (scaling 1.5), local (100,100) → global (150,150)
        var a = DpiMapper.ToGlobalPhysical(Primary, 100, 100);
        // Góc sau trên màn phụ (scaling 1.0), local (50,50) → global (2610,50)
        var b = DpiMapper.ToGlobalPhysical(Secondary, 50, 50);

        var rect = DpiMapper.RectFrom(a, b);

        Assert.Equal(new PhysicalRect(150, 50, 2460, 100), rect);
    }

    [Fact]
    public void RectFrom_chuan_hoa_khi_keo_nguoc_tu_duoi_phai_len_tren_trai()
    {
        var rect = DpiMapper.RectFrom(new PhysicalPoint(300, 200), new PhysicalPoint(100, 50));
        Assert.Equal(new PhysicalRect(100, 50, 200, 150), rect);
    }

    [Fact]
    public void RectFrom_hai_diem_trung_nhau_cho_rect_rong()
    {
        var rect = DpiMapper.RectFrom(new PhysicalPoint(10, 10), new PhysicalPoint(10, 10));
        Assert.Equal(new PhysicalRect(10, 10, 0, 0), rect);
    }
}
