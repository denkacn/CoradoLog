using UnityEditor;
using UnityEngine;

namespace CoradoLog.Editor
{
    [CustomEditor(typeof(CoLoggerInitiator))]
    public class CoLoggerInitiatorInspector : UnityEditor.Editor
    {
        private int _selectedIndex;
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            DrawVarsGenerator();
            DrawSenderGenerator();
        }

        private void DrawVarsGenerator()
        {
            GUILayout.Space(10);
            GUILayout.Label("------------------------------");
            GUILayout.Space(10);
            
            if (GUILayout.Button("Generate Vars"))
            {
                var initiator = target as CoLoggerInitiator;
                if (initiator == null) return;

                var generator = new CoLoggerVarsGenerator();
                generator.GenerateVarsClass(initiator.GetSenderNames().ToArray(),
                    initiator.GetSeContextNames().ToArray(), initiator.GeneratePath);
            } 
        }

        private void DrawSenderGenerator()
        {
            GUILayout.Space(10);
            GUILayout.Label("------------------------------");
            GUILayout.Space(10);
            
            var initiator = target as CoLoggerInitiator;
            if (initiator == null) return;

            var names = initiator.GetSenderNames().ToArray();
            if (names.Length == 0)
            {
                EditorGUILayout.HelpBox("No senders available for generation.", MessageType.Info);
                return;
            }

            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, names.Length - 1);
            _selectedIndex = EditorGUILayout.Popup("Select Name: ", _selectedIndex, names);
            
            if (GUILayout.Button("Generate Sender"))
            {
                var selectedName = names[_selectedIndex];
                if (string.IsNullOrWhiteSpace(selectedName))
                {
                    EditorGUILayout.HelpBox("Selected sender name is empty.", MessageType.Warning);
                    return;
                }

                var generator = new CoLoggerSenderAccessGenerator();
                generator.GenerateSenderClass(selectedName, initiator.GeneratePath);
            } 
        }
    }
}
