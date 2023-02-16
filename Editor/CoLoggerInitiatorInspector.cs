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
                var initiator = (target as CoLoggerInitiator);
                var generator = new CoLoggerVarsGenerator();
                generator.GenerateVarsClass(initiator.GetSenderNames().ToArray(),
                    initiator.GetSeContextNames().ToArray());
            } 
        }

        private void DrawSenderGenerator()
        {
            GUILayout.Space(10);
            GUILayout.Label("------------------------------");
            GUILayout.Space(10);
            
            var initiator = (target as CoLoggerInitiator);
            if (initiator != null)
            {
                var names = initiator.GetSenderNames().ToArray();
                _selectedIndex = EditorGUILayout.Popup("Select Name: ", _selectedIndex, names);
                
                if (GUILayout.Button("Generate Sender"))
                {
                    var selectedName = names[_selectedIndex];

                    var generator = new CoLoggerSenderAccessGenerator();
                    generator.GenerateSenderClass(selectedName);
                } 
            }
        }
    }
}
