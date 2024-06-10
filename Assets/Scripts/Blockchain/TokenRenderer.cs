using System.Collections;
using System.Collections.Generic;
using Mona.SDK.Brains.Core.Utils.Interfaces;
using Mona.SDK.Brains.Core.Utils.Structs;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System;

namespace Mona.WalletSDK
{
    public class TokenRenderer : MonoBehaviour
    {
        public event Action<Token> OnSelect = delegate { };

        private Token _token;

        public Image Image;
        public TextMeshProUGUI Title;
        public TextMeshProUGUI Description;

        public void SetToken(Token token)
        {
            _token = token;

            Debug.Log($"{nameof(TokenRenderer)} {nameof(SetToken)} image: {token.Image}");
            if (Image != null && !string.IsNullOrEmpty(token.Image)) StartCoroutine(LoadImage(token.Image));
            if (Title != null) Title.text = token.Nft.Metadata.Name;
            if (Description != null) Description.text = token.Nft.Metadata.Description;
        }

        private IEnumerator LoadImage(string url)
        {
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
            yield return request.SendWebRequest();

            Texture2D texture;
            switch (request.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.ProtocolError:
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogError($"{nameof(TokenRenderer)}.{nameof(LoadImage)} - Request error: {request.error} {request.result}");
                    texture = null;
                    break;
                case UnityWebRequest.Result.Success:
                    texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                    break;
                default:
                    Debug.LogError($"{nameof(TokenRenderer)}.{nameof(LoadImage)} - Request error: {request.error}");
                    texture = null;
                    break;
            }

            if (texture != null)
                SetImage(texture);
        }

        private void SetImage(Texture2D imageTex)
        {
            Sprite newSprite = Sprite.Create(imageTex, new Rect(0, 0, imageTex.width, imageTex.height), new Vector2(.5f, .5f));
            Image.sprite = newSprite;
        }

        public void Select()
        {
            OnSelect?.Invoke(_token);
        }

    }
}
