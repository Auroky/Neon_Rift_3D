using Godot;
using System;
using System.Collections.Generic;

// No collision shapes or damage: all pieces live briefly then free with their root.
public partial class TankExplosion3D : Node3D
{
    private readonly List<(MeshInstance3D mesh, StandardMaterial3D material, Vector3 velocity)> _bits = new();
    private MeshInstance3D _core = null!, _ring = null!;
    private StandardMaterial3D _coreMat = null!, _ringMat = null!;
    private float _age, _size;
    public static void Spawn(Node3D actor, Color accent, bool heavy)
    {
        TankExplosion3D effect = new() { Name = "TankExplosion", _size = heavy ? 1.25f : .85f };
        actor.GetParent().AddChild(effect);
        effect.GlobalPosition = actor.GlobalPosition;
        effect.Build(accent);
    }
    private StandardMaterial3D Material(Color color)
        => new() { AlbedoColor = color, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha, CullMode = BaseMaterial3D.CullModeEnum.Disabled };
    private MeshInstance3D Piece(Mesh mesh, StandardMaterial3D material, Vector3 position)
    {
        MeshInstance3D piece = new() { Mesh=mesh, MaterialOverride=material, Position=position,
            CastShadow=GeometryInstance3D.ShadowCastingSetting.Off };
        AddChild(piece); return piece;
    }
    private void Build(Color accent)
    {
        _coreMat=Material(new Color("ffe9ad"));
        _core=Piece(new SphereMesh { Radius=.6f*_size, Height=1.2f*_size, RadialSegments=12, Rings=6 },_coreMat,new Vector3(0,.35f,0));
        _ringMat=Material(new Color("ffc878"));
        _ring=Piece(new TorusMesh { InnerRadius=.76f, OuterRadius=.88f, Rings=32, RingSegments=6 },_ringMat,new Vector3(0,-.3f,0));
        for (int i=0;i<14;i++)
        {
            float angle=i*Mathf.Tau/14;
            float speed=(1.8f+(i%4)*.65f)*_size;
            Color color=i%3==0 ? accent : new Color("ffc878");
            StandardMaterial3D material=Material(color);
            float length=i%3==0 ? .17f : .08f;
            MeshInstance3D bit=Piece(new BoxMesh { Size=new Vector3(length,length,length*2) },material,new Vector3(0,.35f,0));
            bit.Rotation=new Vector3(angle,angle*.6f,angle*.8f);
            _bits.Add((bit,material,new Vector3(Mathf.Cos(angle)*speed,1.6f+(i%3)*.7f,Mathf.Sin(angle)*speed)));
        }
        for (int i=0;i<5;i++)
        {
            StandardMaterial3D smoke=Material(new Color(.25f,.19f,.3f,.65f));
            MeshInstance3D cloud=Piece(new SphereMesh { Radius=.25f*_size,Height=.5f*_size,RadialSegments=8,Rings=4 },smoke,new Vector3(0,.3f,0));
            _bits.Add((cloud,smoke,new Vector3(Mathf.Cos(i*1.4f)*.7f,1.2f+i*.12f,Mathf.Sin(i*1.4f)*.7f)));
        }
    }
    public override void _Process(double delta)
    {
        _age+=(float)delta;
        if (_age>=1.15f) { QueueFree(); return; }
        float burst=Math.Clamp(_age/.28f,0,1);
        _core.Scale=Vector3.One*(.4f+burst*1.5f);
        _coreMat.AlbedoColor=new Color(_coreMat.AlbedoColor,Math.Max(0,1-_age/.3f));
        _ring.Scale=Vector3.One*(.25f+_age*3.8f)*_size;
        _ringMat.AlbedoColor=new Color(_ringMat.AlbedoColor,Math.Max(0,1-_age/.55f));
        for (int i=0;i<_bits.Count;i++)
        {
            var bit=_bits[i];
            bool smoke=i>=14;
            bit.mesh.Position=new Vector3(0,.35f,0)+bit.velocity*_age;
            if (!smoke) bit.mesh.Position-=Vector3.Up*2.6f*_age*_age;
            bit.mesh.RotateY((float)delta*(smoke?.2f:4));
            bit.mesh.Scale=Vector3.One*(smoke ? 1+_age*3 : Math.Max(.15f,1-_age*.6f));
            float opacity=(1-_age/1.15f)*(smoke ? .6f*Math.Clamp((_age-.1f)*5,0,1) : 1);
            bit.material.AlbedoColor=new Color(bit.material.AlbedoColor,Math.Max(0,opacity));
        }
    }
}
