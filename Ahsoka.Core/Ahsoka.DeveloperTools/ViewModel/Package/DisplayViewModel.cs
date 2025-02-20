using Ahsoka.Core.Hardware;
using Ahsoka.Core.Utility;
using Ahsoka.Services.LayerManager;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Ahsoka.DeveloperTools;

internal class DisplayViewModel : ChildViewModelBase<PackageViewModel>
{
    LayerManagerConfiguration configuration;
    SurfaceSetupViewModel selectedSurfaceSetup;
    Bitmap bitmap;
    float defaultHeight = 200;
    float simWidth = 300;
    float simHeight = 200;
    HardwareInfo hardwareDef;

    public ObservableCollection<SurfaceSetupViewModel> Surfaces
    {
        get;
        set;
    } = new ObservableCollection<SurfaceSetupViewModel>();

    public SurfaceSetupViewModel SelectedSurface
    {
        get { return selectedSurfaceSetup; }
        set
        {
            selectedSurfaceSetup = value;
            foreach (var surface in Surfaces)
                surface.SimColor = value == surface ? SurfaceSetupViewModel.SelectedBrush : SurfaceSetupViewModel.DeselectedBrush;
            OnPropertyChanged();
        }
    }

    public Orientation SystemOrientation
    {
        get { return configuration.Orientation.IntToEnum<Orientation>(); }
        set { configuration.Orientation = value.EnumToInt(); OnPropertyChanged(); }
    }

    public string ScreenDescription
    {
        get
        {
            if (hardwareDef != null)
                return $"{hardwareDef.ScreenInfo.ScreenWidth}x{hardwareDef.ScreenInfo.ScreenHeight}";
            else
                return "Un;nown";
        }
        set
        { }
    }


    #region Logo
    public string StartupLogoPath
    {
        get { return configuration.BootLogoPath; }
        set
        {
            configuration.BootLogoPath = value;
            OnPropertyChanged();
            LoadStartLogo();
        }
    }

    public bool ShowUnknownSurfaces
    {
        get { return configuration.ShowUnknownSurfaces; }
        set { configuration.ShowUnknownSurfaces = value; OnPropertyChanged(); }
    }

    public Bitmap StartupLogoImage
    {
        get { return bitmap; }
        private set { bitmap = value; OnPropertyChanged(); }
    }


    public float SimWidth
    {
        get { return simWidth; }
        private set { simWidth = value; OnPropertyChanged(); }
    }


    public float SimHeight
    {
        get { return simHeight; }
        private set { simHeight = value; OnPropertyChanged(); }
    }


    public Orientation[] Orientations
    {
        get
        {
            return Enum.GetValues<Orientation>().ToArray();
        }
    }

    public void LoadStartLogo()
    {
        try
        {
            string path = Path.GetFullPath(StartupLogoPath ?? "", Path.GetDirectoryName(ParentViewModel.PackageInfo.GetPackageInfoPath()));

            if (File.Exists(path))
            {
                var bytes = File.ReadAllBytes(path);
                Stream stream = new MemoryStream(bytes);
                var image = new Avalonia.Media.Imaging.Bitmap(stream);
                StartupLogoImage = image;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            StartupLogoImage = null;
        }
    }

    internal async void SetStartLogo()
    {
        string currentPath = Path.Combine(Path.GetDirectoryName(ParentViewModel.PackageInfo.GetPackageInfoPath()), StartupLogoPath ?? "");

        var path = await ParentViewModel.ParentViewModel.FileLocatorAsync("Pick Output Directory", "*", "Images", currentPath);
        if (path != null)
            StartupLogoPath = Path.GetRelativePath(Path.GetDirectoryName(ParentViewModel.PackageInfo.GetPackageInfoPath()), path);
    }
    #endregion


    public DisplayViewModel(PackageViewModel parentViewModel)
        : base(parentViewModel)
    {
        configuration = parentViewModel.PackageInfo.ScreenConfiguration;

        LoadStartLogo();
        LoadInfo();
        UpdateUI();
    }

    private void RefreshSize()
    {
        float ratio = (float)hardwareDef.ScreenInfo.ScreenWidth / (float)hardwareDef.ScreenInfo.ScreenHeight;
        float tempWidth = defaultHeight * ratio;

        if (tempWidth < 350) // Just handling Rounding
        {
            SimWidth = tempWidth;
            SimHeight = 200;
        }
        else
        {
            SimWidth = 350;
            ratio = (float)hardwareDef.ScreenInfo.ScreenHeight / (float)hardwareDef.ScreenInfo.ScreenWidth;
            SimHeight = (int)(simWidth * ratio);
        }
    }


    internal void UpdateUI()
    {
        hardwareDef = HardwareInfo.GetHardwareInfoByDescription(ParentViewModel.PlatformDescription);
        if (hardwareDef != null)
        {
            RefreshSize();
        }

        OnPropertyChanged(nameof(ScreenDescription));
    }


    internal void AddSurface()
    {
        int id = Surfaces.Count > 0 ? Surfaces.Max(x => x.SurfaceId) + 1 : 1001;
        var surface = new SurfaceSetup()
        {
            SurfaceId = id,
            SurfaceAppName = "NewApp-" + id,
            Visibility = new() { IsVisible = true, Opacity = 1.0f },
            LocationRectangle = new() { Height = hardwareDef.ScreenInfo.ScreenHeight, Width = hardwareDef.ScreenInfo.ScreenWidth, SurfaceId = 12121212 },
            SourceRectangle = new() { Height = hardwareDef.ScreenInfo.ScreenHeight, Width = hardwareDef.ScreenInfo.ScreenWidth, SurfaceId = 12121212 },

        };

        configuration.SurfaceSetup.Add(surface);
        Surfaces.Add(new SurfaceSetupViewModel(surface, this));
        SelectedSurface = Surfaces.Last();
    }

    internal void RemoveSurface()
    {
        if (selectedSurfaceSetup != null)
        {
            configuration.SurfaceSetup.Remove(selectedSurfaceSetup.SurfaceSetup);
            Surfaces.Remove(selectedSurfaceSetup);
            SelectedSurface = Surfaces.FirstOrDefault();
        }
    }


    private void LoadInfo()
    {
        foreach (var setup in configuration.SurfaceSetup)
            Surfaces.Add(new SurfaceSetupViewModel(setup, this));

        SelectedSurface = Surfaces.FirstOrDefault();
    }

    internal float GetYScale()
    {
        return hardwareDef.ScreenInfo.ScreenHeight / SimHeight;
    }

    internal float GetXScale()
    {
        return hardwareDef.ScreenInfo.ScreenWidth / SimWidth;
    }
}

internal class SurfaceSetupViewModel : ChildViewModelBase<DisplayViewModel>
{
    SurfaceSetup setup;
    IImmutableBrush color;
    internal static IImmutableBrush SelectedBrush = new SolidColorBrush(Colors.SteelBlue, .8f).ToImmutable();
    internal static IImmutableBrush DeselectedBrush = new SolidColorBrush(Colors.SteelBlue, .2f).ToImmutable();

    public int SurfaceId
    {
        get { return setup.SurfaceId; }
        set
        {
            setup.SurfaceId = value;
            OnPropertyChanged();
        }
    }

    public int SurfaceX
    {
        get { return setup.LocationRectangle.X; }
        set
        {
            setup.LocationRectangle.X = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SimX));
        }
    }

    public int SurfaceY
    {
        get { return setup.LocationRectangle.Y; }
        set
        {
            setup.LocationRectangle.Y = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SimY));
        }
    }

    public int SurfaceWidth
    {
        get { return setup.LocationRectangle.Width; }
        set
        {
            setup.LocationRectangle.Width = setup.SourceRectangle.Width = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SimWidth));
        }
    }

    public int SurfaceHeight
    {
        get { return setup.LocationRectangle.Height; }
        set
        {
            setup.LocationRectangle.Height = setup.SourceRectangle.Height = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SimHeight));
        }
    }


    public IImmutableBrush SimColor
    {
        get { return color; }
        set
        {
            color = value;
            OnPropertyChanged();
        }
    }

    public float SimX
    {
        get { return setup.LocationRectangle.X / ParentViewModel.GetXScale(); }
        set
        {
            OnPropertyChanged();
        }
    }

    public float SimY
    {
        get { return setup.LocationRectangle.Y / ParentViewModel.GetYScale(); }
        set
        {
            OnPropertyChanged();
        }
    }

    public float SimWidth
    {
        get { return setup.LocationRectangle.Width / ParentViewModel.GetXScale(); }
        set
        {
            OnPropertyChanged();
        }
    }

    public float SimHeight
    {
        get { return setup.LocationRectangle.Height / ParentViewModel.GetYScale(); }
        set
        {

            OnPropertyChanged();
        }
    }

    public string SurfaceName
    {
        get { return setup.SurfaceAppName; }
        set
        {
            setup.SurfaceAppName = value;
            OnPropertyChanged();
        }
    }

    public SurfaceSetup SurfaceSetup
    {
        get => setup;
    }

    public SurfaceSetupViewModel(SurfaceSetup setup, DisplayViewModel parent)
        : base(parent)
    {
        this.setup = setup;
    }
}
