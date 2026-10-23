using UnityEngine;

namespace EscapeMaze
{
    public sealed class KeyPickup : MazeInteractable
    {
        [SerializeField] private int keyId;
        [SerializeField] private Transform visual;
        [SerializeField] private Renderer floorIndicator;
        [SerializeField] private Material spentMaterial;
        public int KeyId { get { return keyId; } }
        public override Color Accent { get { return MazePalette.Keys[keyId]; } }

        private void Update()
        {
            visual.Rotate(0f, 50f * Time.deltaTime, 0f, Space.Self);
            visual.localPosition = Vector3.up * (Mathf.Sin(Time.time * 2f + keyId) * 0.08f);
            visual.localScale = Vector3.Lerp(visual.localScale, Vector3.one * (IsFocused ? 1.14f : 1f), 8f * Time.deltaTime);
        }

        public override string Prompt(MazeSession session) { return "Recover " + MazePalette.KeyNames[keyId].ToLowerInvariant() + " key"; }

        public override void Use(MazeSession session)
        {
            if (!session.CollectKey(keyId)) return;
            floorIndicator.sharedMaterial = spentMaterial;
            gameObject.SetActive(false);
        }
    }
}
