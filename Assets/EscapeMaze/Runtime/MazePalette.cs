using UnityEngine;

namespace EscapeMaze
{
    public static class MazePalette
    {
        public static readonly Color Ink = new Color(.025f, .043f, .06f, .96f);
        public static readonly Color Muted = new Color(.48f, .61f, .66f);
        public static readonly Color Paper = new Color(.89f, .95f, .93f);
        public static readonly Color Mint = new Color(.4f, .92f, .76f);
        public static readonly Color[] Keys = {
            new Color(1f, .67f, .22f), new Color(.28f, .74f, 1f), new Color(1f, .38f, .35f)
        };
        public static readonly string[] KeyNames = { "AMBER", "SKY", "CORAL" };
    }
}
