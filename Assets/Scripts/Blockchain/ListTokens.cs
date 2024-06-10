using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mona.SDK.Brains.Core;
using Mona.SDK.Brains.Core.Events;
using Mona.SDK.Brains.Core.Utils;
using Mona.SDK.Brains.Core.Utils.Enums;
using Mona.SDK.Brains.Core.Utils.Structs;
using Mona.SDK.Brains.Tiles.Conditions.Enums;
using Mona.SDK.Core;
using Mona.SDK.Core.Events;
using Mona.SDK.Core.Utils;
using Monaverse.Api;
using Monaverse.Redcode.Awaiting;
using Unity.VisualScripting;
using UnityEngine;

namespace Mona.WalletSDK
{
    public class ListTokens : MonoBehaviour
    {
        [SerializeField]
        private MonaBrainBlockchain _sdk;

        public Transform TokenRendererParent;
        public GameObject Root;
        public GameObject Loading;
        public GameObject Changing;
        public GameObject NoneFound;
        public GameObject List;

        public TokenRenderer TokenRenderer;

        private List<TokenRenderer> _renderers = new List<TokenRenderer>();

        private void Awake()
        {
            if(_sdk == null)
                _sdk = GameObject.FindObjectOfType<MonaBrainBlockchain>(false);
            Root.SetActive(false);
            List.SetActive(false);
            Changing.SetActive(false);
            NoneFound.SetActive(false);
            Loading.SetActive(false);
        }

        public async void Show()
        {
            Root.SetActive(true);
            List.SetActive(false);
            Changing.SetActive(false);
            NoneFound.SetActive(false);
            Loading.SetActive(true);
            await FetchTokens();
        }

        public void Hide()
        {
            Root.SetActive(false);
        }

        [SerializeField] private MonaBrainTokenFilterType _tokenFilter = MonaBrainTokenFilterType.IncludeAll;
        [SerializeField] protected string _traitName = "";
        [SerializeField] protected string _traitValue = "";
        [SerializeField] protected string _tokenId = "";
        [SerializeField] protected string _contract = "";
        [SerializeField] protected string _name = "";
        [SerializeField] protected string _description = "";

        private async Task FetchTokens()
        {
            ClearParent();
            List<Token> tokens = new List<Token>();

            tokens = await _sdk.OwnsTokens();

            if (_tokenFilter == MonaBrainTokenFilterType.OnlyAvatars)
            {
                tokens = tokens.FindAll(x => x.AssetType == TokenAssetType.Avatar);
            }
            else if (_tokenFilter == MonaBrainTokenFilterType.OnlyObjects)
            {
                tokens = tokens.FindAll(x => x.AssetType == TokenAssetType.Artifact);
            }
            else if (_tokenFilter == MonaBrainTokenFilterType.OnlyAvatarsAndObjects)
            {
                tokens = tokens.FindAll(x => x.AssetType == TokenAssetType.Artifact || x.AssetType == TokenAssetType.Avatar);
            }

            tokens = tokens.FindAll(x =>
            {
                if (!string.IsNullOrEmpty(_name) && !x.Nft.Metadata.Name.ToLower().Contains(_name.ToLower()))
                    return false;

                if (!string.IsNullOrEmpty(_contract) && !(x.Contract == _contract))
                    return false;

                if (!string.IsNullOrEmpty(_tokenId) && !(x.Nft.TokenId == _tokenId))
                    return false;

                if (!string.IsNullOrEmpty(_description) && !(x.Description.ToLower().Contains(_description.ToLower())))
                    return false;

                if (!string.IsNullOrEmpty(_traitName) && x.Traits.ContainsKey(_traitName.ToLower()))
                {
                    string value = x.Traits[_traitName.ToLower()].ToString().ToLower();
                    if (_traitValue != null && value != _traitValue.ToLower())
                        return false;
                }

                return true;
            });           

            if (tokens.Count > 0)
            {

                NoneFound.SetActive(false);
                Changing.SetActive(false);
                Loading.SetActive(false);
                List.SetActive(true);

                for (var i = 0; i < tokens.Count; i++)
                {
                    TokenRenderer item;
                    if (_renderers.Count <= i)
                    {
                        item = Instantiate<TokenRenderer>(TokenRenderer);
                        item.OnSelect += HandleSelect;
                        item.transform.parent = TokenRendererParent;
                        item.transform.localScale = Vector3.one;
                        _renderers.Add(item);
                    }
                    else
                        item = _renderers[i];

                    item.gameObject.SetActive(true);
                    item.SetToken(tokens[i]);
                }
            }
            else
            {
                Loading.SetActive(false);
                Changing.SetActive(false);
                NoneFound.SetActive(true);
                List.SetActive(false);
            }
        }

        private void ClearParent()
        {
            for (var i = 0; i < _renderers.Count; i++)
            {
                var child = _renderers[i];
                child.OnSelect -= HandleSelect;
                Destroy(child.gameObject);
            }
            _renderers.Clear();
        }

        private Action<MonaChangeAvatarEvent> OnChangeAvatar;
        private Action<MonaChangeSpaceEvent> OnChangeSpace;
        private Action<MonaChangeSpawnEvent> OnChangeSpawn;

        private void HandleSelect(Token token)
        {
            OnChangeAvatar = HandleChangeAvatar;
            MonaEventBus.Register<MonaChangeAvatarEvent>(new EventHook(MonaCoreConstants.ON_CHANGE_AVATAR_EVENT), OnChangeAvatar);

            OnChangeSpace = HandleChangeSpace;
            MonaEventBus.Register<MonaChangeSpaceEvent>(new EventHook(MonaCoreConstants.ON_CHANGE_SPACE_EVENT), OnChangeSpace);

            OnChangeSpawn = HandleChangeSpawn;
            MonaEventBus.Register<MonaChangeSpawnEvent>(new EventHook(MonaCoreConstants.ON_CHANGE_SPAWN_EVENT), OnChangeSpawn);

            List.SetActive(false);
            Changing.SetActive(true);
            MonaEventBus.Trigger<MonaWalletTokenSelectedEvent>(new EventHook(MonaBrainConstants.WALLET_TOKEN_SELECTED_EVENT), new MonaWalletTokenSelectedEvent(token));
        }

        private void HandleChangeAvatar(MonaChangeAvatarEvent evt)
        {
            Changing.SetActive(false);
            Root.SetActive(false);
        }

        private void HandleChangeSpace(MonaChangeSpaceEvent evt)
        {
            Changing.SetActive(false);
            Root.SetActive(false);
        }

        private void HandleChangeSpawn(MonaChangeSpawnEvent evt)
        {
            Changing.SetActive(false);
            Root.SetActive(false);
        }
    }
}
