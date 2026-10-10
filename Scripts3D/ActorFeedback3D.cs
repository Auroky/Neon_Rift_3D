using Godot;
using System;
using System.Collections.Generic;

// Visual child only. Reads actor HP, projects a HUD bar, and flashes private materials.
public partial class ActorFeedback3D : Node3D
{
    private Node3D _actor = null!;
    private Func<float> _health = null!;
    private Func<float> _maximum = null!;
    private HealthBar _bar = null!;
    private readonly List<ShaderMaterial> _materials = new();
    private float _flash, _headHeight = 1.3f;
    public Color Accent { get; private set; }

    public static ActorFeedback3D Attach(Node3D actor, Func<float> health, Func<float> maximum, string title, Color accent)
    {
        ActorFeedback3D visual = new() { Name = "VisualFeedback", _actor = actor, _health = health,
            _maximum = maximum, Accent = accent };
        actor.AddChild(visual);
        visual._bar.Title = title;
        return visual;
    }
    public override void _Ready()
    {
        CanvasLayer layer = new() { Layer = 5 };
        AddChild(layer);
        _bar = new HealthBar { Name = "HealthBar", Accent = Accent, MouseFilter = Control.MouseFilterEnum.Ignore };
        layer.AddChild(_bar);
        PrepareMeshes(_actor.GetNodeOrNull<Node3D>("TankVisual"));
    }
    private void PrepareMeshes(Node node)
    {
        if (node == null) return;
        if (node is MeshInstance3D mesh && mesh.Mesh != null)
        {
            Aabb bounds = (_actor.GlobalTransform.AffineInverse()*mesh.GlobalTransform)*mesh.GetAabb();
            _headHeight = Math.Max(_headHeight, bounds.End.Y + .25f);
            for (int surface=0;surface<mesh.Mesh.GetSurfaceCount();surface++)
            {
                if (mesh.GetActiveMaterial(surface) is ShaderMaterial shared)
                {
                    ShaderMaterial material = (ShaderMaterial)shared.Duplicate();
                    mesh.SetSurfaceOverrideMaterial(surface,material);
                    _materials.Add(material);
                }
            }
        }
        foreach (Node child in node.GetChildren()) PrepareMeshes(child);
    }
    public void Hit() => _flash = 1f;
    public float ReadFraction() => _bar.Fraction;
    public void ResetVisuals() { _flash = 0; _bar.ResetTrail(); }
    public override void _Process(double delta)
    {
        float hp = _health(), max = _maximum();
        Camera3D camera = GetViewport().GetCamera3D();
        Vector3 head = _actor.GlobalPosition + Vector3.Up*_headHeight;
        _bar.Visible = camera != null && _actor.IsVisibleInTree() && hp>0 && max>0 && !camera.IsPositionBehind(head);
        if (_bar.Visible)
        {
            _bar.Position = camera.UnprojectPosition(head) - new Vector2(43,27);
            _bar.Fraction = Math.Clamp(hp/max,0,1);
            _bar.Advance((float)delta);
        }
        _flash = Math.Max(0,_flash-(float)delta*5);
        foreach (ShaderMaterial material in _materials) material.SetShaderParameter("hit_flash",_flash);
    }
    public partial class HealthBar : Control
    {
        public string Title = "";
        public Color Accent;
        public float Fraction = 1;
        private float _trail = 1;
        public void ResetTrail() { _trail=1; QueueRedraw(); }
        public void Advance(float delta) { _trail = Math.Max(Fraction,_trail-delta*.6f); QueueRedraw(); }
        public override void _Draw()
        {
            DrawStyleBox(CombatHud.Dashboard.Box(new Color("241e38"),new Color(Accent,.7f),4),new Rect2(0,15,86,12));
            DrawRect(new Rect2(3,18,80*_trail,6),new Color("fff0d7"));
            if (Fraction>0) DrawRect(new Rect2(3,18,80*Fraction,6),Accent);
            Font font = ThemeDB.FallbackFont;
            float width=font.GetStringSize(Title,fontSize:10).X;
            DrawStringOutline(font,new Vector2((86-width)/2,10),Title,HorizontalAlignment.Left,-1,10,3,new Color("241e38"));
            DrawString(font,new Vector2((86-width)/2,10),Title,HorizontalAlignment.Left,-1,10,new Color("fff0d7"));
        }
    }
}
