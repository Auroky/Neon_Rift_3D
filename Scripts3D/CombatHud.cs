using Godot;
using System;
using System.Collections.Generic;

// Presentation only: receives snapshots from Game3D; owns no game rules or saves.
public partial class CombatHud : CanvasLayer
{
    public Label Stats { get; private set; } = null!;
    public Label Result { get; private set; } = null!;
    public event Action RestartRequested;
    private Dashboard _dashboard = null!;

    public override void _Ready()
    {
        Layer = 10;
        // Preserve the existing diagnostic labels and their order for regression tools.
        Stats = new Label { Name = "Stats", Visible = false };
        AddChild(Stats);
        AddChild(new Label { Name = "Controls", Visible = false });
        Result = new Label { Name = "Result", Visible = false };
        AddChild(Result);
        _dashboard = new Dashboard { Name = "Dashboard", MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(_dashboard);
        _dashboard.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _dashboard.Replay.Pressed += () => RestartRequested?.Invoke();
    }

    public void UpdateStats(double seconds, float health, int kills, int score)
    {
        _dashboard.Seconds = seconds;
        _dashboard.Health = health;
        _dashboard.Kills = kills;
        _dashboard.Score = score;
        _dashboard.QueueRedraw();
    }

    public void HideResults() { _dashboard.Round = null; _dashboard.Replay.Hide(); _dashboard.QueueRedraw(); }
    public void ShowResults(RoundScore round, int rank, RoundScore[] leaders, bool saved)
    {
        _dashboard.Round = round; _dashboard.Rank = rank;
        _dashboard.Leaders = leaders; _dashboard.Saved = saved;
        _dashboard.Replay.Show(); _dashboard.QueueRedraw();
    }

    public partial class Dashboard : Control
    {
        public double Seconds;
        public float Health;
        public int Kills, Score, Rank;
        public bool Saved;
        public RoundScore Round;
        public RoundScore[] Leaders = Array.Empty<RoundScore>();
        public Button Replay { get; } = new() { Text = "PLAY AGAIN   /   R", Visible = false };
        public static readonly Color Ink = new("241e38"), Muted = new("b3a9c4"), Cream = new("fff0d7");
        public static readonly Color Mint = new("76e0c3"), Gold = new("ffc878"), Coral = new("ff887f"), Violet = new("c39df8");
        private Font _font = null!;
        private static readonly Dictionary<(Color,Color,int),StyleBoxFlat> Boxes = new();
        public override void _Ready()
        {
            _font = ThemeDB.FallbackFont;
            AddChild(Replay);
            Replay.MouseFilter = MouseFilterEnum.Stop;
            Replay.MouseDefaultCursorShape = CursorShape.PointingHand;
            Replay.AddThemeFontSizeOverride("font_size", 18);
            Replay.AddThemeColorOverride("font_color", Ink);
            Replay.AddThemeColorOverride("font_hover_color", Ink);
            Replay.AddThemeColorOverride("font_pressed_color", Ink);
            Replay.AddThemeStyleboxOverride("normal", Box(Gold, Gold, 10));
            Replay.AddThemeStyleboxOverride("hover", Box(new Color("ffe0ac"), Cream, 10));
            Replay.AddThemeStyleboxOverride("pressed", Box(new Color("e5a854"), Gold, 10));
            Replay.AddThemeStyleboxOverride("focus", Box(new Color(0,0,0,0), Cream, 10));
            Resized += QueueRedraw;
        }

        public static StyleBoxFlat Box(Color fill, Color border, int radius = 12)
        {
            var key=(fill,border,radius);
            if (Boxes.TryGetValue(key,out StyleBoxFlat existing)) return existing;
            StyleBoxFlat style = new() { BgColor = fill, BorderColor = border,
                BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
                CornerRadiusTopLeft = radius, CornerRadiusTopRight = radius,
                CornerRadiusBottomLeft = radius, CornerRadiusBottomRight = radius };
            Boxes[key]=style;
            return style;
        }
        private void Panel(Rect2 rect, Color accent, bool solid = false)
        {
            DrawStyleBox(Box(new Color(Ink, solid ? 1f : .94f), new Color(accent, .32f)), rect);
            DrawStyleBox(Box(accent, accent, 2), new Rect2(rect.Position.X + 17, rect.Position.Y + 17, 4, 28));
        }
        private void Text(string text, float x, float y, int size, Color color)
            => DrawString(_font, new Vector2(x, y), text, HorizontalAlignment.Left, -1, size, color);
        private void Right(string text, float x, float y, int size, Color color)
            => Text(text, x - _font.GetStringSize(text, fontSize: size).X, y, size, color);
        private void Track(float x, float y, float width, float fraction, Color color, float height = 5)
        {
            DrawStyleBox(Box(new Color("494058"), new Color("494058"), 2), new Rect2(x,y,width,height));
            if (fraction > 0) DrawStyleBox(Box(color,color,2), new Rect2(x,y,Math.Max(2,width*Math.Clamp(fraction,0,1)),height));
        }
        private void Tank(float x, float y, Color color)
        {
            DrawStyleBox(Box(new Color(color,.2f), color, 3),new Rect2(x,y,29,19));
            DrawRect(new Rect2(x+7,y+4,15,10),color);
            DrawLine(new Vector2(x+14,y+9),new Vector2(x+36,y+9),color,3,true);
            DrawLine(new Vector2(x+3,y-3),new Vector2(x+3,y+22),color,3,true);
            DrawLine(new Vector2(x+26,y-3),new Vector2(x+26,y+22),color,3,true);
        }
        public override void _Draw()
        {
            Vector2 scale = Size / new Vector2(1280,720);
            DrawSetTransform(Vector2.Zero,0,scale);
            int seconds = (int)Math.Ceiling(Seconds);
            Color healthColor = Health <= 35 ? Coral : Mint;
            Color timeColor = Seconds <= 10 ? Coral : Gold;
            Panel(new Rect2(24,24,285,96),Violet);
            Tank(53,62,Gold);
            Text("NEON RIFT",105,67,27,Cream);
            Text("MOTOR DEPOT  /  SCORE ATTACK",105,90,10,Muted);
            Panel(new Rect2(321,24,225,96),healthColor);
            Text("HULL INTEGRITY",355,52,11,Muted);
            Text($"{Health:0}",355,87,29,healthColor); Text("/ 100",414,86,13,Muted);
            Track(355,101,165,Health/100,healthColor);
            Panel(new Rect2(558,24,190,96),timeColor);
            Text("ROUND REMAINING",592,52,11,Muted);
            Text($"{seconds/60:00}:{seconds%60:00}",592,87,30,timeColor);
            Track(592,101,132,(float)(Seconds/60),timeColor);
            Panel(new Rect2(760,24,162,96),Coral);
            Text("ELIMINATIONS",794,52,11,Muted); Text($"{Kills:00}",794,92,32,Cream);
            Panel(new Rect2(934,24,322,96),Gold);
            Text("TOTAL SCORE",968,52,11,Muted); Text($"{Score:N0}",968,92,34,Gold);
            Right("PTS",1233,90,12,Muted);

            DrawStyleBox(Box(new Color(Ink,.9f),new Color(Violet,.3f),12),new Rect2(24,650,680,46));
            Tank(43,663,Coral); Text("CHASER",88,670,10,Muted); Text("100 PTS",88,686,12,Coral);
            DrawLine(new Vector2(230,661),new Vector2(230,686),new Color(Muted,.25f));
            Tank(251,663,Violet); Text("STRIKER",296,670,10,Muted); Text("180 PTS",296,686,12,Violet);
            DrawLine(new Vector2(449,661),new Vector2(449,686),new Color(Muted,.25f));
            Tank(470,663,new Color("a6c1d8")); Text("HEAVY",515,670,10,Muted); Text("300 PTS",515,686,12,new Color("a6c1d8"));
            DrawStyleBox(Box(new Color(Ink,.9f),new Color(Mint,.3f),12),new Rect2(716,650,540,46));
            Text("W A S D",737,679,13,Mint);
            Text("MOVE    /    MOUSE AIM    /    HOLD LMB FIRE",821,679,12,Cream);

            if (Round == null) return;
            DrawRect(new Rect2(0,0,1280,720),new Color(.07f,.045f,.12f,.76f));
            Panel(new Rect2(322,139,636,548),Gold,true);
            Text(Round.Result == "TIME UP" ? "ROUND COMPLETE" : "TANK DESTROYED",358,178,13,Gold);
            Right("60 SECOND CHALLENGE",922,178,11,Muted);
            Text($"{Round.Score:N0}",356,244,54,Cream);
            Text("TOTAL POINTS",359,266,11,Muted);
            Right($"PERSONAL RANK   #{Rank}",921,228,18,Gold);
            Right($"{Round.Kills} ELIMINATIONS   /   {Round.SecondsPlayed:0.0}s",921,257,12,Muted);
            DrawLine(new Vector2(357,287),new Vector2(923,287),new Color(Violet,.25f));
            Text("PERSONAL TOP 5",357,316,15,Cream);
            Right("SCORE       KILLS      DATE / TIME",922,316,10,Muted);
            for (int i=0;i<5;i++)
            {
                float y=330+i*44;
                RoundScore leader = i<Leaders.Length ? Leaders[i] : null;
                bool current = leader?.Id == Round.Id;
                Color accent = i==0 ? Gold : (current ? Mint : Muted);
                DrawStyleBox(Box(current ? new Color("34443f") : new Color("30283f"),new Color(accent,current?.5f:.12f),7),new Rect2(357,y,566,38));
                DrawStyleBox(Box(new Color(accent,.18f),new Color(accent,.35f),5),new Rect2(365,y+6,27,26));
                Text($"{i+1}",374,y+25,13,accent);
                Text(leader==null ? "EMPTY SLOT" : current ? "THIS RUN" : "PREVIOUS RUN",406,y+24,11,current ? Mint : Muted);
                Right(leader==null ? "—" : $"{leader.Score:N0}",668,y+25,18,accent);
                Right(leader==null ? "—" : $"{leader.Kills}",743,y+25,12,Muted);
                Right(leader==null ? "—" : leader.PlayedAt.ToString("MM/dd  HH:mm"),909,y+25,12,Muted);
            }
            Text(Saved ? "LOCAL RECORD SAVED" : "SAVE UNAVAILABLE / SESSION ONLY",357,579,10,Saved ? Mint : Coral);
            Right("YOUR BEST FIVE RUNS",922,579,10,Muted);
            Replay.Position = new Vector2(357,603)*scale;
            Replay.Size = new Vector2(566,49)*scale;
            Replay.AddThemeFontSizeOverride("font_size",(int)(18*scale.Y));
        }
    }
}
