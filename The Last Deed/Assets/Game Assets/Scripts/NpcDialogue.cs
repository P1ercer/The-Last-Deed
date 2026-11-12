using System.Collections.Generic;
using UnityEngine;

namespace GameAssets.Scripts
{
    public class NpcDialogue : MonoBehaviour
    {
        [Header("NPC Information")]
        [SerializeField] private string npcName = "NPC";

        [Header("NPC Dialogue")]
        [SerializeField, TextArea(2, 5)]
        private List<string> dialogueLines =
            new List<string>();

        public string NpcName => npcName;

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

        public void SetNpcName(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                npcName = name;
            }
        }
    }
}