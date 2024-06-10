using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Mona.SDK.Brains.Core.Brain;
using Mona.SDK.Brains.Core.Utils.Enums;
using Mona.SDK.Brains.Core.Utils.Interfaces;
using Mona.SDK.Brains.Core.Utils.Structs;
using Monaverse;
using Monaverse.Api;
using Monaverse.Api.Configuration;
using Monaverse.Api.Modules.Collectibles.Dtos;
using Monaverse.Api.Options;
using Monaverse.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;
using Monaverse.Redcode.Awaiting;
using TMPro;
using Monaverse.Modal;
using Mona.SDK.Core.Events;
using Unity.VisualScripting;
using Mona.SDK.Core;
using Mona.SDK.Core.Utils;
using Mona.SDK.Brains.Core.Events;

namespace Mona.SDK.Brains.Core.Utils
{
    public class MonaBrainsWalletSDK : MonaBrainBlockchain
    {

        private bool _caching;
        private string _cachedWallet;
        private List<Token> _cachedTokens = new List<Token>();
        private string _accessToken;
        private bool _authorized;

        public List<MockTokenTrait> Traits = new List<MockTokenTrait>();

        public MonaBrainRunner Status;
        public TextMeshProUGUI StatusText;

        IMonaApiClient _client;

        public UnityEvent OnWalletConnected;
        public UnityEvent OnWalletDisconnected;
        public UnityEvent OnTokens;

        public bool DisconnectOnLoad;
        public bool LogoutOnLoad;

        private async void Start()
        {
            MonaverseManager.Initialize();
            MonaverseManager.Instance.SDK.Connected += HandleConnected;
            MonaverseManager.Instance.SDK.Disconnected += HandleDisconnected;
            MonaverseModal.ImportCollectibleClicked += HandleClicked;

            if (DisconnectOnLoad)
            {
                await MonaverseManager.Instance.SDK.ConnectWallet();

                await MonaverseManager.Instance.SDK.Disconnect();
                Debug.Log("Disconnected");
            }

            if(LogoutOnLoad)
            {
                MonaverseManager.Instance.SDK.ApiClient.ClearSession();
            }

        }

        private void HandleConnected(object sender, string message)
        {

        }

        private void HandleDisconnected(object sender, EventArgs e)
        {
            _walletConnected = false;

            MonaGlobalBrainRunner.Instance.HandleWalletDisconnected(_walletAddress);

            OnWalletDisconnected?.Invoke();
        }
        
        public async void Disconnect()
        {
            await DoDisconnect();
        }

        public async Task DoDisconnect()
        {
            Status?.Body.SetActive(true);

            await new WaitForSeconds(.1f);

            Status?.SendMessageToBody("Disconnecting");

            await MonaverseManager.Instance.SDK.Disconnect();

            Invoke("HideStatus", 1f);
        }

        private void HideStatus()
        {
            Status?.Body.SetActive(false);
        }

        private async Task CacheTokens()
        {
            if (_cachedTokens.Count == 0)
            {
                _cachedTokens.Clear();
                _caching = true;
                var tokens = await MonaApi.ApiClient.Collectibles.GetWalletCollectibles();
                if (tokens.IsSuccess)
                {
                    var response = tokens.Data;
                    if (tokens != null && response != null && response.TotalCount > 0)
                    {
                        for (var i = 0; i < response.TotalCount; i++)
                        {
                            var nft = response.Data[i];
                            Token token = CopyTo(nft, new Token());
                            _cachedTokens.Add(token);
                        }
                    }

                    Debug.Log($"{nameof(CacheTokens)} {response.TotalCount}");

                }

                OnTokens?.Invoke();

                _caching = false;
            }
        }

        private async Task<bool> CheckWalletCache()
        {
            try
            {
                await CacheTokens();
                return true;
            }
            catch (Exception e)
            {
                Debug.Log($"{nameof(MonaBrainsWalletSDK)} {nameof(OwnsToken)} {e.Message} no wallet connected");
            }
            return false;
        }

        public async override Task<Token> OwnsToken(string collectionAddress, string tokenId)
        {
            if (!await CheckWalletCache()) return default;

            if (string.IsNullOrEmpty(_walletAddress))
            {
                return default;
            }

            while(_caching)
            {
                await Task.Yield();
            }

            var index = _cachedTokens.FindIndex(x => x.Contract == collectionAddress && x.Nft.TokenId == tokenId);

            if (index > -1)
                return _cachedTokens[index];
            return default;
        }

        public async override Task<List<Token>> OwnsTokens(string collectionAddress)
        {
            while (_caching)
            {
                await Task.Yield();
            }

            await CheckWalletCache();

            var tokens = _cachedTokens.FindAll(x => x.Contract == collectionAddress);

            return tokens;
        }

        public async override Task<List<Token>> OwnsTokens()
        {
            while (_caching)
            {
                await Task.Yield();
            }

            await CheckWalletCache();

            var tokens = _cachedTokens;

            return tokens;
        }


        public void ShowModal()
        {
            MonaverseModal.Open((x) =>
            {
                return x.Description.ToLower().Contains("#dice");
            });
        }

        private void HandleClicked(object sender, CollectibleDto item)
        {
            HandleSelect(CopyTo(item, new Token()));
        }

        private Token CopyTo(CollectibleDto nft, Token token)
        {
            token.Contract = nft.Nft.Contract;
            token.CollectionId = nft.CollectionId;
            token.Description = nft.Description;

            if (!string.IsNullOrEmpty(nft.Image))
            {
                if (nft.Image.Contains("arweave"))
                    token.Image = nft.Image;
                else if (!nft.Image.Contains("http"))
                    token.Image = nft.Image.CidToIpfsUrl();
            }

            if (token.Image != null && token.Image.Contains("ipfs"))
                token.Image = token.Image.ReplaceIPFS();

            token.Nft.TokenId = nft.Nft.TokenId.ToString();
            //token.Nft.TokenStandard = nft.Nft.;
            token.Nft.TokenUri = nft.Nft.TokenUri;

            token.Nft.Metadata.Id = nft.Id;
            token.Nft.Metadata.Description = nft.Description;
            token.Nft.Metadata.Image = token.Image;
            token.Nft.Metadata.Name = nft.Title;

            token.Traits = new Dictionary<string, object>();

            if (nft.Traits != null)
            {
                foreach (var pair in nft.Traits)
                {
                    var traitName = pair.Key.ToLower();
                    var traitValue = pair.Value.ToLower();
                    if (!token.Traits.ContainsKey(traitName))
                        token.Traits.Add(traitName, traitValue);
                }
            }

            for (var i = 0; i < Traits.Count; i++)
            {
                var trait = Traits[i];
                if (trait.TokenId == token.Nft.TokenId)
                {
                    var traitName = trait.TraitName.ToLower();
                    var traitValue = trait.TokenValue.ToLower();
                    if (!token.Traits.ContainsKey(traitName))
                        token.Traits.Add(trait.TraitName.ToLower(), trait.TokenValue.ToLower());
                }
            }

            var version = nft.Versions[nft.ActiveVersion];

            token.AssetType = (TokenAssetType)Enum.Parse(typeof(TokenAssetType), nft.Type);

            if (token.AssetType == TokenAssetType.Space)
            {
                token.AssetUrl = version.Assets.Space;
            }
            else
            {
                token.AssetUrl = version.Asset;
            }

            token.Artist = nft.Creator;

            Debug.Log($"{nameof(MonaBrainsWalletSDK)}.parse token url: {token.AssetUrl} image: {token.Image}");

            return token;
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

            MonaEventBus.Trigger<MonaWalletTokenSelectedEvent>(new EventHook(MonaBrainConstants.WALLET_TOKEN_SELECTED_EVENT), new MonaWalletTokenSelectedEvent(token));
        }

        private void HandleChangeAvatar(MonaChangeAvatarEvent evt)
        {
            
        }

        private void HandleChangeSpace(MonaChangeSpaceEvent evt)
        {
            
        }

        private void HandleChangeSpawn(MonaChangeSpawnEvent evt)
        {
            
        }
    }
}
