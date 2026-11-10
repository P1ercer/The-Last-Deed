using System.Collections.Generic;
using UnityEngine;

namespace GameAssets.Scripts
{
    public class NpcDialogue : MonoBehaviour
    {
        [Header("NPC Dialogue")]
        [SerializeField, TextArea(2, 5)]
        private List<string> dialogueLines =
            new List<string>();

        public int LineCount => dialogueLines.Count;

        public string GetLine(int lineIndex)
        {
            if (lineIndex < 0 ||
                lineIndex >= dialogueLines.Count)
            {
                return string.Empty;
            }

            return dialogueLines[lineIndex];
        }
    }
}