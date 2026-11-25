using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Firebase.Firestore;
using BubblesGame.Data;

namespace BubblesGame.Editor
{
    /// <summary>
    /// Helper editor para fazer upload de arquivos JSON locais para o Firebase Firestore
    /// Instruções de uso no menu: Tools > Bubbles > Upload Level to Firebase
    /// </summary>
    public class FirebaseDataUploader : EditorWindow
    {
        private string gameId = "bubbles";
        private string presetId = "preset_1";
        private string presetDescription = "Nível Iniciante - Movimentos Básicos";
        private int levelOrder = 1;
        private TextAsset jsonFile;

        [MenuItem("Tools/Bubbles/Upload Level to Firebase")]
        public static void ShowWindow()
        {
            GetWindow<FirebaseDataUploader>("Firebase Level Uploader");
        }

        void OnGUI()
        {
            GUILayout.Label("Upload Level Data to Firebase", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "Este tool converte um arquivo JSON local em dados Firestore.\n" +
                "IMPORTANTE: Execute em Play Mode com Firebase inicializado!", 
                MessageType.Info);

            EditorGUILayout.Space();

            gameId = EditorGUILayout.TextField("Game ID", gameId);
            presetId = EditorGUILayout.TextField("Preset ID", presetId);
            presetDescription = EditorGUILayout.TextField("Preset Description", presetDescription);
            levelOrder = EditorGUILayout.IntField("Level Order", levelOrder);
            
            EditorGUILayout.Space();
            jsonFile = (TextAsset)EditorGUILayout.ObjectField("JSON File", jsonFile, typeof(TextAsset), false);

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate Firestore Commands"))
            {
                GenerateFirestoreCommands();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Como usar:\n" +
                "1. Preencha os campos acima\n" +
                "2. Selecione o arquivo JSON (ex: preset_1_level_1.json)\n" +
                "3. Clique em 'Generate Firestore Commands'\n" +
                "4. Copie os comandos do Console\n" +
                "5. Use no Firebase Console ou em script runtime",
                MessageType.None);
        }

        void GenerateFirestoreCommands()
        {
            if (jsonFile == null)
            {
                EditorUtility.DisplayDialog("Error", "Selecione um arquivo JSON primeiro!", "OK");
                return;
            }

            // Valida o JSON
            string jsonText = jsonFile.text;
            try
            {
                // Tenta deserializar para validar
                BallDataArrayWrapper wrapper = JsonUtility.FromJson<BallDataArrayWrapper>("{\"balls\":" + jsonText + "}");
                
                Debug.Log("=== FIREBASE FIRESTORE STRUCTURE ===");
                Debug.Log($"\nPath: games/{gameId}/presets/{presetId}");
                Debug.Log($"Document Data:");
                Debug.Log($"  description: \"{presetDescription}\"");
                Debug.Log($"\nSubcollection Path: games/{gameId}/presets/{presetId}/levels/level_{levelOrder}");
                Debug.Log($"Document Data:");
                Debug.Log($"  order: {levelOrder}");
                Debug.Log($"  ballsData: {jsonText}");
                Debug.Log($"\n=== Total de {wrapper.balls.Length} bolas carregadas ===");
                Debug.Log("\nVocê pode copiar esta estrutura para criar no Firebase Console");
                
                EditorUtility.DisplayDialog("Success", 
                    $"Comandos gerados! Verifique o Console.\n\n" +
                    $"Total de bolas: {wrapper.balls.Length}", "OK");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"JSON inválido: {e.Message}", "OK");
            }
        }

        [System.Serializable]
        private class BallDataArrayWrapper
        {
            public BallData[] balls;
        }
    }

    /// <summary>
    /// Script runtime para fazer upload programático (para usar em Play Mode)
    /// </summary>
    public class FirebaseDataUploaderRuntime : MonoBehaviour
    {
        public void UploadLevelToFirebase(string gameId, string presetId, string description, int levelOrder, string ballsDataJson)
        {
#if UNITY_EDITOR
            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
            
            // Cria/atualiza o preset
            var presetRef = db.Collection("games").Document(gameId)
                .Collection("presets").Document(presetId);
            
            var presetData = new Dictionary<string, object>
            {
                { "description", description }
            };
            
            presetRef.SetAsync(presetData).ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log($"Preset {presetId} criado/atualizado");
                    
                    // Cria o nível
                    var levelRef = presetRef.Collection("levels").Document($"level_{levelOrder}");
                    var levelData = new Dictionary<string, object>
                    {
                        { "order", levelOrder },
                        { "ballsData", ballsDataJson }
                    };
                    
                    levelRef.SetAsync(levelData).ContinueWith(levelTask =>
                    {
                        if (levelTask.IsCompleted)
                        {
                            Debug.Log($"Level {levelOrder} uploaded com sucesso!");
                        }
                        else
                        {
                            Debug.LogError($"Erro ao fazer upload do level: {levelTask.Exception}");
                        }
                    });
                }
                else
                {
                    Debug.LogError($"Erro ao criar preset: {task.Exception}");
                }
            });
#else
            Debug.LogError("Upload só funciona no Editor!");
#endif
        }
    }
}

