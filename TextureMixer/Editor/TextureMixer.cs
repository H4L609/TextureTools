using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.UI;
using Toggle = UnityEngine.UIElements.Toggle;
using Slider = UnityEngine.UIElements.Slider;
using TextureMixer.Editor.Enum;

// Haruki Yano 氏の記事を大いに参考にさせていただきました！ありがとうございます！

/*
 * このスクリプトに置いて常に注意しないといけないこと
 * - コピー元のテクスチャの解像度がすべて同じではないことがあり得る
 * - コピー元のテクスチャと出力されるテクスチャの解像度が同じでないことがあり得る
 * - コーディングのミスで、途中使うRenderTextureの縦横のピクセル数が一致しないこと、
 *
 */

namespace TextureMixer.Editor{

    public class TextureMixer : EditorWindow
    {
        [MenuItem("Tools/TextureMixer")]
        private static void ShowWindow()
        {
            var window = GetWindow<TextureMixer>("UIElements");
            window.titleContent = new GUIContent("Texture Mixer");
            window.Show();
        }


        [SerializeField] private VisualTreeAsset _rootVisualTreeAsset;
        [SerializeField] private StyleSheet _rootStyleSheet;
        private string saveFilePath = "";
        private float _valueWhenTextureIsEmpty = 1.0f;

        private void CreateGUI()
        {
            _rootVisualTreeAsset.CloneTree(rootVisualElement);
            rootVisualElement.styleSheets.Add(_rootStyleSheet);

            var useTextureToggleRed   = rootVisualElement.Q<Toggle>("useTextureRed");
            var useTextureToggleGreen = rootVisualElement.Q<Toggle>("useTextureGreen");
            var useTextureToggleBlue  = rootVisualElement.Q<Toggle>("useTextureBlue");
            var useTextureToggleAlpha = rootVisualElement.Q<Toggle>("useTextureAlpha");

            var flipTextureToggleRed   = rootVisualElement.Q<Toggle>("flipTextureRed");
            var flipTextureToggleGreen = rootVisualElement.Q<Toggle>("flipTextureGreen");
            var flipTextureToggleBlue  = rootVisualElement.Q<Toggle>("flipTextureBlue");
            var flipTextureToggleAlpha = rootVisualElement.Q<Toggle>("flipTextureAlpha");

            var valueSliderRed   = rootVisualElement.Q<Slider>("valueRed");
            var valueSliderGreen = rootVisualElement.Q<Slider>("valueGreen");
            var valueSliderBlue  = rootVisualElement.Q<Slider>("valueBlue");
            var valueSliderAlpha = rootVisualElement.Q<Slider>("valueAlpha");

            var redTexField   = rootVisualElement.Q<ObjectField>("redTex");
            var greenTexField = rootVisualElement.Q<ObjectField>("greenTex");
            var blueTexField  = rootVisualElement.Q<ObjectField>("blueTex");
            var alphaTexField = rootVisualElement.Q<ObjectField>("alphaTex");

            var channelForTexEnumRed   = rootVisualElement.Q<EnumField>("channelForRed");
            var channelForTexEnumGreen = rootVisualElement.Q<EnumField>("channelForGreen");
            var channelForTexEnumBlue  = rootVisualElement.Q<EnumField>("channelForBlue");
            var channelForTexEnumAlpha = rootVisualElement.Q<EnumField>("channelForAlpha");

            var saveFilePathTextField = rootVisualElement.Q<TextField>("saveFilePath");

            #region Set Initial State

            valueSliderRed  .SetEnabled(!useTextureToggleRed.value);
            valueSliderGreen.SetEnabled(!useTextureToggleGreen.value);
            valueSliderBlue .SetEnabled(!useTextureToggleBlue.value);
            valueSliderAlpha.SetEnabled(!useTextureToggleAlpha.value);

            redTexField  .SetEnabled(useTextureToggleRed.value);
            greenTexField.SetEnabled(useTextureToggleGreen.value);
            blueTexField .SetEnabled(useTextureToggleBlue.value);
            alphaTexField.SetEnabled(useTextureToggleAlpha.value);

            flipTextureToggleRed  .SetEnabled(useTextureToggleRed.value);
            flipTextureToggleGreen.SetEnabled(useTextureToggleGreen.value);
            flipTextureToggleBlue .SetEnabled(useTextureToggleBlue.value);
            flipTextureToggleAlpha.SetEnabled(useTextureToggleAlpha.value);

            channelForTexEnumRed  .SetEnabled(useTextureToggleRed.value);
            channelForTexEnumGreen.SetEnabled(useTextureToggleGreen.value);
            channelForTexEnumBlue .SetEnabled(useTextureToggleBlue.value);
            channelForTexEnumAlpha.SetEnabled(useTextureToggleAlpha.value);

            saveFilePathTextField.value = saveFilePath;
            #endregion

            useTextureToggleRed.RegisterValueChangedCallback(evt =>
            {
                valueSliderRed.SetEnabled(!evt.newValue);
                flipTextureToggleRed.SetEnabled(evt.newValue);
                redTexField.SetEnabled(evt.newValue);
                channelForTexEnumRed.SetEnabled(evt.newValue);
            });
            useTextureToggleGreen.RegisterValueChangedCallback(evt =>
            {
                valueSliderGreen.SetEnabled(!evt.newValue);
                flipTextureToggleGreen.SetEnabled(evt.newValue);
                greenTexField.SetEnabled(evt.newValue);
                channelForTexEnumGreen.SetEnabled(evt.newValue);
            });
            useTextureToggleBlue.RegisterValueChangedCallback(evt =>
            {
                valueSliderBlue.SetEnabled(!evt.newValue);
                flipTextureToggleBlue.SetEnabled(evt.newValue);
                blueTexField.SetEnabled(evt.newValue);
                channelForTexEnumBlue.SetEnabled(evt.newValue);
            });

            useTextureToggleAlpha.RegisterValueChangedCallback(evt =>
            {
                valueSliderAlpha.SetEnabled(!evt.newValue);
                flipTextureToggleAlpha.SetEnabled(evt.newValue);
                alphaTexField.SetEnabled(evt.newValue);
                channelForTexEnumAlpha.SetEnabled(evt.newValue);
            });

            var bakeButton = rootVisualElement.Q<UnityEngine.UIElements.Button>("bakeButton");
            
            bakeButton.clicked += () =>
            {
                Texture2D redTex   = (Texture2D)redTexField.value;
                Texture2D greenTex = (Texture2D)greenTexField.value;
                Texture2D blueTex  = (Texture2D)blueTexField.value;
                Texture2D alphaTex = (Texture2D)alphaTexField.value;

                int channelForRedTex   = (int)(Channels)channelForTexEnumRed.value;
                int channelForGreenTex = (int)(Channels)channelForTexEnumGreen.value;
                int channelForBlueTex  = (int)(Channels)channelForTexEnumBlue.value;
                int channelForAlphaTex = (int)(Channels)channelForTexEnumAlpha.value;


                int resolution = (int)(Channels)rootVisualElement.Q<EnumField>("outputResolution").value;

                var output      = new Texture2D(resolution,resolution, TextureFormat.RGBA64, false);
                var destination = output.GetPixels();
                var reshapedTexture = RenderTexture.GetTemporary(resolution, resolution);

                #region Set Source Texture Value To Output Texture

                // RED
                Graphics.Blit(redTex, reshapedTexture);
                var pixels = GetPixelsFromRenderTexture(reshapedTexture);
                for (int i = 0; i < pixels.Length; i++)
                {
                    if (!useTextureToggleRed.value)
                    {
                        destination[i][0] = valueSliderRed.value;
                    } else if (!(bool)redTex)
                    {
                        destination[i][0] = _valueWhenTextureIsEmpty;
                    } else
                    {
                        destination[i][0] = flipTextureToggleRed.value ? 1.0f - pixels[i][channelForRedTex] : pixels[i][channelForRedTex];
                    }   
                }

                // GREEN
                Graphics.Blit(greenTex, reshapedTexture);
                pixels = GetPixelsFromRenderTexture(reshapedTexture);
                for (int i = 0; i < pixels.Length; i++)
                {
                    if (!useTextureToggleGreen.value)
                    {
                        destination[i][1] = valueSliderGreen.value;
                    }
                    else if (!(bool)greenTex)
                    {
                        destination[i][1] = _valueWhenTextureIsEmpty;
                    }
                    else
                    {
                        destination[i][1] = flipTextureToggleGreen.value ? 1.0f - pixels[i][channelForGreenTex] : pixels[i][channelForGreenTex];
                    }
                }

                // BLUE
                Graphics.Blit(blueTex, reshapedTexture);
                pixels = GetPixelsFromRenderTexture(reshapedTexture);
                for (int i = 0; i < pixels.Length; i++)
                {
                    if (!useTextureToggleBlue.value)
                    {
                        destination[i][2] = valueSliderBlue.value;
                    }
                    else if (!(bool)blueTex)
                    {
                        destination[i][2] = _valueWhenTextureIsEmpty;
                    }
                    else
                    {
                        destination[i][2] = flipTextureToggleBlue.value ? 1.0f - pixels[i][channelForBlueTex] : pixels[i][channelForBlueTex];
                    }
                }

                // ALPHA
                Graphics.Blit(alphaTex, reshapedTexture);
                pixels = GetPixelsFromRenderTexture(reshapedTexture);
                for (int i = 0; i < pixels.Length; i++)
                {
                    if (!useTextureToggleAlpha.value)
                    {
                        destination[i][3] = valueSliderAlpha.value;
                    }
                    else if (!(bool)alphaTex)
                    {
                        destination[i][3] = _valueWhenTextureIsEmpty;
                    }
                    else
                    {
                        destination[i][3] = flipTextureToggleAlpha.value ? 1.0f - pixels[i][channelForAlphaTex] : pixels[i][channelForAlphaTex];
                    }
                }

                RenderTexture.ReleaseTemporary(reshapedTexture);
                output.SetPixels(destination);
                output.Apply();
                #endregion

                // Save Generated Texture As a file
                
                if (!string.IsNullOrEmpty(saveFilePath)) {
                    BakeTexture(saveFilePath, output);
                }
            };

            var fileBrowseButton = rootVisualElement.Q<UnityEngine.UIElements.Button>("fileBrowseButton");
            fileBrowseButton.clicked += () =>
            {
                saveFilePath = EditorUtility.SaveFilePanel("Select Output Path & Filename",Application.dataPath,"TextureMixerOutput", "png");
                saveFilePathTextField.value = saveFilePath;
            };

            saveFilePathTextField.RegisterValueChangedCallback(text =>
            {
                saveFilePath = text.newValue;
            });
        }

        private void BakeTexture(string filepath, Texture2D texture)
        {
            var bytes = texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(filepath, bytes);
            AssetDatabase.Refresh();
        }

        private Color[] GetPixelsFromRenderTexture(RenderTexture target)
        {
            var previousRenderTexture = RenderTexture.active;
            RenderTexture.active = target;

            // ReadPixelsでレンダーターゲットからテクスチャ情報を生成する
            var texture = new Texture2D(target.width, target.height);
            texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            texture.Apply();
            RenderTexture.active = previousRenderTexture;
            return texture.GetPixels();
        }

        // private void ValidateTextureField()
    }
}