using UnityEngine;

namespace EscapeMaze
{
    // A replaceable desktop view: no UI packages, external fonts, textures, or device input.
    public sealed class MazeHud : MonoBehaviour
    {
        [SerializeField] private MazeSession session;
        [SerializeField] private DesktopMazeInput input;
        [SerializeField] private MazeInteractor interactor;
        [SerializeField] private MazeSurvey survey;
        [SerializeField] private MazeSentinel enemy;
        public bool VictoryVisible { get { return session.Progress.HasWon; } }
        public bool CaughtVisible { get { return session.Progress.WasCaught; } }
        private GUIStyle micro, small, text, subtitle, title, button;
        private static readonly Color Rule = new Color(.23f, .34f, .38f, .8f);
        private static readonly string[] Directions = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

        private void InitializeStyles()
        {
            if (micro != null) return;
            micro = Style(11, MazePalette.Muted);
            small = Style(14, MazePalette.Muted);
            text = Style(18, MazePalette.Paper);
            subtitle = Style(25, MazePalette.Paper);
            title = Style(58, MazePalette.Paper);
            button = Style(16, MazePalette.Ink);
            button.alignment = TextAnchor.MiddleCenter;
        }

        private static GUIStyle Style(int size, Color color)
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = size, wordWrap = false, padding = new RectOffset() };
            style.normal.textColor = color;
            return style;
        }

        private static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void Panel(Rect rect)
        {
            Fill(rect, MazePalette.Ink);
            Fill(new Rect(rect.x, rect.y, rect.width, 1), Rule);
            Fill(new Rect(rect.x, rect.y + rect.height - 1, rect.width, 1), Rule);
        }

        private void Label(float x, float y, float w, string value, GUIStyle style)
        {
            GUI.Label(new Rect(x, y, w, style.fontSize + 14), value, style);
        }

        private bool ActionButton(Rect rect, string label)
        {
            Fill(rect, rect.Contains(Event.current.mousePosition) ? MazePalette.Paper : MazePalette.Mint);
            GUI.Label(rect, label, button);
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        private void OnGUI()
        {
            InitializeStyles();
            Matrix4x4 previous = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280f * scale) * .5f,
                (Screen.height - 720f * scale) * .5f, 0f), Quaternion.identity, Vector3.one * scale);
            DrawHeader();
            if (VictoryVisible) DrawVictory();
            else if (CaughtVisible) DrawCaught();
            else if (!input.IsActive) DrawWelcome();
            else DrawPlaying();
            GUI.matrix = previous;
        }

        private void DrawHeader()
        {
            Panel(new Rect(28, 24, 280, 76));
            Fill(new Rect(28, 24, 3, 76), MazePalette.Mint);
            Label(48, 34, 250, "L U M E N   /   E S C A P E", text);
            Label(49, 66, 245, "RECOVER THE SIGNAL. FIND THE WAY OUT.", micro);
            for (int i = 0; i < 3; i++)
            {
                float x = 370 + 166 * i;
                bool recovered = session.Progress.HasKey(i);
                Panel(new Rect(x, 24, 154, 76));
                Fill(new Rect(x, 98, 154, 2), recovered ? MazePalette.Keys[i] : Rule);
                Label(x + 14, 34, 36, (i + 1).ToString("00"), subtitle);
                Label(x + 56, 38, 90, MazePalette.KeyNames[i], small);
                Label(x + 56, 64, 90, recovered ? "RECOVERED" : "MISSING", micro);
            }
            Panel(new Rect(1020, 24, 232, 76));
            Label(1040, 35, 190, Clock() + "   /   " + Compass(), text);
            Label(1040, 66, 190, "M   DISCOVERED MAP", micro);
            Panel(new Rect(872, 24, 132, 76));
            bool alert = enemy.IsAlert && !session.Progress.IsFinished;
            Fill(new Rect(872, 98, 132, 2), alert ? MazePalette.Keys[2] : Rule);
            Label(886, 38, 110, "WARDEN", small);
            string status = session.Progress.HasWon ? "OFFLINE" : session.Progress.WasCaught ? "CAPTURED"
                : enemy.GraceRemaining > 0f ? "STANDBY" : enemy.State.ToString().ToUpperInvariant();
            Label(886, 66, 110, status, micro);
        }

        private string Clock()
        {
            int seconds = Mathf.FloorToInt(session.ElapsedSeconds);
            return (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        }

        private string Compass() { return Directions[Mathf.RoundToInt(survey.Heading / 45f) % 8]; }

        private void DrawPlaying()
        {
            bool focused = interactor.Focus != null;
            Color accent = focused ? interactor.Focus.Accent : MazePalette.Paper;
            float gap = focused ? 8 : 4;
            Fill(new Rect(639, 359, 2, 2), accent);
            Fill(new Rect(640 - gap - 6, 359, 6, 2), accent);
            Fill(new Rect(640 + gap, 359, 6, 2), accent);
            Fill(new Rect(639, 360 - gap - 6, 2, 6), accent);
            Fill(new Rect(639, 360 + gap, 2, 6), accent);
            if (focused)
            {
                Panel(new Rect(390, 551, 500, 68));
                Fill(new Rect(390, 551, 3, 68), accent);
                Fill(new Rect(408, 568, 34, 34), accent);
                GUI.Label(new Rect(408, 568, 34, 34), "E", button);
                Label(458, 560, 420, interactor.Prompt, text);
                Label(458, 588, 420, "OR LEFT CLICK TO INTERACT", micro);
            }
            if (session.MessageAge < 6f)
            {
                Panel(new Rect(28, 586, 340, 54));
                var toast = new GUIStyle(small) { wordWrap = true };
                GUI.Label(new Rect(44, 596, 308, 42), session.Message, toast);
            }
            Label(30, 675, 700, "WASD  MOVE     /     MOUSE  LOOK     /     E  INTERACT     /     ESC  PAUSE", micro);
            Label(1070, 675, 182, "SIGNALS  " + session.Progress.KeyCount + " / 3", small);
            if (input.MapVisible) DrawMap();
            if (enemy.IsAlert)
            {
                Panel(new Rect(390, 120, 500, 44));
                Fill(new Rect(390, 120, 3, 44), MazePalette.Keys[2]);
                Label(408, 131, 470, enemy.PlayerVisible ? "WARDEN PURSUIT  /  BREAK LINE OF SIGHT" : "WARDEN SEARCHING  /  KEEP MOVING", small);
            }
        }

        private void DrawMap()
        {
            const float left = 1020, top = 116, size = 15;
            Panel(new Rect(left, top, 232, 245));
            Label(left + 16, top + 14, 200, "LOCAL SURVEY", small);
            MazeLayout layout = survey.Layout;
            for (int row = 0; row < layout.Height; row++)
                for (int x = 0; x < layout.Width; x++)
                {
                    var cell = new MazeCell(x, row);
                    if (!survey.Exploration.IsDiscovered(cell)) continue;
                    char token = layout.At(cell);
                    Color color = token == '#' ? Rule : new Color(.18f, .28f, .31f);
                    if (token >= 'a' && token <= 'c' && !session.Progress.HasKey(token - 'a')) color = MazePalette.Keys[token - 'a'];
                    if (token == 'D' || token == 'E') color = MazePalette.Mint;
                    if (cell.Equals(survey.PlayerCell)) color = MazePalette.Paper;
                    Fill(new Rect(left + 18 + x * size, top + 48 + row * size, size - 2, size - 2), color);
                }
            Label(left + 16, top + 219, 210, "ONLY EXPLORED AREAS ARE SHOWN", micro);
        }

        private void DrawWelcome()
        {
            Fill(new Rect(0, 114, 1280, 550), new Color(.01f, .025f, .04f, .35f));
            Panel(new Rect(74, 169, 680, 443));
            Label(110, 197, 600, "EXPLORATION  /  THREE SIGNALS  /  ONE EXIT", small);
            Label(106, 245, 620, "THE WAY OUT", title);
            Fill(new Rect(110, 328, 96, 3), MazePalette.Mint);
            Label(110, 355, 600, "Three keys. One sealed door. Your way home.", subtitle);
            Label(110, 401, 600, "Follow the coloured alcoves. Recover each key.", text);
            Label(110, 431, 600, "Unlock the brass seal, then step into the green exit.", text);
            Label(110, 465, 600, "THE WARDEN PATROLS. BREAK SIGHT AT CORNERS. YOU ARE FASTER.", micro);
            Label(110, 494, 600, "CLICK THE GAME VIEW OR PRESS ENTER TO BEGIN / RESUME", small);
            Label(110, 538, 600, "WASD  MOVE     MOUSE  LOOK     E  USE     M  MAP     ESC  PAUSE", micro);
        }

        private void DrawVictory()
        {
            Fill(new Rect(0, 114, 1280, 550), new Color(.01f, .025f, .04f, .45f));
            Panel(new Rect(300, 177, 680, 430));
            Fill(new Rect(300, 177, 680, 3), MazePalette.Mint);
            Label(342, 208, 600, "ALL SIGNALS RESTORED  /  EXIT RELEASED", small);
            Label(336, 255, 620, "YOU ESCAPED.", title);
            Label(342, 341, 590, "The vault is behind you. The way ahead is yours.", text);
            Fill(new Rect(342, 393, 596, 1), Rule);
            Label(342, 416, 250, "ACTIVE TIME", micro);
            Label(342, 444, 250, Clock(), subtitle);
            Label(610, 416, 300, "KEYS RECOVERED", micro);
            Label(610, 444, 300, "03 / 03", subtitle);
            if (ActionButton(new Rect(342, 517, 596, 48), "EXPLORE AGAIN    →")) session.Restart();
        }

        private void DrawCaught()
        {
            Fill(new Rect(0, 114, 1280, 550), new Color(.035f, .01f, .025f, .55f));
            Panel(new Rect(300, 177, 680, 430));
            Fill(new Rect(300, 177, 680, 3), MazePalette.Keys[2]);
            Label(342, 208, 600, "THE WARDEN FOUND YOU", small);
            Label(336, 255, 620, "CAUGHT.", title);
            Label(342, 341, 590, "Break sight at corners. You can outrun the Warden.", text);
            Fill(new Rect(342, 393, 596, 1), Rule);
            Label(342, 416, 250, "ACTIVE TIME", micro);
            Label(342, 444, 250, Clock(), subtitle);
            Label(610, 416, 300, "KEYS RECOVERED", micro);
            Label(610, 444, 300, session.Progress.KeyCount.ToString("00") + " / 03", subtitle);
            if (ActionButton(new Rect(342, 517, 596, 48), "TRY AGAIN    →")) session.Restart();
        }
    }
}
