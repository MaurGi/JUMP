using UnityEngine;
using System.Collections;
using UnityEditor;

namespace JUMP
{
    [CustomEditor(typeof(JUMPMultiplayer))]
    public class JUMPMultiplayerInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            JUMPMultiplayer jump = (JUMPMultiplayer)target;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Stage"));

            switch (jump.Stage)
            {
                case JUMPMultiplayer.Stages.Connection:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("OnMasterConnect"));
                    break;
                case JUMPMultiplayer.Stages.Master:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("OnMasterDisconnect"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("GameServerEngineTypeName"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("OnGameRoomConnect"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("BotTypeName"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("OnOfflinePlayConnect"));
                    break;
                //case JUMPMultiplayer.Stages.MatchmakeLobby:
                //    EditorGUILayout.PropertyField(serializedObject.FindProperty("OnMatchmakeLobbyDisconnect"));
                //    EditorGUILayout.PropertyField(serializedObject.FindProperty("OnMatchmakeLobbyConnect"));
                //    break;
                case JUMPMultiplayer.Stages.GameRoom:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("OnGameRoomDisconnect"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("OnPlayConnect"));
                    break;
                case JUMPMultiplayer.Stages.Play:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("OnPlayDisconnected"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("OnSnapshotReceived"));
                    break;
                case JUMPMultiplayer.Stages.OfflinePlay:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("OnPlayDisconnected"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("OnSnapshotReceived"));
                    break;
                default:
                    break;
            }


            serializedObject.ApplyModifiedProperties();
        }

    }
}