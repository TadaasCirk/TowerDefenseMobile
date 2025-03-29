using UnityEngine;
using UnityEditor;

namespace TowerDefense.Editor
{
    /// <summary>
    /// Editor tool to validate project configuration for mobile development
    /// Place this script in an Editor folder
    /// </summary>
    public class ProjectConfigValidator : EditorWindow
    {
        [MenuItem("Tower Defense/Validate Project Configuration")]
        public static void ValidateProjectConfiguration()
        {
            // Create and show window
            ProjectConfigValidator window = GetWindow<ProjectConfigValidator>("Project Validator");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Tower Defense Mobile Configuration Validator", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Validate Configuration"))
            {
                RunValidation();
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space();
            
            // Display validation results
            if (_validationRun)
            {
                DisplayResults();
            }
        }
        
        private bool _validationRun = false;
        private bool _orientationCorrect = false;
        private bool _androidTargetEnabled = false;
        private bool _qualitySettingsCorrect = false;
        private bool _folderStructureCorrect = false;
        
        private void RunValidation()
        {
            // Check project orientation
            _orientationCorrect = PlayerSettings.defaultInterfaceOrientation == UIOrientation.Portrait;
            
            // Check if Android build target is enabled
            _androidTargetEnabled = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android;
            
            // Check if we have multiple quality settings
            _qualitySettingsCorrect = QualitySettings.names.Length > 1;
            
            // Check basic folder structure
            _folderStructureCorrect = AssetDatabase.IsValidFolder("Assets/_Project") &&
                                      AssetDatabase.IsValidFolder("Assets/_Project/Scripts") &&
                                      AssetDatabase.IsValidFolder("Assets/_Project/Prefabs");
            
            _validationRun = true;
        }
        
        private void DisplayResults()
        {
            GUIStyle successStyle = new GUIStyle(EditorStyles.label);
            successStyle.normal.textColor = Color.green;
            
            GUIStyle errorStyle = new GUIStyle(EditorStyles.label);
            errorStyle.normal.textColor = Color.red;
            
            EditorGUILayout.LabelField("Validation Results:", EditorStyles.boldLabel);
            
            // Orientation
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Project Orientation:", GUILayout.Width(150));
            EditorGUILayout.LabelField(_orientationCorrect ? "✓ Portrait" : "✗ Not set to Portrait", 
                _orientationCorrect ? successStyle : errorStyle);
            EditorGUILayout.EndHorizontal();
            
            // Android Target
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Android Platform:", GUILayout.Width(150));
            EditorGUILayout.LabelField(_androidTargetEnabled ? "✓ Active" : "✗ Not active", 
                _androidTargetEnabled ? successStyle : errorStyle);
            EditorGUILayout.EndHorizontal();
            
            // Quality Settings
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Quality Settings:", GUILayout.Width(150));
            EditorGUILayout.LabelField(_qualitySettingsCorrect ? "✓ Multiple quality levels" : "✗ Only default quality", 
                _qualitySettingsCorrect ? successStyle : errorStyle);
            EditorGUILayout.EndHorizontal();
            
            // Folder Structure
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Folder Structure:", GUILayout.Width(150));
            EditorGUILayout.LabelField(_folderStructureCorrect ? "✓ Basic structure exists" : "✗ Missing basic folders", 
                _folderStructureCorrect ? successStyle : errorStyle);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Overall result
            bool allCorrect = _orientationCorrect && _androidTargetEnabled && 
                             _qualitySettingsCorrect && _folderStructureCorrect;
            
            EditorGUILayout.LabelField("Overall Configuration:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(allCorrect ? 
                "✓ Project is correctly configured for mobile development" : 
                "✗ Project needs configuration adjustments", 
                allCorrect ? successStyle : errorStyle);
            
            if (!allCorrect)
            {
                EditorGUILayout.HelpBox("Some configuration issues were found. See details above.", MessageType.Warning);
            }
        }
    }
}