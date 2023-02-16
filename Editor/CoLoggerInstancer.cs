using UnityEditor;
using UnityEngine;

namespace CoradoLog.Editor
{
    public class CoLoggerInstancer 
    {
        [MenuItem("CoLogger/Instantiate CoLogger To Scene")]
        private static void NewMenuOption()
        {
            var coLoggerObj = Resources.Load("CoLoggerInitializer");
            var instObject = GameObject.Instantiate(coLoggerObj);
            instObject.name = "CoLoggerInitializer";
        }
    }
}
