namespace Doprez.Stride.DotRecast;

public enum NavMeshLayer
{
    None = 0,
    Layer1 = 0x1,
    Layer2 = 0x2,
    Layer3 = 0x4,
    Layer4 = 0x8,
    Layer5 = 0x10,
    Layer6 = 0x20,
    Layer7 = 0x40,
    Layer8 = 0x80,
    Layer9 = 0x100,
    Layer10 = 0x200,
    Layer11 = 0x400,
    Layer12 = 0x800,
    Layer13 = 0x1000,
    Layer14 = 0x2000,
    Layer15 = 0x4000,
    Layer16 = 0x8000,
}

[Flags]
public enum NavMeshLayerGroup
{
    None = 0,
    Layer1 = 0x1,
    Layer2 = 0x2,
    Layer3 = 0x4,
    Layer4 = 0x8,
    Layer5 = 0x10,
    Layer6 = 0x20,
    Layer7 = 0x40,
    Layer8 = 0x80,
    Layer9 = 0x100,
    Layer10 = 0x200,
    Layer11 = 0x400,
    Layer12 = 0x800,
    Layer13 = 0x1000,
    Layer14 = 0x2000,
    Layer15 = 0x4000,
    Layer16 = 0x8000,
    All = 0xFFFF,
}

