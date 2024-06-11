using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Monaverse.Modal;
using Monaverse.Api.Modules.Collectibles.Dtos;
using UnityGLTF;
using Mona.SDK.Brains.Core.Utils;

public class TestModal : MonoBehaviour
{
    public GLTFComponent _gltf;
    public BrainsGlbLoader _glb;
    public bool _useGLTFComponent = true;

    void Awake()
    {
        MonaverseModal.ImportCollectibleClicked += HandleClicked;
    }
    // Update is called once per frame
    public void Show()
    {
        MonaverseModal.Open();
    }

    public async void HandleClicked(object target, CollectibleDto token)
    {
        var uri = token.Versions[token.ActiveVersion].Asset;
        Debug.Log($"{nameof(HandleClicked)} {uri}");

        if (_useGLTFComponent)
        {
            _gltf.GLTFUri = uri;
            await _gltf.Load();
        }
        else
        {
            _glb.Load(uri, true, (obj) =>
            {
                Debug.Log($"Loaded {uri}");
                obj.transform.rotation = Quaternion.Euler(0, 180f, 0);
            }, 1);
        }
    }
}
